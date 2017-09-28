﻿using Advobot.Actions;
using Advobot.Actions.Formatting;
using Advobot.Enums;
using Advobot.Interfaces;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Advobot.Modules.Log
{
	internal class UserLogger : Logger, IUserLogger
	{
		internal UserLogger(ILogModule logging, IServiceProvider provider) : base(logging, provider) { }

		protected override void HookUpEvents()
		{
			if (_Client is DiscordSocketClient socketClient)
			{
				socketClient.UserJoined		+= OnUserJoined;
				socketClient.UserLeft		+= OnUserLeft;
				socketClient.UserUpdated	+= OnUserUpdated;
			}
			else if (_Client is DiscordShardedClient shardedClient)
			{
				shardedClient.UserJoined	+= OnUserJoined;
				shardedClient.UserLeft		+= OnUserLeft;
				shardedClient.UserUpdated	+= OnUserUpdated;
			}
			else
			{
				throw new ArgumentException($"Invalid client provided. Must be either a {nameof(DiscordSocketClient)} or a {nameof(DiscordShardedClient)}.");
			}
		}

		/// <summary>
		/// Checks for banned names and raid prevention, logs their join to the server log, or says the welcome message.
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		internal async Task OnUserJoined(SocketGuildUser user)
		{
			_Logging.TotalUsers.Increment();
			_Logging.UserJoins.Increment();

			if (Logging.VerifyBotLogging(_BotSettings, _GuildSettings, user, out var guildSettings))
			{
				//Bans people who join with a given word in their name
				if (guildSettings.BannedNamesForJoiningUsers.Any(x => user.Username.CaseInsContains(x.Phrase)))
				{
					await Punishments.AutomaticPunishments(PunishmentType.Ban, user, reason: "banned name");
					return;
				}

				await HelperFunctions.HandleJoiningUsersForRaidPrevention(_Timers, guildSettings, user);
				if (Logging.VerifyLogAction(guildSettings, LogAction.UserJoined))
				{
					var inviteStr = await DiscordObjectFormatting.FormatInviteJoin(guildSettings, user);
					var ageWarningStr = DiscordObjectFormatting.FormatAccountAgeWarning(user);
					var embed = EmbedActions.MakeNewEmbed(null, $"**ID:** {user.Id}\n{inviteStr}\n{ageWarningStr}", Colors.JOIN)
						.MyAddAuthor(user)
						.MyAddFooter(user.IsBot ? "Bot Joined" : "User Joined");
					await MessageActions.SendEmbedMessage(guildSettings.ServerLog, embed);
				}

				//Welcome message
				if (guildSettings.WelcomeMessage != null)
				{
					await guildSettings.WelcomeMessage.Send(user);
				}
			}
		}
		/// <summary>
		/// Does nothing if the bot is the user, logs their leave to the server log, or says the goodbye message.
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		internal async Task OnUserLeft(SocketGuildUser user)
		{
			_Logging.TotalUsers.Decrement();
			_Logging.UserLeaves.Increment();

			//Check if the bot was the one that left
			if (user.Id.ToString() == Config.Configuration[ConfigKeys.Bot_Id])
			{
				return;
			}

			if (Logging.VerifyBotLogging(_BotSettings, _GuildSettings, user, out var guildSettings))
			{
				//Don't log them to the server if they're someone who was just banned for joining with a banned name
				if (guildSettings.BannedNamesForJoiningUsers.Any(x => user.Username.CaseInsContains(x.Phrase)))
				{
					return;
				}
				else if (Logging.VerifyLogAction(guildSettings, LogAction.UserLeft))
				{
					var embed = EmbedActions.MakeNewEmbed(null, $"**ID:** {user.Id}\n{DiscordObjectFormatting.FormatStayLength(user)}", Colors.LEAV)
						.MyAddAuthor(user)
						.MyAddFooter(user.IsBot ? "Bot Left" : "User Left");
					await MessageActions.SendEmbedMessage(guildSettings.ServerLog, embed);
				}

				//Goodbye message
				if (guildSettings.GoodbyeMessage != null)
				{
					await guildSettings.GoodbyeMessage.Send(user);
				}
			}
		}
		/// <summary>
		/// Logs their name change to every server that has OnUserUpdated enabled.
		/// </summary>
		/// <param name="beforeUser"></param>
		/// <param name="afterUser"></param>
		/// <returns></returns>
		internal async Task OnUserUpdated(SocketUser beforeUser, SocketUser afterUser)
		{
			if (_BotSettings.Pause || beforeUser.Username.CaseInsEquals(afterUser.Username))
			{
				return;
			}

			foreach (var guild in (await _Client.GetGuildsAsync()).Where(x => (x as SocketGuild).Users.Select(y => y.Id).Contains(afterUser.Id)))
			{
				if (Logging.VerifyBotLogging(_BotSettings, _GuildSettings, guild, out var guildSettings) &&
					Logging.VerifyLogAction(guildSettings, LogAction.UserUpdated))
				{
					_Logging.UserChanges.Increment();
					var embed = EmbedActions.MakeNewEmbed(null, null, Colors.UEDT)
						.MyAddAuthor(afterUser)
						.MyAddField("Before:", "`" + beforeUser.Username + "`")
						.MyAddField("After:", "`" + afterUser.Username + "`", false)
						.MyAddFooter("Name Changed");
					await MessageActions.SendEmbedMessage(guildSettings.ServerLog, embed);
				}
			}
		}
	}
}
