﻿using Advobot.Actions;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Advobot
{
	namespace UserModeration
	{
		[Group("mute"), Alias("m")]
		[Usage("[User] <Number>")]
		[Summary("Prevents a user from typing and speaking in the guild. Time is in minutes, and if no time is given then the mute will not expire.")]
		[PermissionRequirement(new[] { GuildPermission.ManageRoles, GuildPermission.ManageMessages }, null)]
		[DefaultEnabled(true)]
		public sealed class Mute : MyModuleBase
		{
			[Command]
			public async Task Command(IGuildUser user, [Optional] uint time)
			{
				await CommandRunner(user, time);
			}

			private async Task CommandRunner(IGuildUser user, uint time)
			{
				var muteRole = await Roles.GetMuteRole(Context.GuildSettings, user.Guild, user);
				if (user.RoleIds.Contains(muteRole.Id))
				{
					await Punishments.ManualRoleUnmuteUser(user, muteRole);

					var response = String.Format("Successfully unmuted `{0}`.", user.FormatUser());
					await Messages.MakeAndDeleteSecondaryMessage(Context, response);
				}
				else
				{
					await Punishments.RoleMuteUser(user, muteRole, time);

					var response = String.Format("Successfully muted `{0}`.", user.FormatUser());
					if (time != 0)
					{
						response += String.Format("\nThe mute will last for `{0}` minute{1}.", time, Gets.GetPlural(time));
					}
					await Messages.MakeAndDeleteSecondaryMessage(Context, response);
				}
			}
		}

		[Group("voicemute"), Alias("vm")]
		[Usage("[User] <Time")]
		[Summary("Prevents a user from speaking. Time is in minutes, and if no time is given then the mute will not expire.")]
		[PermissionRequirement(new[] { GuildPermission.MuteMembers }, null)]
		[DefaultEnabled(true)]
		public sealed class VoiceMute : MyModuleBase
		{
			[Command]
			public async Task Command(IGuildUser user, [Optional] uint time)
			{
				await CommandRunner(user, time);
			}

			private async Task CommandRunner(IGuildUser user, uint time)
			{
				if (user.IsMuted)
				{
					await Punishments.ManualVoiceUnmuteUser(user);

					var response = String.Format("Successfully unvoicemuted `{0}`.", user.FormatUser());
					await Messages.MakeAndDeleteSecondaryMessage(Context, response);
				}
				else
				{
					await Punishments.VoiceMuteUser(user, time);

					var response = String.Format("Successfully voicemuted `{0}`.", user.FormatUser());
					if (time != 0)
					{
						response += String.Format("\nThe voicemute will last for `{0}` minute{1}.", time, Gets.GetPlural(time));
					}
					await Messages.MakeAndDeleteSecondaryMessage(Context, response);
				}
			}
		}

		[Group("deafen"), Alias("dfn", "d")]
		[Usage("[User] <Time>")]
		[Summary("Prevents a user from hearing. Time is in minutes, and if no time is given then the mute will not expire.")]
		[PermissionRequirement(new[] { GuildPermission.DeafenMembers }, null)]
		[DefaultEnabled(true)]
		public sealed class Deafen : MyModuleBase
		{
			[Command]
			public async Task Command(IGuildUser user, [Optional] uint time)
			{
				await CommandRunner(user, time);
			}

			private async Task CommandRunner(IGuildUser user, uint time)
			{
				if (user.IsDeafened)
				{
					await Punishments.ManualUndeafenUser(user);

					var response = String.Format("Successfully undeafened `{0}`.", user.FormatUser());
					await Messages.MakeAndDeleteSecondaryMessage(Context, response);
				}
				else
				{
					await Punishments.DeafenUser(user, time);

					var response = String.Format("Successfully deafened `{0}`.", user.FormatUser());
					if (time != 0)
					{
						response += String.Format("\nThe deafen will last for `{0}` minute{1}.", time, Gets.GetPlural(time));
					}
					await Messages.MakeAndDeleteSecondaryMessage(Context, response);
				}
			}
		}

		[Group("moveuser"), Alias("mu")]
		[Usage("[User] [Channel]")]
		[Summary("Moves the user to the given voice channel.")]
		[PermissionRequirement(new[] { GuildPermission.MoveMembers }, null)]
		[DefaultEnabled(true)]
		public sealed class MoveUser : MyModuleBase
		{
			[Command]
			public async Task Command(IGuildUser user, [VerifyObject(false, ObjectVerification.CanMoveUsers)] IVoiceChannel channel)
			{
				await CommandRunner(user, channel);
			}

			private async Task CommandRunner(IGuildUser user, IVoiceChannel channel)
			{
				if (user.VoiceChannel == null)
				{
					await Messages.MakeAndDeleteSecondaryMessage(Context, Formatting.ERROR("User is not in a voice channel."));
					return;
				}
				else if (user.VoiceChannel == channel)
				{
					await Messages.MakeAndDeleteSecondaryMessage(Context, Formatting.ERROR("User is already in that channel."));
					return;
				}

				await Users.MoveUser(user, channel);
				await Messages.MakeAndDeleteSecondaryMessage(Context, String.Format("Successfully moved `{0}` to `{1}`.", user.FormatUser(), channel.FormatChannel()));
			}
		}

		//TODO: put in cancel tokens for the commands that user bypass strings in case people need to cancel
		[Group("moveusers"), Alias("mus")]
		[Usage("[Channel] [Channel] <" + Constants.BYPASS_STRING + ">")]
		[Summary("Moves all users from one channel to another. Max is 100 users per use unless the bypass string is said.")]
		[PermissionRequirement(new[] { GuildPermission.MoveMembers }, null)]
		[DefaultEnabled(true)]
		public sealed class MoveUsers : MyModuleBase
		{
			[Command(RunMode = RunMode.Async)]
			public async Task Command([VerifyObject(false, ObjectVerification.CanMoveUsers)] IVoiceChannel inputChannel,
									  [VerifyObject(false, ObjectVerification.CanMoveUsers)] IVoiceChannel outputChannel,
									  [OverrideTypeReader(typeof(BypassUserLimitTypeReader))] bool bypass)
			{
				await CommandRunner(inputChannel, outputChannel, bypass);
			}

			private async Task CommandRunner(IVoiceChannel inputChannel, IVoiceChannel outputChannel, bool bypass)
			{
				var users = (await inputChannel.GetUsersAsync().Flatten()).ToList().GetUpToAndIncludingMinNum(Gets.GetMaxAmountOfUsersToGather(Context.GlobalInfo, bypass));

				await Users.MoveManyUsers(Context, users, outputChannel);
			}
		}

		[Group("pruneusers"), Alias("pu")]
		[Usage("[1|7|30] [True|False]")]
		[Summary("Removes users who have no roles and have not been seen in the given amount of days. True means an actual prune, otherwise this returns the number of users that would have been pruned.")]
		[PermissionRequirement(null, null)]
		[DefaultEnabled(true)]
		public sealed class PruneUsers : MyModuleBase
		{
			[Command]
			public async Task Command(uint days, bool simulate)
			{
				await CommandRunner(days, simulate);
			}

			private static readonly uint[] validDays = { 1, 7, 30 };

			private async Task CommandRunner(uint days, bool simulate)
			{
				if (validDays.Contains(days))
				{
					await Messages.MakeAndDeleteSecondaryMessage(Context, Formatting.ERROR(String.Format("Invalid days supplied, must be one of the following: `{0}`", String.Join("`, `", validDays))));
					return;
				}

				var amt = await Context.Guild.PruneUsersAsync((int)days, simulate);
				await Messages.MakeAndDeleteSecondaryMessage(Context, String.Format("`{0}` members{1} have been pruned with a prune period of `{2}` days.", amt, (simulate ? " would" : ""), days));
			}
		}

		[Group("softban"), Alias("sb")]
		[Usage("[User] <Reason>")]
		[Summary("Bans then unbans a user, which removes all recent messages from them.")]
		[PermissionRequirement(new[] { GuildPermission.BanMembers }, null)]
		[DefaultEnabled(true)]
		public sealed class SoftBan : MyModuleBase
		{
			[Command]
			public async Task Command([VerifyObject(false, ObjectVerification.CanBeEdited)] IGuildUser user, [Optional, Remainder] string reason)
			{
				await CommandRunner(user, reason);
			}

			private async Task CommandRunner(IGuildUser user, string reason)
			{


				var response = String.Format("Successfully softbanned `{0}`.", user.FormatUser());
				if (!String.IsNullOrWhiteSpace(reason))
				{
					response += String.Format("\nThe given reason for softbanning is: `{0}`.", reason);
				}
				await Messages.MakeAndDeleteSecondaryMessage(Context, response);
			}
		}

		[Group("ban"), Alias("b")]
		[Usage("[User] <Time> <Days> <Reason>")]
		[Summary("Bans the user from the guild. Days specifies how many days worth of messages to delete. Time specifies how long and is in minutes.")]
		[PermissionRequirement(new[] { GuildPermission.BanMembers }, null)]
		[DefaultEnabled(true)]
		public sealed class Ban : MyModuleBase
		{
			[Command]
			public async Task Command([VerifyObject(false, ObjectVerification.CanBeEdited)] IUser user, [Optional] uint time, [Optional] uint days, [Optional, Remainder] string reason)
			{
				await CommandRunner(user, time, days, reason);
			}
			[Command]
			public async Task Command([VerifyObject(false, ObjectVerification.CanBeEdited)] IUser user, [Optional] uint time, [Optional, Remainder] string reason)
			{
				await CommandRunner(user, time, 0, reason);
			}
			[Command]
			public async Task Command([VerifyObject(false, ObjectVerification.CanBeEdited)] IUser user, [Optional, Remainder] string reason)
			{
				await CommandRunner(user, 0, 0, reason);
			}

			private static readonly uint[] validDays = { 0, 1, 7 };

			private async Task CommandRunner(IUser user, uint time, uint days, string reason)
			{
				if (validDays.Contains(days))
				{
					await Messages.MakeAndDeleteSecondaryMessage(Context, Formatting.ERROR(String.Format("Invalid days supplied, must be one of the following: `{0}`", String.Join("`, `", validDays))));
					return;
				}

				await Punishments.ManualBan(Context, user.Id, (int)days, time, reason);

				var response = String.Format("Successfully banned `{0}`.", user.FormatUser());
				if (!String.IsNullOrWhiteSpace(reason))
				{
					response += String.Format("\nThe given reason for banning is: `{0}`.", reason);
				}
				await Messages.MakeAndDeleteSecondaryMessage(Context, response);
			}
		}

		[Group("unban"), Alias("ub")]
		[Usage("<User ID|\"Username#Discriminator\"> <True|False>")]
		[Summary("Unbans the user from the guild. If the reason argument is true it only says the reason without unbanning.")]
		[PermissionRequirement(new[] { GuildPermission.BanMembers }, null)]
		[DefaultEnabled(true)]
		public sealed class Unban : MyModuleBase
		{
			[Command]
			public async Task Command(IBan ban, [Optional] bool reason)
			{
				await CommandRunner(ban, reason);
			}

			private async Task CommandRunner(IBan ban, bool reason)
			{
				if (reason)
				{
					await Messages.SendChannelMessage(Context, String.Format("`{0}`'s ban reason is `{1}`.", ban.User.FormatUser(), ban.Reason ?? "Nothing"));
				}
				else
				{
					await Context.Guild.RemoveBanAsync(ban.User);
					await Messages.MakeAndDeleteSecondaryMessage(Context, String.Format("Successfully unbanned `{0}`", ban.User.FormatUser()));
				}
			}
		}

		[Group("kick"), Alias("k")]
		[Usage("[User] <Reason>")]
		[Summary("Kicks the user from the guild.")]
		[PermissionRequirement(new[] { GuildPermission.KickMembers }, null)]
		[DefaultEnabled(true)]
		public sealed class Kick : MyModuleBase
		{
			[Command]
			public async Task Command([VerifyObject(false, ObjectVerification.CanBeEdited)] IGuildUser user, [Optional, Remainder] string reason)
			{
				await CommandRunner(user, reason);
			}

			private async Task CommandRunner(IGuildUser user, string reason)
			{
				await Punishments.ManualKick(Context, user, reason);

				var response = String.Format("Successfully kicked `{0}`.", user.FormatUser());
				if (!String.IsNullOrWhiteSpace(reason))
				{
					response += String.Format("\nThe given reason for kicking is: `{0}`.", reason);
				}
				await Messages.MakeAndDeleteSecondaryMessage(Context, response);
			}
		}

		[Group("displaycurrentbanlist"), Alias("dcbl")]
		[Usage("")]
		[Summary("Displays all the bans on the guild.")]
		[PermissionRequirement(new[] { GuildPermission.BanMembers }, null)]
		[DefaultEnabled(true)]
		public sealed class DisplayCurrentBanList : MyModuleBase
		{
			[Command]
			public async Task Command()
			{
				await CommandRunner();
			}

			private async Task CommandRunner()
			{
				var bans = await Context.Guild.GetBansAsync();
				if (!bans.Any())
				{
					await Messages.SendChannelMessage(Context, "This guild has no bans.");
					return;
				}

				var desc = bans.FormatNumberedList("`{0}`", x => x.User.FormatUser());
				await Messages.SendEmbedMessage(Context.Channel, Embeds.MakeNewEmbed("Current Bans", desc));
			}
		}

		[Group("removemessages"), Alias("rm")]
		[Usage("[Number] <User> <Channel>")]
		[Summary("Removes the provided number of messages from either the user, the channel, both, or, if neither is input, the current channel.")]
		[PermissionRequirement(new[] { GuildPermission.ManageMessages }, null)]
		[DefaultEnabled(true)]
		public sealed class RemoveMessages : MyModuleBase
		{
			[Command]
			public async Task Command(uint requestCount, [Optional] IGuildUser user, [Optional, VerifyObject(true, ObjectVerification.CanDeleteMessages)] ITextChannel channel)
			{
				await CommandRunner((int)requestCount, user, channel);
			}
			[Command]
			public async Task Command(uint requestCount, [Optional, VerifyObject(true, ObjectVerification.CanDeleteMessages)] ITextChannel channel, [Optional] IGuildUser user)
			{
				await CommandRunner((int)requestCount, user, channel);
			}

			private async Task CommandRunner(int requestCount, IGuildUser user, ITextChannel channel)
			{
				var serverLog = Context.GuildSettings.ServerLog?.Id == channel.Id;
				var modLog = Context.GuildSettings.ModLog?.Id == channel.Id;
				var imageLog = Context.GuildSettings.ImageLog?.Id == channel.Id;
				if (Context.User.Id != Context.Guild.OwnerId && (serverLog || modLog || imageLog))
				{
					var DMChannel = await (await Context.Guild.GetOwnerAsync()).GetOrCreateDMChannelAsync();
					await Messages.SendDMMessage(DMChannel, String.Format("`{0}` is trying to delete stuff from a log channel: `{1}`.", Context.User.FormatUser(), channel.FormatChannel()));
					return;
				}

				var response = String.Format("Successfully deleted `{0}` message{1}", await Messages.RemoveMessages(channel, Context.Message, requestCount, user), Gets.GetPlural(requestCount));
				var userResp = user != null ? String.Format(" from `{0}`", user.FormatUser()) : null;
				var chanResp = channel != null ? String.Format(" on `{0}`", channel.FormatChannel()) : null;
				await Messages.MakeAndDeleteSecondaryMessage(Context, Formatting.JoinNonNullStrings(" ", response, userResp, chanResp) + ".");
			}
		}
	}
	/*
	//User Moderation commands are commands that affect the users of a guild
	[Name("UserModeration")]
	public class Advobot_Commands_User_Mod : ModuleBase
	{
		[Command("modifyslowmode")]
		[Alias("msm")]
		[Usage("<\"Roles:.../.../\"> <Messages:1 to 5> <Time:1 to 30> <Guild:Yes> | [Off] [Guild|Channel|All]")]
		[Summary("The first argument is the roles that get ignored by slowmode, the second is the amount of messages, and the third is the time period. Default is: none, 1, 5." +
			"Bots are unaffected by slowmode. Any users who are immune due to roles stay immune even if they lose said role until a new slowmode is started.")]
		[PermissionRequirement(null, null)]
		[DefaultEnabled(true)]
		public async Task SlowMode([Optional, Remainder] string input)
		{
			var guildInfo = await Actions.CreateOrGetGuildInfo(Context.Guild);

			var returnedArgs = Actions.GetArgs(Context, input, new ArgNumbers(0, 4), new[] { "roles", "messages", "time", "guild" });
			if (returnedArgs.Reason != FailureReason.NotFailure)
			{
				await Actions.HandleArgsGettingErrors(Context, returnedArgs);
				return;
			}
			var roleStr = returnedArgs.GetSpecifiedArg("roles");
			var msgStr = returnedArgs.GetSpecifiedArg("messages");
			var timeStr = returnedArgs.GetSpecifiedArg("time");
			var guildStr = returnedArgs.GetSpecifiedArg("guild");

			if (Actions.CaseInsEquals(returnedArgs.Arguments[0], "off"))
			{
				var targStr = returnedArgs.Arguments[1];
				if (returnedArgs.ArgCount != 2)
				{
					await Messages.MakeAndDeleteSecondaryMessage(Context, Formatting.ERROR(Constants.ARGUMENTS_ERROR));
				}
				else if (Actions.CaseInsEquals(targStr, "guild"))
				{
					guildInfo.SetSetting(SettingOnGuild.SlowmodeGuild, null, false);
					await Messages.MakeAndDeleteSecondaryMessage(Context, "Successfully removed the slowmode on the guild.");
				}
				else if (Actions.CaseInsEquals(targStr, "channel"))
				{
					((List<SlowmodeChannel>)guildInfo.GetSetting(SettingOnGuild.SlowmodeChannels)).ThreadSafeRemoveAll(x => x.ChannelID == Context.Channel.Id);
					await Messages.MakeAndDeleteSecondaryMessage(Context, "Successfully removed the slowmode on the channel.");
				}
				else if (Actions.CaseInsEquals(targStr, "all"))
				{
					guildInfo.SetSetting(SettingOnGuild.SlowmodeGuild, null, false);
					((List<SlowmodeChannel>)guildInfo.GetSetting(SettingOnGuild.SlowmodeChannels)).Clear();
					await Messages.MakeAndDeleteSecondaryMessage(Context, "Successfully removed all slowmodes on the guild and its channels.");
				}
				else
				{
					await Messages.MakeAndDeleteSecondaryMessage(Context, Formatting.ERROR("With off, the second argument must be either Guild, Channel, or All."));
				}
				return;
			}

			//Check if the target is already in either dictionary
			var guild = !String.IsNullOrWhiteSpace(guildStr);
			if (guild)
			{
				var smGuild = ((SlowmodeGuild)guildInfo.GetSetting(SettingOnGuild.SlowmodeGuild));
				if (smGuild != null)
				{
					await Messages.MakeAndDeleteSecondaryMessage(Context, "Guild already is in slowmode.");
					return;
				}
			}
			else
			{
				var smChannel = ((List<SlowmodeChannel>)guildInfo.GetSetting(SettingOnGuild.SlowmodeChannels)).FirstOrDefault(x => x.ChannelID == Context.Channel.Id);
				if (smChannel != null)
				{
					await Messages.MakeAndDeleteSecondaryMessage(Context, "Channel already is in slowmode.");
					return;
				}
			}

			//Get the roles
			var roles = new List<IRole>();
			if (!String.IsNullOrWhiteSpace(roleStr))
			{
				roleStr.Split('/').ToList().ForEach(x =>
				{
					var returnedRole = Actions.GetRole(Context, new[] { ObjectVerification.None }, false, x);
					if (returnedRole.Reason == FailureReason.NotFailure)
					{
						roles.Add(returnedRole.Object);
					}
				});
			}
			roles = roles.Distinct().ToList();
			var roleNames = roles.Select(x => x.Name);
			var roleIDs = roles.Select(x => x.Id);

			//Get the messages limit
			var msgsLimit = 1;
			if (!String.IsNullOrWhiteSpace(msgStr))
			{
				if (int.TryParse(msgStr, out msgsLimit))
				{
					if (msgsLimit > 5 || msgsLimit < 1)
					{
						await Messages.MakeAndDeleteSecondaryMessage(Context, Formatting.ERROR("Message limit must be between 1 and 5 inclusive."));
						return;
					}
				}
				else
				{
					await Messages.MakeAndDeleteSecondaryMessage(Context, Formatting.ERROR("The input for messages was not a number. Remember: no space after the colon."));
					return;
				}
			}

			//Get the time limit
			var timeLimit = 5;
			if (!String.IsNullOrWhiteSpace(timeStr))
			{
				if (int.TryParse(timeStr, out timeLimit))
				{
					if (timeLimit > 30 || timeLimit < 1)
					{
						await Messages.MakeAndDeleteSecondaryMessage(Context, Formatting.ERROR("Time must be between 1 and 10 inclusive."));
						return;
					}
				}
				else
				{
					await Messages.MakeAndDeleteSecondaryMessage(Context, Formatting.ERROR("The input for time was not a number. Remember: no space after the colon."));
					return;
				}
			}

			var slowmodeUsers = (await Context.Guild.GetUsersAsync()).Where(x => !x.RoleIds.Intersect(roleIDs).Any()).Select(x => new SlowmodeUser(x, msgsLimit, timeLimit)).ToList();
			if (guild)
			{
				guildInfo.SetSetting(SettingOnGuild.SlowmodeGuild, new SlowmodeGuild(msgsLimit, timeLimit, slowmodeUsers), false);
			}
			else
			{
				guildInfo.SetSetting(SettingOnGuild.SlowmodeChannels, new SlowmodeGuild(msgsLimit, timeLimit, slowmodeUsers), false);
			}

			//Send a success message
			await Messages.MakeAndDeleteSecondaryMessage(Context, String.Format("Successfully enabled slowmode on `{0}` with a message limit of `{1}` and time interval of `{2}` seconds.{3}",
				guild ? Context.Guild.FormatGuild() : Context.Channel.FormatChannel(),
				msgsLimit,
				timeLimit,
				roleNames.Any() ? String.Format("\nImmune roles: `{0}`.", String.Join("`, `", roleNames)) : ""));
		}

		//TODO: Split this up into separate commands
		/*
		[Command("forallwithrole")]
		[Alias("fawr")]
		[Usage("[Give_Role|GR|Take_Role|TR|Give_Nickname|GNN|Take_Nickname|TNN] [\"Role\"] <\"Role\"|\"Nickname\"> <" + Constants.BYPASS_STRING + ">")]
		[Summary("Max is 100 users per use unless the bypass string is said. All actions but `Take_Nickame` require the output role/nickname.")]
		[PermissionRequirement]
		[DefaultEnabled(true)]
		public async Task ForAllWithRole([Remainder] string input)
		{
			//Split arguments
			var returnedArgs = Actions.GetArgs(Context, input, new ArgNumbers(2, 4));
			if (returnedArgs.Reason != FailureReason.NotFailure)
			{
				await Actions.HandleArgsGettingErrors(Context, returnedArgs);
				return;
			}
			var actionStr = returnedArgs.Arguments[0];
			var inputStr = returnedArgs.Arguments[1];
			var outputStr = returnedArgs.Arguments[2];

			if (!Enum.TryParse(actionStr, true, out FAWRType action))
			{
				await Messages.MakeAndDeleteSecondaryMessage(Context, Formatting.ERROR(Constants.ACTION_ERROR));
				return;
			}
			action = Actions.ClarifyFAWRType(action);

			if (action != FAWRType.Take_Nickname)
			{
				if (returnedArgs.ArgCount < 3)
				{
					await Messages.MakeAndDeleteSecondaryMessage(Context, Formatting.ERROR(Constants.ARGUMENTS_ERROR));
					return;
				}
			}

			//Input role
			var returnedInputRole = Actions.GetRole(Context, new[] { RoleCheck.None }, false, inputStr);
			if (returnedInputRole.Reason != FailureReason.NotFailure)
			{
				await Actions.HandleObjectGettingErrors(Context, returnedInputRole);
				return;
			}
			var inputRole = returnedInputRole.Object;

			switch (action)
			{
				case FAWRType.Give_Role:
				{
					if (Actions.CaseInsEquals(inputStr, outputStr))
					{
						await Messages.MakeAndDeleteSecondaryMessage(Context, Formatting.ERROR("Cannot give the same role that is being gathered."));
						return;
					}
					break;
				}
				case FAWRType.Give_Nickname:
				{
					if (outputStr.Length > Constants.MAX_NICKNAME_LENGTH)
					{
						await Messages.MakeAndDeleteSecondaryMessage(Context, Formatting.ERROR(String.Format("Nicknames cannot be longer than `{0}` charaters.", Constants.MAX_NICKNAME_LENGTH)));
						return;
					}
					else if (outputStr.Length < Constants.MIN_NICKNAME_LENGTH)
					{
						await Messages.MakeAndDeleteSecondaryMessage(Context, Formatting.ERROR(String.Format("Nicknames cannot be less than `{0}` characters.", Constants.MIN_NICKNAME_LENGTH)));
						return;
					}
					break;
				}
			}

			//Get the amount of users allowed
			var len = Actions.GetMaxNumOfUsersToGather(Context, returnedArgs.Arguments);
			var users = (await Actions.GetUsersTheBotAndUserCanEdit(Context, (x => x.RoleIds.Contains(inputRole.Id)))).GetUpToAndIncludingMinNum(len);
			var userCount = users.Count;
			if (userCount == 0)
			{
				await Messages.MakeAndDeleteSecondaryMessage(Context, Formatting.ERROR("Unable to find any users with the input role that could be modified."));
				return;
			}

			//Nickname stuff
			switch (action)
			{
				case FAWRType.Give_Nickname:
				{
					Actions.RenicknameALotOfPeople(Context, users, outputStr).Forget();
					return;
				}
				case FAWRType.Take_Nickname:
				{
					Actions.RenicknameALotOfPeople(Context, users, null).Forget();
					return;
				}
			}

			//Output role
			var returnedOutputRole = Actions.GetRole(Context, new[] { RoleCheck.CanBeEdited, RoleCheck.IsEveryone }, false, outputStr);
			if (returnedOutputRole.Reason != FailureReason.NotFailure)
			{
				await Actions.HandleObjectGettingErrors(Context, returnedOutputRole);
				return;
			}
			var outputRole = returnedOutputRole.Object;

			//Make sure the users trying to give role to don't have it and trying to take from do have it.
			switch (action)
			{
				case FAWRType.Give_Role:
				{
					users = users.Where(x => !x.RoleIds.Contains(outputRole.Id)).ToList();
					break;
				}
				case FAWRType.Take_Role:
				{
					users = users.Where(x => x.RoleIds.Contains(outputRole.Id)).ToList();
					break;
				}
			}

			var msg = await Messages.SendChannelMessage(Context, String.Format("Attempted to edit `{0}` user{1}.", userCount, Actions.GetPlural(userCount))) as IUserMessage;
			var typing = Context.Channel.EnterTypingState();
			var count = 0;

			Task.Run(async () =>
			{
				switch (action)
				{
					case FAWRType.Give_Role:
					{
						foreach (var user in users)
						{
							++count;
							if (count % 10 == 0)
							{
								await msg.ModifyAsync(x => x.Content = String.Format("ETA on completion: `{0}` seconds.", (int)((userCount - count) * 1.2)));
								if (Context.Guild.GetRole(outputRole.Id) == null)
								{
									await Messages.MakeAndDeleteSecondaryMessage(Context, Formatting.ERROR("The output role has been deleted."));
									return;
								}
							}

							await Actions.GiveRole(user, outputRole);
						}

						await Messages.SendChannelMessage(Context, String.Format("Successfully gave the role `{0}` to `{1}` users.", outputRole.FormatRole(), count));
						break;
					}
					case FAWRType.Take_Role:
					{
						foreach (var user in users)
						{
							++count;
							if (count % 10 == 0)
							{
								await msg.ModifyAsync(x => x.Content = String.Format("ETA on completion: `{0}` seconds.", (int)((userCount - count) * 1.2)));
								if (Context.Guild.GetRole(outputRole.Id) == null)
								{
									await Messages.MakeAndDeleteSecondaryMessage(Context, Formatting.ERROR("The output role has been deleted."));
									return;
								}
							}

							await Actions.TakeRole(user, outputRole);
						}

						await Messages.SendChannelMessage(Context, String.Format("Successfully took the role `{0}` from `{1}` users.", outputRole.FormatRole(), count));
						break;
					}
				}
				typing.Dispose();
				await msg.DeleteAsync();
			}).Forget();
		}
	}*/
}
