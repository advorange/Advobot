﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Advobot.Core;
using Advobot.Core.Classes;
using Advobot.Core.Classes.Attributes;
using Advobot.Core.Classes.TypeReaders;
using Advobot.Core.Enums;
using Advobot.Core.Interfaces;
using Advobot.Core.Utilities;
using Advobot.Core.Utilities.Formatting;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Advobot.Commands.ChannelModeration
{
	[Group(nameof(CreateChannel)), TopLevelShortAlias(typeof(CreateChannel))]
	[Summary("Adds a channel to the guild of the given type with the given name. " +
		"Text channel names cannot contain any spaces.")]
	[PermissionRequirement(new[] { GuildPermission.ManageChannels }, null)]
	[DefaultEnabled(true)]
	public sealed class CreateChannel : AdvobotModuleBase
	{
		[Command(nameof(Text)), ShortAlias(nameof(Text))]
		public async Task Text([Remainder, VerifyStringLength(Target.Channel)] string name)
		{
			if (name.Contains(' '))
			{
				await MessageUtils.SendErrorMessageAsync(Context, new Error("No spaces are allowed in a text channel name.")).CAF();
				return;
			}

			var channel = await ChannelUtils.CreateTextChannelAsync(Context.Guild, name, new ModerationReason(Context.User, null)).CAF();
			await MessageUtils.MakeAndDeleteSecondaryMessageAsync(Context, $"Successfully created `{channel.Format()}`.").CAF();
		}
		[Command(nameof(Voice)), ShortAlias(nameof(Voice))]
		public async Task Voice([Remainder, VerifyStringLength(Target.Channel)] string name)
		{
			var channel = await ChannelUtils.CreateVoiceChannelAsync(Context.Guild, name, new ModerationReason(Context.User, null)).CAF();
			await MessageUtils.MakeAndDeleteSecondaryMessageAsync(Context, $"Successfully created `{channel.Format()}`.").CAF();
		}
		[Command(nameof(Category)), ShortAlias(nameof(Category))]
		public async Task Category([Remainder, VerifyStringLength(Target.Category)] string name)
		{
			var channel = await ChannelUtils.CreateCategoryAsync(Context.Guild, name, new ModerationReason(Context.User, null)).CAF();
			await MessageUtils.MakeAndDeleteSecondaryMessageAsync(Context, $"Successfully created `{channel.Format()}`.").CAF();
		}
	}

	[Group(nameof(SoftDeleteChannel)), TopLevelShortAlias(typeof(SoftDeleteChannel))]
	[Summary("Makes everyone unable to see the channel and moves it to the bottom of the channel list.")]
	[PermissionRequirement(null, new[] { GuildPermission.ManageChannels, GuildPermission.ManageRoles })]
	[DefaultEnabled(true)]
	public sealed class SoftDeleteChannel : AdvobotModuleBase
	{
		[Command]
		public async Task Command([VerifyObject(false, ObjectVerification.CanBeManaged)] IGuildChannel channel)
		{
			await ChannelUtils.SoftDeleteChannelAsync(channel, new ModerationReason(Context.User, null)).CAF();
			await MessageUtils.MakeAndDeleteSecondaryMessageAsync(Context, $"Successfully softdeleted `{channel.Format()}`.").CAF();
		}
	}

	[Group(nameof(DeleteChannel)), TopLevelShortAlias(typeof(DeleteChannel))]
	[Summary("Deletes the channel.")]
	[PermissionRequirement(new[] { GuildPermission.ManageChannels }, null)]
	[DefaultEnabled(true)]
	public sealed class DeleteChannel : AdvobotModuleBase
	{
		[Command]
		public async Task Command([VerifyObject(false, ObjectVerification.CanBeManaged)] IGuildChannel channel)
		{
			await ChannelUtils.DeleteChannelAsync(channel, new ModerationReason(Context.User, null)).CAF();
			await MessageUtils.MakeAndDeleteSecondaryMessageAsync(Context, $"Successfully deleted `{channel.Format()}`.").CAF();
		}
	}

	[Group(nameof(ModifyChannelPosition)), TopLevelShortAlias(typeof(ModifyChannelPosition))]
	[Summary("If only the channel is input the channel's position will be listed. " +
		"Position zero is the top most position.")]
	[PermissionRequirement(new[] { GuildPermission.ManageChannels }, null)]
	[DefaultEnabled(true)]
	public sealed class ModifyChannelPosition : AdvobotModuleBase
	{
		[Command]
		public async Task Command(SocketGuildChannel channel)
		{
			var resp = $"The channel `{channel.Format()}` has the position `{channel.Position}`.";
			await MessageUtils.SendMessageAsync(Context.Channel, resp).CAF();
		}
		[Command]
		public async Task Command([VerifyObject(false, ObjectVerification.CanBeReordered)] IGuildChannel channel, uint position)
		{
			await ChannelUtils.ModifyPositionAsync(channel, (int)position, new ModerationReason(Context.User, null)).CAF();
			var resp = $"Successfully moved `{channel.Format()}` to position `{position}`.";
			await MessageUtils.MakeAndDeleteSecondaryMessageAsync(Context, resp).CAF();
		}
	}

	[Group(nameof(DisplayChannelPosition)), TopLevelShortAlias(typeof(DisplayChannelPosition))]
	[Summary("Lists the positions of each text or voice channel on the guild.")]
	[PermissionRequirement(new[] { GuildPermission.ManageChannels }, null)]
	[DefaultEnabled(true)]
	public sealed class DisplayChannelPosition : AdvobotModuleBase
	{
		[Command(nameof(Text)), ShortAlias(nameof(Text))]
		public async Task Text(ChannelType channelType)
		{
			await CommandRunner(((SocketGuild)Context.Guild).TextChannels, "Text Channel Positions").CAF();
		}
		[Command(nameof(Voice)), ShortAlias(nameof(Voice))]
		public async Task Voice()
		{
			await CommandRunner(((SocketGuild)Context.Guild).VoiceChannels, "Voice Channel Positions").CAF();
		}
		[Command(nameof(Category)), ShortAlias(nameof(Category))]
		public async Task Category()
		{
			await CommandRunner(((SocketGuild)Context.Guild).CategoryChannels, "Category Channel Positions").CAF();
		}

		private async Task CommandRunner(IEnumerable<SocketGuildChannel> channels, string title)
		{
			var embed = new EmbedWrapper
			{
				Title = title,
				Description = String.Join("\n", channels.OrderBy(x => x.Position).Select(x => $"`{x.Position:00}.` `{x.Name}`"))
			};
			await MessageUtils.SendEmbedMessageAsync(Context.Channel, embed).CAF();
		}
	}

	[Group(nameof(ModifyChannelPerms)), TopLevelShortAlias(typeof(ModifyChannelPerms))]
	[Summary("Permissions must be separated by a `/` or their rawvalue can be said instead " +
		"Type `" + nameof(ModifyChannelPerms) + " [Show]` to see the available permissions. " +
		"Type `" + nameof(ModifyChannelPerms) + " [Show] [Channel]` to see all permissions on a channel. " +
		"Type `" + nameof(ModifyChannelPerms) + " [Show] [Channel] [Role|User]` to see permissions a role/user has on a channel.")]
	[PermissionRequirement(null, new[] { GuildPermission.ManageChannels, GuildPermission.ManageRoles })]
	[DefaultEnabled(true)]
	public sealed class ModifyChannelPerms : AdvobotModuleBase
	{
		[Group(nameof(Show)), ShortAlias(nameof(Show))]
		public sealed class Show : AdvobotModuleBase
		{
			[Command]
			public async Task Command()
			{
				var embed = new EmbedWrapper
				{
					Title = "Channel Permissions",
					Description = $"`{String.Join("`, `", ChannelPermsUtils.Permissions.Select(x => x.Name))}`"
				};
				await MessageUtils.SendEmbedMessageAsync(Context.Channel, embed).CAF();
			}
			[Command]
			public async Task Command([VerifyObject(false, ObjectVerification.CanModifyPermissions)] IGuildChannel channel)
			{
				var roleOverwrites = channel.PermissionOverwrites.Where(x => x.TargetType == PermissionTarget.Role);
				var userOverwrites = channel.PermissionOverwrites.Where(x => x.TargetType == PermissionTarget.User);
				var roleNames = roleOverwrites.Select(x => Context.Guild.GetRole(x.TargetId).Name).ToArray();
				var userNames = userOverwrites.Select(x => (((SocketGuild)Context.Guild).GetUser(x.TargetId)).Username).ToArray();

				var embed = new EmbedWrapper
				{
					Title = channel.Format()
				};
				embed.TryAddField("Role", $"`{(roleNames.Any() ? String.Join("`, `", roleNames) : "None")}`", true, out _);
				embed.TryAddField("User", $"`{(userNames.Any() ? String.Join("`, `", userNames) : "None")}`", false, out _);
				await MessageUtils.SendEmbedMessageAsync(Context.Channel, embed).CAF();
			}
			[Command]
			public async Task Command([VerifyObject(false, ObjectVerification.CanModifyPermissions)] IGuildChannel channel, IRole role)
			{
				if (!channel.PermissionOverwrites.Any())
				{
					var error = new Error($"Unable to show permissions for `{role.Format()}` on `{channel.Format()}`.");
					await MessageUtils.SendErrorMessageAsync(Context, error).CAF();
					return;
				}

				var embed = new EmbedWrapper
				{
					Title = $"Overwrite On {channel.Format()}",
					Description = $"Role:** `{role.Format()}`\n```{channel.FormatOverwritePerms(role)}```"
				};
				await MessageUtils.SendEmbedMessageAsync(Context.Channel, embed).CAF();
			}
			[Command]
			public async Task Command([VerifyObject(false, ObjectVerification.CanModifyPermissions)] IGuildChannel channel, IGuildUser user)
			{
				if (!channel.PermissionOverwrites.Any())
				{
					var error = new Error($"Unable to show permissions for `{user.Format()}` on `{channel.Format()}`.");
					await MessageUtils.SendErrorMessageAsync(Context, error).CAF();
					return;
				}

				var embed = new EmbedWrapper
				{
					Title = $"Overwrite On {channel.Format()}",
					Description = $"User:** `{user.Format()}`\n```{channel.FormatOverwritePerms(user)}```"
				};
				await MessageUtils.SendEmbedMessageAsync(Context.Channel, embed).CAF();
			}
		}
		[Command]
		public async Task Command(PermValue action,
			[VerifyObject(false, ObjectVerification.CanModifyPermissions)] IGuildChannel channel,
			SocketRole role,
			[Remainder, OverrideTypeReader(typeof(ChannelPermissionsTypeReader))] ulong permissions)
		{
			await CommandRunner(action, channel, role, permissions).CAF();
		}
		[Command]
		public async Task Command(PermValue action,
			[VerifyObject(false, ObjectVerification.CanModifyPermissions)] IGuildChannel channel,
			SocketGuildUser user,
			[Remainder, OverrideTypeReader(typeof(ChannelPermissionsTypeReader))] ulong permissions)
		{
			await CommandRunner(action, channel, user, permissions).CAF();
		}

		private async Task CommandRunner<T>(PermValue action, IGuildChannel channel, T discordObject, ulong permissions) where T : ISnowflakeEntity
		{
			var actionStr = "";
			switch (action)
			{
				case PermValue.Allow:
					actionStr = "allowed";
					break;
				case PermValue.Inherit:
					actionStr = "inherited";
					break;
				case PermValue.Deny:
					actionStr = "denied";
					break;
			}

			var givenPerms = OverwriteUtils.ModifyOverwritePermissionsAsync(action, channel, discordObject, permissions, Context.User as IGuildUser);
			var fObj = DiscordObjectFormatting.FormatDiscordObject(discordObject);
			var response = $"Successfully {actionStr} `{String.Join("`, `", givenPerms)}` for `{fObj}` on `{channel.Format()}`.";
			await MessageUtils.MakeAndDeleteSecondaryMessageAsync(Context, response).CAF();
		}
	}

	[Group(nameof(CopyChannelPerms)), TopLevelShortAlias(typeof(CopyChannelPerms))]
	[Summary("Copy permissions from one channel to another. " +
		"Works for a role, a user, or everything. " +
		"If nothing is specified, copies everything.")]
	[PermissionRequirement(null, new[] { GuildPermission.ManageChannels, GuildPermission.ManageRoles })]
	[DefaultEnabled(true)]
	public sealed class CopyChannelPerms : AdvobotModuleBase
	{
		[Command]
		public async Task Command([VerifyObject(false, ObjectVerification.CanModifyPermissions)] IGuildChannel inputChannel,
			[VerifyObject(false, ObjectVerification.CanModifyPermissions)] IGuildChannel outputChannel)
		{
			await CommandRunner(inputChannel, outputChannel, default(IGuildUser)).CAF();
		}
		[Command]
		public async Task Command([VerifyObject(false, ObjectVerification.CanModifyPermissions)] IGuildChannel inputChannel,
			[VerifyObject(false, ObjectVerification.CanModifyPermissions)] IGuildChannel outputChannel, IRole role)
		{
			await CommandRunner(inputChannel, outputChannel, role).CAF();
		}
		[Command]
		public async Task Command([VerifyObject(false, ObjectVerification.CanModifyPermissions)] IGuildChannel inputChannel,
			[VerifyObject(false, ObjectVerification.CanModifyPermissions)] IGuildChannel outputChannel, IGuildUser user)
		{
			await CommandRunner(inputChannel, outputChannel, user).CAF();
		}

		private async Task CommandRunner<T>(IGuildChannel inputChannel, IGuildChannel outputChannel, T discordObject) where T : ISnowflakeEntity
		{
			//Make sure channels are the same type
			if (inputChannel.GetType() != outputChannel.GetType())
			{
				await MessageUtils.SendErrorMessageAsync(Context, new Error("Channels must be the same type.")).CAF();
				return;
			}

			string target;
			ulong allowBits;
			ulong denyBits;
			var reason = new ModerationReason(Context.User, null);
			if (discordObject == null)
			{
				target = "All";
				foreach (var overwrite in inputChannel.PermissionOverwrites)
				{
					switch (overwrite.TargetType)
					{
						case PermissionTarget.Role:
							var role = Context.Guild.GetRole(overwrite.TargetId);
							allowBits = overwrite.Permissions.AllowValue;
							denyBits = overwrite.Permissions.DenyValue;
							await OverwriteUtils.ModifyOverwriteAsync(outputChannel, role, allowBits, denyBits, reason).CAF();
							break;
						case PermissionTarget.User:
							var user = await Context.Guild.GetUserAsync(overwrite.TargetId).CAF();
							allowBits = overwrite.Permissions.AllowValue;
							denyBits = overwrite.Permissions.DenyValue;
							await OverwriteUtils.ModifyOverwriteAsync(outputChannel, user, allowBits, denyBits, reason).CAF();
							break;
					}
				}
			}
			else
			{
				target = DiscordObjectFormatting.FormatDiscordObject(discordObject);
				var overwrite = inputChannel.GetPermissionOverwrite(discordObject);
				if (!overwrite.HasValue)
				{
					var error = new Error($"A permission overwrite for {target} does not exist to copy over.");
					await MessageUtils.SendErrorMessageAsync(Context, error).CAF();
					return;
				}

				allowBits = overwrite.Value.AllowValue;
				denyBits = overwrite.Value.DenyValue;
				await OverwriteUtils.ModifyOverwriteAsync(outputChannel, discordObject, allowBits, denyBits, reason).CAF();
			}

			var resp = $"Successfully copied `{target}` from `{inputChannel.Format()}` to `{outputChannel.Format()}`";
			await MessageUtils.MakeAndDeleteSecondaryMessageAsync(Context, resp).CAF();
		}
	}

	[Group(nameof(ClearChannelPerms)), TopLevelShortAlias(typeof(ClearChannelPerms))]
	[Summary("Removes all permissions set on a channel.")]
	[PermissionRequirement(null, new[] { GuildPermission.ManageChannels, GuildPermission.ManageRoles })]
	[DefaultEnabled(true)]
	public sealed class ClearChannelPerms : AdvobotModuleBase
	{
		[Command]
		public async Task Command([VerifyObject(false, ObjectVerification.CanModifyPermissions)] IGuildChannel channel)
		{
			await OverwriteUtils.ClearOverwritesAsync(channel, new ModerationReason(Context.User, null)).CAF();
			var resp = $"Successfully removed all channel permission overwrites from `{channel.Format()}`.";
			await MessageUtils.MakeAndDeleteSecondaryMessageAsync(Context, resp).CAF();
		}
	}

	[Group(nameof(ModifyChannelNsfw)), TopLevelShortAlias(typeof(ModifyChannelNsfw))]
	[Summary("Toggles the NSFW option on a channel.")]
	[PermissionRequirement(new[] { GuildPermission.ManageChannels }, null)]
	[DefaultEnabled(true)]
	public sealed class ModifyChannelNsfw : AdvobotModuleBase
	{
		[Command]
		public async Task Command([VerifyObject(false, ObjectVerification.CanBeManaged)] ITextChannel channel)
		{
			var isNsfw = channel.IsNsfw;
			await channel.ModifyAsync(x => x.IsNsfw = !isNsfw).CAF();
			var resp = $"Successfully {(isNsfw ? "un" : "")}marked `{channel.Format()}` as NSFW.";
			await MessageUtils.MakeAndDeleteSecondaryMessageAsync(Context, resp).CAF();
		}
	}

	[Group(nameof(ModifyChannelName)), TopLevelShortAlias(typeof(ModifyChannelName))]
	[Summary("Changes the name of the channel.")]
	[PermissionRequirement(new[] { GuildPermission.ManageChannels }, null)]
	[DefaultEnabled(true)]
	public sealed class ModifyChannelName : AdvobotModuleBase
	{
		[Command, Priority(1)]
		public async Task Command([VerifyObject(false, ObjectVerification.CanBeManaged)] IGuildChannel channel,
			[Remainder, VerifyStringLength(Target.Channel)] string name)
		{
			if (channel is ITextChannel && name.Contains(' '))
			{
				await MessageUtils.SendErrorMessageAsync(Context, new Error("Spaces are not allowed in text channel names.")).CAF();
				return;
			}

			await ChannelUtils.ModifyNameAsync(channel, name, new ModerationReason(Context.User, null)).CAF();
			var resp = $"Successfully changed the name of `{channel.Format()}` to `{name}`.";
			await MessageUtils.MakeAndDeleteSecondaryMessageAsync(Context, resp).CAF();
		}
		[Command(nameof(Voice)), ShortAlias(nameof(Voice))]
		public async Task Voice(uint channelPosition, [Remainder, VerifyStringLength(Target.Channel)] string name)
		{
			await ChangeByPosition(Context, ((SocketGuild)Context.Guild).VoiceChannels, channelPosition, name).CAF();
		}

		[Command(nameof(Text)), ShortAlias(nameof(Text))]
		public async Task Text(uint channelPosition, [Remainder, VerifyStringLength(Target.Channel)] string name)
		{
			if (name.Contains(' '))
			{
				await MessageUtils.SendErrorMessageAsync(Context, new Error("Spaces are not allowed in text channel names.")).CAF();
				return;
			}

			await ChangeByPosition(Context, ((SocketGuild)Context.Guild).TextChannels, channelPosition, name).CAF();
		}
		[Command(nameof(Category)), ShortAlias(nameof(Category))]
		public async Task Category(uint channelPosition, [Remainder, VerifyStringLength(Target.Category)] string name)
		{
			await ChangeByPosition(Context, ((SocketGuild)Context.Guild).CategoryChannels, channelPosition, name).CAF();
		}

		private async Task ChangeByPosition(IAdvobotCommandContext context, IEnumerable<SocketGuildChannel> channels, uint channelPos, string name)
		{
			var samePos = channels.Where(x => x.Position == channelPos).ToList();
			if (!samePos.Any())
			{
				await MessageUtils.SendErrorMessageAsync(Context, new Error($"No channel has the position `{channelPos}`.")).CAF();
				return;
			}

			if (samePos.Count() > 1)
			{
				await MessageUtils.SendErrorMessageAsync(Context, new Error($"Multiple channels have the position `{channelPos}`.")).CAF();
				return;
			}

			var channel = samePos.First();
			var result = channel.Verify(context, new[] { ObjectVerification.CanBeManaged });
			if (!result.IsSuccess)
			{
				await MessageUtils.SendErrorMessageAsync(context, new Error(result.ErrorReason)).CAF();
			}

			await ChannelUtils.ModifyNameAsync(channel, name, new ModerationReason(Context.User, null)).CAF();
			var resp = $"Successfully changed the name of `{channel.Format()}` to `{name}`.";
			await MessageUtils.MakeAndDeleteSecondaryMessageAsync(Context, resp).CAF();
		}
	}

	[Group(nameof(ModifyChannelTopic)), TopLevelShortAlias(typeof(ModifyChannelTopic))]
	[Summary("Changes the topic of a channel to whatever is input. " +
		"Clears the topic if nothing is input")]
	[PermissionRequirement(new[] { GuildPermission.ManageChannels }, null)]
	[DefaultEnabled(true)]
	public sealed class ModifyChannelTopic : AdvobotModuleBase
	{
		[Command]
		public async Task Command([VerifyObject(false, ObjectVerification.CanBeManaged)] ITextChannel channel,
			[Optional, Remainder, VerifyStringLength(Target.Topic)] string topic)
		{
			var oldTopic = channel.Topic ?? "Nothing";
			await ChannelUtils.ModifyTopicAsync(channel, topic, new ModerationReason(Context.User, null)).CAF();
			var resp = $"Successfully changed the topic in `{channel.Format()}` from `{oldTopic}` to `{(topic ?? "Nothing")}`.";
			await MessageUtils.MakeAndDeleteSecondaryMessageAsync(Context, resp).CAF();
		}
	}

	[Group(nameof(ModifyChannelLimit)), TopLevelShortAlias(typeof(ModifyChannelLimit))]
	[Summary("Changes the limit to how many users can be in a voice channel. " +
		"The limit ranges from 0 (no limit) to 99.")]
	[PermissionRequirement(new[] { GuildPermission.ManageChannels }, null)]
	[DefaultEnabled(true)]
	public sealed class ModifyChannelLimit : AdvobotModuleBase
	{
		[Command]
		public async Task Command([VerifyObject(false, ObjectVerification.CanBeManaged)] IVoiceChannel channel, uint limit)
		{
			if (limit > Constants.MAX_VOICE_CHANNEL_USER_LIMIT)
			{
				var error = new Error($"The highest a voice channel user limit can be is `{Constants.MAX_VOICE_CHANNEL_USER_LIMIT}`.");
				await MessageUtils.SendErrorMessageAsync(Context, error).CAF();
			}

			await ChannelUtils.ModifyLimitAsync(channel, (int)limit, new ModerationReason(Context.User, null)).CAF();
			var resp = $"Successfully set the user limit for `{channel.Format()}` to `{limit}`.";
			await MessageUtils.MakeAndDeleteSecondaryMessageAsync(Context, resp).CAF();
		}
	}

	[Group(nameof(ModifyChannelBitRate)), TopLevelShortAlias(typeof(ModifyChannelBitRate))]
	[Summary("Changes the bitrate on a voice channel. " +
		"Lowest is 8, highest is 96 (unless on a partnered guild, then it goes up to 128), default is 64.")]
	[PermissionRequirement(new[] { GuildPermission.ManageChannels }, null)]
	[DefaultEnabled(true)]
	public sealed class ModifyChannelBitRate : AdvobotModuleBase
	{
		[Command]
		public async Task Command([VerifyObject(false, ObjectVerification.CanBeManaged)] IVoiceChannel channel, uint bitrate)
		{
			if (bitrate < Constants.MIN_BITRATE)
			{
				var error = new Error($"The bitrate must be above or equal to `{Constants.MIN_BITRATE}`.");
				await MessageUtils.SendErrorMessageAsync(Context, error).CAF();
				return;
			}

			if (!Context.Guild.Features.CaseInsContains(Constants.VIP_REGIONS) && bitrate > Constants.MAX_BITRATE)
			{
				var error = new Error($"The bitrate must be below or equal to `{Constants.MAX_BITRATE}`.");
				await MessageUtils.SendErrorMessageAsync(Context, error).CAF();
				return;
			}

			if (bitrate > Constants.VIP_BITRATE)
			{
				var error = new Error($"The bitrate must be below or equal to `{Constants.VIP_BITRATE}`.");
				await MessageUtils.SendErrorMessageAsync(Context, error).CAF();
				return;
			}

			//Have to multiply by 1000 because in bps and for some reason treats, say, 50 as 50bps and not 50kbps
			await ChannelUtils.ModifyBitrateAsync(channel, (int)bitrate * 1000, new ModerationReason(Context.User, null)).CAF();
			var resp = $"Successfully set the user limit for `{channel.Format()}` to `{bitrate}kbps`.";
			await MessageUtils.MakeAndDeleteSecondaryMessageAsync(Context, resp).CAF();
		}
	}
}
