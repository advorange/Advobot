﻿using System;
using System.Collections.Generic;

namespace Advobot
{
	public static class Constants
	{
		public const String BOT_PREFIX = ">>";
		public const String IGNORE_ERROR = "Cx";
		public const String ZERO_LENGTH_CHAR = "\u180E";
		public const String ERROR_MESSAGE = "**ERROR:** ";
		public const String ARGUMENTS_ERROR = "Invalid number of arguments.";
		public const String USER_ERROR = "Invalid user.";
		public const String ROLE_ERROR = "Invalid role.";
		public const String CHANNEL_ERROR = "Invalid channel.";
		public const String MUTE_ROLE_NAME = "Muted";
		public const String BASE_CHANNEL_NAME = "advobot";
		public const String PREFERENCES_FILE = "commandPreferences.txt";
		public const String BAN_REFERENCE_FILE = "banReferences.txt";
		public const String SERVERLOG_AND_MODLOG = "serverlogAndModlog.txt";
		public const String SERVER_LOG_CHECK_STRING = "Serverlog:";
		public const String MOD_LOG_CHECK_STRING = "Modlog:";
		public const String CHANNEL_INSTRUCTIONS = "[#Channel|[Channel/[Text|Voice]]]";
		public const String CHANNEL_PERMISSIONS_ERROR = "You do not have the ability to edit that channel.";
		public const String VOICE_TYPE = "voice";
		public const String TEXT_TYPE = "text";

		public const UInt64 OWNER_ID = 172138437246320640;
		public const Int32 WAIT_TIME = 3000;
		public const int MEMBER_LIMIT = 0;
		public const int MESSAGES_TO_GATHER = 100;
		public const int TIME_FOR_WAIT_BETWEEN_DELETING_MESSAGES_UNTIL_THEY_PRINT_TO_THE_SERVER_LOG = 1;
		public const int NICKNAME_LENGTH = 32;
		public const int TOPIC_LENGTH = 1024;
		public const int OWNER_POSITION = 9001;

		public const bool DISCONNECT = false;
	}

	public static class Variables
	{
		public static String SetGame_Error;

		public static UInt64 Key = (ulong)new Random().Next(0, 10000000);
		public static DateTime StartupTime = DateTime.UtcNow;
		public static int TotalUsers = 0;
		public static int TotalGuilds = 0;
		public static int FailedCommands = 0;
		public static int AttemptedCommands = 0;
		public static int LoggedJoins = 0;
		public static int LoggedLeaves = 0;
		public static int LoggedBans = 0;
		public static int LoggedUnbans = 0;
		public static int LoggedUserChanges = 0;
		public static int LoggedEdits = 0;
		public static int LoggedDeletes = 0;

		public static Dictionary<ulong, List<PreferenceCategory>> mCommandPreferences = new Dictionary<ulong, List<PreferenceCategory>>();
		public static Dictionary<ulong, Dictionary<ulong, String>> mBanList = new Dictionary<ulong, Dictionary<ulong, String>>();
		public static Dictionary<ulong, List<String>> mDeletedMessages = new Dictionary<ulong, List<String>>();
		public static Dictionary<ulong, System.Threading.CancellationTokenSource> mCancelTokens = new Dictionary<ulong, System.Threading.CancellationTokenSource>();
		public static Dictionary<String, int> mInviteLinks = new Dictionary<String, int>();
		public static Dictionary<String, int> mPermissionValues = new Dictionary<String, int>();
		public static Dictionary<ulong, Dictionary<String, bool>> mOwnerCommands = new Dictionary<ulong, Dictionary<string, bool>>();
		public static List<String> mPermissionNames = new List<String>();
		public static List<String> mCommandNames = new List<String>();
		public static List<Discord.IGuild> mGuilds = new List<Discord.IGuild>();
		public static List<HelpEntry> HelpList = new List<HelpEntry>();
	}
}