﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Advobot.Classes;
using Advobot.Logging.Caches;
using Advobot.Logging.Context;
using Advobot.Logging.Context.Messages;
using Advobot.Logging.Database;
using Advobot.Logging.Utilities;
using Advobot.Utilities;

using AdvorangesUtils;

using Discord;
using Discord.WebSocket;

namespace Advobot.Logging.Service
{
	public sealed class MessageLogger
	{
		private const int MAX_DESCRIPTION_LENGTH = EmbedBuilder.MaxDescriptionLength - 250;
		private const int MAX_DESCRIPTION_LINES = EmbedWrapper.MAX_DESCRIPTION_LINES;
		private const int MAX_FIELD_LENGTH = EmbedFieldBuilder.MaxFieldValueLength - 50;
		private const int MAX_FIELD_LINES = MAX_DESCRIPTION_LENGTH / 2;

		private readonly ConcurrentDictionary<ulong, DeletedMessageCache> _Caches =
			new();
		private readonly TimeSpan _MessageDeleteDelay = TimeSpan.FromSeconds(3);

		#region Handlers
		private readonly LogHandler<MessageDeletedState> _MessageDeleted;
		private readonly LogHandler<MessageState> _MessageReceived;
		private readonly LogHandler<MessagesBulkDeletedState> _MessagesBulkDeleted;
		private readonly LogHandler<MessageEditState> _MessageUpdated;
		#endregion Handlers

		public MessageLogger(ILoggingDatabase db)
		{
			_MessageDeleted = new LogHandler<MessageDeletedState>(LogAction.MessageDeleted, db)
			{
				HandleMessageDeletedLogging,
			};
			_MessagesBulkDeleted = new LogHandler<MessagesBulkDeletedState>(LogAction.MessageDeleted, db)
			{
				HandleMessagesBulkDeletedLogging,
			};
			_MessageReceived = new LogHandler<MessageState>(LogAction.MessageReceived, db)
			{
				HandleImageLoggingAsync,
			};
			_MessageUpdated = new LogHandler<MessageEditState>(LogAction.MessageUpdated, db)
			{
				HandleMessageEditedLoggingAsync,
				HandleMessageEditedImageLoggingAsync,
			};
		}

		public Task OnMessageDeleted(
			Cacheable<IMessage, ulong> cached,
			Cacheable<IMessageChannel, ulong> _)
			=> _MessageDeleted.HandleAsync(new MessageDeletedState(cached));

		public Task OnMessageReceived(SocketMessage message)
			=> _MessageReceived.HandleAsync(new MessageState(message));

		public Task OnMessagesBulkDeleted(
			IReadOnlyCollection<Cacheable<IMessage, ulong>> cached,
			Cacheable<IMessageChannel, ulong> _)
			=> _MessagesBulkDeleted.HandleAsync(new MessagesBulkDeletedState(cached));

		public Task OnMessageUpdated(
			Cacheable<IMessage, ulong> cached,
			SocketMessage message,
			ISocketMessageChannel _)
			=> _MessageUpdated.HandleAsync(new MessageEditState(cached, message));

		private async Task HandleImageLoggingAsync(ILogContext<MessageState> context)
		{
			if (context.ImageLog == null)
			{
				return;
			}

			var state = context.State;
			foreach (var loggable in ImageLogItem.GetAllImages(state.Message))
			{
				var jump = state.Message.GetJumpUrl();
				var description = $"[Message]({jump}), [Embed Source]({loggable.Url})";
				if (loggable.ImageUrl != null)
				{
					description += $", [Image]({loggable.ImageUrl})";
				}

				await context.ImageLog.SendMessageAsync(new EmbedWrapper
				{
					Description = description,
					Color = EmbedWrapper.Attachment,
					ImageUrl = loggable.ImageUrl,
					Author = state.User.CreateAuthor(),
					Footer = new EmbedFooterBuilder
					{
						Text = loggable.Footer,
						IconUrl = state.User.GetAvatarUrl()
					},
				}.ToMessageArgs()).CAF();
			}
		}

