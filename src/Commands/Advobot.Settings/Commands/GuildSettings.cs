﻿using Advobot.Attributes;

using Discord.Commands;

namespace Advobot.Settings.Commands
{
	[Category(nameof(GuildSettings))]
	public sealed class GuildSettings : ModuleBase
	{
		/*
		[LocalizedGroup(nameof(Groups.ShowGuildSettings))]
		[LocalizedAlias(nameof(Aliases.ShowGuildSettings))]
		[LocalizedSummary(nameof(Summaries.ShowGuildSettings))]
		[Meta("b6ee91c4-05dc-4017-a08f-0c1478435179", IsEnabled = true)]
		[RequireGenericGuildPermissions]
		public sealed class ShowGuildSettings : ReadOnlySettingsModule<IGuildSettings>
		{
			protected override IGuildSettings Settings => Context.Settings;

			[LocalizedCommand(nameof(Groups.Json))]
			[LocalizedAlias(nameof(Aliases.Json))]
			[Priority(1)]
			public Task<RuntimeResult> Json()
				=> Responses.GuildSettings.DisplayJson(Settings);

			[LocalizedCommand(nameof(Groups.Names))]
			[LocalizedAlias(nameof(Aliases.Names))]
			[Priority(1)]
			public Task<RuntimeResult> Names()
				=> Responses.GuildSettings.DisplayNames(Settings);

			[LocalizedCommand(nameof(Groups.All))]
			[LocalizedAlias(nameof(Aliases.All))]
			[Priority(1)]
			public Task<RuntimeResult> All()
				=> Responses.GuildSettings.DisplaySettings(Context.Client, Context.Guild, Settings);

			[Command]
			public Task<RuntimeResult> Command([Remainder, GuildSettingName] string name)
				=> Responses.GuildSettings.DisplaySetting(Context.Client, Context.Guild, Settings, name);
		}

		[LocalizedGroup(nameof(Groups.ResetGuildSettings))]
		[LocalizedAlias(nameof(Aliases.ResetGuildSettings))]
		[LocalizedSummary(nameof(Summaries.ResetGuildSettings))]
		[Meta("316df0fc-1c5e-40fe-8580-7b8ca5f63b43", IsEnabled = true)]
		[RequireGuildPermissions]
		public sealed class ResetGuildSettings : SettingsModule<IGuildSettings>
		{
			protected override IGuildSettings Settings => Context.Settings;

			[LocalizedCommand(nameof(Groups.All))]
			[LocalizedAlias(nameof(Aliases.All))]
			[Priority(1)]
			public Task<RuntimeResult> All()
			{
				foreach (var setting in Settings.GetSettingNames())
				{
					Settings.ResetSetting(setting);
				}
				return Responses.GuildSettings.ResetAll();
			}

			[Command]
			public Task<RuntimeResult> Command([Remainder, GuildSettingName] string name)
			{
				Settings.ResetSetting(name);
				return Responses.GuildSettings.Reset(name);
			}
		}

		/*
		[LocalizedGroup(nameof(Groups.ModifyGuildSettings))][LocalizedAlias(nameof(Aliases.ModifyGuildSettings))]
		[Summary("Modify the given setting on the guild.")]
		[UserPermissionRequirement(GuildPermission.Administrator)]
		[EnabledByDefault(false)]
		public sealed class ModifyGuildSettings : AdvobotSettingsModuleBase<IGuildSettings>
		{
			protected override IGuildSettings Settings => Context.GuildSettings;

			[ImplicitCommand, ImplicitAlias]
			public Task Reset(string settingName)
				=> ResetAsync(settingName);
			[ImplicitCommand, ImplicitAlias]
			public Task Prefix([ValidatePrefix] string value)
				=> ModifyAsync(x => x.Prefix, value);
			[ImplicitCommand, ImplicitAlias]
			public Task NonVerboseErrors(bool value)
				=> ModifyAsync(x => x.NonVerboseErrors, value);
			[ImplicitCommand, ImplicitAlias]
			public Task ServerLogId([Optional, ValidateTextChannel(CPerm.ManageChannels)] SocketTextChannel value)
				=> ModifyAsync(x => x.ServerLogId, value?.Id ?? 0);
			[ImplicitCommand, ImplicitAlias]
			public Task ModLogId([Optional, ValidateTextChannel(CPerm.ManageChannels)] SocketTextChannel value)
				=> ModifyAsync(x => x.ModLogId, value?.Id ?? 0);
			[ImplicitCommand, ImplicitAlias]
			public Task ImageLogId([Optional, ValidateTextChannel(CPerm.ManageChannels)] SocketTextChannel value)
				=> ModifyAsync(x => x.ImageLogId, value?.Id ?? 0);
			[ImplicitCommand, ImplicitAlias]
			public Task MuteRoleId([NotEveryoneOrManaged] SocketRole value)
				=> ModifyAsync(x => x.MuteRoleId, value.Id);
			[ImplicitCommand, ImplicitAlias]
			public Task LogActions(bool add, params LogAction[] values)
				=> ModifyCollectionAsync(x => x.LogActions, add, values);
			[ImplicitCommand, ImplicitAlias]
			public Task ImageOnlyChannels(
				bool add,
				[ValidateTextChannel(CPerm.ManageChannels)] params SocketTextChannel[] values)
				=> ModifyCollectionAsync(x => x.ImageOnlyChannels, add, values.Select(x => x.Id));
			[ImplicitCommand, ImplicitAlias]
			public Task IgnoredLogChannels(
				bool add,
				[ValidateTextChannel(CPerm.ManageChannels)] params SocketTextChannel[] values)
				=> ModifyCollectionAsync(x => x.IgnoredLogChannels, add, values.Select(x => x.Id));
			[ImplicitCommand, ImplicitAlias]
			public Task IgnoredXpChannels(
				bool add,
				[ValidateTextChannel(CPerm.ManageChannels)] params SocketTextChannel[] values)
				=> ModifyCollectionAsync(x => x.IgnoredXpChannels, add, values.Select(x => x.Id));
			[ImplicitCommand, ImplicitAlias]
			public Task IgnoredCommandChannels(
				bool add,
				[ValidateTextChannel(CPerm.ManageChannels)] params SocketTextChannel[] values)
				=> ModifyCollectionAsync(x => x.IgnoredCommandChannels, add, values.Select(x => x.Id));

			[ImplicitCommand, ImplicitAlias]
			public Task Quotes(bool add, string name, [Optional, Remainder] string text)
				=> ModifyCollectionAsync(x => x.Quotes, add, new[] { new Quote(name, text ?? "") });

			[ImplicitCommand, ImplicitAlias]
			public Task BotUsers(
				bool add,
				IUser user,
				[Remainder, OverrideTypeReader(typeof(PermissionsTypeReader<GuildPermission>))] ulong permissions)
				=> ModifyCollectionValuesAsync(
					x => x.BotUsers,
					x => x.UserId == user.Id,
					() => new BotUser(user.Id),
					x =>
					{
						var modified = x.ModifyPermissions(add, Context.User, permissions);
						return $"Successfully {(add ? "removed" : "added")} the following bot permissions on `{user.Format()}`: `{modified}`.";
					});

			[ImplicitCommand, ImplicitAlias]
			public Task WelcomeMessage(
				[ValidateTextChannel(CPerm.ManageChannels, FromContext = true)] SocketTextChannel channel,
				[Remainder] GuildNotification args)
			{
				args.ChannelId = channel.Id;
				return ModifyAsync(x => x.WelcomeMessage, args);
			}
			[ImplicitCommand, ImplicitAlias]
			public Task GoodbyeMessage(
				[ValidateTextChannel(CPerm.ManageChannels, FromContext = true)] SocketTextChannel channel,
				[Remainder] GuildNotification args)
			{
				args.ChannelId = channel.Id;
				return ModifyAsync(x => x.GoodbyeMessage, args);
			}
		}*/

