﻿using Advobot.Classes;
using Advobot.Classes.BannedPhrases;
using Advobot.Classes.Permissions;
using Advobot.Classes.SpamPrevention;
using Advobot.Enums;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Advobot.Interfaces
{
	/// <summary>
	/// Holds guild settings and some readonly information.
	/// </summary>
	public interface IGuildSettings
	{
		//Saved settings
		List<BotImplementedPermissions> BotUsers { get; set; }
		List<SelfAssignableGroup> SelfAssignableGroups { get; set; }
		List<Quote> Quotes { get; set; }
		List<LogAction> LogActions { get; set; }
		List<ulong> IgnoredCommandChannels { get; set; }
		List<ulong> IgnoredLogChannels { get; set; }
		List<ulong> ImageOnlyChannels { get; set; }
		List<BannedPhrase> BannedPhraseStrings { get; set; }
		List<BannedPhrase> BannedPhraseRegex { get; set; }
		List<BannedPhrase> BannedNamesForJoiningUsers { get; set; }
		List<BannedPhrasePunishment> BannedPhrasePunishments { get; set; }
		List<CommandSwitch> CommandSwitches { get; set; }
		List<CommandOverride> CommandsDisabledOnUser { get; set; }
		List<CommandOverride> CommandsDisabledOnRole { get; set; }
		List<CommandOverride> CommandsDisabledOnChannel { get; set; }
		List<PersistentRole> PersistentRoles { get; set; }
		ITextChannel ServerLog { get; set; }
		ITextChannel ModLog { get; set; }
		ITextChannel ImageLog { get; set; }
		IRole MuteRole { get; set; }
		Dictionary<SpamType, SpamPreventionInfo> SpamPreventionDictionary { get; set; }
		Dictionary<RaidType, RaidPreventionInfo> RaidPreventionDictionary { get; set; }
		GuildNotification WelcomeMessage { get; set; }
		GuildNotification GoodbyeMessage { get; set; }
		ListedInvite ListedInvite { get; set; }
		Slowmode Slowmode { get; set; }
		string Prefix { get; set; }
		bool VerboseErrors { get; set; }

		//Non-saved settings
		List<BannedPhraseUser> BannedPhraseUsers { get; }
		List<SpamPreventionUser> SpamPreventionUsers { get; }
		List<CachedInvite> Invites { get; }
		List<string> EvaluatedRegex { get; }
		MessageDeletion MessageDeletion { get; }
		SocketGuild Guild { get; }
		bool Loaded { get; }

		/// <summary>
		/// Returns commands from guildsettings that are in a specific category.
		/// </summary>
		/// <param name="guildSettings"></param>
		/// <param name="category"></param>
		/// <returns></returns>
		CommandSwitch[] GetCommands(CommandCategory category);
		/// <summary>
		/// Returns a command from guildsettings with the passed in command name/alias.
		/// </summary>
		/// <param name="guildSettings"></param>
		/// <param name="commandNameOrAlias"></param>
		/// <returns></returns>
		CommandSwitch GetCommand(string name);
		/// <summary>
		/// Sets the specified log type channel to the passed in channel.
		/// </summary>
		/// <param name="logChannelType"></param>
		/// <param name="channel"></param>
		bool SetLogChannel(LogChannelType type, ITextChannel channel);
		/// <summary>
		/// Removes the specified log type's channel.
		/// </summary>
		/// <param name="logChannelType"></param>
		bool RemoveLogChannel(LogChannelType type);

		/// <summary>
		/// Saves the settings to a JSON file.
		/// </summary>
		void SaveSettings();
		/// <summary>
		/// Removes some potential null values and sets channels/roles for some settings.
		/// </summary>
		/// <param name="guild"></param>
		/// <returns></returns>
		Task<IGuildSettings> PostDeserialize(IGuild guild);

		/// <summary>
		/// Returns a string of all the guild's settings in human readable format.
		/// </summary>
		/// <returns></returns>
		string ToString();
		/// <summary>
		/// Returns a string of a guild setting in human readable format.
		/// </summary>
		/// <param name="property"></param>
		/// <returns></returns>
		string ToString(PropertyInfo property);
	}
}