﻿using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Advobot
{
	//Channel Moderation commands are commands that affect the channels in a guild
	[Name("Channel_Moderation")]
	public class Advobot_Commands_Channel_Mod : ModuleBase
	{
		[Command("channelcreate")]
		[Alias("chc")]
		[Usage("[Name] [Text|Voice]")]
		[Summary("Adds a channel to the guild of the given type with the given name. The name CANNOT contain any spaces: use underscores or dashes instead.")]
		[PermissionRequirement(1U << (int)GuildPermission.ManageChannels)]
		[DefaultEnabled(true)]
		public async Task CreateChannel([Remainder] string input)
		{
			//Split the input
			if (input.IndexOf(' ') < 0)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR(Constants.ARGUMENTS_ERROR));
				return;
			}
			var lastspace = input.LastIndexOf(' ');
			var name = input.Substring(0, lastspace);
			var type = input.Substring(lastspace + 1);

			//Make sure valid type
			if (Actions.CaseInsEquals(type, Constants.TEXT_TYPE) && Actions.CaseInsEquals(type, Constants.VOICE_TYPE))
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("Invalid channel type."));
				return;
			}

			//Test for name validity
			if (Actions.CaseInsEquals(type, Constants.TEXT_TYPE) && name.Contains(' '))
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("No spaces are allowed in a text channel name."));
				return;
			}
			else if (name.Length > Constants.CHANNEL_NAME_MAX_LENGTH)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR(String.Format("Name cannot be more than `{0}` characters.", Constants.CHANNEL_NAME_MAX_LENGTH)));
				return;
			}
			else if (name.Length < Constants.CHANNEL_NAME_MIN_LENGTH)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR(String.Format("Name cannot be less than `{0}` characters.", Constants.CHANNEL_NAME_MIN_LENGTH)));
				return;
			}

			//Get the channel
			var channel = Actions.CaseInsEquals(type, Constants.TEXT_TYPE) ? await Context.Guild.CreateTextChannelAsync(name) as IGuildChannel : await Context.Guild.CreateVoiceChannelAsync(name);
			await Actions.MakeAndDeleteSecondaryMessage(Context, String.Format("Successfully created `{0}`.", Actions.FormatChannel(channel)));
		}

		[Command("channelsoftdelete")]
		[Alias("chsd")]
		[Usage("[#Channel]")]
		[Summary("Makes most roles unable to read the channel and moves it to the bottom of the channel list. Only works for text channels.")]
		[PermissionRequirement(0, (1U << (int)GuildPermission.ManageChannels) | (1U << (int)GuildPermission.ManageRoles))]
		[DefaultEnabled(true)]
		public async Task SoftDeleteChannel([Remainder] string input)
		{
			//See if the user can see and thus edit that channel
			var channel = await Actions.GetChannelEditAbility(Context, input);
			if (channel == null)
				return;

			//See if not attempted on a text channel
			if (Actions.GetChannelType(channel) != Constants.TEXT_TYPE)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("Softdelete only works on text channels inside a guild."));
				return;
			}
			//Check if tried on the base channel
			else if (channel.Id == Context.Guild.DefaultChannelId)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("Unable to softdelete the base channel."));
				return;
			}

			//Make it so only admins/the owner can read the channel
			await channel.PermissionOverwrites.ToList().ForEachAsync(async overwrite =>
			{
				if (overwrite.TargetType == PermissionTarget.Role)
				{
					var role = Context.Guild.GetRole(overwrite.TargetId);
					var allowBits = (uint)channel.GetPermissionOverwrite(role).Value.AllowValue & ~(1U << (int)ChannelPermission.ReadMessages);
					var denyBits = (uint)channel.GetPermissionOverwrite(role).Value.DenyValue | (1U << (int)ChannelPermission.ReadMessages);
					await channel.RemovePermissionOverwriteAsync(role);
					await channel.AddPermissionOverwriteAsync(role, new OverwritePermissions(allowBits, denyBits));
				}
				else
				{
					var user = await Context.Guild.GetUserAsync(overwrite.TargetId);
					var allowBits = (uint)channel.GetPermissionOverwrite(user).Value.AllowValue & ~(1U << (int)ChannelPermission.ReadMessages);
					var denyBits = (uint)channel.GetPermissionOverwrite(user).Value.DenyValue | (1U << (int)ChannelPermission.ReadMessages);
					await channel.RemovePermissionOverwriteAsync(user);
					await channel.AddPermissionOverwriteAsync(user, new OverwritePermissions(allowBits, denyBits));
				}
			});

			//Double check the everyone role has the correct perms
			if (!channel.PermissionOverwrites.Any(x => x.TargetId == Context.Guild.EveryoneRole.Id))
			{
				await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, new OverwritePermissions(readMessages: PermValue.Deny));
			}

			//Determine the highest position (kind of backwards, the lower the closer to the top, the higher the closer to the bottom)
			await Actions.ModifyChannelPosition(channel, (await Context.Guild.GetTextChannelsAsync()).Max(x => x.Position));
			await Actions.SendChannelMessage(channel as ITextChannel, "Successfully softdeleted this channel. Only admins and the owner will be able to read anything on this channel.");
		}

		[Command("channeldelete")]
		[Alias("chd")]
		[Usage(Constants.CHANNEL_INSTRUCTIONS)]
		[Summary("Deletes the channel.")]
		[PermissionRequirement(1U << (int)GuildPermission.ManageChannels)]
		[DefaultEnabled(true)]
		public async Task DeleteChannel([Remainder] string input)
		{
			//See if the user can see and thus edit that channel
			var channel = await Actions.GetChannelEditAbility(Context, input);
			if (channel == null)
				return;

			//Check if tried on the base channel
			if (channel.Id == Context.Guild.DefaultChannelId)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("Unable to delete the base channels."));
				return;
			}

			await channel.DeleteAsync();
			await Actions.MakeAndDeleteSecondaryMessage(Context, String.Format("Successfully deleted `{0}`.", Actions.FormatChannel(channel)));
		}

		[Command("channelposition")]
		[Alias("chpos")]
		[Usage(Constants.CHANNEL_INSTRUCTIONS + " <New Position>")]
		[Summary("Gives the channel the given position. Position one is the top most position and counting starts at zero. This command is extremely buggy!")]
		[PermissionRequirement(1U << (int)GuildPermission.ManageChannels)]
		[DefaultEnabled(true)]
		public async Task ChannelPosition([Remainder] string input)
		{
			var inputArray = Actions.SplitByCharExceptInQuotes(input, ' ');

			//Get the channel
			var channel = await Actions.GetChannelEditAbility(Context, inputArray[0], true);
			if (channel == null)
				return;

			//Argument count checking
			if (inputArray.Length != 2)
			{
				await Actions.SendChannelMessage(Context, String.Format("`{0}` has a position of `{1}`.", Actions.FormatChannel(channel), channel.Position));
				return;
			}

			//Get the position as an int
			if (!int.TryParse(inputArray[1], out int position))
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("Invalid position."));
				return;
			}

			//Check the min against the current position
			if (position < 0)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("Cannot set a channel to a position lower than or equal to zero."));
				return;
			}

			//Modify the channel's position
			await Actions.ModifyChannelPosition(channel, position);
			await Actions.SendChannelMessage(Context, String.Format("Successfully moved `{0}` to position `{1}`.", Actions.FormatChannel(channel), channel.Position));
		}

		[Command("channelpositions")]
		[Alias("chposs")]
		[Usage("[Text|Voice]")]
		[Summary("Lists the positions of each text or voice channel on the guild.")]
		[PermissionRequirement(1U << (int)GuildPermission.ManageChannels)]
		[DefaultEnabled(true)]
		public async Task ListChannelPositions([Remainder] string input)
		{
			//Check if valid type
			if (!(Actions.CaseInsEquals(input, Constants.VOICE_TYPE) || Actions.CaseInsEquals(input, Constants.TEXT_TYPE)))
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("Invalid type."));
				return;
			}

			//Initialize the string
			var title = "";
			var description = "";
			if (input.Equals(Constants.VOICE_TYPE))
			{
				title = "Voice Channels Positions";

				//Put the positions into the string
				(await Context.Guild.GetVoiceChannelsAsync()).OrderBy(x => x.Position).ToList().ForEach(x =>
				{
					description += "`" + x.Position.ToString("00") + ".` " + x.Name + "\n";
				});
			}
			else
			{
				title = "Text Channels Positions";

				//Put the positions into the string
				(await Context.Guild.GetTextChannelsAsync()).OrderBy(x => x.Position).ToList().ForEach(x =>
				{
					description += "`" + x.Position.ToString("00") + ".` " + x.Name + "\n";
				});
			}

			//Send the embed
			await Actions.SendEmbedMessage(Context.Channel, Actions.MakeNewEmbed(title, description));
		}

		[Command("channelperms")]
		[Alias("chp")]
		[Usage("[Show|Allow|Inherit|Deny] " + Constants.OPTIONAL_CHANNEL_INSTRUCTIONS + " <\"Role\"|User> <Permission/...>")]
		[Summary("Type `" + Constants.BOT_PREFIX + "chp [Show]` to see the available permissions. Permissions must be separated by a `/`! " +
			"Type `" + Constants.BOT_PREFIX + "chp [Show] [Channel]` to see all permissions on a channel. " +
			"Type `" + Constants.BOT_PREFIX + "chp [Show] [Channel] [Role|User]` to see permissions a role/user has on a channel.")]
		[PermissionRequirement(0, (1U << (int)GuildPermission.ManageChannels) | (1U << (int)GuildPermission.ManageRoles))]
		[DefaultEnabled(true)]
		public async Task ChannelPermissions([Remainder] string input)
		{
			//Split the input
			var inputArray = Actions.SplitByCharExceptInQuotes(input, ' ');
			if (inputArray.Length > 4)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR(Constants.ARGUMENTS_ERROR));
				return;
			}

			//Get the variables
			var actionStr = inputArray[0];
			var chanStr = inputArray.Length > 1 ? inputArray[1] : null;
			var targStr = inputArray.Length > 2 ? inputArray[2] : null;
			var permStr = inputArray.Length > 3 ? inputArray[3] : null;

			//Get the action
			if (!Enum.TryParse(actionStr, true, out CHPType action))
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR(Constants.ACTION_ERROR));
				return;
			}

			//Set the variables
			var permissions = new List<string>();
			IGuildChannel channel = null;
			IGuildUser user = null;
			IRole role = null;

			switch (action)
			{
				case CHPType.Show:
				{
					//If only show, take that as a person wanting to see the permission types
					if (inputArray.Length == 1)
					{
						//Embed showing the channel permission types
						await Actions.SendEmbedMessage(Context.Channel, Actions.MakeNewEmbed("Channel Permission Types", String.Join("\n", Variables.ChannelPermissions.Select(x => x.Name))));
						return;
					}

					channel = await Actions.GetChannelEditAbility(Context, chanStr);
					if (channel == null)
						return;

					//Say the overwrites on a channel
					if (inputArray.Length == 2)
					{
						var roleOverwrites = new List<string>();
						var userOverwrites = new List<string>();
						await channel.PermissionOverwrites.ToList().ForEachAsync(async x =>
						{
							if (x.TargetType == PermissionTarget.Role)
							{
								roleOverwrites.Add(Context.Guild.GetRole(x.TargetId).Name);
							}
							else
							{
								userOverwrites.Add((await Context.Guild.GetUserAsync(x.TargetId)).Username);
							}
						});

						//Make an embed saying the overwrites
						var embed = Actions.MakeNewEmbed(Actions.FormatChannel(channel));
						Actions.AddField(embed, "Role", roleOverwrites.Any() ? String.Join("\n", roleOverwrites) : "None");
						Actions.AddField(embed, "User", userOverwrites.Any() ? String.Join("\n", userOverwrites) : "None");
						await Actions.SendEmbedMessage(Context.Channel, embed);
						return;
					}

					//Check if valid role or user
					role = await Actions.GetRole(Context, targStr);
					user = await Actions.GetUser(Context.Guild, targStr);
					if (role == null && user == null)
					{
						await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("Invalid role or user supplied."));
						return;
					}

					//Check to see if there are any overwrites
					if (!channel.PermissionOverwrites.Any())
					{
						await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR(String.Format("Unable to show permissions for `{0}` on `{1}`.", targStr, Actions.FormatChannel(channel))));
						return;
					}

					//Say the permissions of the overwrite
					await channel.PermissionOverwrites.ToList().ForEachAsync(async overwrite =>
					{
						if (role != null && overwrite.TargetId.Equals(role.Id))
						{
							//Embed showing the perm overwrites on a role
							var embed = Actions.MakeNewEmbed(title: String.Format("{0} on {1}", role.Name, Actions.FormatChannel(channel)));
							Actions.AddField(embed, "Permission", String.Join("\n", Actions.GetPerms(overwrite, channel).Keys));
							Actions.AddField(embed, "Value", String.Join("\n", Actions.GetPerms(overwrite, channel).Values));
							await Actions.SendEmbedMessage(Context.Channel, embed);
						}
						else if (user != null && overwrite.TargetId.Equals(user.Id))
						{
							//Embed showing the perm overwrites on a user
							var embed = Actions.MakeNewEmbed(title: String.Format("{0}#{1} on {2}", user.Username, user.Discriminator, Actions.FormatChannel(channel)));
							Actions.AddField(embed, "Permission", String.Join("\n", Actions.GetPerms(overwrite, channel).Keys));
							Actions.AddField(embed, "Value", String.Join("\n", Actions.GetPerms(overwrite, channel).Values));
							await Actions.SendEmbedMessage(Context.Channel, embed);
						}
					});
					break;
				}
				case CHPType.Allow:
				case CHPType.Inherit:
				case CHPType.Deny:
				{
					//Check if valid number of arguments
					if (inputArray.Length == 1)
					{
						await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR(Constants.ARGUMENTS_ERROR));
						return;
					}

					//See if the user can see and thus edit that channel
					channel = await Actions.GetChannelEditAbility(Context, chanStr);
					if (channel == null)
						return;

					//See if valid role or user
					role = Actions.GetRole(Context.Guild, targStr);
					user = await Actions.GetUser(Context.Guild, targStr);
					if (role == null && user == null)
					{
						//Give error if no user or role that's valid
						await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("Invalid role or user supplied."));
						return;
					}

					//Get the permissions
					permissions = permStr.Split('/').ToList();
					break;
				}
			}

			//Get the generic permissions
			var genericPerms = Variables.ChannelPermissions.Select(x => x.Name).ToList();
			//Check if valid permissions
			var validPerms = permissions.Intersect(genericPerms, StringComparer.OrdinalIgnoreCase).ToList();
			if (validPerms.Count != permissions.Count)
			{
				var invalidPerms = new List<string>();
				permissions.ForEach(x =>
				{
					if (!validPerms.Contains(x, StringComparer.OrdinalIgnoreCase))
					{
						invalidPerms.Add(x);
					}
				});
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR(String.Format("Invalid {0} supplied: `{1}`.",
					invalidPerms.Count == 1 ? "permission" : "permissions",
					String.Join("`, `", invalidPerms))), 7500);
				return;
			}

			//Remove any attempt to change readmessages on the base channel because nothing can change that
			if (channel.Id == Context.Guild.DefaultChannelId && permissions.Contains("readmessages"))
			{
				permissions.RemoveAll(x => Actions.CaseInsIndexOf(x, "readmessages"));
			}

			//Get the permissions
			uint changeValue = 0;
			uint allowBits = 0;
			uint denyBits = 0;
			if (role != null)
			{
				if (channel.GetPermissionOverwrite(role).HasValue)
				{
					allowBits = (uint)channel.GetPermissionOverwrite(role).Value.AllowValue;
					denyBits = (uint)channel.GetPermissionOverwrite(role).Value.DenyValue;
				}
			}
			else
			{
				if (channel.GetPermissionOverwrite(user).HasValue)
				{
					allowBits = (uint)channel.GetPermissionOverwrite(user).Value.AllowValue;
					denyBits = (uint)channel.GetPermissionOverwrite(user).Value.DenyValue;
				}
			}

			//Changing the bit values
			await permissions.ToList().ForEachAsync(async x => changeValue = await Actions.GetBit(Context, x, changeValue));
			switch (action)
			{
				case CHPType.Allow:
				{
					allowBits |= changeValue;
					denyBits &= ~changeValue;
					actionStr = "allowed";
					break;
				}
				case CHPType.Inherit:
				{
					allowBits &= ~changeValue;
					denyBits &= ~changeValue;
					actionStr = "inherited";
					break;
				}
				case CHPType.Deny:
				{
					allowBits &= ~changeValue;
					denyBits |= changeValue;
					actionStr = "denied";
					break;
				}
			}

			//Change the permissions
			var roleNameOrUsername = "";
			if (role != null)
			{
				await channel.AddPermissionOverwriteAsync(role, new OverwritePermissions(allowBits, denyBits));
				roleNameOrUsername = Actions.FormatRole(role);
			}
			else
			{
				await channel.AddPermissionOverwriteAsync(user, new OverwritePermissions(allowBits, denyBits));
				roleNameOrUsername = Actions.FormatUser(user);
			}

			await Actions.MakeAndDeleteSecondaryMessage(Context, String.Format("Successfully {0} `{1}` for `{2}` on `{3}`",
				actionStr, String.Join("`, `", permissions), roleNameOrUsername, Actions.FormatChannel(channel)), 7500);
		}

		[Command("channelpermscopy")]
		[Alias("chpc")]
		[Usage(Constants.CHANNEL_INSTRUCTIONS + " " + Constants.CHANNEL_INSTRUCTIONS + " [Role|User|All]")]
		[Summary("Copy permissions from one channel to another. Works for a role, a user, or everything.")]
		[PermissionRequirement(0, (1U << (int)GuildPermission.ManageChannels) | (1U << (int)GuildPermission.ManageRoles))]
		[DefaultEnabled(true)]
		public async Task CopyChannelPermissions([Remainder] string input)
		{
			//Get arguments
			var inputArray = Actions.SplitByCharExceptInQuotes(input, ' ');
			if (inputArray.Length != 3)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR(Constants.ARGUMENTS_ERROR));
				return;
			}

			//Separating the channels
			var inputChannel = await Actions.GetChannel(Context, inputArray[0]);
			if (inputChannel == null)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR(Constants.CHANNEL_ERROR));
				return;
			}

			//See if the user can see and thus edit that channel
			var outputChannel = await Actions.GetChannelEditAbility(Context, inputArray[1]);
			if (outputChannel == null)
				return;

			//Make sure channels are the same type
			if (inputChannel.GetType() != outputChannel.GetType())
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("Channels must be the same type."));
				return;
			}

			//Trim the third arg
			var target = inputArray[2].Trim();

			//Copy the selected target
			if (Actions.CaseInsEquals(target, "all"))
			{
				target = "ALL";
				await inputChannel.PermissionOverwrites.ToList().ForEachAsync(async permissionOverwrite =>
				{
					if (permissionOverwrite.TargetType == PermissionTarget.Role)
					{
						var role = Context.Guild.GetRole(permissionOverwrite.TargetId);
						await outputChannel.AddPermissionOverwriteAsync(role, new OverwritePermissions(inputChannel.GetPermissionOverwrite(role).Value.AllowValue,
							inputChannel.GetPermissionOverwrite(role).Value.DenyValue));
					}
					else
					{
						var user = await Context.Guild.GetUserAsync(permissionOverwrite.TargetId);
						await outputChannel.AddPermissionOverwriteAsync(user, new OverwritePermissions(inputChannel.GetPermissionOverwrite(user).Value.AllowValue,
							inputChannel.GetPermissionOverwrite(user).Value.DenyValue));
					}
				});
			}
			else
			{
				var role = await Actions.GetRole(Context, target);
				if (role != null)
				{
					target = role.Name;
					await outputChannel.AddPermissionOverwriteAsync(role, new OverwritePermissions(inputChannel.GetPermissionOverwrite(role).Value.AllowValue,
						inputChannel.GetPermissionOverwrite(role).Value.DenyValue));
				}
				else
				{
					var user = await Actions.GetUser(Context.Guild, target);
					if (user != null)
					{
						target = user.Username;
						await outputChannel.AddPermissionOverwriteAsync(user, new OverwritePermissions(inputChannel.GetPermissionOverwrite(user).Value.AllowValue,
							inputChannel.GetPermissionOverwrite(user).Value.DenyValue));
					}
					else
					{
						await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("No valid role/user or all input."));
						return;
					}
				}
			}

			await Actions.MakeAndDeleteSecondaryMessage(Context, String.Format("Successfully copied `{0}` from `{1}` to `{2}`",
				target, Actions.FormatChannel(inputChannel), Actions.FormatChannel(outputChannel)), 7500);
		}

		[Command("channelpermsclear")]
		[Alias("chpcl")]
		[Usage(Constants.CHANNEL_INSTRUCTIONS)]
		[Summary("Removes all permissions set on a channel.")]
		[PermissionRequirement(0, (1U << (int)GuildPermission.ManageChannels) | (1U << (int)GuildPermission.ManageRoles))]
		[DefaultEnabled(true)]
		public async Task ClearChannelPermissions([Remainder] string input)
		{
			//See if the user can see and thus edit that channel
			var channel = await Actions.GetChannelEditAbility(Context, input);
			if (channel == null)
				return;

			//Check if channel has permissions to clear
			if (channel.PermissionOverwrites.Count == 0)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("Channel has no permissions to clear."));
				return;
			}

			//Remove all the permission overwrites
			await channel.PermissionOverwrites.ToList().ForEachAsync(async x =>
			{
				if (x.TargetType == PermissionTarget.Role)
				{
					await channel.RemovePermissionOverwriteAsync(Context.Guild.GetRole(x.TargetId));
				}
				else
				{
					await channel.RemovePermissionOverwriteAsync(await Context.Guild.GetUserAsync(x.TargetId));
				}
			});
			await Actions.MakeAndDeleteSecondaryMessage(Context, String.Format("Successfully removed all channel permissions from `{0}`.", Actions.FormatChannel(channel)));
		}

		[Command("channelname")]
		[Alias("chn")]
		[Usage("[#Channel|\"Channel Name\"|Position{x}/Text|Voice] [\"New Name\"]")]
		[Summary("Changes the name of the channel. This is *extremely* useful for when multiple channels have the same name but you want to edit things.")]
		[PermissionRequirement(1U << (int)GuildPermission.ManageChannels)]
		[DefaultEnabled(true)]
		public async Task ChangeChannelName([Remainder] string input)
		{
			var inputArray = Actions.SplitByCharExceptInQuotes(input, ' ');
			if (inputArray.Length != 2)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR(Constants.ARGUMENTS_ERROR));
				return;
			}

			//See if it's a position trying to be gotten instead
			var channelInput = inputArray[0];
			var channel = await Actions.GetChannelEditAbility(Context, channelInput);
			if (channel == null)
			{
				if (Actions.CaseInsIndexOf(channelInput, "position{"))
				{
					//Get the position
					var leftBracePos = channelInput.IndexOf('{');
					var rightBracePos = channelInput.IndexOf('}');
					if (!int.TryParse(channelInput.Substring(leftBracePos, rightBracePos), out int position))
					{
						await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("Invalid position supplied."));
						return;
					}

					//Split the input
					var splitInputArray = channelInput.Split(new char[] { '/' }, 2);
					//Give the channeltype
					var channelType = splitInputArray[1];

					//Initialize the channels list
					var textChannels = new List<ITextChannel>();
					var voiceChannels = new List<IVoiceChannel>();

					if (Actions.CaseInsEquals(channelType, Constants.TEXT_TYPE))
					{
						textChannels = (await Context.Guild.GetTextChannelsAsync()).Where(x => x.Position == position).ToList();
					}
					else if (Actions.CaseInsEquals(channelType, Constants.VOICE_TYPE))
					{
						voiceChannels = (await Context.Guild.GetVoiceChannelsAsync()).Where(x => x.Position == position).ToList();
					}
					else
					{
						await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("No valid channel type."));
						return;
					}

					//Check the count now
					if (textChannels.Count == 0 && voiceChannels.Count == 0)
					{
						await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR(String.Format("No {0} channel has a position of `{1}`.", channelType, position)));
						return;
					}
					else if (textChannels.Count == 1 || voiceChannels.Count == 1)
					{
						//Get the channel
						var chan = textChannels.Count == 1 ? textChannels.First() as IGuildChannel : voiceChannels.First() as IGuildChannel;
						channel = Actions.GetChannelEditAbility(chan, Context.User);
					}
					else
					{
						//Get the count
						var count = textChannels.Any() ? textChannels.Count : voiceChannels.Count;
						await Actions.MakeAndDeleteSecondaryMessage(Context, String.Format("`{0}` {1} channels have the position `{2}`.", count, channelType, position));
						return;
					}
				}
				else
				{
					await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR(Constants.CHANNEL_ERROR));
					return;
				}
			}

			//Checking if valid name
			var name = inputArray[1];
			if (Actions.CaseInsIndexOf(channel.GetType().ToString(), Constants.TEXT_TYPE) && name.Contains(' '))
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("No spaces are allowed in a text channel name."));
				return;
			}
			else if (name.Length > Constants.CHANNEL_NAME_MAX_LENGTH)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR(String.Format("Name cannot be more than `{0}` characters.", Constants.CHANNEL_NAME_MAX_LENGTH)));
				return;
			}
			else if (name.Length < Constants.CHANNEL_NAME_MIN_LENGTH)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR(String.Format("Name cannot be less than `{0}` characters.", Constants.CHANNEL_NAME_MIN_LENGTH)));
				return;
			}

			var previousName = channel.Name;
			await channel.ModifyAsync(x => x.Name = inputArray[1]);
			await Actions.MakeAndDeleteSecondaryMessage(Context, String.Format("Successfully changed channel `{0}` to `{1}`.", previousName, inputArray[1]));
		}

		[Command("channeltopic")]
		[Alias("cht")]
		[Usage("[#Channel] [New Topic]")]
		[Summary("Changes the subtext of a channel to whatever is input.")]
		[PermissionRequirement(1U << (int)GuildPermission.ManageChannels)]
		[DefaultEnabled(true)]
		public async Task ChangeChannelTopic([Remainder] string input)
		{
			//Split the input
			var inputArray = input.Split(new char[] { ' ' }, 2);
			if (inputArray.Length != 2)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR(Constants.ARGUMENTS_ERROR));
				return;
			}

			//Test if valid channel
			var channel = await Actions.GetChannelEditAbility(Context, inputArray[0]) as ITextChannel;
			if (channel == null)
				return;
			//See if not a text channel
			else if (Actions.GetChannelType(channel) != Constants.TEXT_TYPE)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("Only text channels can have their topic set."));
				return;
			}

			//See if valid length
			var newTopic = inputArray[1];
			if (newTopic.Length > Constants.TOPIC_MAX_LENGTH)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("Topics cannot be longer than 1024 characters in length."));
				return;
			}

			//See what current topic is
			var currentTopic = channel.Topic;
			if (String.IsNullOrWhiteSpace(currentTopic))
			{
				currentTopic = "NOTHING";
			}

			await channel.ModifyAsync(x => x.Topic = newTopic);
			await Actions.MakeAndDeleteSecondaryMessage(Context, String.Format("Successfully changed the topic in `{0}` from `{1}` to `{2}`.",
				Actions.FormatChannel(channel), currentTopic, String.IsNullOrWhiteSpace(newTopic) ? "NOTHING" : newTopic));
		}

		[Command("channellimit")]
		[Alias("chl")]
		[Usage("[Channel Name] [New Limit]")]
		[Summary("Changes the limit to how many users can be in a voice channel. The limit ranges from 0 (no limit) to 99.")]
		[PermissionRequirement(1U << (int)GuildPermission.ManageChannels)]
		[DefaultEnabled(true)]
		public async Task ChangeChannelLimit([Remainder] string input)
		{
			//Separate the input
			if (input.IndexOf(' ') < 0)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR(Constants.ARGUMENTS_ERROR));
				return;
			}
			var lastSpace = input.LastIndexOf(' ');
			var channelName = input.Substring(0, lastSpace);
			var newLimit = input.Substring(lastSpace + 1);

			//Check if valid channel that the user can edit
			var channel = await Actions.GetChannelEditAbility(Context, channelName);
			if (channel == null)
				return;
			var vc = channel as IVoiceChannel;
			if (vc == null)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("This command will not work on a text channel."));
				return;
			}

			//Check if valid number
			if (!int.TryParse(newLimit, out int limit))
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("The second argument is not a valid number."));
				return;
			}

			//Check if number between 0 and 99
			if (limit > 99 || limit < 0)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("Number must be between 0 and 99 inclusive."));
				return;
			}

			//Change it and send a success message
			await vc.ModifyAsync(x => x.UserLimit = limit);
			await Actions.MakeAndDeleteSecondaryMessage(Context, String.Format("Successfully set the user limit for `{0}` to `{1}`.", Actions.FormatChannel(channel), limit));
		}

		[Command("channelbitrate")]
		[Alias("chbr")]
		[Usage("[Channel Name] [8 to 96]")]
		[Summary("Changes the bit rate (in kbps) on the selected channel to the given value. The default value is 64. The bitrate can go up to 128 on a partnered guild.")]
		[PermissionRequirement(1U << (int)GuildPermission.ManageChannels)]
		[DefaultEnabled(true)]
		public async Task ChangeChannelBitRate([Remainder] string input)
		{
			//Separate the input
			if (input.IndexOf(' ') < 0)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR(Constants.ARGUMENTS_ERROR));
				return;
			}
			var lastSpace = input.LastIndexOf(' ');
			var channelName = input.Substring(0, lastSpace);
			var newBitRate = input.Substring(lastSpace + 1);

			//Check if valid channel that the user can edit
			var channel = await Actions.GetChannelEditAbility(Context, channelName);
			if (channel == null)
				return;
			var vc = channel as IVoiceChannel;
			if (vc == null)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("This command will not work on a text channel."));
				return;
			}

			//Check if valid number
			if (!int.TryParse(newBitRate, out int bitRate))
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("The second argument is not a valid number."));
				return;
			}

			//Check if number between 8 and 96
			if (bitRate < Constants.MIN_BITRATE)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR(String.Format("The bitrate must be above or equal to {0}.", Constants.MIN_BITRATE)));
				return;
			}
			else if (!Context.Guild.Features.Contains(Constants.VIP_REGIONS, StringComparer.OrdinalIgnoreCase) && bitRate > Constants.MAX_BITRATE)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR(String.Format("The bitrate must be below or equal to {0}.", Constants.MAX_BITRATE)));
				return;
			}
			else if (bitRate > Constants.VIP_BITRATE)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR(String.Format("The bitrate must be below or equal to {0}.", Constants.VIP_BITRATE)));
				return;
			}

			//Change it and send a success message
			await vc.ModifyAsync(x => x.Bitrate = bitRate * 1000);
			await Actions.MakeAndDeleteSecondaryMessage(Context, String.Format("Successfully set the user limit for `{0}` to `{1}kbps`.", Actions.FormatChannel(channel), bitRate));
		}
	}
}