		private Task HandleMessageDeletedLogging(ILogContext<MessageDeletedState> context)
		{
			if (context.ServerLog == null)
			{
				return Task.CompletedTask;
			}

			var cache = _Caches.GetOrAdd(context.Guild.Id, _ => new DeletedMessageCache());
			cache.Add(context.State.Message);
			var cancelToken = cache.GetNewCancellationToken();

			//Has to run on completely separate thread, else prints early
			_ = Task.Run(async () =>
			{
				//Wait three seconds. If a new message comes in then the token will be canceled and this won't continue.
				//If more than 25 messages just start printing them out so people can't stall the messages forever.
				if (cache.Count < 25)
				{
					try
					{
						await Task.Delay(_MessageDeleteDelay, cancelToken).CAF();
					}
					catch (TaskCanceledException)
					{
						return;
					}
				}

				//Give the messages to a new list so they can be removed from the old one
				var messages = cache.Empty();
				await PrintDeletedMessagesAsync(context.ServerLog, messages).CAF();
			});
			return Task.CompletedTask;
		}

		private Task HandleMessageEditedImageLoggingAsync(ILogContext<MessageEditState> context)
		{
			//If the before message is not specified always take that as it should be logged.
			//If the embed counts are greater take that as logging too.
			if (context.State.Before?.Embeds.Count < context.State.Message.Embeds.Count)
			{
				return HandleImageLoggingAsync(context);
			}
			return Task.CompletedTask;
		}

		private Task HandleMessageEditedLoggingAsync(ILogContext<MessageEditState> context)
		{
			var state = context.State;
			if (context.ServerLog == null || state.Before?.Content == state.Message?.Content)
			{
				return Task.CompletedTask;
			}

			static (bool Valid, string Text) FormatContent(IMessage? message)
			{
				if (message == null)
				{
					return (true, "Unknown");
				}

				var text = (message.Content ?? "Empty").RemoveAllMarkdown().RemoveDuplicateNewLines();
				var valid = text.Length <= MAX_FIELD_LENGTH && text.CountLineBreaks() < MAX_FIELD_LINES;
				return (valid, text);
			}

			var (beforeValid, beforeContent) = FormatContent(state.Before);
			var (afterValid, afterContent) = FormatContent(state.Message);
			if (beforeValid && afterValid) //Send file instead if text too long
			{
				return context.ServerLog.SendMessageAsync(new EmbedWrapper
				{
					Color = EmbedWrapper.MessageEdit,
					Author = state.User.CreateAuthor(),
					Footer = new EmbedFooterBuilder { Text = "Message Updated", },
					Fields = new List<EmbedFieldBuilder>
					{
						new EmbedFieldBuilder { Name = "Before", Value = beforeContent, },
						new EmbedFieldBuilder { Name = "After", Value = afterContent, },
					},
				}.ToMessageArgs());
			}

			return context.ServerLog.SendMessageAsync(new SendMessageArgs
			{
				File = new TextFileInfo
				{
					Name = "Edited_Message",
					Text = $"Before:\n{beforeContent}\n\nAfter:\n{afterContent}",
				}
			});
		}

		private Task HandleMessagesBulkDeletedLogging(ILogContext<MessagesBulkDeletedState> context)
		{
			if (context.ServerLog == null)
			{
				return Task.CompletedTask;
			}

			return PrintDeletedMessagesAsync(context.ServerLog, context.State.Messages);
		}

		private Task PrintDeletedMessagesAsync(ITextChannel log, IReadOnlyCollection<IMessage> messages)
		{
			//Needs to be not a lot of messages to fit in an embed
			var inEmbed = messages.Count < 10;
			var sb = new StringBuilder();

			var lineCount = 0;
			foreach (var m in messages)
			{
				var text = m.Format(withMentions: true).RemoveDuplicateNewLines();
				lineCount += text.CountLineBreaks();
				sb.AppendLineFeed(text);

				//Can only stay in an embed if the description is less than 2048
				//and if the line numbers are less than 20
				if (sb.Length > MAX_DESCRIPTION_LENGTH || lineCount > MAX_DESCRIPTION_LINES)
				{
					inEmbed = false;
					break;
				}
			}

			if (inEmbed)
			{
				return log.SendMessageAsync(new EmbedWrapper
				{
					Title = "Deleted Messages",
					Description = sb.ToString(),
					Color = EmbedWrapper.MessageDelete,
					Footer = new EmbedFooterBuilder { Text = "Deleted Messages", },
				}.ToMessageArgs());
			}
			else
			{
				sb.Clear();
				foreach (var m in messages)
				{
					sb.AppendLineFeed(m.Format(withMentions: false).RemoveDuplicateNewLines().RemoveAllMarkdown());
				}

				return log.SendMessageAsync(new SendMessageArgs
				{
					File = new TextFileInfo
					{
						Name = $"{messages.Count}_Deleted_Messages",
						Text = sb.ToString(),
					}
				});
			}
		}
	}
}