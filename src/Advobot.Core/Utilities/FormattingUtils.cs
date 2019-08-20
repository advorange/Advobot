﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using AdvorangesUtils;
using Discord;

namespace Advobot.Utilities
{
	/// <summary>
	/// Formatting for information about Discord objects.
	/// </summary>
	public static class FormattingUtils
	{
		/// <summary>
		/// Returns a string with the object's name and id.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static string Format<T>(this IEntity<T> obj) where T : IEquatable<T> => obj switch
		{
			IUser user => user.Format(),
			IRole role => role.Format(),
			IChannel channel => channel.Format(),
			IGuild guild => guild.Format(),
			IMessage message => message.Format(true),
			IActivity activity => activity.Format(),
			IWebhook webhook => webhook.Format(),
			IInviteMetadata invite => invite.Format(),
			IEmote emote => emote.Format(),
			_ => obj.ToString(),
		};
		/// <summary>
		/// Returns a string with the user's name, discriminator and id.
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		public static string Format(this IUser user)
			=> user != null ? $"'{user.Username.EscapeBackTicks()}#{user.Discriminator}' ({user.Id})" : "Irretrievable User";
		/// <summary>
		/// Returns a string with the role's name and id.
		/// </summary>
		/// <param name="role"></param>
		/// <returns></returns>
		public static string Format(this IRole role)
			=> role != null ? $"'{role.Name.EscapeBackTicks()}' ({role.Id})" : "Irretrievable Role";
		/// <summary>
		/// Returns a string with the channel's name and id.
		/// </summary>
		/// <param name="channel"></param>
		/// <returns></returns>
		public static string Format(this IChannel channel)
			=> channel != null ? $"'{channel.Name.EscapeBackTicks()}' ({channel.GetChannelType()}) ({channel.Id})" : "Irretrievable Channel";
		/// <summary>
		/// Returns a string with the guild's name and id.
		/// </summary>
		/// <param name="guild"></param>
		/// <returns></returns>
		public static string Format(this IGuild guild)
			=> guild != null ? $"'{guild.Name.EscapeBackTicks()}' ({guild.Id})" : "Irretrievable Guild";
		/// <summary>
		/// Returns a string with the messages content, embeds, and attachments listed.
		/// </summary>
		/// <param name="msg"></param>
		/// <param name="withMentions"></param>
		/// <returns></returns>
		public static string Format(this IMessage msg, bool withMentions)
		{
			var text = string.IsNullOrEmpty(msg.Content) ? "Empty message content" : msg.Content;
			var time = msg.CreatedAt.ToString("HH:mm:ss");
			var header = withMentions
				? $"`[{time}]` {((ITextChannel)msg.Channel).Mention} {msg.Author.Mention} `{msg.Id}`"
				: $"`[{time}]` `{msg.Channel.Format()}` `{msg.Author.Format()}` `{msg.Id}`";

			var sb = new StringBuilder($"{header}\n```\n{text.EscapeBackTicks()}");

			var currentEmbed = 1;
			foreach (var embed in msg.Embeds)
			{
				if (embed.Description == null && embed.Url == null && !embed.Image.HasValue)
				{
					continue;
				}

				sb.Append($"Embed {currentEmbed}: {embed.Description ?? "No description"}".EscapeBackTicks());
				if (embed.Url != null)
				{
					sb.Append($" URL: {embed.Url}");
				}
				if (embed.Image.HasValue)
				{
					sb.Append($" IURL: {embed.Image.Value.Url}");
				}
				sb.AppendLineFeed();
				++currentEmbed;
			}
			var attachments = msg.Attachments.Select(x => x.Filename).ToArray();
			if (attachments.Any())
			{
				sb.AppendLineFeed($" + {string.Join(" + ", attachments).EscapeBackTicks()}");
			}
			return sb.Append("```").ToString();
		}
		/// <summary>
		/// Returns a string with the game's name or stream name/url.
		/// </summary>
		/// <param name="activity"></param>
		/// <returns></returns>
		public static string Format(this IActivity activity) => activity switch
		{
			SpotifyGame sp => $"Listening to {sp.TrackTitle}",
			StreamingGame sg => $"Streaming {sg.Name.EscapeBackTicks()} at {sg.Url}",
			RichGame rg => $"Playing {rg.Name.EscapeBackTicks()} ({rg.State.EscapeBackTicks()})",
			Game g => $"Playing {g.Name.EscapeBackTicks()}",
			_ => "N/A",
		};
		/// <summary>
		/// Returns a string with the webhook's name and id.
		/// </summary>
		/// <param name="webhook"></param>
		/// <returns></returns>
		public static string Format(this IWebhook webhook)
			=> webhook != null ? $"'{webhook.Name.EscapeBackTicks()}' ({webhook.Id})" : "Irretrievable Webhook";
		/// <summary>
		/// Formats information about an invite.
		/// </summary>
		/// <param name="invite"></param>
		/// <returns></returns>
		public static string Format(this IInviteMetadata invite)
		{
			const string INF = "\u221E"; //∞
			if (invite == null)
			{
				return "Irretrievable Invite";
			}

			var uses = invite.MaxUses.HasValue ? invite.MaxUses.Value.ToString() : INF;
			var time = invite.MaxAge.HasValue ? (invite.MaxAge.Value / 60).ToString() : INF;
			var temp = invite.IsTemporary ? ", temp" : "";
			return $"'{invite.Code}' ({uses} uses, {time} minutes{temp})";
		}
		/// <summary>
		/// Formats information about an emote.
		/// </summary>
		/// <param name="emote"></param>
		/// <returns></returns>
		public static string Format(this IEmote emote) => emote switch
		{
			Emote e => $"'{e.Name.EscapeBackTicks()}' ({e.Id})",
			IEmote e => $"'{e.Name.EscapeBackTicks()}'",
			_ => "Irretrievable Emote",
		};
		/// <summary>
		/// Invokes <see cref="string.Format(IFormatProvider, string, object)"/>
		/// </summary>
		/// <param name="provider"></param>
		/// <param name="format"></param>
		/// <param name="arg0"></param>
		/// <returns></returns>
		public static string Format(
			this IFormatProvider provider,
			string format,
			object arg0)
			=> string.Format(provider, format, arg0);
		/// <summary>
		/// Invokes <see cref="string.Format(IFormatProvider, string, object, object)"/>.
		/// </summary>
		/// <param name="provider"></param>
		/// <param name="format"></param>
		/// <param name="arg0"></param>
		/// <param name="arg1"></param>
		/// <returns></returns>
		public static string Format(
			this IFormatProvider provider,
			string format,
			object arg0,
			object arg1)
			=> string.Format(provider, format, arg0, arg1);
		/// <summary>
		/// Invokes <see cref="string.Format(IFormatProvider, string, object, object, object)"/>.
		/// </summary>
		/// <param name="provider"></param>
		/// <param name="format"></param>
		/// <param name="arg0"></param>
		/// <param name="arg1"></param>
		/// <param name="arg2"></param>
		/// <returns></returns>
		public static string Format(
			this IFormatProvider provider,
			string format,
			object arg0,
			object arg1,
			object arg2)
			=> string.Format(provider, format, arg0, arg1, arg2);
		/// <summary>
		/// Invokes <see cref="string.Format(IFormatProvider, string, object[])"/>.
		/// </summary>
		/// <param name="provider"></param>
		/// <param name="format"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public static string Format(
			this IFormatProvider provider,
			string format,
			object[] args)
			=> string.Format(provider, format, args);
		/// <summary>
		/// Formats the interpolated string with the specified format provider.
		/// </summary>
		/// <param name="provider"></param>
		/// <param name="formattable"></param>
		/// <returns></returns>
		public static string FormatInterpolated(
			this IFormatProvider provider,
			FormattableString formattable)
			=> formattable.ToString(provider);
		/// <summary>
		/// Returns a dictionary of the names of each permission and its value.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="values"></param>
		/// <param name="localizer"></param>
		/// <param name="padLength"></param>
		/// <returns></returns>
		public static IDictionary<string, string> FormatPermissionValues<T>(
			this IDictionary<T, PermValue> values,
			Func<T, string> localizer,
			out int padLength) where T : Enum
		{
			padLength = -1;
			var temp = new Dictionary<string, string>();
			foreach (var kvp in values)
			{
				var name = localizer(kvp.Key);
				var value = kvp.Value switch
				{
					PermValue.Allow => Constants.ALLOWED,
					PermValue.Deny => Constants.DENIED,
					_ => throw new ArgumentOutOfRangeException(nameof(kvp.Value)),
				};
				padLength = Math.Max(padLength, name.Length);
				temp.Add(name, value);
			}
			return temp;
		}
		/// <summary>
		/// Invokes <see cref="string.Join(string, string[])"/> with the current culture's list separator.
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public static string ToDelimitedString(
			this IEnumerable<string> source)
		{
			var separator = CultureInfo.CurrentCulture.TextInfo.ListSeparator + " ";
			return source.ToDelimitedString(separator);
		}
		/// <summary>
		/// Invokes <see cref="string.Join(string, string[])"/> with <paramref name="separator"/>.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="separator"></param>
		/// <returns></returns>
		public static string ToDelimitedString(
			this IEnumerable<string> source,
			string separator)
			=> string.Join(separator, source);
		/// <summary>
		/// Invokes <see cref="string.Join(string, string[])"/> with the current culture's list separator.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="converter"></param>
		/// <returns></returns>
		public static string ToDelimitedString<T>(
			this IEnumerable<T> source,
			Func<T, string> converter)
		{
			var separator = CultureInfo.CurrentCulture.TextInfo.ListSeparator + " ";
			return source.ToDelimitedString(converter, separator);
		}
		/// <summary>
		/// Invokes <see cref="string.Join(string, string[])"/> with <paramref name="separator"/>.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="converter"></param>
		/// <param name="separator"></param>
		/// <returns></returns>
		public static string ToDelimitedString<T>(
			this IEnumerable<T> source,
			Func<T, string> converter,
			string separator)
			=> string.Join(separator, source.Select(converter));
		/// <summary>
		/// Invokes <see cref="string.Format(string, object[])"/>.
		/// </summary>
		/// <param name="format"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public static string Format(this string format, params MarkdownFormattedArg[] args)
			=> string.Format(format, args);
		/// <summary>
		/// Formats the string as a title (in title case, with a colon at the end, and in bold).
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static MarkdownFormattedArg AsTitleWithColon(this string value)
		{
			var title = value.FormatTitle();
			if (!title.EndsWith(':'))
			{
				title += ":";
			}
			return new MarkdownFormattedArg(value, title.AddMarkdown("**"));
		}
		/// <summary>
		/// Returns the string as itself.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static MarkdownFormattedArg WithNoMarkdown(this string value)
			=> new MarkdownFormattedArg(value, value);
		/// <summary>
		/// Returns the string in title case.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static MarkdownFormattedArg WithTitleCase(this string value)
			=> new MarkdownFormattedArg(value, value.FormatTitle());
		/// <summary>
		/// Formats the string inside a standard code block.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static MarkdownFormattedArg WithBlock(this string value)
			=> new MarkdownFormattedArg(value, value.AddMarkdown("`"));
		/// <summary>
		/// Formats the string inside a big code block.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static MarkdownFormattedArg WithBigBlock(this string value)
			=> new MarkdownFormattedArg(value, value.AddMarkdown("```"));
		/// <summary>
		/// Returns a new <see cref="EmbedAuthorBuilder"/> containing the user's info.
		/// </summary>
		/// <param name="author"></param>
		/// <returns></returns>
		public static EmbedAuthorBuilder CreateAuthor(this IUser author)
			=> new EmbedAuthorBuilder { IconUrl = author?.GetAvatarUrl(), Name = author?.Format(), Url = author?.GetAvatarUrl(), };

		private static string AddMarkdown(this string value, string markdown)
			=> $"{markdown}{value}{markdown}";
		private static string GetChannelType(this IChannel channel) => channel switch
		{
			IMessageChannel _ => "text",
			IVoiceChannel _ => "voice",
			ICategoryChannel _ => "category",
			_ => "unknown",
		};

		/// <summary>
		/// Contains the original value and a newly formatted value.
		/// </summary>
		public sealed class MarkdownFormattedArg
		{
			/// <summary>
			/// The original value.
			/// </summary>
			public string Original { get; }
			/// <summary>
			/// The newly created value.
			/// </summary>
			public string Value { get; }

			/// <summary>
			/// Creates an instance of <see cref="MarkdownFormattedArg"/>.
			/// </summary>
			/// <param name="original"></param>
			/// <param name="current"></param>
			public MarkdownFormattedArg(string original, string current)
			{
				Original = original;
				Value = current;
			}

			/// <inheritdoc />
			public override string ToString()
				=> Value;
		}
	}
}
