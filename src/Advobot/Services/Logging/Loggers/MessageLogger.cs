﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Advobot.Classes;
using Advobot.Classes.CloseWords;
using Advobot.Classes.Settings;
using Advobot.Classes.UserInformation;
using Advobot.Enums;
using Advobot.Interfaces;
using Advobot.Utilities;
using AdvorangesUtils;
using Discord;
using Discord.WebSocket;

namespace Advobot.Services.Logging.Loggers
{
	/// <summary>
	/// Handles logging message events.
	/// </summary>
	internal sealed class MessageLogger : Logger, IMessageLogger
	{
		private static readonly ImmutableDictionary<SpamType, Func<IMessage, int?>> _GetSpamNumberFuncs = new Dictionary<SpamType, Func<IMessage, int?>>
		{
			{ SpamType.Message,     m => int.MaxValue },
			{ SpamType.LongMessage, m => m.Content?.Length },
			{ SpamType.Link,        m => m.Content?.Split(' ')?.Count(x => Uri.IsWellFormedUriString(x, UriKind.Absolute)) },
			{ SpamType.Image,       m => m.Attachments.Count(x => x.Height != null || x.Width != null) + m.Embeds.Count(x => x.Image != null || x.Video != null) },
			{ SpamType.Mention,     m => m.MentionedUserIds.Distinct().Count() }
		}.ToImmutableDictionary();

		/// <summary>
		/// Creates an instance of <see cref="MessageLogger"/>.
		/// </summary>
		/// <param name="provider"></param>
		public MessageLogger(IServiceProvider provider) : base(provider) { }

