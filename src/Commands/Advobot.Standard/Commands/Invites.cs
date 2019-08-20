﻿using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Advobot.Attributes;
using Advobot.Attributes.ParameterPreconditions.DiscordObjectValidation.Channels;
using Advobot.Attributes.ParameterPreconditions.DiscordObjectValidation.Invites;
using Advobot.Attributes.Preconditions.Permissions;
using Advobot.Classes;
using Advobot.Modules;
using Advobot.Standard.Localization;
using Advobot.Standard.Resources;
using AdvorangesUtils;
using Discord;
using Discord.Commands;
using static Discord.ChannelPermission;

namespace Advobot.Standard.Commands
{
	public sealed class Invites : ModuleBase
	{
		[Group(nameof(DisplayInvites)), ModuleInitialismAlias(typeof(DisplayInvites))]
		[LocalizedSummary(nameof(Summaries.DisplayInvites))]
		[CommandMeta("958c8da4-352e-468e-8279-0fd80276cd24", IsEnabled = true)]
		[RequireGuildPermissions(GuildPermission.ManageGuild)]
		public sealed class DisplayInvites : AdvobotModuleBase
		{
			[Command]
			public async Task<RuntimeResult> Command()
			{
				var invites = await Context.Guild.GetInvitesAsync().CAF();
				var ordered = invites.OrderByDescending(x => x.Uses).ToArray();
				return Responses.Invites.DisplayInvites(ordered);
			}
		}

		[Group(nameof(CreateInvite)), ModuleInitialismAlias(typeof(CreateInvite))]
		[LocalizedSummary(nameof(Summaries.CreateInvite))]
		[CommandMeta("6e8233c0-c8f4-456a-85e4-6f5203add299", IsEnabled = true)]
		[RequireGuildPermissions(GuildPermission.CreateInstantInvite)]
		public sealed class CreateInvite : AdvobotModuleBase
		{
			[Command]
			public Task<RuntimeResult> Command(
				[Channel(CreateInstantInvite)] ITextChannel channel,
				[Optional] CreateInviteArguments? arguments)
				=> CommandRunner(channel, arguments);
			[Command]
			public Task<RuntimeResult> Command(
				[Channel(CreateInstantInvite)] IVoiceChannel channel,
				[Optional] CreateInviteArguments? arguments)
				=> CommandRunner(channel, arguments);

			private async Task<RuntimeResult> CommandRunner(
				INestedChannel channel,
				CreateInviteArguments? arguments)
			{
				arguments ??= new CreateInviteArguments();
				var invite = await arguments.CreateInviteAsync(channel, GenerateRequestOptions()).CAF();
				return Responses.Snowflakes.Created(invite);
			}
		}

		[Group(nameof(DeleteInvite)), ModuleInitialismAlias(typeof(DeleteInvite))]
		[LocalizedSummary(nameof(Summaries.DeleteInvite))]
		[CommandMeta("993e5613-6cdb-4ff3-925d-98e3a534ddc8", IsEnabled = true)]
		[RequireGuildPermissions(GuildPermission.ManageChannels)]
		public sealed class DeleteInvite : AdvobotModuleBase
		{
			[Command]
			public async Task<RuntimeResult> Command([FromThisGuild] IInviteMetadata invite)
			{
				await invite.DeleteAsync(GenerateRequestOptions()).CAF();
				return Responses.Snowflakes.Deleted(invite);
			}
		}

		[Group(nameof(DeleteMultipleInvites)), ModuleInitialismAlias(typeof(DeleteMultipleInvites))]
		[LocalizedSummary(nameof(Summaries.DeleteMultipleInvites))]
		[CommandMeta("a53c0e51-d580-436e-869c-e566ff268c3e", IsEnabled = true)]
		[RequireGuildPermissions(GuildPermission.ManageChannels)]
		public sealed class DeleteMultipleInvites : AdvobotModuleBase
		{
			[Command(RunMode = RunMode.Async)]
			public async Task<RuntimeResult> Command([Remainder] LocalInviteGatherer gatherer)
			{
				var invites = gatherer.GatherInvites(await Context.Guild.GetInvitesAsync().CAF()).ToArray();
				if (!invites.Any())
				{
					return Responses.Invites.NoInviteMatches();
				}

				foreach (var invite in invites)
				{
					await invite.DeleteAsync(GenerateRequestOptions()).CAF();
				}
				return Responses.Invites.DeletedMultipleInvites(invites);
			}
		}
	}
}
