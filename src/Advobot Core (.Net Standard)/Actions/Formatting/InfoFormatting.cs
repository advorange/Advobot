﻿using Advobot.Interfaces;
using Discord;
using Discord.WebSocket;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Advobot.Actions.Formatting
{
	public static class InfoFormatting
	{
		/// <summary>
		/// Returns a new <see cref="EmbedBuilder"/> containing information about a user on a guild.
		/// </summary>
		/// <param name="guildSettings"></param>
		/// <param name="guild"></param>
		/// <param name="user"></param>
		/// <returns></returns>
		public static EmbedBuilder FormatUserInfo(SocketGuild guild, SocketGuildUser user)
		{
			var textChannels = guild.TextChannels.Where(x => user.GetPermissions(x).ReadMessages).OrderBy(x => x.Position).Select(x => x.Name);
			var voiceChannels = guild.VoiceChannels.Where(x => user.GetPermissions(x).Connect).OrderBy(x => x.Position).Select(x => x.Name + " (Voice)");
			var channels = textChannels.Concat(voiceChannels);
			var users = guild.Users.Where(x => x.JoinedAt != null).OrderBy(x => x.JoinedAt.Value.Ticks).ToList();
			var roles = user.Roles.OrderBy(x => x.Position).Where(x => !x.IsEveryone);

			var desc = new StringBuilder()
				.AppendLineFeed($"**ID:** `{user.Id}`")
				.AppendLineFeed($"**Nickname:** `{(String.IsNullOrWhiteSpace(user.Nickname) ? "No nickname" : user.Nickname.EscapeBackTicks())}`")
				.AppendLineFeed(TimeFormatting.FormatDateTimeForCreatedAtMessage(user.CreatedAt.UtcDateTime))
				.AppendLineFeed($"**Joined:** `{TimeFormatting.FormatReadableDateTime(user.JoinedAt.Value.UtcDateTime)}` (`{users.IndexOf(user) + 1}` to join the guild)\n")
				.AppendLineFeed(DiscordObjectFormatting.FormatGame(user))
				.AppendLineFeed($"**Online status:** `{user.Status}`");

			var color = roles.OrderBy(x => x.Position).LastOrDefault(x => x.Color.RawValue != 0)?.Color;
			var embed = EmbedActions.MakeNewEmbed(null, desc.ToString(), color, thumbnailUrl: user.GetAvatarUrl())
				.MyAddAuthor(user)
				.MyAddFooter("User Info");

			if (channels.Count() != 0)
			{
				embed.AddField("Channels", String.Join(", ", channels));
			}
			if (roles.Count() != 0)
			{
				embed.AddField("Roles", String.Join(", ", roles.Select(x => x.Name)));
			}
			if (user.VoiceChannel != null)
			{
				var value = new StringBuilder()
					.AppendLineFeed($"Server mute: `{user.IsMuted}`")
					.AppendLineFeed($"Server deafen: `{user.IsDeafened}`")
					.AppendLineFeed($"Self mute: `{user.IsSelfMuted}`")
					.AppendLineFeed($"Self deafen: `{user.IsSelfDeafened}`");
				embed.MyAddField("Voice Channel: " + user.VoiceChannel.Name, value.ToString());
			}
			return embed;
		}
		/// <summary>
		/// Returns a new <see cref="EmbedBuilder"/> containing information about a user not on a guild.
		/// </summary>
		/// <param name="guildSettings"></param>
		/// <param name="guild"></param>
		/// <param name="user"></param>
		/// <returns></returns>
		public static EmbedBuilder FormatUserInfo(SocketGuild guild, SocketUser user)
		{
			var desc = new StringBuilder()
				.AppendLineFeed(TimeFormatting.FormatDateTimeForCreatedAtMessage(user.CreatedAt.UtcDateTime))
				.AppendLineFeed(DiscordObjectFormatting.FormatGame(user))
				.AppendLineFeed($"**Online status:** `{user.Status}`");

			return EmbedActions.MakeNewEmbed(null, desc.ToString(), null, thumbnailUrl: user.GetAvatarUrl())
				.MyAddAuthor(user)
				.MyAddFooter("User Info");
		}
		/// <summary>
		/// Returns a new <see cref="EmbedBuilder"/> containing information about a role.
		/// </summary>
		/// <param name="guildSettings"></param>
		/// <param name="guild"></param>
		/// <param name="role"></param>
		/// <returns></returns>
		public static EmbedBuilder FormatRoleInfo(SocketGuild guild, SocketRole role)
		{
			var desc = new StringBuilder()
				.AppendLineFeed(TimeFormatting.FormatDateTimeForCreatedAtMessage(role.CreatedAt.UtcDateTime))
				.AppendLineFeed($"**Position:** `{role.Position}`")
				.AppendLineFeed($"**User Count:** `{guild.Users.Where(x => x.Roles.Any(y => y.Id == role.Id)).Count()}`");

			return EmbedActions.MakeNewEmbed(null, desc.ToString(), role.Color)
				.MyAddAuthor(role.FormatRole())
				.MyAddFooter("Role Info");
		}
		/// <summary>
		/// Returns a new <see cref="EmbedBuilder"/> containing information about a channel.
		/// </summary>
		/// <param name="guildSettings"></param>
		/// <param name="guild"></param>
		/// <param name="channel"></param>
		/// <returns></returns>
		public static EmbedBuilder FormatChannelInfo(IGuildSettings guildSettings, SocketGuild guild, SocketChannel channel)
		{
			var ignoredFromLog = guildSettings.IgnoredLogChannels.Contains(channel.Id);
			var ignoredFromCmd = guildSettings.IgnoredCommandChannels.Contains(channel.Id);
			var imageOnly = guildSettings.ImageOnlyChannels.Contains(channel.Id);
			var serverLog = guildSettings.ServerLog?.Id == channel.Id;
			var modLog = guildSettings.ModLog?.Id == channel.Id;
			var imageLog = guildSettings.ImageLog?.Id == channel.Id;

			var desc = new StringBuilder()
				.AppendLineFeed(TimeFormatting.FormatDateTimeForCreatedAtMessage(channel.CreatedAt.UtcDateTime))
				.AppendLineFeed($"**User Count:** `{channel.Users.Count}`\n")
				.AppendLineFeed($"\n**Ignored From Log:** `{(ignoredFromLog ? "Yes" : "No")}`")
				.AppendLineFeed($"**Ignored From Commands:** `{(ignoredFromCmd ? "Yes" : "No")}`")
				.AppendLineFeed($"**Image Only:** `{(imageOnly ? "Yes" : "No")}`")
				.AppendLineFeed($"\n**Serverlog:** `{(serverLog ? "Yes" : "No")}`")
				.AppendLineFeed($"**Modlog:** `{(modLog ? "Yes" : "No")}`")
				.AppendLineFeed($"**Imagelog:** `{(imageLog ? "Yes" : "No")}`");

			return EmbedActions.MakeNewEmbed(null, desc.ToString())
				.MyAddAuthor(channel.FormatChannel())
				.MyAddFooter("Channel Info");
		}
		/// <summary>
		/// Returns a new <see cref="EmbedBuilder"/> containing information about a guild.
		/// </summary>
		/// <param name="guildSettings"></param>
		/// <param name="guild"></param>
		/// <returns></returns>
		public static EmbedBuilder FormatGuildInfo(SocketGuild guild)
		{
			var owner = guild.Owner;
			var onlineCount = guild.Users.Where(x => x.Status != UserStatus.Offline).Count();
			var nicknameCount = guild.Users.Where(x => x.Nickname != null).Count();
			var gameCount = guild.Users.Where(x => x.Game.HasValue).Count();
			var botCount = guild.Users.Where(x => x.IsBot).Count();
			var voiceCount = guild.Users.Where(x => x.VoiceChannel != null).Count();
			var localECount = guild.Emotes.Where(x => !x.IsManaged).Count();
			var globalECount = guild.Emotes.Where(x => x.IsManaged).Count();

			var desc = new StringBuilder()
				.AppendLineFeed(TimeFormatting.FormatDateTimeForCreatedAtMessage(guild.CreatedAt.UtcDateTime))
				.AppendLineFeed($"**Owner:** `{owner.FormatUser()}`")
				.AppendLineFeed($"**Region:** `{guild.VoiceRegionId}`")
				.AppendLineFeed($"**Emotes:** `{localECount + globalECount}` (`{localECount}` local, `{globalECount}` global)\n")
				.AppendLineFeed($"**User Count:** `{guild.MemberCount}` (`{onlineCount}` online, `{botCount}` bots)")
				.AppendLineFeed($"**Users With Nickname:** `{nicknameCount}`")
				.AppendLineFeed($"**Users Playing Games:** `{gameCount}`")
				.AppendLineFeed($"**Users In Voice:** `{voiceCount}`\n")
				.AppendLineFeed($"**Role Count:** `{guild.Roles.Count}`")
				.AppendLineFeed($"**Channel Count:** `{guild.Channels.Count}` (`{guild.TextChannels.Count}` text, `{guild.VoiceChannels.Count}` voice)")
				.AppendLineFeed($"**AFK Channel:** `{guild.AFKChannel.FormatChannel()}` (`{guild.AFKTimeout / 60}` minute{GetActions.GetPlural(guild.AFKTimeout / 60)})");

			var color = owner.Roles.FirstOrDefault(x => x.Color.RawValue != 0)?.Color;
			return EmbedActions.MakeNewEmbed(null, desc.ToString(), color, thumbnailUrl: guild.IconUrl)
				.MyAddAuthor(guild.FormatGuild())
				.MyAddFooter("Guild Info");
		}
		/// <summary>
		/// Returns a new <see cref="EmbedBuilder"/> containing information about an emote.
		/// </summary>
		/// <param name="guildSettings"></param>
		/// <param name="emote"></param>
		/// <returns></returns>
		public static EmbedBuilder FormatEmoteInfo(Emote emote)
		{
			var desc = new StringBuilder()
				.AppendLineFeed($"**ID:** `{emote.Id}`");

			return EmbedActions.MakeNewEmbed(null, desc.ToString(), thumbnailUrl: emote.Url)
				.MyAddAuthor(emote.Name)
				.MyAddFooter("Emoji Info");
		}
		/// <summary>
		/// Returns a new <see cref="EmbedBuilder"/> containing information about an invite.
		/// </summary>
		/// <param name="guildSettings"></param>
		/// <param name="guild"></param>
		/// <param name="invite"></param>
		/// <returns></returns>
		public static EmbedBuilder FormatInviteInfo(IInviteMetadata invite)
		{
			var desc = new StringBuilder()
				.AppendLineFeed(TimeFormatting.FormatDateTimeForCreatedAtMessage(invite.CreatedAt.UtcDateTime))
				.AppendLineFeed($"**Inviter:** `{invite.Inviter.FormatUser()}`")
				.AppendLineFeed($"**Channel:** `{invite.Channel.FormatChannel()}`")
				.AppendLineFeed($"**Uses:** `{invite.Uses}`");

			return EmbedActions.MakeNewEmbed(null, desc.ToString())
				.MyAddAuthor(invite.Code)
				.MyAddFooter("Emote Info");
		}
		/// <summary>
		/// Returns a new <see cref="EmbedBuilder"/> containing information about the bot.
		/// </summary>
		/// <param name="globalInfo"></param>
		/// <param name="client"></param>
		/// <param name="logModule"></param>
		/// <param name="guild"></param>
		/// <returns></returns>
		public static EmbedBuilder FormatBotInfo(IBotSettings globalInfo, IDiscordClient client, ILogService logModule, IGuild guild)
		{
			var desc = new StringBuilder()
				.AppendLineFeed($"**Online Since:** `{TimeFormatting.FormatReadableDateTime(Process.GetCurrentProcess().StartTime)}`")
				.AppendLineFeed($"**Uptime:** `{TimeFormatting.FormatUptime()}`")
				.AppendLineFeed($"**Guild Count:** `{logModule.TotalGuilds}`")
				.AppendLineFeed($"**Cumulative Member Count:** `{logModule.TotalUsers}`")
				.AppendLineFeed($"**Current Shard:** `{ClientActions.GetShardIdFor(client, guild)}`");

			var firstField = new StringBuilder()
				.AppendLineFeed(logModule.FormatLoggedActions());

			var secondField = logModule.FormatLoggedCommands();

			var thirdField = new StringBuilder()
				.AppendLineFeed($"**Latency:** `{ClientActions.GetLatency(client)}ms`")
				.AppendLineFeed($"**Memory Usage:** `{GetActions.GetMemory().ToString("0.00")}MB`")
				.AppendLineFeed($"**Thread Count:** `{Process.GetCurrentProcess().Threads.Count}`");

			return EmbedActions.MakeNewEmbed(null, desc.ToString())
				.MyAddAuthor(client.CurrentUser)
				.MyAddField("Logged Actions", firstField.ToString())
				.MyAddField("Commands", secondField.ToString())
				.MyAddField("Technical", thirdField.ToString())
				.MyAddFooter("Version " + Constants.BOT_VERSION);
		}
	}
}