		/// <inheritdoc />
		public async Task OnMessageReceived(SocketMessage message)
		{
			NotifyLogCounterIncrement(nameof(ILogService.Messages), 1);
			if (!(message.Author is SocketGuildUser user))
			{
				return;
			}

			//For some meme server
			if (user.Guild.Id == 294173126697418752)
			{
				const string name = "jeff";
				if (user.Username != name && user.Nickname != name && user.Guild.CurrentUser.HasHigherPosition(user))
				{
					await user.ModifyAsync(x => x.Nickname = name, ClientUtils.CreateRequestOptions($"my nama {name}")).CAF();
				}
			}

			//Actions which require the ability to log on this channel
			if (CanLog(LogAction.MessageReceived, message, out var settings))
			{
				await HandleImageLoggingAsync(settings, user, message).CAF();
				await HandleSlowmodeAsync(settings, user, message).CAF();
				await HandleSpamPreventionAsync(settings, user, message).CAF();
				await HandleSpamPreventionVotingAsync(settings, user, message).CAF();
				await HandleBannedPhrasesAsync(settings, user, message).CAF();
			}
			//Actions which should happen no matter what
			await HandleCloseWords(settings, user, message).CAF();
			await HandleChannelSettingsAsync(settings, user, message).CAF();
		}
		/// <inheritdoc />
		public async Task OnMessageUpdated(Cacheable<IMessage, ulong> cached, SocketMessage message, ISocketMessageChannel channel)
		{
			NotifyLogCounterIncrement(nameof(ILogService.MessageEdits), 1);
			if (!(message.Author is SocketGuildUser user))
			{
				return;
			}

			if (CanLog(LogAction.MessageUpdated, message, out var settings))
			{
				await HandleBannedPhrasesAsync(settings, user, message).CAF();
				await HandleMessageEditedImageLoggingAsync(settings, user, cached.Value as SocketMessage, message).CAF();
				await HandleMessageEditedLoggingAsync(settings, user, cached.Value as SocketMessage, message).CAF();
			}
		}
		/// <inheritdoc />
		public Task OnMessageDeleted(Cacheable<IMessage, ulong> cached, ISocketMessageChannel channel)
		{
			//Ignore uncached messages since not much can be done with them
			NotifyLogCounterIncrement(nameof(ILogService.MessageDeletes), 1);
			if (!cached.HasValue
				|| !(cached.Value is SocketMessage message)
				|| !(channel is SocketGuildChannel guildChannel)
				|| !CanLog(LogAction.MessageDeleted, message, out var settings)
				|| settings.ServerLogId == 0)
			{
				return Task.CompletedTask;
			}

			settings.MessageDeletion.Messages.Add(message);
			//The old cancel token gets canceled in its getter
			var cancelToken = settings.MessageDeletion.CancelToken;

			//Has to run on completely separate thread, else prints early
			Task.Run(async () =>
			{
				//Wait three seconds. If a new message comes in then the token will be canceled and this won't continue.
				//If more than 25 messages just start printing them out so people can't stall the messages forever.
				var inEmbed = settings.MessageDeletion.Messages.Count < 10; //Needs very few messages to fit in an embed
				if (settings.MessageDeletion.Messages.Count < 25)
				{
					try
					{
						await Task.Delay(TimeSpan.FromSeconds(3), cancelToken).CAF();
					}
					catch (TaskCanceledException)
					{
						return;
					}
				}

				//Give the messages to a new list so they can be removed from the old one
				var messages = new List<IMessage>(settings.MessageDeletion.Messages).OrderBy(x => x?.CreatedAt.Ticks).ToList();
				settings.MessageDeletion.ClearBag();

				var sb = new StringBuilder();
				while (inEmbed)
				{
					foreach (var m in messages)
					{
						sb.AppendLineFeed(m.Format(withMentions: true));
						//Can only stay in an embed if the description length is less than the max length
						//and if the line numbers are less than 20
						var validDesc = sb.Length < EmbedBuilder.MaxDescriptionLength;
						var validLines = sb.ToString().RemoveDuplicateNewLines().CountLineBreaks() < EmbedWrapper.MAX_DESCRIPTION_LINES;
						inEmbed = validDesc && validLines;
					}
					break;
				}

				var c = guildChannel.Guild.GetTextChannel(settings.ServerLogId);
				if (inEmbed)
				{
					var embed = new EmbedWrapper
					{
						Title = "Deleted Messages",
						Description = sb.ToString().RemoveDuplicateNewLines(),
						Color = EmbedWrapper.MessageDelete
					};
					embed.TryAddFooter("Deleted Messages", null, out _);
					await MessageUtils.SendMessageAsync(c, null, embed).CAF();
				}
				else
				{
					sb.Clear();
					foreach (var m in messages)
					{
						sb.AppendLineFeed(m.Format(false));
					}

					var tf = new TextFileInfo
					{
						Name = "Deleted_Messages",
						Text = sb.ToString().RemoveDuplicateNewLines().RemoveAllMarkdown(),
					};
					await MessageUtils.SendMessageAsync(c, $"**{messages.Count()} Deleted Messages:**", textFile: tf).CAF();
				}
			});
			return Task.CompletedTask;
		}