		/*
		[LocalizedGroup(nameof(Groups.ModifyCommands))]
		[LocalizedAlias(nameof(Aliases.ModifyCommands))]
		[LocalizedSummary(nameof(Summaries.ModifyCommands))]
		[Meta("6fb02198-9eab-4e44-a59a-7ba7f7317c10", IsEnabled = true, CanToggle = false)]
		[RequireGuildPermissions]
		public sealed class ModifyCommands : SettingsModule<IGuildSettings>
		{
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
			public IHelpEntryService HelpEntries { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

			protected override IGuildSettings Settings => Context.Settings;

			[LocalizedCommand(nameof(Groups.All))]
			[LocalizedAlias(nameof(Aliases.All))]
			public Task<RuntimeResult> All(bool enable)
			{
				var entries = HelpEntries.GetHelpEntries();
				var commands = Settings.CommandSettings.ModifyCommandValues(entries, enable);
				return Responses.ModifyCommands.ModifiedMultiple(commands, enable);
			}

			[Command]
			public Task<RuntimeResult> Command(
				[CommandCategory] string category,
				bool enable)
			{
				var entries = HelpEntries.GetHelpEntries(category);
				var commands = Settings.CommandSettings.ModifyCommandValues(entries, enable);
				return Responses.ModifyCommands.ModifiedMultiple(commands, enable);
			}

			[Command]
			public Task<RuntimeResult> Command(
				[CanToggle] IModuleHelpEntry command,
				bool enable)
			{
				if (Settings.CommandSettings.ModifyCommandValue(command, enable))
				{
					return Responses.ModifyCommands.Modified(command.Name, enable);
				}
				return Responses.ModifyCommands.Unmodified(command.Name, enable);
			}
		}

		[LocalizedGroup(nameof(Groups.ModifyIgnoredCommandChannels))]
		[LocalizedAlias(nameof(Aliases.ModifyIgnoredCommandChannels))]
		[LocalizedSummary(nameof(Summaries.ModifyIgnoredCommandChannels))]
		[Meta("e485777b-1b3f-411a-afd7-59f24858cd24", IsEnabled = true, CanToggle = false)]
		[RequireGuildPermissions]
		public sealed class ModifyIgnoredCommandChannels : SettingsModule<IGuildSettings>
		{
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
			public IHelpEntryService HelpEntries { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

			protected override IGuildSettings Settings => Context.Settings;
			//protected override string SettingName => nameof(IGuildSettings.IgnoredCommandChannels);

			/*
			[Command]
			public async Task Command(
				bool enable,
				[ValidateTextChannel(FromContext = true)] SocketTextChannel channel)
				=> await ModifyCollectionAsync(x => x.IgnoredCommandChannels, enable, channel.Id).CAF();*/
		/*
	[ImplicitCommand, ImplicitAlias]
	public Task Category(bool enable, [ValidateCommandCategory] string category, [ValidateTextChannel(FromContext = true)] SocketTextChannel channel)
	{
		throw new NotImplementedException();
		/*
		var commands = Settings.CommandSettings.ModifyOverrides(HelpEntries.GetHelpEntries(category), channel, enable);
		if (!commands.Any())
		{
			return ReplyErrorAsync($"`{category}` is already {(enable ? "unignored" : "ignored")} on `{channel.Format()}`.");
		}
		return ReplyTimedAsync($"Successfully {(enable ? "unignored" : "ignored")} `{commands.Join("`, `")}` on `{channel.Format()}`.");
	}
	[Command]
	public Task Command(bool enable, IHelpEntry helpEntry, [ValidateTextChannel(FromContext = true)] SocketTextChannel channel)
	{
		throw new NotImplementedException();
		/*
		if (!Settings.CommandSettings.ModifyOverride(helpEntry, channel, enable))
		{
			return ReplyErrorAsync($"`{helpEntry.Name}` is already {(enable ? "unignored" : "ignored")} on `{channel.Format()}`.");
		}
		return ReplyTimedAsync($"Successfully {(enable ? "unignored" : "ignored")} `{helpEntry.Name}` on `{channel.Format()}`.");
	}*/

