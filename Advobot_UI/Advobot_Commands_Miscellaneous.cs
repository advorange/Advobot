﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Advobot
{
	//Miscellaneous commands are random commands that don't exactly fit the other groups
	[Name("Miscellaneous")]
	public class Advobot_Commands_Miscellaneous : ModuleBase
	{
		[Command("help")]
		[Alias("h", "info")]
		[Usage("<Command>")]
		[Summary("Prints out the aliases of the command, the usage of the command, and the description of the command. If left blank will print out a link to the documentation of this bot.")]
		[DefaultEnabled(true)]
		public async Task Help([Optional, Remainder] string input)
		{
			var prefix = Actions.GetPrefix(Context.Guild);
			if (String.IsNullOrWhiteSpace(input))
			{
			    var emb = Actions.MakeNewEmbed("General Help", String.Format("Type `{0}commands` for the list of commands.\nType `{0}help [Command]` for help with a command.", prefix));
				Actions.AddField(emb, "Basic Syntax", "`[]` means required.\n`<>` means optional.\n`|` means or.");
				Actions.AddField(emb, "Mention Syntax", String.Format("`User` means `{0}`.\n`Role` means `{1}`.\n`Channel` means `{2}`.",
					Constants.USER_INSTRUCTIONS,
					Constants.ROLE_INSTRUCTIONS,
					Constants.CHANNEL_INSTRUCTIONS));
				Actions.AddField(emb, "Links", String.Format("[GitHub Repository]({0})\n[Discord Server]({1})", Constants.REPO, Constants.DISCORD_INV));
				Actions.AddFooter(emb, "Help");
				await Actions.SendEmbedMessage(Context.Channel, emb);
				return;
			}

			//Send the message for that command
			var helpEntry = Variables.HelpList.FirstOrDefault(x => Actions.CaseInsEquals(x.Name, input));
			if (helpEntry == null)
			{
				//Find the command based on its aliases
				Variables.HelpList.ForEach(x =>
				{
					if (x.Aliases.Contains(input))
					{
						helpEntry = x;
						return;
					}
				});
				if (helpEntry == null)
				{
					//Find close help entries
					var closeHelps = Actions.GetCommandsWithInputInName(Actions.GetCommandsWithSimilarName(input), input)?.Distinct().ToList();
					if (closeHelps != null && closeHelps.Any())
					{
						//Format a message to be said
						var count = 1;
						var msg = "Did you mean any of the following:\n" + String.Join("\n", closeHelps.Select(x => String.Format("`{0}.` {1}", count++.ToString("00"), x.Help.Name)));

						//Create a new list, remove all others the user has, add the new one to the guild's list, remove it and the message that goes along with it after five seconds
						var acHelp = new ActiveCloseHelp(Context.User as IGuildUser, closeHelps);
						Variables.ActiveCloseHelp.ThreadSafeRemoveAll(x => x.User == Context.User);
						Variables.ActiveCloseHelp.ThreadSafeAdd(acHelp);
						await Actions.MakeAndDeleteSecondaryMessage(Context, msg, Constants.ACTIVE_CLOSE);
					}
					else
					{
						await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("Nonexistent command."));
					}
					return;
				}
			}

			var embed = Actions.MakeNewEmbed(helpEntry.Name, Actions.GetHelpString(helpEntry, prefix));
			Actions.AddFooter(embed, "Help");
			await Actions.SendEmbedMessage(Context.Channel, embed);
		}

		[Command("commands")]
		[Alias("cmds")]
		[Usage("<Category|All>")]
		[Summary("Prints out the commands in that category of the command list.")]
		[DefaultEnabled(true)]
		public async Task Commands([Optional, Remainder] string input)
		{
			if (String.IsNullOrWhiteSpace(input))
			{
				var desc = String.Format("Type `{0}commands [Category]` for commands from that category.\n\n{1}", Actions.GetPrefix(Context.Guild), String.Format("`{0}`", String.Join("`, `", Enum.GetNames(typeof(CommandCategory)))));
				var embed = Actions.MakeNewEmbed("Categories", desc);
				await Actions.SendEmbedMessage(Context.Channel, embed);
				return;
			}
			else if (Actions.CaseInsEquals(input, "all"))
			{
				await Actions.SendEmbedMessage(Context.Channel, Actions.MakeNewEmbed("All Commands", String.Format("`{0}`", String.Join("`, `", Variables.CommandNames))));
				return;
			}

			if (!Enum.TryParse(input, true, out CommandCategory category))
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("Category does not exist."));
				return;
			}
			await Actions.SendEmbedMessage(Context.Channel, Actions.MakeNewEmbed(Enum.GetName(typeof(CommandCategory), category), String.Format("`{0}`", String.Join("`, `", Actions.GetCommands(category)))));
		}

		[Command("getid")]
		[Alias("gid")]
		[Usage("[Guild|Channel|Role|User|Emoji|Bot] <\"Other Input\">")]
		[Summary("Shows the ID of the given object. Channels, roles, users, and emojis need to be supplied for the command to work if targetting those.")]
		[DefaultEnabled(true)]
		public async Task GetID([Remainder] string input)
		{
			var returnedArgs = Actions.GetArgs(Context, input, new ArgNumbers(1, 2));
			if (returnedArgs.Reason != ArgFailureReason.Not_Failure)
			{
				await Actions.HandleArgsGettingErrors(Context, returnedArgs);
				return;
			}
			var targetStr = returnedArgs.Arguments[0];
			var otherStr = returnedArgs.Arguments[1];

			if (Actions.CaseInsEquals(targetStr, "guild"))
			{
				await Actions.SendChannelMessage(Context, String.Format("This guild has the ID `{0}`.", Context.Guild.Id));
			}
			else if (Actions.CaseInsEquals(targetStr, "channel"))
			{
				var channel = Actions.GetChannel(Context, new[] { ChannelCheck.None }, true, otherStr).Object ?? Context.Channel as IGuildChannel;

				await Actions.SendChannelMessage(Context, String.Format("The {0} channel `{1}` has the ID `{2}`.", Actions.GetChannelType(channel), Actions.EscapeMarkdown(channel.Name), channel.Id));
			}
			else if (Actions.CaseInsEquals(targetStr, "role"))
			{
				var returnedRole = Actions.GetRole(Context, new[] { RoleCheck.None }, true, otherStr);
				if (returnedRole.Reason != FailureReason.Not_Failure)
				{
					await Actions.HandleObjectGettingErrors(Context, returnedRole);
					return;
				}
				var role = returnedRole.Object;

				await Actions.SendChannelMessage(Context, String.Format("The role `{0}` has the ID `{1}`.", Actions.EscapeMarkdown(role.Name), role.Id));
			}
			else if (Actions.CaseInsEquals(targetStr, "user"))
			{
				var user = Actions.GetGuildUser(Context, new[] { UserCheck.None }, true, otherStr).Object ?? Context.User as IGuildUser;

				await Actions.SendChannelMessage(Context, String.Format("The user `{0}#{1}` has the ID `{2}`.", Actions.EscapeMarkdown(user.Username), user.Discriminator, user.Id));
			}
			else if (Actions.CaseInsEquals(targetStr, "emoji"))
			{
				var returnedEmoji = Actions.GetEmoji(Context, true, otherStr);
				if (returnedEmoji.Reason != FailureReason.Not_Failure)
				{
					await Actions.HandleObjectGettingErrors(Context, returnedEmoji);
					return;
				}
				var emoji = returnedEmoji.Object;

				await Actions.SendChannelMessage(Context, String.Format("The emoji `{0}` has the ID `{1}`.", Actions.EscapeMarkdown(emoji.Name), emoji.Id));
			}
			else if (Actions.CaseInsEquals(targetStr, "bot"))
			{
				await Actions.SendChannelMessage(Context, String.Format("The bot has the ID `{0}.`", Variables.BotID));
			}
		}

		[Command("getinfo")]
		[Alias("ginf")]
		[Usage("[Guild|Channel|Role|User|Emoji|Invite|Bot] <\"Other Input\">")]
		[Summary("Shows information about the given object. Channels, roles, users, and emojis need to be supplied for the command to work if targetting those.")]
		[DefaultEnabled(true)]
		public async Task GetInfo([Remainder] string input)
		{
			var returnedArgs = Actions.GetArgs(Context, input, new ArgNumbers(1, 2));
			if (returnedArgs.Reason != ArgFailureReason.Not_Failure)
			{
				await Actions.HandleArgsGettingErrors(Context, returnedArgs);
				return;
			}
			var targetStr = returnedArgs.Arguments[0];
			var otherStr = returnedArgs.Arguments[1];

			if (Actions.CaseInsEquals(targetStr, "guild"))
			{
				var sGuild = Context.Guild as SocketGuild;
				var sOwner = sGuild.Owner;
				var title = sGuild.FormatGuild();
				var onlineCount = sGuild.Users.Where(x => x.Status != UserStatus.Offline).Count();
				var nicknameCount = sGuild.Users.Where(x => x.Nickname != null).Count();
				var gameCount = sGuild.Users.Where(x => x.Game.HasValue).Count();
				var botCount = sGuild.Users.Where(x => x.IsBot).Count();
				var voiceCount = sGuild.Users.Where(x => x.VoiceChannel != null).Count();
				var localECount = sGuild.Emotes.Where(x => !x.IsManaged).Count();
				var globalECount = sGuild.Emotes.Where(x => x.IsManaged).Count();

				var ageStr = String.Format("**Created:** `{0}` (`{1}` days ago)", sGuild.CreatedAt.UtcDateTime, DateTime.UtcNow.Subtract(sGuild.CreatedAt.UtcDateTime).Days);
				var ownerStr = String.Format("**Owner:** `{0}`", sOwner.FormatUser());
				var regionStr = String.Format("**Region:** `{0}`", sGuild.VoiceRegionId);
				var emoteStr = String.Format("**Emotes:** `{0}` (`{1}` local, `{2}` global)\n", localECount + globalECount, localECount, globalECount);
				var userStr = String.Format("**User Count:** `{0}` (`{1}` online, `{2}` bots)", sGuild.MemberCount, onlineCount, botCount);
				var nickStr = String.Format("**Users With Nickname:** `{0}`", nicknameCount);
				var gameStr = String.Format("**Users Playing Games:** `{0}`", gameCount);
				var voiceStr = String.Format("**Users In Voice:** `{0}`\n", voiceCount);
				var roleStr = String.Format("**Role Count:** `{0}`", sGuild.Roles.Count);
				var channelStr = String.Format("**Channel Count:** `{0}` (`{1}` text, `{2}` voice)", sGuild.Channels.Count, sGuild.TextChannels.Count, sGuild.VoiceChannels.Count);
				var afkChanStr = String.Format("**AFK Channel:** `{0}` (`{1}` minute{2})", sGuild.AFKChannel.FormatChannel(), sGuild.AFKTimeout / 60, Actions.GetPlural(sGuild.AFKTimeout / 60));
				var all = String.Join("\n", new List<string>() { ageStr, ownerStr, regionStr, emoteStr, userStr, nickStr, gameStr, voiceStr, roleStr, channelStr, afkChanStr });

				var embed = Actions.MakeNewEmbed(title, all, thumbnailURL: sGuild.IconUrl);
				Actions.AddFooter(embed, "Guild Info");
				await Actions.SendEmbedMessage(Context.Channel, embed);
			}
			else if (Actions.CaseInsEquals(targetStr, "channel"))
			{
				var channel = Actions.GetChannel(Context, new[] { ChannelCheck.None }, true, otherStr).Object as SocketChannel ?? Context.Channel as SocketChannel;

				var title = channel.FormatChannel();
				var age = String.Format("**Created:** `{0}` (`{1}` days ago)", channel.CreatedAt.UtcDateTime, DateTime.UtcNow.Subtract(channel.CreatedAt.UtcDateTime).Days);
				var users = String.Format("**User Count:** `{0}`", channel.Users.Count);
				var desc = String.Join("\n", new[] { age, users });

				var embed = Actions.MakeNewEmbed(title, desc);
				Actions.AddFooter(embed, "Channel Info");
				await Actions.SendEmbedMessage(Context.Channel, embed);
			}
			else if (Actions.CaseInsEquals(targetStr, "role"))
			{
				var returnedRole = Actions.GetRole(Context, new[] { RoleCheck.None }, true, otherStr);
				if (returnedRole.Reason != FailureReason.Not_Failure)
				{
					await Actions.HandleObjectGettingErrors(Context, returnedRole);
					return;
				}
				var role = returnedRole.Object as SocketRole;

				var title = role.FormatRole();
				var age = String.Format("**Created:** `{0}` (`{1}` days ago)", role.CreatedAt.UtcDateTime, DateTime.UtcNow.Subtract(role.CreatedAt.UtcDateTime).Days);
				var position = String.Format("**Position:** `{0}`", role.Position);
				var users = String.Format("**User Count:** `{0}`", (await Context.Guild.GetUsersAsync()).Where(x => x.RoleIds.Contains(role.Id)).Count());
				var desc = String.Join("\n", new[] { age, position, users });

				var embed = Actions.MakeNewEmbed(title, desc);
				Actions.AddFooter(embed, "Role Info");
				await Actions.SendEmbedMessage(Context.Channel, embed);
			}
			else if (Actions.CaseInsEquals(targetStr, "user"))
			{
				var user = Actions.GetGuildUser(Context, new[] { UserCheck.None }, true, otherStr).Object ?? Context.User as IGuildUser;

				var embed = Actions.FormatUserInfo(Context.Guild as SocketGuild, user);
				await Actions.SendEmbedMessage(Context.Channel, embed);
			}
			else if (Actions.CaseInsEquals(targetStr, "emoji"))
			{
				var returnedEmoji = Actions.GetEmoji(Context, true, otherStr);
				if (returnedEmoji.Reason != FailureReason.Not_Failure)
				{
					await Actions.HandleObjectGettingErrors(Context, returnedEmoji);
					return;
				}
				var emoji = returnedEmoji.Object;

				//Try to find the emoji if global
				var guilds = (await Context.Client.GetGuildsAsync()).Where(x =>
				{
					var placeholder = x.Emotes.FirstOrDefault(y => y.Id == emoji.Id);
					return placeholder.IsManaged && placeholder.RequireColons;
				});

				//Format a description
				var description = String.Format("**ID:** `{0}`\n", emoji.Id);
				if (guilds.Any())
				{
					description += String.Format("**From:** `{0}`", String.Join("`, `", guilds.Select(x => x.FormatGuild())));
				}

				await Actions.SendEmbedMessage(Context.Channel, Actions.MakeNewEmbed(emoji.Name, description, thumbnailURL: emoji.Url));
			}
			else if (Actions.CaseInsEquals(targetStr, "invite"))
			{
				var inv = (await Context.Guild.GetInvitesAsync()).FirstOrDefault(x => x.Code == otherStr);
				if (inv == null)
				{
					await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("No invite with that code could be gotten."));
					return;
				}

				var inviterStr = String.Format("**Inviter:** `{0}`", inv.Inviter.FormatUser());
				var channelStr = String.Format("**Channel:** `{0}`", (await Context.Guild.GetChannelAsync(inv.ChannelId)).FormatChannel());
				var usesStr = String.Format("**Uses:** `{0}`", inv.Uses);
				var createdStr = String.Format("**Created At:** `{0} on {1}`", inv.CreatedAt.UtcDateTime.ToShortTimeString(), inv.CreatedAt.UtcDateTime.ToShortDateString());
				var desc = String.Join("\n", new[] { inviterStr, channelStr, usesStr, createdStr });

				var embed = Actions.MakeNewEmbed(inv.Code, desc);
				await Actions.SendEmbedMessage(Context.Channel, embed);

			}
			else if (Actions.CaseInsEquals(targetStr, "bot"))
			{
				//Make the description
				var online = String.Format("**Online Since:** {0}", Variables.StartupTime);
				var uptime = Actions.GetUptime();
				var guildCount = String.Format("**Guild Count:** {0}", Variables.TotalGuilds);
				var memberCount = String.Format("**Cumulative Member Count:** {0}", Variables.TotalUsers);
				var currShard = String.Format("**Current Shard:** {0}", Variables.Client.GetShardFor(Context.Guild).ShardId);
				var description = String.Join("\n", new[] { online, uptime, guildCount, memberCount, currShard });

				//Make the embed
				var embed = Actions.MakeNewEmbed(null, description);
				Actions.AddAuthor(embed, Variables.BotName, Context.Client.CurrentUser.GetAvatarUrl());
				Actions.AddFooter(embed, "Version " + Constants.BOT_VERSION);

				//First field
				var firstField = Actions.FormatLoggedThings();
				Actions.AddField(embed, "Logged Actions", firstField);

				//Second field
				var attempt = String.Format("**Attempted Commands:** {0}", Variables.AttemptedCommands);
				var successful = String.Format("**Successful Commands:** {0}", Variables.AttemptedCommands - Variables.FailedCommands);
				var failed = String.Format("**Failed Commands:** {0}", Variables.FailedCommands);
				var secondField = String.Join("\n", new[] { attempt, successful, failed });
				Actions.AddField(embed, "Commands", secondField);

				//Third field
				var latency = String.Format("**Latency:** {0}ms", Variables.Client.GetLatency());
				var memory = String.Format("**Memory Usage:** {0}MB", Actions.GetMemory().ToString("0.00"));
				var threads = String.Format("**Thread Count:** {0}", Process.GetCurrentProcess().Threads.Count);
				var thirdField = String.Join("\n", new[] { latency, memory, threads });
				Actions.AddField(embed, "Technical", thirdField);

				//Send the embed
				await Actions.SendEmbedMessage(Context.Channel, embed);
			}
		}

		[Command("displayguilds")]
		[Alias("dgs")]
		[Usage("")]
		[Summary("Lists the name, ID, owner, and owner's ID of every guild the bot is on.")]
		[OtherRequirement(1U << (int)Precondition.Bot_Owner)]
		[DefaultEnabled(true)]
		public async Task ListGuilds()
		{
			var guilds = Variables.Client.GetGuilds().ToList();
			if (guilds.Count < 10)
			{
				var embed = Actions.MakeNewEmbed("Guilds");
				guilds.ForEach(x =>
				{
					Actions.AddField(embed, x.FormatGuild(), String.Format("**Owner:** `{0}`", x.Owner.FormatUser()));
				});
				await Actions.SendEmbedMessage(Context.Channel, embed);
			}
			else
			{
				var count = 1;
				var guildStrings = guilds.Select(x => String.Format("`{0}.` `{1}` Owner: `{2}`", count++.ToString("00"), x.FormatGuild(), x.Owner.FormatUser()));
				await Actions.SendEmbedMessage(Context.Channel, Actions.MakeNewEmbed("Guilds", String.Join("\n", guildStrings)));
			}
		}

		[Command("getuseravatar")]
		[Alias("gua")]
		[Usage("<User> <Type:Gif|Png|Jpg|Webp>")]
		[Summary("Shows the URL of the given user's avatar (no formatting in case people on mobile want it easily).")]
		[DefaultEnabled(true)]
		public async Task UserAvatar([Optional, Remainder] string input)
		{
			//Split the input
			var returnedArgs = Actions.GetArgs(Context, input, new ArgNumbers(0, 2), new[] { "type" });
			if (returnedArgs.Reason != ArgFailureReason.Not_Failure)
			{
				await Actions.HandleArgsGettingErrors(Context, returnedArgs);
				return;
			}
			var userStr = returnedArgs.Arguments[0];
			var formatStr = returnedArgs.GetSpecifiedArg("type");

			//Get the type of image
			var format = ImageFormat.Auto;
			if (!String.IsNullOrWhiteSpace(formatStr))
			{
				if (!Enum.TryParse(formatStr, true, out format))
				{
					await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("Invalid avatar format supplied."));
					return;
				}
			}

			//Get the user
			var returnedUser = Actions.GetGuildUser(Context, new[] { UserCheck.None }, true, userStr);
			var user = returnedUser.Object ?? Context.User as IGuildUser;

			//Send a message with the URL
			await Context.Channel.SendMessageAsync(user.GetAvatarUrl(format));
		}

		[Command("displayuserjoinlist")]
		[Alias("dujl")]
		[Usage("")]
		[Summary("Lists most of the users who have joined the guild. Not 100% accurate.")]
		[OtherRequirement(1U << (int)Precondition.User_Has_A_Perm)]
		[DefaultEnabled(true)]
		public async Task UserJoins()
		{
			var users = (await Context.Guild.GetUsersAsync()).Where(x => x.JoinedAt.HasValue).OrderBy(x => x.JoinedAt);
			var count = 1;
			var padLength = users.Count().ToString().Length;
			var userMsg = String.Join("\n", users.Select(x =>
			{
				var time = x.JoinedAt.Value.UtcDateTime;
				return String.Format("`{0}.` `{1}` joined at `{2}` on `{3}`.",
					count++.ToString().PadLeft(padLength, '0'), x.FormatUser(), time.ToShortTimeString(), time.ToShortDateString());
			}));

			await Actions.WriteAndUploadTextFile(Context.Guild, Context.Channel, userMsg, "User_Joins_");
		}

		[Command("getuserjoinedat")]
		[Alias("gujat")]
		[Usage("[Position]")]
		[Summary("Shows the user which joined the guild in that position. Not 100% accurate.")]
		[OtherRequirement(1U << (int)Precondition.User_Has_A_Perm)]
		[DefaultEnabled(true)]
		public async Task UserJoinedAt([Remainder] string input)
		{
			if (int.TryParse(input, out int position))
			{
				var guildUsers = await Context.Guild.GetUsersAsync();
				var users = guildUsers.Where(x => x.JoinedAt != null).OrderBy(x => x.JoinedAt.Value.Ticks).ToList();
				if (position >= 1 && position < users.Count)
				{
					var user = users[position - 1];
					await Actions.SendChannelMessage(Context, String.Format("`{0}` was #{1} to join the guild on `{2} {3}, {4}` at `{5}`.",
						user.FormatUser(),
						position,
						System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(user.JoinedAt.Value.UtcDateTime.Month),
						user.JoinedAt.Value.UtcDateTime.Day,
						user.JoinedAt.Value.UtcDateTime.Year,
						user.JoinedAt.Value.UtcDateTime.ToLongTimeString()));
				}
				else
				{
					await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("Invalid position."));
				}
			}
			else
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("Something besides a number was input."));
			}
		}

		[Command("getcurrentmembercount")]
		[Alias("cmc")]
		[Usage("")]
		[Summary("Shows the current number of members in the guild.")]
		[OtherRequirement(1U << (int)Precondition.User_Has_A_Perm)]
		[DefaultEnabled(true)]
		public async Task CurrentMemberCount()
		{
			await Actions.SendChannelMessage(Context, String.Format("The current member count is `{0}`.", (Context.Guild as SocketGuild).MemberCount));
		}

		[Command("getuserswithrole")]
		[Alias("guwr")]
		[Usage("[Role]")]
		[Summary("Prints out a list of all users with the given role.")]
		[OtherRequirement(1U << (int)Precondition.User_Has_A_Perm)]
		[DefaultEnabled(true)]
		public async Task UsersWithRole([Remainder] string input)
		{
			var returnedRole = Actions.GetRole(Context, new[] { RoleCheck.None }, true, input);
			if (returnedRole.Reason != FailureReason.Not_Failure)
			{
				await Actions.HandleObjectGettingErrors(Context, returnedRole);
				return;
			}
			var role = returnedRole.Object;

			var count = 1;
			var users = String.Join("\n", (await Context.Guild.GetUsersAsync()).Where(x => x.JoinedAt.HasValue).OrderBy(x => x.JoinedAt).Where(x => x.RoleIds.Contains(role.Id)).ToList().Select(x =>
			{
				return String.Format("`{0}.` `{1}`", count++.ToString("00"), x.FormatUser());
			}));

			var roleName = role.Name.Substring(0, 3) + Constants.ZERO_LENGTH_CHAR + role.Name.Substring(3);
			await Actions.SendEmbedMessage(Context.Channel, Actions.MakeNewEmbed(roleName, users));
		}

		[Command("getuserswithname")]
		[Alias("guwn")]
		[Usage("[\"Name to Search For\"] <Exact:True|False> <Count:True|False> <Nickname:True|False>")]
		[Summary("Lists all users where their username contains the given string.")]
		[OtherRequirement(1U << (int)Precondition.User_Has_A_Perm)]
		[DefaultEnabled(true)]
		public async Task UsersWithName([Remainder] string input)
		{
			var returnedArgs = Actions.GetArgs(Context, input, new ArgNumbers(1, 4), new[] { "exact", "count", "nickname" });
			if (returnedArgs.Reason != ArgFailureReason.Not_Failure)
			{
				await Actions.HandleArgsGettingErrors(Context, returnedArgs);
				return;
			}
			var nameStr = returnedArgs.Arguments[0];
			var exactStr = returnedArgs.GetSpecifiedArg("exact");
			var countStr = returnedArgs.GetSpecifiedArg("count");
			var nickStr = returnedArgs.GetSpecifiedArg("nickname");

			var exact = false;
			if (!String.IsNullOrWhiteSpace(exactStr))
			{
				if (!bool.TryParse(exactStr, out exact))
				{
					await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("Invalid input for exact."));
					return;
				}
			}
			var count = false;
			if (!String.IsNullOrWhiteSpace(countStr))
			{
				if (!bool.TryParse(countStr, out count))
				{
					await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("Invalid input for count."));
					return;
				}
			}
			var nickname = false;
			if (!String.IsNullOrWhiteSpace(nickStr))
			{
				if (!bool.TryParse(nickStr, out nickname))
				{
					await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("Invalid input for nickname."));
					return;
				}
			}

			var users = await Context.Guild.GetUsersAsync();
			if (exact)
			{
				users = users.Where(x =>
				{
					return Actions.CaseInsEquals(x.Username, nameStr) || (nickname && Actions.CaseInsEquals(x?.Nickname, nameStr));
				}).ToList();
			}
			else
			{
				users = users.Where(x =>
				{
					return Actions.CaseInsIndexOf(x.Username, nameStr) || (nickname && Actions.CaseInsIndexOf(x?.Nickname, nameStr));
				}).ToList();
			}

			if (count)
			{
				await Actions.SendChannelMessage(Context, String.Format("The following number of users have a name containing `{0}`: `{1}`.", nameStr, users.Count));
			}
			else
			{
				var c = 1;
				var response = String.Join("\n", users.OrderBy(x => x.JoinedAt).ToList().Select(x =>
				{
					return String.Format("`{0}.` `{1}`", c++.ToString("00"), x.FormatUser());
				}));

				var title = String.Format("Users With Names Containing '{0}'", input);
				await Actions.SendEmbedMessage(Context.Channel, Actions.MakeNewEmbed(title, response));
			}
		}

		[Command("displayemojis")]
		[Alias("demojis")]
		[Usage("[Global|Guild]")]
		[Summary("Lists the emoji in the guild. As of right now, with the current API wrapper version this bot uses, there's no way to upload or remove emojis yet; sorry.")]
		[OtherRequirement(1U << (int)Precondition.User_Has_A_Perm)]
		[DefaultEnabled(true)]
		public async Task ListEmojis([Remainder] string input)
		{
			var emotes = new List<GuildEmote>();
			if (Actions.CaseInsEquals(input, "guild"))
			{
				emotes = Context.Guild.Emotes.Where(x => !x.IsManaged).ToList();
			}
			else if (Actions.CaseInsEquals(input, "global"))
			{
				emotes = Context.Guild.Emotes.Where(x => x.IsManaged).ToList();
			}
			else
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("Invalid option."));
				return;
			}

			int count = 1;
			var description = String.Join("\n", emotes.Select(x =>
			{
				return String.Format("`{0}.` <:{1}:{2}> `{3}`", count++.ToString("00"), x.Name, x.Id, x.Name);
			}));

			description = description ?? String.Format("This guild has no {0} emojis.", input.ToLower());
			await Actions.SendEmbedMessage(Context.Channel, Actions.MakeNewEmbed("Emojis", description));
		}

		[Command("displayinvites")]
		[Alias("dinvs")]
		[Usage("")]
		[Summary("Gives a list of all the instant invites on the guild.")]
		[OtherRequirement(1U << (int)Precondition.User_Has_A_Perm)]
		[DefaultEnabled(true)]
		public async Task ListInstantInvites()
		{
			//Get the invites
			var invites = (await Context.Guild.GetInvitesAsync()).OrderBy(x => x.Uses).Reverse().ToList();

			//Make sure there are some invites
			if (!invites.Any())
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("This guild has no invites."));
				return;
			}

			//Format the description
			var description = "";
			var count = 1;
			var lengthForCount = invites.Count.ToString().Length;
			var lengthForCode = invites.Max(x => x.Code.Length);
			var lengthForUses = invites.Max(x => x.Uses).ToString().Length;
			invites.ForEach(x =>
			{
				var cnt = count++.ToString().PadLeft(lengthForCount, '0');
				var code = x.Code.PadRight(lengthForCode);
				var uses = x.Uses.ToString().PadRight(lengthForUses);
				description += String.Format("`{0}.` `{1}` `{2}` `{3}`\n", cnt, code, uses, x.Inviter.Username);
			});

			//Send a success message
			await Actions.SendEmbedMessage(Context.Channel, Actions.MakeNewEmbed("Instant Invite List", description));
		}

		[Command("createinvite")]
		[Alias("cinv")]
		[Usage("[Channel] <Time:1800|3600|21600|43200|86400> <Uses:1|5|10|25|50|100> <TempMem:True|False>")]
		[Summary("Creates an invite on the given channel. No time specifies to not expire. No uses has no usage limit. Temp membership means when the user goes offline they get kicked.")]
		[PermissionRequirement(1U << (int)GuildPermission.CreateInstantInvite)]
		[DefaultEnabled(true)]
		public async Task CreateInstantInvite([Remainder] string input)
		{
			//Split the input
			var returnedArgs = Actions.GetArgs(Context, input, new ArgNumbers(0, 4), new[] { "time", "uses", "tempmem" });
			if (returnedArgs.Reason != ArgFailureReason.Not_Failure)
			{
				await Actions.HandleArgsGettingErrors(Context, returnedArgs);
				return;
			}
			var chanStr = returnedArgs.Arguments[0];
			var timeStr = returnedArgs.GetSpecifiedArg("time");
			var usesStr = returnedArgs.GetSpecifiedArg("uses");
			var tempStr = returnedArgs.GetSpecifiedArg("tempmem");

			//Check validity of channel
			var returnedChannel = Actions.GetChannel(Context, new[] { ChannelCheck.Can_Modify_Permissions }, true, chanStr);
			if (returnedChannel.Reason != FailureReason.Not_Failure)
			{
				await Actions.HandleObjectGettingErrors(Context, returnedChannel);
				return;
			}
			var channel = returnedChannel.Object;

			int? nullableTime = null;
			int[] validTimes = { 1800, 3600, 21600, 43200, 86400 };
			if (!String.IsNullOrWhiteSpace(timeStr))
			{
				if (int.TryParse(timeStr, out int time) && validTimes.Contains(time))
				{
					nullableTime = time;
				}
				else
				{
					await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("Invalid time supplied."));
					return;
				}
			}

			int? nullableUsers = null;
			int[] validUsers = { 1, 5, 10, 25, 50, 100 };
			if (!String.IsNullOrWhiteSpace(usesStr))
			{
				if (int.TryParse(usesStr, out int users) && validUsers.Contains(users))
				{
					nullableUsers = users;
				}
				else
				{
					await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("Invalid uses supplied."));
					return;
				}
			}

			var tempMembership = false;
			if (!String.IsNullOrWhiteSpace(tempStr))
			{
				if (!bool.TryParse(tempStr, out tempMembership))
				{
					await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("Invalid temp membership boolean supplied."));
					return;
				}
			}

			var inv = await channel.CreateInviteAsync(nullableTime, nullableUsers, tempMembership);

			//Format the response message
			var timeOutputStr = "";
			if (nullableTime == null)
			{
				timeOutputStr = "It will last until manually revoked.";
			}
			else
			{
				timeOutputStr = String.Format("It will last for this amount of time: `{0}`.", timeStr);
			}
			var usersOutputStr = "";
			if (nullableUsers == null)
			{
				usersOutputStr = "It has no usage limit.";
			}
			else
			{
				usersOutputStr = String.Format("It will last for this amount of uses: `{0}`.", usesStr);
			}
			var tempOutputStr = "";
			if (tempMembership)
			{
				tempOutputStr = "Users will be kicked when they go offline unless they get a role.";
			}

			await Actions.SendChannelMessage(Context, String.Format("Here is your invite for `{0}`: {1}\n{2}\n{3}\n{4}", channel.FormatChannel(), inv.Url, timeOutputStr, usersOutputStr, tempOutputStr));
		}

		[Command("deleteinvite")]
		[Alias("dinv")]
		[Usage("[Invite Code]")]
		[Summary("Deletes the invite with the given code.")]
		[PermissionRequirement(1U << (int)GuildPermission.ManageChannels)]
		[DefaultEnabled(true)]
		public async Task DeleteInstantInvite([Remainder] string input)
		{
			//Get the input
			var invite = (await Context.Guild.GetInvitesAsync()).FirstOrDefault(x => x.Code == input);
			if (invite == null)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("That invite doesn't exist."));
				return;
			}

			//Delete the invite and send a success message
			await invite.DeleteAsync();
			await Actions.MakeAndDeleteSecondaryMessage(Context, String.Format("Successfully deleted the invite `{0}`.", invite.Code));
		}

		[Command("deletemultipleinvites")]
		[Alias("dminv")]
		[Usage("User:User|Role:Role|Uses:Number|Expires:True|False]")]
		[Summary("Deletes all invites satisfying the given condition of either user, creation channel, uses, or expiry time.")]
		[PermissionRequirement(1U << (int)GuildPermission.ManageChannels)]
		[DefaultEnabled(true)]
		public async Task DeleteMultipleInvites([Remainder] string input)
		{
			//Get the guild's invites
			var invites = (await Context.Guild.GetInvitesAsync()).ToList();
			if (!invites.Any())
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("This guild has no invites."));
				return;
			}

			//Get the given variable out
			var returnedArgs = Actions.GetArgs(Context, input, new ArgNumbers(0, 4), new[] { "user", "channel", "uses", "expires" });
			if (returnedArgs.Reason != ArgFailureReason.Not_Failure)
			{
				await Actions.HandleArgsGettingErrors(Context, returnedArgs);
				return;
			}
			var userStr = returnedArgs.GetSpecifiedArg("user");
			var chanStr = returnedArgs.GetSpecifiedArg("channel");
			var usesStr = returnedArgs.GetSpecifiedArg("uses");
			var exprStr = returnedArgs.GetSpecifiedArg("expires");

			if (String.IsNullOrWhiteSpace(userStr) && new[] { userStr, chanStr, usesStr, exprStr }.CaseInsEverythingSame())
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("At least one of the arguments must be specified."));
				return;
			}

			//User
			if (!String.IsNullOrWhiteSpace(userStr))
			{
				if (ulong.TryParse(userStr, out ulong userID))
				{
					invites = invites.Where(x => x.Inviter.Id == userID).ToList();
				}
				else if (MentionUtils.TryParseUser(userStr, out userID))
				{
					invites = invites.Where(x => x.Inviter.Id == userID).ToList();
				}
				else
				{
					invites = invites.Where(x => Actions.CaseInsEquals(x.Inviter.Username, userStr)).ToList();
				}
			}
			//Channel
			if (!String.IsNullOrWhiteSpace(chanStr))
			{
				var returnedChannel = Actions.GetChannel(Context, new[] { ChannelCheck.Can_Modify_Permissions }, true, chanStr);
				if (returnedChannel.Reason == FailureReason.Not_Failure)
				{
					invites = invites.Where(x => x.ChannelId == returnedChannel.Object.Id).ToList();
				}
			}
			//Uses
			if (!String.IsNullOrWhiteSpace(usesStr))
			{
				if (int.TryParse(usesStr, out int uses))
				{
					invites = invites.Where(x => x.Uses == uses).ToList();
				}
			}
			//Expiry
			if (!String.IsNullOrWhiteSpace(exprStr))
			{
				if (bool.TryParse(exprStr, out bool expires))
				{
					if (expires)
					{
						invites = invites.Where(x => x.MaxAge != null).ToList();
					}
					else
					{
						invites = invites.Where(x => x.MaxAge == null).ToList();
					}
				}
			}

			if (!invites.Any())
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("No invites satisfied the given conditions."));
				return;
			}

			Task.Run(async () =>
			{
				var typing = Context.Channel.EnterTypingState();
				await invites.ForEachAsync(async x => await x.DeleteAsync());
				typing.Dispose();
				await Actions.MakeAndDeleteSecondaryMessage(Context, String.Format("Successfully deleted `{0}` instant invites on this guild.", invites.Count));
			}).Forget();
		}

		[Command("makeanembed")]
		[Alias("mae")]
		[Usage("<\"Title:input\"> <\"Desc:input\"> <Img:url> <Url:url> <Thumb:url> <Color:int/int/int> <\"Author:input\"> <AuthorIcon:url> <AuthorUrl:url> <\"Foot:input\"> <FootIcon:url> " +
				"<\"Field[1-25]:input\"> <\"FieldText[1-25]:input\"> <FieldInline[1-25]:true|false>")]
		[Summary("Every single piece is optional. The stuff in quotes *must* be in quotes. URLs need the https:// in front. Fields need *both* Field and FieldText to work.")]
		[OtherRequirement(1U << (int)Precondition.User_Has_A_Perm)]
		[DefaultEnabled(true)]
		public async Task MakeAnEmbed([Remainder] string input)
		{
			//Split the input
			var returnedArgs = Actions.GetArgs(Context, input, new ArgNumbers(0, 100), new[] { "title", "desc", "img", "url", "thumb", "author", "authoricon", "authorurl", "foot", "footicon" });
			if (returnedArgs.Reason != ArgFailureReason.Not_Failure)
			{
				await Actions.HandleArgsGettingErrors(Context, returnedArgs);
				return;
			}
			var title = returnedArgs.GetSpecifiedArg("title");
			var description = returnedArgs.GetSpecifiedArg("desc");
			var imageURL = returnedArgs.GetSpecifiedArg("img");
			var URL = returnedArgs.GetSpecifiedArg("url");
			var thumbnail = returnedArgs.GetSpecifiedArg("thumb");
			var authorName = returnedArgs.GetSpecifiedArg("author");
			var authorIcon = returnedArgs.GetSpecifiedArg("authoricon");
			var authorURL = returnedArgs.GetSpecifiedArg("authorurl");
			var footerText = returnedArgs.GetSpecifiedArg("foot");
			var footerIcon = returnedArgs.GetSpecifiedArg("footicon");

			//Get the color
			var color = Constants.BASE;
			var colorRGB = Actions.GetVariableAndRemove(returnedArgs.Arguments, "color")?.Split('/');
			if (colorRGB != null && colorRGB.Length == 3)
			{
				const byte MAX_VAL = 255;
				if (byte.TryParse(colorRGB[0], out byte r) && byte.TryParse(colorRGB[1], out byte g) && byte.TryParse(colorRGB[2], out byte b))
				{
					color = new Color(Math.Min(r, MAX_VAL), Math.Min(g, MAX_VAL), Math.Min(b, MAX_VAL));
				}
			}

			//Make the embed
			var embed = Actions.MakeNewEmbed(title, description, color, imageURL, URL, thumbnail);
			//Add in the author
			Actions.AddAuthor(embed, authorName, authorIcon, authorURL);
			//Add in the footer
			Actions.AddFooter(embed, footerText, footerIcon);

			//Add in the fields and text
			for (int i = 1; i < 25; i++)
			{
				//Get the input for fields
				var field = Actions.GetVariableAndRemove(returnedArgs.Arguments, "field" + i);
				var fieldText = Actions.GetVariableAndRemove(returnedArgs.Arguments, "fieldtext" + i);
				//If either is null break out of this loop because they shouldn't be null
				if (field == null || fieldText == null)
					break;

				//Get the bool for the field
				bool.TryParse(Actions.GetVariableAndRemove(returnedArgs.Arguments, "fieldinline" + i), out bool inlineBool);

				//Add in the field
				Actions.AddField(embed, field, fieldText, inlineBool);
			}

			//Send the embed
			await Actions.SendEmbedMessage(Context.Channel, embed);
		}

		[Command("mentionrole")]
		[Alias("mnr")]
		[Usage("[Role] [\"Message\"]")]
		[Summary("Mention an unmentionable role with the given message.")]
		[OtherRequirement(1U << (int)Precondition.User_Has_A_Perm)]
		[DefaultEnabled(true)]
		public async Task MentionRole([Remainder] string input)
		{
			var returnedArgs = Actions.GetArgs(Context, input, new ArgNumbers(2, 2));
			if (returnedArgs.Reason != ArgFailureReason.Not_Failure)
			{
				await Actions.HandleArgsGettingErrors(Context, returnedArgs);
				return;
			}
			var roleStr = returnedArgs.Arguments[0];
			var textStr = returnedArgs.Arguments[1];

			if (textStr.Length > Constants.MAX_MESSAGE_LENGTH_LONG)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR(String.Format("Please keep the message to send under `{0}` characters.", Constants.MAX_MESSAGE_LENGTH_LONG)));
				return;
			}

			//Get the role and see if it can be changed
			var returnedRole = Actions.GetRole(Context, new[] { RoleCheck.Can_Be_Edited, RoleCheck.Is_Everyone }, true, roleStr);
			if (returnedRole.Reason != FailureReason.Not_Failure)
			{
				await Actions.HandleObjectGettingErrors(Context, returnedRole);
				return;
			}
			var role = returnedRole.Object;

			//See if people can already mention the role
			if (role.IsMentionable)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("You can already mention this role."));
				return;
			}

			//Make the role mentionable
			await role.ModifyAsync(x => x.Mentionable = true);
			//Send the message
			var user = Context.User;
			await Actions.SendChannelMessage(Context, String.Format("`{0}`, {1}: {2}", user.FormatUser(), role.Mention, textStr));
			//Remove the mentionability
			await role.ModifyAsync(x => x.Mentionable = false);
		}

		[Command("messagebotowner")]
		[Alias("mbo")]
		[Usage("[Message]")]
		[Summary("Sends a message to the bot owner with the given text. Messages will be cut down to 250 characters.")]
		[OtherRequirement(1U << (int)Precondition.User_Has_A_Perm)]
		[DefaultEnabled(true)]
		public async Task MessageBotOwner([Remainder] string input)
		{
			var cutMsg = input.Substring(0, Math.Min(input.Length, 250));
			var fromMsg = String.Format("From `{0}` in `{1}`:", Context.User.FormatUser(), Context.Guild.FormatGuild());
			var newMsg = String.Format("{0}\n```{1}```", fromMsg, cutMsg);

			var owner = Variables.Client.GetUser(Variables.BotInfo.BotOwnerID);
			if (owner == null)
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("The owner is unable to be gotten."));
				return;
			}
			await (await owner.CreateDMChannelAsync()).SendMessageAsync(newMsg);
		}

		[Command("getpermnamesfromvalue")]
		[Alias("getperms")]
		[Usage("[Number]")]
		[Summary("Lists all the perms that come from the given value.")]
		[OtherRequirement(1U << (int)Precondition.User_Has_A_Perm)]
		[DefaultEnabled(true)]
		public async Task GetPermsFromValue([Remainder] string input)
		{
			if (!uint.TryParse(input, out uint num))
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("Input is not a number."));
				return;
			}

			var perms = Actions.GetPermissionNames(num);
			if (!perms.Any())
			{
				await Actions.MakeAndDeleteSecondaryMessage(Context, Actions.ERROR("The given number holds no permissions."));
				return;
			}
			await Actions.SendChannelMessage(Context.Channel, String.Format("The number `{0}` has the following permissions: `{1}`.", num, String.Join("`, `", perms)));
		}

		[Command("test")]
		[OtherRequirement(1U << (int)Precondition.Bot_Owner)]
		[DefaultEnabled(true)]
		public async Task Test([Optional, Remainder] string input)
		{
			var a = new string('a', 2050);
			var embed = Actions.MakeNewEmbed("test", a);
			Actions.AddField(embed, "name", a);
			Actions.AddField(embed, "2", "test");
			Actions.AddField(embed, "asdf", a);

			await Actions.SendEmbedMessage(Context.Channel, embed);

			await Actions.MakeAndDeleteSecondaryMessage(Context, "test");
		}
	}
}