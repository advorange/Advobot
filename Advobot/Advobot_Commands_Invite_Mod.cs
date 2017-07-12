﻿using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Advobot
{
	namespace InviteModeration
	{
		[Group("displayinvites")]
		[Alias("dinvs")]
		[Usage("")]
		[Summary("Gives a list of all the instant invites on the guild.")]
		[OtherRequirement(Precondition.UserHasAPerm)]
		[DefaultEnabled(true)]
		public class DisplayInvites : ModuleBase<MyCommandContext>
		{
			[Command]
			public async Task Command()
			{
				await CommandRunner();
			}

			private async Task CommandRunner()
			{
				var invites = (await Context.Guild.GetInvitesAsync()).OrderByDescending(x => x.Uses);
				if (!invites.Any())
				{
					await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("This guild has no invites."));
					return;
				}

				var lenForCode = invites.Max(x => x.Code.Length);
				var lenForUses = invites.Max(x => x.Uses).ToString().Length;
				var desc = String.Join("\n", invites.FormatNumberedList("`{0}` `{1}` `{2}`", x => x.Code.PadRight(lenForCode), x => x.Uses.ToString().PadRight(lenForUses), x => x.Inviter.FormatUser()));
				await Actions.SendEmbedMessage(Context.Channel, Actions.MakeNewEmbed("Instant Invite List", desc));
			}
		}

		[Group("createinvite")]
		[Alias("cinv")]
		[Usage("[Channel] <1800|3600|21600|43200|86400> <1|5|10|25|50|100> <True|False>")]
		[Summary("Creates an invite on the given channel. No time specifies to not expire. No uses has no usage limit. Temp membership means when the user goes offline they get kicked.")]
		[PermissionRequirement(new[] { GuildPermission.CreateInstantInvite }, null)]
		[DefaultEnabled(true)]
		public class CreateInvite : ModuleBase<MyCommandContext>
		{
			[Command]
			public async Task Command(IGuildChannel channel, [Optional] int time, [Optional] int uses, [Optional] bool tempMem)
			{
				await CommandRunner(channel, time, uses, tempMem);
			}

			private static readonly int[] validTimes = { 1800, 3600, 21600, 43200, 86400 };
			private static readonly int[] validUses = { 1, 5, 10, 25, 50, 100 };

			private async Task CommandRunner(IGuildChannel channel, int? time = 86400, int? uses = null, bool tempMem = false)
			{
				var returnedChannel = Actions.GetChannel(Context, new[] { ObjectVerification.CanCreateInstantInvite }, channel);
				if (returnedChannel.Reason != FailureReason.NotFailure)
				{
					await Actions.HandleObjectGettingErrors(Context, returnedChannel);
					return;
				}
				else if (time.HasValue && !validTimes.Contains(time.Value))
				{
					await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR(String.Format("Invalid time supplied, must be one of the following: `{0]`.", String.Join("`, `", validTimes))));
					return;
				}
				else if (uses.HasValue && !validUses.Contains(uses.Value))
				{
					await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR(String.Format("Invalid uses supplied, must be one of the following: `{0}`", String.Join("`, `", validUses))));
					return;
				}

				var inv = await channel.CreateInviteAsync(time, uses, tempMem);

				var timeOutputStr = time.HasValue ? String.Format("It will last for this amount of time: `{0}`.", time) : "It will last until manually revoked.";
				var usesOutputStr = uses.HasValue ? String.Format("It will last for this amount of uses: `{0}`.", uses) : "It has no usage limit.";
				var tempOutputStr = tempMem ? "Users will be kicked when they go offline unless they get a role." : "Users will not be kicked when they go offline and do not have a role.";
				await Actions.SendChannelMessage(Context, String.Format("Here is your invite for `{0}`: {1}",
					channel.FormatChannel(), 
					Actions.JoinNonNullStrings("\n", inv.Url, timeOutputStr, usesOutputStr, tempOutputStr)));
			}
		}

		[Group("deleteinvite")]
		[Alias("dinv")]
		[Usage("[Invite Code]")]
		[Summary("Deletes the invite with the given code.")]
		[PermissionRequirement(new[] { GuildPermission.ManageChannels }, null)]
		[DefaultEnabled(true)]
		public class DeleteInvite : ModuleBase<MyCommandContext>
		{
			[Command]
			public async Task Command(IInvite invite)
			{
				await CommandRunner(invite);
			}

			private async Task CommandRunner(IInvite invite)
			{
				await invite.DeleteAsync();
				await Actions.MakeAndDeleteSecondaryMessage(Context, String.Format("Successfully deleted the invite `{0}`.", invite.Code));
			}
		}

		[Group("deletemultipleinvites")]
		[Alias("dminv")]
		[Usage("[User|Channel|Number|True|False]")]
		[Summary("Deletes all invites satisfying the given condition of either user, creation channel, use limit, or if it expires or not.")]
		[PermissionRequirement(new[] { GuildPermission.ManageChannels }, null)]
		[DefaultEnabled(true)]
		public class DeleteMultipleInvites : ModuleBase<MyCommandContext>
		{
			[Command(RunMode = RunMode.Async)]
			public async Task Command(IGuildUser user)
			{
				await CommandRunner(user: user);
			}
			[Command(RunMode = RunMode.Async)]
			public async Task Command(IGuildChannel channel)
			{
				await CommandRunner(channel: channel);
			}
			[Command(RunMode = RunMode.Async)]
			public async Task Command(uint uses)
			{
				await CommandRunner(uses: uses);
			}
			[Command(RunMode = RunMode.Async)]
			public async Task Command(bool expiry)
			{
				await CommandRunner(expiry: expiry);
			}

			//TODO: Put more options in this and other stuff
			private async Task CommandRunner(IGuildUser user = null, IGuildChannel channel = null, uint? uses = null, bool? expiry = null)
			{
				var invites = (await Context.Guild.GetInvitesAsync()).AsEnumerable();
				if (!invites.Any())
				{
					await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("This guild has no invites."));
					return;
				}

				if (user != null)
				{
					invites = invites.Where(x => x.Inviter.Id == user.Id);
				}
				else if (channel != null)
				{
					invites = invites.Where(x => x.ChannelId == channel.Id);
				}
				else if (uses != null)
				{
					invites = invites.Where(x => x.MaxUses == uses);
				}
				else if (expiry != null)
				{
					invites = invites.Where(x => expiry.Value ? x.MaxAge != null : x.MaxAge == null);
				}
				else
				{
					return;
				}

				if (!invites.Any())
				{
					await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("No invites satisfied the given conditions."));
					return;
				}
				
				foreach (var invite in invites)
				{
					await invite.DeleteAsync();
				}
				await Actions.MakeAndDeleteSecondaryMessage(Context, String.Format("Successfully deleted `{0}` instant invites.", invites.Count()));
			}
		}
	}
}