		/// <summary>
		/// Handles settings on channels, such as: image only mode.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="user"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		private async Task HandleChannelSettingsAsync(IGuildSettings settings, SocketGuildUser user, SocketMessage message)
		{
			if (!user.GuildPermissions.Administrator
				&& settings.ImageOnlyChannels.Contains(message.Channel.Id)
				&& !message.Attachments.Any(x => x.Height != null || x.Width != null)
				&& !message.Embeds.Any(x => x.Image != null))
			{
				await message.DeleteAsync().CAF();
			}
		}
		/// <summary>
		/// Logs the image to the image log if set.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="user"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		private async Task HandleImageLoggingAsync(IGuildSettings settings, SocketGuildUser user, SocketMessage message)
		{
			if (settings.ImageLogId == 0)
			{
				return;
			}

			var desc = $"**Channel:** `{message.Channel.Format()}`\n**Message Id:** `{message.Id}`";
			foreach (var attachmentUrl in message.Attachments.Select(x => x.Url).Distinct()) //Attachments
			{
				string footerText;
				var mimeType = MimeTypes.MimeTypeMap.GetMimeType(Path.GetExtension(attachmentUrl));
				if (mimeType.CaseInsContains("video/") || mimeType.CaseInsContains("/gif"))
				{
					NotifyLogCounterIncrement(nameof(ILogService.Animated), 1);
					footerText = "Attached Animated Content";
				}
				else if (mimeType.CaseInsContains("image/"))
				{
					NotifyLogCounterIncrement(nameof(ILogService.Images), 1);
					footerText = "Attached Image";
				}
				else //Random file
				{
					NotifyLogCounterIncrement(nameof(ILogService.Files), 1);
					footerText = "Attached File";
				}

				var embed = new EmbedWrapper
				{
					Description = desc,
					Color = EmbedWrapper.Attachment,
					Url = attachmentUrl,
					ImageUrl = footerText.Contains("File") ? null : attachmentUrl
				};
				embed.TryAddAuthor(user.Username, attachmentUrl, user.GetAvatarUrl(), out _);
				embed.TryAddFooter(footerText, null, out _);
				await MessageUtils.SendMessageAsync(user.Guild.GetTextChannel(settings.ImageLogId), null, embed).CAF();
			}
			foreach (var imageEmbed in message.Embeds.GroupBy(x => x.Url).Select(x => x.First()))
			{
				var embed = new EmbedWrapper
				{
					Description = desc,
					Color = EmbedWrapper.Attachment,
					Url = imageEmbed.Url,
					ImageUrl = imageEmbed.Image?.Url ?? imageEmbed.Thumbnail?.Url
				};
				embed.TryAddAuthor(user.Username, imageEmbed.Url, user.GetAvatarUrl(), out _);

				string footerText;
				if (imageEmbed.Video != null)
				{
					NotifyLogCounterIncrement(nameof(ILogService.Animated), 1);
					footerText = "Embedded Gif/Video";
				}
				else
				{
					NotifyLogCounterIncrement(nameof(ILogService.Images), 1);
					footerText = "Embedded Image";
				}

				embed.TryAddFooter(footerText, null, out _);
				await MessageUtils.SendMessageAsync(user.Guild.GetTextChannel(settings.ImageLogId), null, embed).CAF();
			}
		}
		/// <summary>
		/// Checks the message against the slowmode.
		/// </summary>
		/// <returns></returns>
		private async Task HandleSlowmodeAsync(IGuildSettings settings, SocketGuildUser user, SocketMessage message)
		{
			//Don't bother doing stuff on the user if they're immune
			if (!(settings.Slowmode is Slowmode slowmode) || !slowmode.Enabled || user.Roles.Select(x => x.Id).Intersect(slowmode.ImmuneRoleIds).Any())
			{
				return;
			}

			if (!(settings.SlowmodeUsers.SingleOrDefault(x => x.UserId == user.Id) is SlowmodeUserInfo info))
			{
				settings.SlowmodeUsers.Add(info = new SlowmodeUserInfo(slowmode.Interval, user));
			}
			else if (info.Time < DateTime.UtcNow)
			{
				info.Reset();
			}

			if (info.MessagesSent >= slowmode.BaseMessages)
			{
				await MessageUtils.DeleteMessageAsync(message, ClientUtils.CreateRequestOptions("slowmode")).CAF();
				return;
			}
			if (info.MessagesSent == 0)
			{
				info.UpdateTime(slowmode.Interval);
			}
			info.Increment();
		}
		/// <summary>
		/// If the message author can be modified by the bot then their message is checked for any spam matches.
		/// </summary>
		/// <returns></returns>
		private async Task HandleSpamPreventionAsync(IGuildSettings settings, SocketGuildUser user, SocketMessage message)
		{
			if (!user.Guild.CurrentUser.HasHigherPosition(user))
			{
				return;
			}
			if (!(settings.SpamPreventionUsers.SingleOrDefault(x => x.UserId == user.Id) is SpamPreventionUserInfo info))
			{
				settings.SpamPreventionUsers.Add(info = new SpamPreventionUserInfo(user));
			}

			var spam = false;
			foreach (SpamType type in Enum.GetValues(typeof(SpamType)))
			{
				if (!(settings.SpamPreventionDictionary[type] is SpamPreventionInfo prev) || !prev.Enabled)
				{
					continue;
				}
				if (_GetSpamNumberFuncs[type](message) >= prev.SpamPerMessage)
				{
					info.AddSpamInstance(type, message);
				}
				if (info.GetSpamAmount(type, prev.TimeInterval) < prev.SpamInstances)
				{
					continue;
				}

				//Make sure they have the lowest vote count required to kick and the most severe punishment type
				info.VotesRequired = prev.VotesForKick;
				info.Punishment = prev.Punishment;
				spam = true;
			}
			if (spam)
			{
				var votesReq = info.VotesRequired - info.UsersWhoHaveAlreadyVoted.Count;
				var content = $"`{user.Format()}` needs `{votesReq}` votes to be kicked. Vote by mentioning them.";
				await MessageUtils.MakeAndDeleteSecondaryMessageAsync((SocketTextChannel)message.Channel, null, content, Timers, TimeSpan.FromSeconds(10)).CAF();
				await MessageUtils.DeleteMessageAsync(message, ClientUtils.CreateRequestOptions("spam prevention")).CAF();
			}
		}
		/// <summary>
		/// Checks if there are any mentions to kick a spammer.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="user"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		private async Task HandleSpamPreventionVotingAsync(IGuildSettings settings, SocketGuildUser user, SocketMessage message)
		{
			if (!message.MentionedUsers.Any())
			{
				return;
			}

			var giver = new Punisher(TimeSpan.FromMinutes(0), null);
			var options = ClientUtils.CreateRequestOptions("spam prevention");
			//Iterate through the users who are able to be punished by the spam prevention
			foreach (var spammer in settings.SpamPreventionUsers.Where(x =>
			{
				return x.IsPunishable()
					&& x.UserId != user.Id
					&& message.MentionedUsers.Select(u => u.Id).Contains(x.UserId)
					&& !x.UsersWhoHaveAlreadyVoted.Contains(user.Id);
			}))
			{
				spammer.UsersWhoHaveAlreadyVoted.Add(user.Id);
				if (spammer.UsersWhoHaveAlreadyVoted.Count < spammer.VotesRequired)
				{
					continue;
				}

				await giver.GiveAsync(spammer.Punishment, user.Guild, spammer.UserId, settings.MuteRoleId, options).CAF();
				spammer.Reset();
			}
		}
		/// <summary>
		/// Makes sure a message doesn't have any banned phrases.
		/// </summary>
		/// <returns></returns>
		private async Task HandleBannedPhrasesAsync(IGuildSettings settings, SocketGuildUser user, SocketMessage message)
		{
			//Ignore admins and messages older than an hour. (Accidentally deleted something important once due to not having these checks in place, but this should stop most accidental deletions)
			if (user.GuildPermissions.Administrator || (DateTime.UtcNow - message.CreatedAt.UtcDateTime).Hours > 0)
			{
				return;
			}

			var info = settings.BannedPhraseUsers.SingleOrDefault(x => x.UserId == user.Id);
			if (info == null)
			{
				settings.BannedPhraseUsers.Add(info = new BannedPhraseUserInfo(user));
			}
			var str = settings.BannedPhraseStrings.FirstOrDefault(x => message.Content.CaseInsContains(x.Phrase));
			if (str != null)
			{
				await str.PunishAsync(settings, user.Guild, info, Timers).CAF();
			}
			var regex = settings.BannedPhraseRegex.FirstOrDefault(x => RegexUtils.IsMatch(message.Content, x.Phrase));
			if (regex != null)
			{
				await regex.PunishAsync(settings, user.Guild, info, Timers).CAF();
			}
			if (str != null || regex != null)
			{
				await MessageUtils.DeleteMessageAsync(message, ClientUtils.CreateRequestOptions("banned phrase")).CAF();
			}
		}
		/// <summary>
		/// If there are any active close quotes/help entries, handles them and removes them from the database.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="user"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		private async Task HandleCloseWords(IGuildSettings settings, SocketGuildUser user, SocketMessage message)
		{
			if (Timers == null || !int.TryParse(message.Content, out var i) || i < 0 || i > 7)
			{
				return;
			}
			--i;

			var deleteMessage = false;
			if (await Timers.RemoveActiveCloseQuoteAsync(user.Guild.Id, message.Author.Id).CAF() is CloseQuotes q && q.List.Count > i)
			{
				var embed = new EmbedWrapper
				{
					Title = q.List[i].Name,
					Description = q.List[i].Text,
				};
				embed.TryAddFooter("Quote", null, out _);
				await MessageUtils.SendMessageAsync(message.Channel, null, embed).CAF();
				deleteMessage = true;
			}
			if (await Timers.RemoveActiveCloseHelpAsync(user.Guild.Id, message.Author.Id).CAF() is CloseHelpEntries h && h.List.Count > i)
			{
				var embed = new EmbedWrapper
				{
					Title = h.List[i].Name,
					Description = h.List[i].Text.Replace(Constants.PLACEHOLDER_PREFIX, BotSettings.InternalGetPrefix(settings)),
				};
				embed.TryAddFooter("Help", null, out _);
				await MessageUtils.SendMessageAsync(message.Channel, null, embed).CAF();
				deleteMessage = true;
			}
			if (deleteMessage)
			{
				await MessageUtils.DeleteMessageAsync(message, ClientUtils.CreateRequestOptions("help entry or quote")).CAF();
			}
		}
		/// <summary>
		/// Logs images if the embed counts don't match.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="user"></param>
		/// <param name="before"></param>
		/// <param name="after"></param>
		/// <returns></returns>
		private async Task HandleMessageEditedImageLoggingAsync(IGuildSettings settings, SocketGuildUser user, SocketMessage before, SocketMessage after)
		{
			//If the before message is not specified always take that as it should be logged.
			//If the embed counts are greater take that as logging too.
			if (before?.Embeds.Count() < after.Embeds.Count())
			{
				await HandleImageLoggingAsync(settings, user, after).CAF();
			}
		}
		/// <summary>
		/// Logs the text difference to the server log if set.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="user"></param>
		/// <param name="before"></param>
		/// <param name="after"></param>
		/// <returns></returns>
		private async Task HandleMessageEditedLoggingAsync(IGuildSettings settings, SocketGuildUser user, SocketMessage before, SocketMessage after)
		{
			if (settings.ServerLogId == 0)
			{
				return;
			}

			var bMsgContent = (before?.Content ?? "Empty or unable to be gotten.").RemoveAllMarkdown().RemoveDuplicateNewLines();
			var aMsgContent = (after.Content ?? "Empty or unable to be gotten.").RemoveAllMarkdown().RemoveDuplicateNewLines();
			if (bMsgContent == aMsgContent)
			{
				return;
			}

			var embed = new EmbedWrapper
			{
				Color = EmbedWrapper.MessageEdit
			};
			embed.TryAddAuthor(after.Author, out _);
			embed.TryAddField("Before:", $"`{(bMsgContent.Length > 750 ? "Long message" : bMsgContent)}`", true, out _);
			embed.TryAddField("After:", $"`{(aMsgContent.Length > 750 ? "Long message" : aMsgContent)}`", false, out _);
			embed.TryAddFooter("Message Updated", null, out _);
			await MessageUtils.SendMessageAsync(user.Guild.GetTextChannel(settings.ServerLogId), null, embed).CAF();
		}
	}
}