		/*
		[Category(typeof(ModifyBotUsers)), Group(nameof(ModifyBotUsers)), TopLevelShortAlias(typeof(ModifyBotUsers))]
		[Summary("Gives a user permissions in the bot but not on Discord itself. " +
			"Type `" + nameof(ModifyBotUsers) + " [" + nameof(Show) + "]` to see the available permissions. " +
			"Type `" + nameof(ModifyBotUsers) + " [" + nameof(Show) + "] [User]` to see the permissions of that user.")]
		[PermissionRequirement(null, null)]
		[DefaultEnabled(false)]
		public sealed class ModifyBotUsers : AdvobotSettingsSavingModuleBase<IGuildSettings>
		{
			[ImplicitCommand]
			public async Task Show(IUser user)
			{
				var botUser = Context.GuildSettings.BotUsers.SingleOrDefault(x => x.UserId == user.Id);
				if (botUser == null || botUser.Permissions == 0)
				{
					var error = $"`{user.Format()}` has no extra permissions from the bot.");
					await MessageUtils.SendErrorMessageAsync(Context, error).CAF();
					return;
				}

				var embed = new EmbedWrapper
				{
					Title = $"Permissions for {user.Format()}",
					Description = $"`{string.Join("`, `", EnumUtils.GetFlagNames((GuildPermission)botUser.Permissions))}`"
				};
				await MessageUtils.SendMessageAsync(Context.Channel, null, embed).CAF();
			}
			[Command]
			public async Task Command(bool add, IUser user, [Remainder, OverrideTypeReader(typeof(GuildPermissionsTypeReader))] ulong permissions)
			{
				var botUser = Context.GuildSettings.BotUsers.SingleOrDefault(x => x.UserId == user.Id);
				if (add && botUser == null)
				{
					Context.GuildSettings.BotUsers.Add(botUser = new BotUser(user.Id, permissions));
				}
				if (!add && botUser == null)
				{
					var error = $"`{user.Format()}` does not have any bot permissions to remove");
					await MessageUtils.SendErrorMessageAsync(Context, error).CAF();
					return;
				}

				var modifiedPerms = string.Join("`, `", botUser.ModifyPermissions(add, (IGuildUser)Context.User, permissions));
				var resp = $"Successfully {(add ? "removed" : "added")} the following bot permissions on `{user.Format()}`: `{modifiedPerms}`.";
				await MessageUtils.MakeAndDeleteSecondaryMessageAsync(Context, resp).CAF();
			}

			protected override IGuildSettings GetSettings() => Context.GuildSettings;
		}*/

