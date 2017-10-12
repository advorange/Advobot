﻿using Advobot.Actions.Formatting;
using Advobot.Classes;
using Advobot.Classes.Permissions;
using Advobot.Classes.Results;
using Advobot.Enums;
using Advobot.Interfaces;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Advobot.Actions
{
	public static class RoleActions
	{
		public static VerifiedObjectResult VerifyRoleMeetsRequirements(ICommandContext context, IRole target, IEnumerable<ObjectVerification> checks)
		{
			if (target == null)
			{
				return new VerifiedObjectResult(target, CommandError.ObjectNotFound, "Unable to find a matching role.");
			}

			var invokingUser = context.User as IGuildUser;
			var bot = UserActions.GetBot(context.Guild);
			foreach (var check in checks)
			{
				if (!invokingUser.GetIfUserCanDoActionOnRole(target, check))
				{
					return new VerifiedObjectResult(target, CommandError.UnmetPrecondition,
						$"You are unable to make the given changes to the role: `{DiscordObjectFormatting.FormatDiscordObject(target)}`.");
				}
				else if (!bot.GetIfUserCanDoActionOnRole(target, check))
				{
					return new VerifiedObjectResult(target, CommandError.UnmetPrecondition,
						$"I am unable to make the given changes to the role: `{DiscordObjectFormatting.FormatDiscordObject(target)}`.");
				}

				switch (check)
				{
					case ObjectVerification.IsEveryone:
					{
						if (context.Guild.EveryoneRole.Id != target.Id)
						{
							return new VerifiedObjectResult(target, CommandError.UnmetPrecondition, "The everyone role cannot be modified in that way.");
						}
						break;
					}
					case ObjectVerification.IsManaged:
					{
						if (!target.IsManaged)
						{
							return new VerifiedObjectResult(target, CommandError.UnmetPrecondition, "Managed roles cannot be modified in that way.");
						}
						break;
					}
				}
			}

			return new VerifiedObjectResult(target, null, null);
		}

		public static async Task<IEnumerable<string>> ModifyRolePermissionsAsync(IRole role, PermValue permValue, ulong changeValue, IGuildUser user)
		{
			//Only modify permissions the user has the ability to
			changeValue &= user.GuildPermissions.RawValue;

			var roleBits = role.Permissions.RawValue;
			switch (permValue)
			{
				case PermValue.Allow:
				{
					roleBits |= changeValue;
					break;
				}
				case PermValue.Deny:
				{
					roleBits &= ~changeValue;
					break;
				}
				default:
				{
					throw new ArgumentException("Invalid ActionType provided.");
				}
			}

			await ModifyRolePermissionsAsync(role, roleBits, new ModerationReason(user, null));
			return GuildPerms.ConvertValueToNames(changeValue);
		}
		public static async Task<int> ModifyRolePositionAsync(IRole role, int position, ModerationReason reason)
		{
			if (role == null)
			{
				return -1;
			}

			var roles = role.Guild.Roles.Where(x => x.Id != role.Id && x.Position < UserActions.GetBot(role.Guild).GetPosition()).OrderBy(x => x.Position).ToArray();
			position = Math.Max(1, Math.Min(position, roles.Length));

			var reorderProperties = new ReorderRoleProperties[roles.Length + 1];
			for (int i = 0; i < reorderProperties.Length; ++i)
			{
				if (i > position)
				{
					reorderProperties[i] = new ReorderRoleProperties(roles[i - 1].Id, i);
				}
				else if (i < position)
				{
					reorderProperties[i] = new ReorderRoleProperties(roles[i].Id, i);
				}
				else
				{
					reorderProperties[i] = new ReorderRoleProperties(role.Id, i);
				}
			}

			await role.Guild.ReorderRolesAsync(reorderProperties, reason.CreateRequestOptions());
			return reorderProperties.FirstOrDefault(x => x.Id == role.Id)?.Position ?? -1;
		}
		public static async Task ModifyRolePermissionsAsync(IRole role, ulong permissions, ModerationReason reason)
		{
			await role.ModifyAsync(x => x.Permissions = new GuildPermissions(permissions), reason.CreateRequestOptions());
		}
		public static async Task ModifyRoleNameAsync(IRole role, string name, ModerationReason reason)
		{
			await role.ModifyAsync(x => x.Name = name, reason.CreateRequestOptions());
		}
		public static async Task ModifyRoleColorAsync(IRole role, Color color, ModerationReason reason)
		{
			await role.ModifyAsync(x => x.Color = color, reason.CreateRequestOptions());
		}
		public static async Task ModifyRoleHoistAsync(IRole role, ModerationReason reason)
		{
			await role.ModifyAsync(x => x.Hoist = !role.IsHoisted, reason.CreateRequestOptions());
		}
		public static async Task ModifyRoleMentionabilityAsync(IRole role, ModerationReason reason)
		{
			await role.ModifyAsync(x => x.Mentionable = !role.IsMentionable, reason.CreateRequestOptions());
		}

		public static async Task<IRole> GetMuteRoleAsync(ICommandContext context, IGuildSettings guildSettings)
		{
			var muteRole = guildSettings.MuteRole;
			if (!VerifyRoleMeetsRequirements(context, muteRole, new[] { ObjectVerification.CanBeEdited, ObjectVerification.IsManaged }).IsSuccess)
			{
				muteRole = await context.Guild.CreateRoleAsync(Constants.MUTE_ROLE_NAME, new GuildPermissions(0));
				guildSettings.MuteRole = muteRole;
				guildSettings.SaveSettings();
			}

			const uint TEXT_PERMS = 0
				| (1U << (int)ChannelPermission.CreateInstantInvite)
				| (1U << (int)ChannelPermission.ManageChannel)
				| (1U << (int)ChannelPermission.ManagePermissions)
				| (1U << (int)ChannelPermission.ManageWebhooks)
				| (1U << (int)ChannelPermission.SendMessages)
				| (1U << (int)ChannelPermission.ManageMessages)
				| (1U << (int)ChannelPermission.AddReactions);
			foreach (var textChannel in await context.Guild.GetTextChannelsAsync())
			{
				if (textChannel.GetPermissionOverwrite(muteRole) == null)
				{
					await textChannel.AddPermissionOverwriteAsync(muteRole, new OverwritePermissions(0, TEXT_PERMS));
				}
			}

			const uint VOICE_PERMS = 0
				| (1U << (int)ChannelPermission.CreateInstantInvite)
				| (1U << (int)ChannelPermission.ManageChannel)
				| (1U << (int)ChannelPermission.ManagePermissions)
				| (1U << (int)ChannelPermission.ManageWebhooks)
				| (1U << (int)ChannelPermission.Speak)
				| (1U << (int)ChannelPermission.MuteMembers)
				| (1U << (int)ChannelPermission.DeafenMembers)
				| (1U << (int)ChannelPermission.MoveMembers);
			foreach (var voiceChannel in await context.Guild.GetVoiceChannelsAsync())
			{
				if (voiceChannel.GetPermissionOverwrite(muteRole) == null)
				{
					await voiceChannel.AddPermissionOverwriteAsync(muteRole, new OverwritePermissions(0, VOICE_PERMS));
				}
			}

			return muteRole;
		}
		public static async Task<IRole> CreateRoleAsync(IGuild guild, string name, ModerationReason reason)
		{
			return await guild.CreateRoleAsync(name, new GuildPermissions(0), options: reason.CreateRequestOptions());
		}
		public static async Task DeleteRoleAsync(IRole role, ModerationReason reason)
		{
			await role.DeleteAsync(reason.CreateRequestOptions());
		}

		public static async Task GiveRolesAsync(IGuildUser user, IEnumerable<IRole> roles, ModerationReason reason)
		{
			await user.AddRolesAsync(roles, reason.CreateRequestOptions());
		}
		public static async Task TakeRolesAsync(IGuildUser user, IEnumerable<IRole> roles, ModerationReason reason)
		{
			await user.RemoveRolesAsync(roles, reason.CreateRequestOptions());
		}
	}
}