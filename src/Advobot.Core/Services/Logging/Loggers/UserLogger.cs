﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Advobot.Classes;
using Advobot.Services.GuildSettings.Settings;
using Advobot.Services.Logging.Interfaces;
using Advobot.Services.Logging.LoggingContexts;
using Advobot.Utilities;
using AdvorangesUtils;
using Discord;
using Discord.WebSocket;

namespace Advobot.Services.Logging.Loggers
{
	/// <summary>
	/// Handles logging user events.
	/// </summary>
	internal sealed class UserLogger : Logger, IUserLogger
	{
		private static readonly RequestOptions _PersistentRolesOptions = DiscordUtils.GenerateRequestOptions("Persistent roles.");
		private static readonly RequestOptions _BannedNameOptions = DiscordUtils.GenerateRequestOptions("Banned name.");

		/// <summary>
		/// Creates an instance of <see cref="UserLogger"/>.
		/// </summary>
		/// <param name="provider"></param>
		public UserLogger(IServiceProvider provider) : base(provider) { }

		/// <inheritdoc />
		public Task OnUserJoined(SocketGuildUser user)
		{
			NotifyLogCounterIncrement(nameof(ILogService.TotalUsers), 1);
			var context = new UserLoggingContext(GuildSettingsFactory, LogAction.UserJoined, user);
			return HandleAsync(context, nameof(ILogService.UserJoins), new[] { HandleOtherJoinActions(context) }, new Func<Task>[]
			{
				() => HandleJoinLogging(context),
			});
		}
		/// <inheritdoc />
		public Task OnUserLeft(SocketGuildUser user)
		{
			NotifyLogCounterIncrement(nameof(ILogService.TotalUsers), -1);
			var context = new UserLoggingContext(GuildSettingsFactory, LogAction.UserJoined, user);
			return HandleAsync(context, nameof(ILogService.UserLeaves), new[] { HandleOtherLeftActions(context) }, new Func<Task>[]
			{
				() => HandleLeftLogging(context),
			});
		}
		/// <inheritdoc />
		public async Task OnGuildMemberUpdated(SocketGuildUser before, SocketGuildUser after)
		{
			if (before.Username.CaseInsEquals(after.Username))
			{
				return;
			}

			var context = new UserLoggingContext(GuildSettingsFactory, LogAction.UserUpdated, after);
			await HandleAsync(context, nameof(ILogService.UserChanges), Array.Empty<Task>(), new Func<Task>[]
			{
				() => HandleUsernameUpdated(context, before),
			}).CAF();
		}

		/// <summary>
		/// Handles logging joins to the server log.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		private async Task HandleJoinLogging(UserLoggingContext context)
		{
			var inv = await context.Settings.GetInviteCache().GetInviteUserJoinedOnAsync(context.User).CAF();
			var invite = inv != null
				? $"**Invite:** {inv}"
				: "";
			var time = DateTime.UtcNow - context.User.CreatedAt.ToUniversalTime();
			var age = time.TotalHours < 24
				? $"**New Account:** {(int)time.TotalHours} hours, {time.Minutes} minutes old."
				: "";

			await ReplyAsync(context.ServerLog, embedWrapper: new EmbedWrapper
			{
				Description = $"**ID:** {context.User.Id}\n{invite}\n{age}",
				Color = EmbedWrapper.Join,
				Author = context.User.CreateAuthor(),
				Footer = new EmbedFooterBuilder { Text = context.User.IsBot ? "Bot Joined" : "User Joined" },
			}).CAF();
		}
		/// <summary>
		/// Handles banned names, antiraid, persistent roles, and the welcome message.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		private async Task HandleOtherJoinActions(UserLoggingContext context)
		{
			//Banned names
			if (context.Settings.BannedPhraseNames.Any(x => x.Phrase.CaseInsEquals(context.User.Username)))
			{
				var punishmentArgs = new PunishmentArgs()
				{
					Options = _BannedNameOptions,
				};
				await PunishmentUtils.GiveAsync(Punishment.Ban, context.Guild, context.User.Id, 0, punishmentArgs).CAF();
			}
			//Antiraid
			foreach (var antiRaid in context.Settings.RaidPrevention)
			{
				await antiRaid.PunishAsync(context.User).CAF();
			}
			//Persistent roles
			var roles = context.Settings.PersistentRoles
				.Where(x => x.UserId == context.User.Id)
				.Select(x => context.Guild.GetRole(x.RoleId))
				.Where(x => x != null).ToArray();
			if (roles.Length > 0)
			{
				await context.User.AddRolesAsync(roles, _PersistentRolesOptions).CAF();
			}
			//Welcome message
            if (context.Settings.WelcomeMessage != null)
            {
                await context.Settings.WelcomeMessage.SendAsync(context.Guild, context.User).CAF();
            }
        }
		/// <summary>
		/// Handles logging leaves to the server log.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		private Task HandleLeftLogging(UserLoggingContext context)
		{
			var stay = "";
			if (context.User.JoinedAt.HasValue)
			{
				var time = DateTime.UtcNow - context.User.JoinedAt.Value.ToUniversalTime();
				stay = $"**Stayed for:** {time.Days}:{time.Hours:00}:{time.Minutes:00}:{time.Seconds:00}";
			}

			return ReplyAsync(context.ServerLog, embedWrapper: new EmbedWrapper
			{
				Description = $"**ID:** {context.User.Id}\n{stay}",
				Color = EmbedWrapper.Leave,
				Author = context.User.CreateAuthor(),
				Footer = new EmbedFooterBuilder { Text = context.User.IsBot ? "Bot Left" : "User Left", },
			});
		}
		/// <summary>
		/// Handles the goodbye message.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		private Task HandleOtherLeftActions(UserLoggingContext context)
		{
			//Goodbye message
            if (context.Settings.GoodbyeMessage != null)
            {
                return context.Settings.GoodbyeMessage.SendAsync(context.Guild, context.User);
            }
            return Task.CompletedTask;
        }
		/// <summary>
		/// Handles logging username changes.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="before"></param>
		/// <returns></returns>
		private Task HandleUsernameUpdated(UserLoggingContext context, SocketUser before)
		{
			return ReplyAsync(context.ServerLog, embedWrapper: new EmbedWrapper
			{
				Color = EmbedWrapper.UserEdit,
				Author = before.CreateAuthor(),
				Footer = new EmbedFooterBuilder { Text = "Name Changed" },
				Fields = new List<EmbedFieldBuilder>
				{
					new EmbedFieldBuilder { Name = "Before", Value = $"`{before.Username}`", IsInline = true },
					new EmbedFieldBuilder { Name = "After", Value = $"`{context.User.Username}`", IsInline = true },
				},
			});
		}
	}
}
