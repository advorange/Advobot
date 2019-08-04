﻿using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Advobot.Attributes;
using Advobot.Attributes.ParameterPreconditions.DiscordObjectValidation.Invites;
using Advobot.Attributes.Preconditions;
using Advobot.Attributes.Preconditions.Permissions;
using Advobot.Commands.Localization;
using Advobot.Commands.Resources;
using Advobot.Modules;
using Advobot.Services.InviteList;
using AdvorangesUtils;
using Discord;
using Discord.Commands;

namespace Advobot.Commands.Settings
{
	public sealed class GuildList : ModuleBase
	{
		[Group(nameof(ModifyGuildListing)), ModuleInitialismAlias(typeof(ModifyGuildListing))]
		[LocalizedSummary(nameof(Summaries.ModifyGuildListing))]
		[GuildPermissionRequirement(GuildPermission.Administrator)]
		[EnabledByDefault(false)]
		public sealed class ModifyGuildListing : AdvobotModuleBase
		{
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
			public IInviteListService Invites { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

			[ImplicitCommand, ImplicitAlias]
			public Task<RuntimeResult> Add(
				[NeverExpires, FromThisGuild] IInviteMetadata invite,
				[Optional] params string[] keywords)
			{
				Invites.Add(Context.Guild, invite, keywords);
				return Responses.GuildList.CreatedListing(invite, keywords);
			}
			[ImplicitCommand, ImplicitAlias]
			public Task<RuntimeResult> Remove()
			{
				Invites.Remove(Context.Guild.Id);
				return Responses.GuildList.DeletedListing();
			}
		}

		[Group(nameof(BumpGuildListing)), ModuleInitialismAlias(typeof(BumpGuildListing))]
		[LocalizedSummary(nameof(Summaries.BumpGuildListing))]
		[GenericGuildPermissionRequirement]
		[EnabledByDefault(false)]
		[NotRecentlyBumped]
		public sealed class BumpGuildListing : AdvobotModuleBase
		{
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
			public IInviteListService Invites { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

			[Command]
			public async Task<RuntimeResult> Command()
			{
				var invite = Invites.Get(Context.Guild.Id);
				await invite.BumpAsync(Context.Guild).CAF();
				return Responses.GuildList.Bumped();
			}
		}

		[Group(nameof(GetGuildListing)), ModuleInitialismAlias(typeof(GetGuildListing))]
		[LocalizedSummary(nameof(Summaries.GetGuildListing))]
		[EnabledByDefault(true)]
		public sealed class GetGuildListing : AdvobotModuleBase
		{
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
			public IInviteListService Invites { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

			[Command]
			public Task<RuntimeResult> Command([Remainder] ListedInviteGatherer args)
			{
				var invites = args.GatherInvites(Invites).ToArray();
				if (!invites.Any())
				{
					return Responses.GuildList.NoInviteMatch();
				}
				if (invites.Length <= 50)
				{
					return Responses.GuildList.InviteMatches(invites);
				}
				return Responses.GuildList.TooManyMatches();
			}
		}
	}
}