		/*
		[LocalizedGroup(nameof(Groups.ModifyPersistentRoles))][LocalizedAlias(nameof(Aliases.ModifyPersistentRoles))]
		[Summary("Gives a user a role that stays even when they leave and rejoin the server.")]
		[UserPermissionRequirement(GuildPermission.ManageRoles)]
		[EnabledByDefault(true)]
		public sealed class ModifyPersistentRoles : AdvobotSettingsModuleBase<IGuildSettings>
		{
			protected override IGuildSettings Settings => Context.GuildSettings;

			[Command, Priority(1)]
			public Task Command(bool add, [ValidateUser] SocketUser user, [ValidateRole] SocketRole role)
				=> CommandRunner(add, user.Id, role);
			[Command] //Should go into the above one if a valid user, so should be fine to not check this one for permission
			public Task Command(bool add, ulong userId, [ValidateRole] SocketRole role)
				=> CommandRunner(add, userId, role);

			private Task CommandRunner(bool add, ulong userId, SocketRole role)
			{
				if (Settings.PersistentRoles.TryGetSingle(x => x.UserId == userId && x.RoleId == role.Id, out var match) == add)
				{
					return ReplyErrorAsync($"{(add ? "A" : "No")} persistent role exists for `{userId}` with `{role.Format()}`.");
				}

				if (add)
				{
					Settings.PersistentRoles.Add(new PersistentRole(userId, role));
				}
				else
				{
					Settings.PersistentRoles.Remove(match);
				}

				var action = add ? "added a" : "removed the";
				return ReplyTimedAsync($"Successfully {action} persistent role for `{userId}` with `{role.Format()}`.");
			}
		}*/

		/* Implemented by editing the image only list
		[Category(typeof(ModifyChannelSettings)), Group(nameof(ModifyChannelSettings)), TopLevelShortAlias(typeof(ModifyChannelSettings))]
		[Summary("Image only works solely on attachments. " +
			"Using the command on an already targetted channel turns it off.")]
		[UserPermissionRequirement(GuildPermission.ManageChannels }, null)]
		[DefaultEnabled(false)]
		[SaveGuildSettings]
		public sealed class ModifyChannelSettings : AdvobotModuleBase
		{
			[ImplicitCommand]
			public async Task ImageOnly([ValidateObject(Verif.CanBeEdited, IfNullCheckFromContext = true)] ITextChannel channel)
			{
				if (Context.GuildSettings.ImageOnlyChannels.Contains(channel.Id))
				{
					Context.GuildSettings.ImageOnlyChannels.Remove(channel.Id);
					var resp = $"Successfully removed the channel `{channel.Format()}` from the image only list.";
					await MessageUtils.MakeAndDeleteSecondaryMessageAsync(Context, resp).CAF();
				}
				else
				{
					Context.GuildSettings.ImageOnlyChannels.Add(channel.Id);
					var resp = $"Successfully added the channel `{channel.Format()}` to the image only list.";
					await MessageUtils.MakeAndDeleteSecondaryMessageAsync(Context, resp).CAF();
				}
			}
		}
	}*/
	}
}