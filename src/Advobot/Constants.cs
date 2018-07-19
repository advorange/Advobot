﻿using System.Reflection;
using Discord;

namespace Advobot
{
	/// <summary>
	/// Global values expected to stay the same.
	/// </summary>
	public static class Constants
	{
		//Regex for checking any awaits are non ConfigureAwait(false): ^(?!.*CAF\(\)).*await.*$
		/// <summary>
		/// The bot's version.
		/// </summary>
		public const string BOT_VERSION = Version.VERSION_NUMBER;
		/// <summary>
		/// The Discord api wrapper version.
		/// </summary>
		public static readonly string API_VERSION = Assembly.GetAssembly(typeof(IDiscordClient)).GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
		/// <summary>
		/// Placeholder prefix for easy replacement.
		/// </summary>
		public const string PLACEHOLDER_PREFIX = "%PREFIX%";
		/// <summary>
		/// The invite to the Discord server.
		/// </summary>
		public const string DISCORD_INV = "https://discord.gg/MBXypxb"; //Switched from /xd to this invite since no matter what this inv will link to my server and never someone else's server
		/// <summary>
		/// The repository of the bot.
		/// </summary>
		public const string REPO = "https://github.com/advorange/Advobot";
		/// <summary>
		/// Partnered server feature of additional voice servers.
		/// </summary>
		public const string VIP_REGIONS = "VIP_REGIONS";
		/// <summary>
		/// Partnered server feature of custom url.
		/// </summary>
		public const string VANITY_URL = "VANITY_URL";
		/// <summary>
		/// Partnered server feature of image that pops up when you join a server.
		/// </summary>
		public const string INVITE_SPLASH = "INVITE_SPLASH";
	}
}