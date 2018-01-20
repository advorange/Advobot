﻿using Advobot.Core.Utilities;
using Advobot.Core.Utilities.Formatting;
using Advobot.Core.Classes.Attributes;
using Advobot.Core.Interfaces;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Advobot.Core.Classes.GuildSettings
{
	/// <summary>
	/// Notification that gets sent whenever certain events happen depending on what <see cref="GuildNotificationType"/> is linked to this notification.
	/// </summary>
	public class GuildNotification : IGuildSetting, IPostDeserialize
	{
		public const string USER_MENTION = "%USERMENTION%";
		public const string USER_STRING = "%USER%";

		[JsonProperty]
		public string Content { get; }
		[JsonProperty]
		public string Title { get; }
		[JsonProperty]
		public string Description { get; }
		[JsonProperty]
		public string ThumbUrl { get; }
		[JsonProperty]
		public ulong ChannelId { get; private set; }
		[JsonIgnore]
		public EmbedWrapper Embed { get; }
		[JsonIgnore]
		private ITextChannel _Channel;
		[JsonIgnore]
		public ITextChannel Channel
		{
			get => _Channel;
			set
			{
				_Channel = value;
				ChannelId = value?.Id ?? 0;
			}
		}

		[JsonConstructor]
		internal GuildNotification(string content, string title, string description, string thumbUrl, ulong channelId)
		{
			Content = content;
			Title = title;
			Description = description;
			ThumbUrl = thumbUrl;
			ChannelId = channelId;
			if (!(String.IsNullOrWhiteSpace(title) && String.IsNullOrWhiteSpace(description) && String.IsNullOrWhiteSpace(thumbUrl)))
			{
				Embed = new EmbedWrapper
				{
					Title = title,
					Description = description,
					ThumbnailUrl = thumbUrl,
				};
			}
		}
		public GuildNotification() : this(null, null, null, null, 0) { }
		[NamedArgumentConstructor]
		public GuildNotification(
			[NamedArgument] string content,
			[NamedArgument] string title,
			[NamedArgument] string description,
			[NamedArgument] string thumbURL,
			ITextChannel channel) : this(content, title, description, thumbURL, channel.Id)
		{
			Channel = channel;
		}

		/// <summary>
		/// Sends the notification to the channel.
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		public async Task SendAsync(IUser user)
		{
			var content = Content
				.CaseInsReplace(USER_MENTION, user != null ? user.Mention : "Invalid User")
				.CaseInsReplace(USER_STRING, user != null ? user.Format() : "Invalid User");
			//Put a zero length character in between invite links for names so the invite links will no longer embed

			if (Embed != null)
			{
				await MessageUtils.SendEmbedMessageAsync(Channel, Embed, content).CAF();
			}
			else
			{
				await MessageUtils.SendMessageAsync(Channel, content).CAF();
			}
		}
		/// <summary>
		/// Sets <see cref="Channel"/> to whichever text channel on <paramref name="guild"/> has the Id <see cref="ChannelId"/>.
		/// </summary>
		/// <param name="guild"></param>
		public void PostDeserialize(SocketGuild guild)
		{
			Channel = guild.GetTextChannel(ChannelId);
		}

		public override string ToString()
		{
			return new StringBuilder()
.AppendLineFeed($"**Channel:** `{Channel.Format()}`")
.AppendLineFeed($"**Content:** `{Content}`")
.AppendLineFeed($"**Title:** `{Title}`")
.AppendLineFeed($"**Description:** `{Description}`")
.AppendLineFeed($"**Thumbnail:** `{ThumbUrl}`").ToString();
		}
		public string ToString(SocketGuild guild)
		{
			return ToString();
		}
	}
}