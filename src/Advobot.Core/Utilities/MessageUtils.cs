﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Advobot.Classes;
using Advobot.Interfaces;
using AdvorangesUtils;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace Advobot.Utilities
{
	/// <summary>
	/// Actions which are done on an <see cref="IMessage"/>.
	/// </summary>
	public static class MessageUtils
	{
		/// <summary>
		/// The zero length character to put before every message.
		/// </summary>
		public const string ZERO_LENGTH_CHAR = "\u180E";
		private static readonly char[] _InvalidChars = Path.GetInvalidFileNameChars();

		/// <summary>
		/// Sends a message to the given channel with the given content.
		/// </summary>
		/// <param name="channel"></param>
		/// <param name="content"></param>
		/// <param name="embedWrapper"></param>
		/// <param name="textFile"></param>
		/// <returns></returns>
		public static async Task<IUserMessage> SendMessageAsync(IMessageChannel channel, string content, EmbedWrapper embedWrapper = null, TextFileInfo textFile = null)
		{
			if (channel == null || (content == null && embedWrapper == null && textFile == null))
			{
				return null;
			}

			textFile = textFile ?? new TextFileInfo();

			//Make sure all the information from the embed that didn't fit goes in.
			if (embedWrapper != null && embedWrapper.Errors.Any())
			{
				textFile.Name = textFile.Name ?? "Embed_Errors";
				textFile.Text = $"Embed Errors:\n{embedWrapper}\n\n{textFile.Text}";
			}

			//Make sure none of the content mentions everyone or doesn't have the zero width character
			content = channel.SanitizeContent(content);
			if (content.Length > 2000)
			{
				textFile.Name = textFile.Name ?? "Long_Message";
				textFile.Text = $"Message Content:\n{content}\n\n{textFile.Text}";
				content = $"{ZERO_LENGTH_CHAR}Response is too long; sent as text file instead.";
			}

			try
			{
				//If the file name and text exists, then attempt to send as a file instead of message
				if (textFile.Name != null && textFile.Text != null)
				{
					using (var stream = new MemoryStream())
					using (var writer = new StreamWriter(stream))
					{
						writer.Write(textFile.Text.Trim());
						writer.Flush();
						stream.Seek(0, SeekOrigin.Begin);
						return await channel.SendFileAsync(stream, textFile.Name, content, embed: embedWrapper).CAF();
					}
				}

				return await channel.SendMessageAsync(content, embed: embedWrapper).CAF();
			}
			//If the message fails to send, then return the error
			catch (Exception e)
			{
				return await channel.SendMessageAsync(channel.SanitizeContent(e.Message));
			}
		}
		/// <summary>
		/// Waits a few seconds then deletes the newly created message and the context message.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="output"></param>
		/// <param name="time"></param>
		/// <returns></returns>
		public static async Task<RemovableMessage> MakeAndDeleteSecondaryMessageAsync(AdvobotCommandContext context, string output, TimeSpan time = default)
		{
			var channel = (SocketTextChannel)context.Channel;
			var timers = context.Provider.GetService<ITimerService>();
			return await MakeAndDeleteSecondaryMessageAsync(channel, context.Message, output, timers, time).CAF();
		}
		/// <summary>
		/// Waits a few seconds then deletes the newly created message and the given message.
		/// </summary>
		/// <param name="channel"></param>
		/// <param name="message"></param>
		/// <param name="output"></param>
		/// <param name="time"></param>
		/// <param name="timers"></param>
		/// <returns></returns>
		public static async Task<RemovableMessage> MakeAndDeleteSecondaryMessageAsync(SocketTextChannel channel, IUserMessage message, string output, ITimerService timers = null, TimeSpan time = default)
		{
			var secondMessage = await SendMessageAsync(channel, ZERO_LENGTH_CHAR + output).CAF();
			var removableMessage = new RemovableMessage(time, channel.Guild, channel, message.Author, message, secondMessage);
			if (timers != null)
			{
				await timers.AddAsync(removableMessage).CAF();
			}
			return removableMessage;
		}
		/// <summary>
		/// If the guild has verbose errors enabled then this acts just like makeanddeletesecondarymessage.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="error"></param>
		/// <param name="time"></param>
		/// <returns></returns>
		public static async Task<RemovableMessage> SendErrorMessageAsync(AdvobotCommandContext context, Error error, TimeSpan time = default)
		{
			var channel = (SocketTextChannel)context.Channel;
			var timers = context.Provider.GetService<ITimerService>();
			return await SendErrorMessageAsync(channel, context.Message, error, context.GuildSettings, timers, time).CAF();
		}
		/// <summary>
		/// If the guild has verbose errors enabled then this acts just like makeanddeletesecondarymessage.
		/// </summary>
		/// <param name="timers"></param>
		/// <param name="settings"></param>
		/// <param name="channel"></param>
		/// <param name="message"></param>
		/// <param name="error"></param>
		/// <param name="time"></param>
		/// <returns></returns>
		public static async Task<RemovableMessage> SendErrorMessageAsync(SocketTextChannel channel, IUserMessage message, Error error, IGuildSettings settings, ITimerService timers, TimeSpan time = default)
		{
			return settings.NonVerboseErrors ? default : await MakeAndDeleteSecondaryMessageAsync(channel, message, $"**ERROR:** {error.Reason}", timers, time).CAF();
		}
		/// <summary>
		/// Gets the given count of messages from a channel.
		/// </summary>
		/// <param name="channel"></param>
		/// <param name="requestCount"></param>
		/// <returns></returns>
		public static async Task<IEnumerable<IMessage>> GetMessagesAsync(SocketTextChannel channel, int requestCount)
		{
			//TODO: do something similar to delete messages async to get more than 100 messages?
			return await channel.GetMessagesAsync(requestCount).FlattenAsync().CAF();
		}
		/// <summary>
		/// Removes the given count of messages from a channel.
		/// </summary>
		/// <param name="channel"></param>
		/// <param name="fromMessage"></param>
		/// <param name="requestCount"></param>
		/// <param name="options"></param>
		/// <param name="fromUser"></param>
		/// <returns></returns>
		public static async Task<int> DeleteMessagesAsync(SocketTextChannel channel, IMessage fromMessage, int requestCount, RequestOptions options, IUser fromUser = null)
		{
			var deletedCount = 0;
			while (requestCount > 0)
			{
				var messages = (await channel.GetMessagesAsync(fromMessage, Direction.Before, 100).FlattenAsync().CAF()).ToList();
				fromMessage = messages.LastOrDefault();

				//Get messages from a targeted user if one is supplied
				var userMessages = fromUser == null ? messages : messages.Where(x => x.Author.Id == fromUser.Id);
				var cutMessages = userMessages.Take(requestCount).ToList();

				//If less messages are deleted than gathered, that means there are some that are too old meaning we can stop
				var deletedThisIteration = await DeleteMessagesAsync(channel, cutMessages, options).CAF();
				deletedCount += deletedThisIteration;
				requestCount -= deletedThisIteration;
				if (deletedThisIteration < cutMessages.Count)
				{
					break;
				}
			}
			return deletedCount;
		}
		/// <summary>
		/// Deletes the passed in messages directly. Will only delete messages under 14 days old.
		/// </summary>
		/// <param name="channel"></param>
		/// <param name="messages"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		public static async Task<int> DeleteMessagesAsync(SocketTextChannel channel, IEnumerable<IMessage> messages, RequestOptions options)
		{
			var validMessages = messages?.Where(x => x != null && (DateTime.UtcNow - x.CreatedAt.UtcDateTime).TotalDays < 14)?.ToList();
			if (validMessages == null || !validMessages.Any())
			{
				return 0;
			}

			try
			{
				if (validMessages.Count == 1)
				{
					await validMessages[0].DeleteAsync(options).CAF();
				}
				else
				{
					await channel.DeleteMessagesAsync(validMessages, options).CAF();
				}
				return validMessages.Count;
			}
			catch
			{
				return 0;
			}
		}
		/// <summary>
		/// Deletes the passed in message directly. Will only delete messages under 14 days old.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		public static async Task<int> DeleteMessageAsync(IMessage message, RequestOptions options)
		{
			return await DeleteMessagesAsync((SocketTextChannel)message.Channel, new[] { message }, options).CAF();
		}

		private static string SanitizeContent(this IMessageChannel channel, string content)
		{
			if (content == null)
			{
				return ZERO_LENGTH_CHAR;
			}
			if (!content.StartsWith(ZERO_LENGTH_CHAR))
			{
				content = ZERO_LENGTH_CHAR + content;
			}
			if (channel is IGuildChannel guildChannel)
			{
				content = content.CaseInsReplace(guildChannel.Guild.EveryoneRole.Mention, $"@{ZERO_LENGTH_CHAR}everyone"); //Everyone and Here have the same role
			}
			return content
				.CaseInsReplace("@everyone", $"@{ZERO_LENGTH_CHAR}everyone")
				.CaseInsReplace("@here", $"@{ZERO_LENGTH_CHAR}here")
				.CaseInsReplace("discord.gg", $"discord{ZERO_LENGTH_CHAR}.gg")
				.CaseInsReplace("\tts", $"\\{ZERO_LENGTH_CHAR}tts");
		}
	}
}