﻿using Advobot.Enums;
using Advobot.Structs;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Advobot
{
	namespace Actions
	{
		public static class ChannelActions
		{
			public static ReturnedObject<IGuildChannel> GetChannel(ICommandContext context, ObjectVerification[] checkingTypes, bool mentions, string input)
			{
				IGuildChannel channel = null;
				if (!String.IsNullOrWhiteSpace(input))
				{
					if (ulong.TryParse(input, out ulong channelID))
					{
						channel = GetChannel(context.Guild, channelID);
					}
					else if (MentionUtils.TryParseChannel(input, out channelID))
					{
						channel = GetChannel(context.Guild, channelID);
					}
					else
					{
						var channels = (context.Guild as SocketGuild).Channels.Where(x => x.Name.CaseInsEquals(input));
						if (channels.Count() == 1)
						{
							channel = channels.First();
						}
						else if (channels.Count() > 1)
						{
							return new ReturnedObject<IGuildChannel>(channel, FailureReason.TooMany);
						}
					}
				}

				if (channel == null && mentions)
				{
					var channelMentions = context.Message.MentionedChannelIds;
					if (channelMentions.Count() == 1)
					{
						channel = GetChannel(context.Guild, channelMentions.First());
					}
					else if (channelMentions.Count() > 1)
					{
						return new ReturnedObject<IGuildChannel>(channel, FailureReason.TooMany);
					}
				}

				return GetChannel(context, checkingTypes, channel);
			}
			public static ReturnedObject<IGuildChannel> GetChannel(ICommandContext context, ObjectVerification[] checkingTypes, ulong inputID)
			{
				return GetChannel(context, checkingTypes, GetChannel(context.Guild, inputID));
			}
			public static ReturnedObject<IGuildChannel> GetChannel(ICommandContext context, ObjectVerification[] checkingTypes, IGuildChannel channel)
			{
				return GetChannel(context.Guild, context.User as IGuildUser, checkingTypes, channel);
			}
			public static ReturnedObject<T> GetChannel<T>(IGuild guild, IGuildUser currUser, ObjectVerification[] checkingTypes, T channel) where T : IGuildChannel
			{
				checkingTypes.AssertEnumsAreAllCorrectTargetType(channel);
				if (channel == null)
				{
					return new ReturnedObject<T>(channel, FailureReason.TooFew);
				}

				var bot = UserActions.GetBot(guild);
				foreach (var type in checkingTypes)
				{
					if (!GetIfUserCanDoActionOnChannel(channel, currUser, type))
					{
						return new ReturnedObject<T>(channel, FailureReason.UserInability);
					}
					else if (!GetIfUserCanDoActionOnChannel(channel, bot, type))
					{
						return new ReturnedObject<T>(channel, FailureReason.BotInability);
					}

					switch (type)
					{
						case ObjectVerification.IsDefault:
						{
							if (channel.Id == guild.DefaultChannelId)
							{
								return new ReturnedObject<T>(channel, FailureReason.DefaultChannel);
							}
							break;
						}
						case ObjectVerification.IsText:
						{
							if (!(channel is ITextChannel))
							{
								return new ReturnedObject<T>(channel, FailureReason.ChannelType);
							}
							break;
						}
						case ObjectVerification.IsVoice:
						{
							if (!(channel is IVoiceChannel))
							{
								return new ReturnedObject<T>(channel, FailureReason.ChannelType);
							}
							break;
						}
					}
				}

				return new ReturnedObject<T>(channel, FailureReason.NotFailure);
			}
			public static IGuildChannel GetChannel(IGuild guild, ulong ID)
			{
				return (guild as SocketGuild).GetChannel(ID);
			}
			public static bool GetIfUserCanDoActionOnChannel(IGuildChannel target, IGuildUser user, ObjectVerification type)
			{
				if (target == null || user == null)
					return false;

				var channelPerms = user.GetPermissions(target);
				var guildPerms = user.GuildPermissions;

				var dontCheckReadPerms = target is IVoiceChannel;
				switch (type)
				{
					case ObjectVerification.CanBeRead:
					{
						return (dontCheckReadPerms || channelPerms.ReadMessages);
					}
					case ObjectVerification.CanCreateInstantInvite:
					{
						return (dontCheckReadPerms || channelPerms.ReadMessages) && channelPerms.CreateInstantInvite;
					}
					case ObjectVerification.CanBeManaged:
					{
						return (dontCheckReadPerms || channelPerms.ReadMessages) && channelPerms.ManageChannel;
					}
					case ObjectVerification.CanModifyPermissions:
					{
						return (dontCheckReadPerms || channelPerms.ReadMessages) && channelPerms.ManageChannel && channelPerms.ManagePermissions;
					}
					case ObjectVerification.CanBeReordered:
					{
						return (dontCheckReadPerms || channelPerms.ReadMessages) && guildPerms.ManageChannels;
					}
					case ObjectVerification.CanDeleteMessages:
					{
						return (dontCheckReadPerms || channelPerms.ReadMessages) && channelPerms.ManageMessages;
					}
					case ObjectVerification.CanMoveUsers:
					{
						return dontCheckReadPerms && channelPerms.MoveMembers;
					}
					default:
					{
						return true;
					}
				}
			}

			public static async Task<int> ModifyChannelPosition(IGuildChannel channel, int position)
			{
				if (channel == null)
					return -1;

				IGuildChannel[] channels;
				if (channel is ITextChannel)
				{
					channels = (await channel.Guild.GetTextChannelsAsync()).Where(x => x.Id != channel.Id).OrderBy(x => x.Position).Cast<IGuildChannel>().ToArray();
				}
				else
				{
					channels = (await channel.Guild.GetVoiceChannelsAsync()).Where(x => x.Id != channel.Id).OrderBy(x => x.Position).Cast<IGuildChannel>().ToArray();
				}
				position = Math.Max(0, Math.Min(position, channels.Length));

				var reorderProperties = new ReorderChannelProperties[channels.Length];
				for (int i = 0; i < channels.Length; ++i)
				{
					if (i > position)
					{
						reorderProperties[i] = new ReorderChannelProperties(channels[i - 1].Id, i);
					}
					else if (i < position)
					{
						reorderProperties[i] = new ReorderChannelProperties(channels[i].Id, i);
					}
					else
					{
						reorderProperties[i] = new ReorderChannelProperties(channel.Id, i);
					}
				}

				await channel.Guild.ReorderChannelsAsync(reorderProperties);
				return reorderProperties.FirstOrDefault(x => x.Id == channel.Id)?.Position ?? -1;
			}

			public static async Task ModifyOverwrite(IGuildChannel channel, object obj, ulong allowBits, ulong denyBits)
			{
				if (obj is IRole)
				{
					await channel.AddPermissionOverwriteAsync(obj as IRole, new OverwritePermissions(allowBits, denyBits));
				}
				else if (obj is IUser)
				{
					await channel.AddPermissionOverwriteAsync(obj as IUser, new OverwritePermissions(allowBits, denyBits));
				}
				else
				{
					throw new ArgumentException("Invalid object passed in. Must either be a role or a user.");
				}
			}
			public static ulong AddChannelPermissions(ulong startBits, params ChannelPermission[] permissions)
			{
				foreach (var permission in permissions)
				{
					startBits = startBits & ~(1U << (int)permission);
				}
				return startBits;
			}
			public static ulong RemoveChannelPermissions(ulong startBits, params ChannelPermission[] permissions)
			{
				foreach (var permission in permissions)
				{
					startBits = startBits | (1U << (int)permission);
				}
				return startBits;
			}
			public static OverwritePermissions? GetOverwrite(IGuildChannel channel, object obj)
			{
				if (obj is IRole)
				{
					return channel.GetPermissionOverwrite(obj as IRole);
				}
				else if (obj is IUser)
				{
					return channel.GetPermissionOverwrite(obj as IUser);
				}
				else
				{
					throw new ArgumentException("Invalid object passed in. Must either be a role or a user.");
				}
			}
			public static ulong GetOverwriteAllowBits(IGuildChannel channel, object obj)
			{
				if (obj is IRole)
				{
					return channel.GetPermissionOverwrite(obj as IRole)?.AllowValue ?? 0;
				}
				else if (obj is IUser)
				{
					return channel.GetPermissionOverwrite(obj as IUser)?.AllowValue ?? 0;
				}
				else
				{
					throw new ArgumentException("Invalid object passed in. Must either be a role or a user.");
				}
			}
			public static ulong GetOverwriteDenyBits(IGuildChannel channel, object obj)
			{
				if (obj is IRole)
				{
					return channel.GetPermissionOverwrite(obj as IRole)?.DenyValue ?? 0;
				}
				else if (obj is IUser)
				{
					return channel.GetPermissionOverwrite(obj as IUser)?.DenyValue ?? 0;
				}
				else
				{
					throw new ArgumentException("Invalid object passed in. Must either be a role or a user.");
				}
			}
		}
	}
}