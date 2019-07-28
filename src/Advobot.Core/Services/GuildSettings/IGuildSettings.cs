﻿using System.Collections.Generic;
using Advobot.Classes.UserInformation;
using Advobot.Enums;
using Advobot.Interfaces;
using Advobot.Services.GuildSettings.Settings;

namespace Advobot.Services.GuildSettings
{
	/// <summary>
	/// Holds guild settings.
	/// </summary>
	public interface IGuildSettings : ISettingsBase
	{
		/// <summary>
		/// Message to display when a user joins the guild.
		/// </summary>
		GuildNotification? WelcomeMessage { get; set; }
		/// <summary>
		/// Message to display when a user leaves the guild.
		/// </summary>
		GuildNotification? GoodbyeMessage { get; set; }
		/// <summary>
		/// The culture to use for the bot's responses.
		/// </summary>
		string Culture { get; set; }
		/// <summary>
		/// The prefix to use for the guild. If this is null, the bot prefix will be used.
		/// </summary>
		string? Prefix { get; set; }
		/// <summary>
		/// The id for the server log.
		/// </summary>
		ulong ServerLogId { get; set; }
		/// <summary>
		/// The id for the mod log.
		/// </summary>
		ulong ModLogId { get; set; }
		/// <summary>
		/// The id for the image log.
		/// </summary>
		ulong ImageLogId { get; set; }
		/// <summary>
		/// The id for the mute role.
		/// </summary>
		ulong MuteRoleId { get; set; }
		/// <summary>
		/// Whether or not errors in commands should be printed to the server.
		/// </summary>
		bool NonVerboseErrors { get; set; }
		/// <summary>
		/// To limit spam.
		/// </summary>
		IList<SpamPrev> SpamPrevention { get; }
		/// <summary>
		/// To limit raids.
		/// </summary>
		IList<RaidPrev> RaidPrevention { get; }
		/// <summary>
		/// Roles that persist across a user leaving and rejoining.
		/// </summary>
		IList<PersistentRole> PersistentRoles { get; }
		/// <summary>
		/// Permissions given through the bot and not Discord itself.
		/// </summary>
		IList<BotUser> BotUsers { get; }
		/// <summary>
		/// Roles users can assign themselves.
		/// </summary>
		IList<SelfAssignableRoles> SelfAssignableGroups { get; }
		/// <summary>
		/// Quotes which can be called up through their name.
		/// </summary>
		IList<Quote> Quotes { get; }
		/// <summary>
		/// Actions which get logged. Users joining, leaving, deleting messages, etc.
		/// </summary>
		IList<LogAction> LogActions { get; }
		/// <summary>
		/// Channels ignored from commands.
		/// </summary>
		IList<ulong> IgnoredCommandChannels { get; }
		/// <summary>
		/// Channels ignored from the log.
		/// </summary>
		IList<ulong> IgnoredLogChannels { get; }
		/// <summary>
		/// Channels ignored from gaining xp in.
		/// </summary>
		IList<ulong> IgnoredXpChannels { get; }
		/// <summary>
		/// Channels which have messages deleted in them unless they have an image attached.
		/// </summary>
		IList<ulong> ImageOnlyChannels { get; }
		/// <summary>
		/// Deletes messages and punishes users if the strings are found in their messages.
		/// </summary>
		IList<BannedPhrase> BannedPhraseStrings { get; }
		/// <summary>
		/// Deletes messages and punishes users if the patterns are found in their messages.
		/// </summary>
		IList<BannedPhrase> BannedPhraseRegex { get; }
		/// <summary>
		/// Banned names for joining users.
		/// </summary>
		IList<BannedPhrase> BannedPhraseNames { get; }
		/// <summary>
		/// Punishments to give when thresholds are reached with banned strings/regex.
		/// </summary>
		IList<BannedPhrasePunishment> BannedPhrasePunishments { get; }
		/// <summary>
		/// List of rules for easy formatting.
		/// </summary>
		RuleHolder Rules { get; }
		/// <summary>
		/// Settings for commands. Which ones are enabled, disabled, for specific roles/users/channel/guild.
		/// </summary>
		CommandSettings CommandSettings { get; }

		/// <summary>
		/// Users which have been affected by banned phrases.
		/// </summary>
		IList<BannedPhraseUserInfo> GetBannedPhraseUsers();
		/// <summary>
		/// Cached invites holding uses.
		/// </summary>
		InviteCache GetInviteCache();
		/// <summary>
		/// Holds messages which have been deleted and waits to print them out.
		/// </summary>
		DeletedMessageCache GetDeletedMessageCache();
	}
}