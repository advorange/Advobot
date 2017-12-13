﻿using Advobot.Core.Actions;
using Advobot.Core.Actions.Formatting;
using Advobot.Core.Enums;
using Advobot.Core.Interfaces;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Advobot.Core.Classes.Settings
{
	/// <summary>
	/// Holds settings for a guild. Settings are only saved by calling <see cref="SaveSettings"/>.
	/// </summary>
	public partial class GuildSettings : IGuildSettings
	{
		/// <summary>
		/// Returns all public properties that have a set method.
		/// </summary>
		/// <returns></returns>
		public static PropertyInfo[] GetSettings() => typeof(IGuildSettings)
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(x => x.CanWrite && x.GetSetMethod(true).IsPublic).ToArray();

		public CommandSwitch[] GetCommands(CommandCategory category)
			=> CommandSwitches.Where(x => x.Category == category).ToArray();
		public CommandSwitch GetCommand(string commandNameOrAlias)
			=> CommandSwitches.FirstOrDefault(x =>
			{
				return x.Name.CaseInsEquals(commandNameOrAlias) || x.Aliases != null && x.Aliases.CaseInsContains(commandNameOrAlias);
			});
		public bool SetLogChannel(LogChannelType logChannelType, ITextChannel channel)
		{
			switch (logChannelType)
			{
				case LogChannelType.Server:
				{
					if (_ServerLogId == channel.Id)
					{
						return false;
					}

					ServerLog = channel;
					return true;
				}
				case LogChannelType.Mod:
				{
					if (_ModLogId == channel.Id)
					{
						return false;
					}

					ModLog = channel;
					return true;
				}
				case LogChannelType.Image:
				{
					if (_ImageLogId == channel.Id)
					{
						return false;
					}

					ImageLog = channel;
					return true;
				}
				default:
				{
					throw new ArgumentException("Invalid channel type supplied.");
				}
			}
		}
		public bool RemoveLogChannel(LogChannelType logChannelType)
		{
			switch (logChannelType)
			{
				case LogChannelType.Server:
				{
					if (_ServerLogId == 0)
					{
						return false;
					}

					ServerLog = null;
					return true;
				}
				case LogChannelType.Mod:
				{
					if (_ModLogId == 0)
					{
						return false;
					}

					ModLog = null;
					return true;
				}
				case LogChannelType.Image:
				{
					if (_ImageLogId == 0)
					{
						return false;
					}

					ImageLog = null;
					return true;
				}
				default:
				{
					throw new ArgumentException("Invalid channel type supplied.");
				}
			}

		}
		public string GetPrefix(IBotSettings botSettings)
			=> String.IsNullOrWhiteSpace(Prefix) ? botSettings.Prefix : Prefix;

		public void SaveSettings()
		{
			if (Guild != null)
			{
				IOActions.OverWriteFile(IOActions.GetServerDirectoryFile(Guild.Id, Constants.GUILD_SETTINGS_LOCATION), IOActions.Serialize(this));
			}
		}
		public async Task<IGuildSettings> PostDeserialize(IGuild guild)
		{
			Guild = guild as SocketGuild;

			//Add in the default values for commands that aren't set
			var unsetCmds = Constants.HELP_ENTRIES.GetUnsetCommands(CommandSwitches.Select(x => x.Name));
			CommandSwitches.AddRange(unsetCmds.Select(x => new CommandSwitch(x.Name, x.DefaultEnabled)));
			//Remove all that have no name/aren't commands anymore
			CommandSwitches.RemoveAll(x => String.IsNullOrWhiteSpace(x.Name) || Constants.HELP_ENTRIES[x.Name] == null);
			CommandsDisabledOnUser.RemoveAll(x => String.IsNullOrWhiteSpace(x.Name));
			CommandsDisabledOnRole.RemoveAll(x => String.IsNullOrWhiteSpace(x.Name));
			CommandsDisabledOnChannel.RemoveAll(x => String.IsNullOrWhiteSpace(x.Name));
			Invites.AddRange((await InviteActions.GetInvitesAsync(guild).CAF()).Select(x => new CachedInvite(x.Code, x.Uses)));

			if (_ListedInvite != null)
			{
				_ListedInvite.PostDeserialize(Guild);
			}
			if (_WelcomeMessage != null)
			{
				_WelcomeMessage.PostDeserialize(Guild);
			}
			if (_GoodbyeMessage != null)
			{
				_GoodbyeMessage.PostDeserialize(Guild);
			}
			if (_SelfAssignableGroups != null)
			{
				foreach (var group in _SelfAssignableGroups)
				{
					group.Roles.RemoveAll(x => x == null || x.GetRole(Guild) == null);
				}
			}
			if (_PersistentRoles != null)
			{
				_PersistentRoles.RemoveAll(x => x.GetRole(Guild) == null);
			}

			Loaded = true;
			return this;
		}

		public string Format()
		{
			var sb = new StringBuilder();
			foreach (var property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				//Only get public editable properties
				if (property.GetGetMethod() == null || property.GetSetMethod() == null)
				{
					continue;
				}

				var formatted = Format(property);
				if (String.IsNullOrWhiteSpace(formatted))
				{
					continue;
				}

				sb.AppendLineFeed($"**{property.Name}**:");
				sb.AppendLineFeed($"{formatted}");
				sb.AppendLineFeed("");
			}
			return sb.ToString();
		}
		public string Format(PropertyInfo property) => FormatObject(property.GetValue(this));
		private string FormatObject(object value)
		{
			if (value == null)
			{
				return "`Nothing`";
			}
			else if (value is ISetting tempISetting)
			{
				return tempISetting.ToString();
			}
			else if (value is ulong tempUlong)
			{
				var chan = Guild.GetChannel(tempUlong);
				if (chan != null)
				{
					return $"`{chan.FormatChannel()}`";
				}

				var role = Guild.GetRole(tempUlong);
				if (role != null)
				{
					return $"`{role.FormatRole()}`";
				}

				var user = Guild.GetUser(tempUlong);
				if (user != null)
				{
					return $"`{user.FormatUser()}`";
				}

				return tempUlong.ToString();
			}
			//Because strings are char[] this has to be here so it doesn't go into IEnumerable
			else if (value is string tempStr)
			{
				return String.IsNullOrWhiteSpace(tempStr) ? "`Nothing`" : $"`{tempStr}`";
			}
			//Has to be above IEnumerable too
			else if (value is IDictionary tempIDictionary)
			{
				var validKeys = tempIDictionary.Keys.Cast<object>().Where(x => tempIDictionary[x] != null);
				return String.Join("\n", validKeys.Select(x =>
				{
					return $"{FormatObject(x)}: {FormatObject(tempIDictionary[x])}";
				}));
			}
			else if (value is IEnumerable tempIEnumerable)
			{
				return String.Join("\n", tempIEnumerable.Cast<object>().Select(x => FormatObject(x)));
			}
			else
			{
				return $"`{value.ToString()}`";
			}
		}
	}
}
