﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.Modules;
using Discord.WebSocket;
using System.Net;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Advobot
{
	public class Actions
	{
		//Loading in all necessary information at bot start up
		public static void loadInformation()
		{
			loadPermissionNames();													//Gets the name of the permission bits in Discord
			//Has to go after loadPermissionNames
			loadCommandInformation();												//Gets the information of a command (name, aliases, usage, summary)
			//Has to go after loadCommandInformation
			Variables.HelpList.ForEach(x => Variables.mCommandNames.Add(x.Name));   //Gets all the active command names
		}

		//Get the information from the commands
		public static void loadCommandInformation()
		{
			var classTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes()).Where(type => type.IsSubclassOf(typeof(ModuleBase)));
			foreach (var classType in classTypes)
			{
				List<MethodInfo> methods = classType.GetMethods(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic).ToList();
				foreach (var method in methods)
				{
					String name = "N/A";
					String[] aliases = { "N/A" };
					String usage = "N/A";
					String basePerm = "N/A";
					String text = "N/A";
					//Console.WriteLine(classType.Name + "." + method.Name);
					{
						CommandAttribute attr = (CommandAttribute)method.GetCustomAttribute(typeof(CommandAttribute));
						if (null != attr)
						{
							//Console.WriteLine(classType.Name + "." + method.Name + ": " + attr.Text);
							name = attr.Text;
						}
						else
						{
							continue;
						}
					}
					{
						AliasAttribute attr = (AliasAttribute)method.GetCustomAttribute(typeof(AliasAttribute));
						if (null != attr)
						{
							//Console.WriteLine(classType.Name + "." + method.Name + ": " + attr.Text);
							aliases = attr.Aliases;
						}
					}
					{
						UsageAttribute attr = (UsageAttribute)method.GetCustomAttribute(typeof(UsageAttribute));
						if (null != attr)
						{
							//Console.WriteLine(classType.Name + "." + method.Name + ": " + attr.Text);
							usage = attr.Text;
						}
					}
					{
						PermissionRequirementsAttribute attr = (PermissionRequirementsAttribute)method.GetCustomAttribute(typeof(PermissionRequirementsAttribute));
						if (null != attr)
						{
							//Console.WriteLine(classType.Name + "." + method.Name + ": " + attr.Text);
							basePerm = attr.Text;
						}
					}
					{
						SummaryAttribute attr = (SummaryAttribute)method.GetCustomAttribute(typeof(SummaryAttribute));
						if (null != attr)
						{
							//Console.WriteLine(classType.Name + "." + method.Name + ": " + attr.Text);
							text = attr.Text;
						}
					}
					Variables.HelpList.Add(new HelpEntry(name, aliases, usage, basePerm, text));
				}
			}
		}

		//Get the permission names to an array
		public static String[] getPermissionNames(uint flags)
		{
			List<String> result = new List<String>();
			for (int i = 0; i < 32; ++i)
			{
				if ((flags & (1 << i)) != 0)
				{
					result.Add(Variables.mPermissionNames[i]);
				}
			}
			return result.ToArray();
		}

		//Find the permission names
		public static void loadPermissionNames()
		{
			for (int i = 0; i < 32; ++i)
			{
				String name = "";
				try
				{
					name = Enum.GetName(typeof(GuildPermission), (GuildPermission)i);
				}
				catch (Exception)
				{
					Console.WriteLine("Bad enum for GuildPermission: " + i);
				}
				Variables.mPermissionNames.Add(name);
			}
		}

		//Find a role on the server
		public static IRole getRole(IGuild guild, String roleName)
		{
			List<IRole> roles = guild.Roles.ToList();
			foreach (IRole role in roles)
			{
				if (role.Name.Equals(roleName))
				{
					return role;
				}
			}
			return null;
		}

		//Create a role on the server if it's not found
		public static async Task<IRole> createRoleIfNotFound(IGuild guild, String roleName)
		{
			if (getRole(guild, roleName) == null)
			{
				IRole role = await guild.CreateRoleAsync(roleName);
				return role;
			}
			return getRole(guild, roleName);
		} 

		//Get top position of a user
		public static int getPosition(IGuild guild, IGuildUser user)
		{
			int position = 0;
			user.RoleIds.ToList().ForEach(x => position = Math.Max(position, guild.GetRole(x).Position));
			return position;
		}

		//Get a user
		public static async Task<IGuildUser> getUser(IGuild guild, String userName)
		{
			IGuildUser user = await guild.GetUserAsync(getUlong(userName.Trim(new char[] { '<', '>', '@', '!' })));
			return user;
		}

		//Convert the input to a ulong
		public static ulong getUlong(String inputString)
		{
			ulong number = 0;
			if (UInt64.TryParse(inputString, out number))
			{
				return number;
			}
			return 0;
		}

		//Give the user the role
		public static async Task giveRole(IGuildUser user, IRole role)
		{
			if (null == role)
				return;
			await user.AddRolesAsync(role);
		}

		public static async Task<IRole> getRoleEditAbility(IGuild guild, IMessageChannel channel, IUserMessage message, IGuildUser user, IGuildUser bot, String input)
		{
			//Check if valid role
			IRole inputRole = getRole(guild, input);
			if (inputRole == null)
			{
				await makeAndDeleteSecondaryMessage(channel, message, ERROR(Constants.ROLE_ERROR), Constants.WAIT_TIME);
				return null;
			}

			//Determine if the user can edit the role
			if ((guild.OwnerId == user.Id ? Constants.OWNER_POSITION : getPosition(guild, user)) <= inputRole.Position)
			{
				await makeAndDeleteSecondaryMessage(channel, message, 
					ERROR(String.Format("`{0}` has a higher position than you are allowed to edit or use.", inputRole.Name)), Constants.WAIT_TIME);
				return null;
			}

			//Determine if the bot can edit the role
			if (getPosition(guild, bot) <= inputRole.Position)
			{
				await makeAndDeleteSecondaryMessage(channel, message, 
					ERROR(String.Format("`{0}` has a higher position than the bot is allowed to edit or use.", inputRole.Name)), Constants.WAIT_TIME);
				return null;
			}

			return inputRole;
		}

		//Remove secondary messages
		public static async Task makeAndDeleteSecondaryMessage(IMessageChannel channel, IUserMessage curMsg, String secondStr, Int32 time)
		{
			IUserMessage secondMsg = await channel.SendMessageAsync(Constants.ZERO_LENGTH_CHAR + secondStr);
			removeCommandMessages(channel, new IUserMessage[] { secondMsg, curMsg }, time);
		}

		//Remove commands
		public static void removeCommandMessages(IMessageChannel channel, IUserMessage[] messages, Int32 time)
		{
			Task t = Task.Run(async () =>
			{
				Thread.Sleep(time);
				await channel.DeleteMessagesAsync(messages);
			});
		}

		//Format the error message
		public static String ERROR(String message)
		{
			return Constants.ZERO_LENGTH_CHAR + Constants.ERROR_MESSAGE + " " + message;
		}

		//Send a message with a zero length char at the front
		public static async Task<IMessage> sendChannelMessage(IMessageChannel channel, String message)
		{
			return await channel.SendMessageAsync(Constants.ZERO_LENGTH_CHAR + message);
		}

		//Remove messages
		public static async Task removeMessages(IMessageChannel channel, int requestCount)
		{
			//To remove the command itself
			++requestCount;

			while (requestCount > 0)
			{
				using (var enumerator = channel.GetMessagesAsync(requestCount).GetEnumerator())
				{
					while (await enumerator.MoveNext())
					{
						var messages = enumerator.Current;
						if (messages.Count == 0)
							continue;
						await channel.DeleteMessagesAsync(messages);
						requestCount -= messages.Count;
					}
				}
			}
		}

		//Remove messages given a user id
		public static async Task removeMessages(IMessageChannel channel, int requestCount, IUser user)
		{
			//Make sure there's a user id
			if (null == user)
			{
				await removeMessages(channel, requestCount);
				return;
			}

			Console.WriteLine(String.Format("Deleting {0} messages.", requestCount));
			List<IMessage> allMessages = new List<IMessage>();
			using (var enumerator = channel.GetMessagesAsync(Constants.MESSAGES_TO_GATHER).GetEnumerator())
			{
				while (await enumerator.MoveNext())
				{
					var messages = enumerator.Current;
					if (messages.Count == 0)
						continue;
					allMessages.AddRange(messages);
				}
			}

			//Get valid amount of messages to delete
			List<IMessage> userMessages = allMessages.Where(x => user == x.Author).ToList();
			if (requestCount > userMessages.Count)
			{
				requestCount = userMessages.Count;
			}
			else if (requestCount < userMessages.Count)
			{
				userMessages.RemoveRange(requestCount, userMessages.Count - requestCount);
			}
			userMessages.Insert(0, allMessages[0]); //Remove the initial command message

			Console.WriteLine(String.Format("Found {0} messages; deleting {1} from user {2}", allMessages.Count, userMessages.Count - 1, user.Username));
			await channel.DeleteMessagesAsync(userMessages.ToArray());
		}

		//Get a channel ID
		public static async Task<IMessageChannel> getChannelID(IGuild guild, String channelName)
		{
			IMessageChannel channel = null;
			ulong channelID = 0;
			if (UInt64.TryParse(channelName.Trim(new char[] { '<', '>', '#' }), out channelID))
			{
				channel = (IMessageChannel)await guild.GetChannelAsync(channelID);
			}
			return channel;
		}

		//Get a channel
		public static async Task<IGuildChannel> getChannel(IGuild guild, String input)
		{
			String[] values = input.Trim().Split(new char[] { '/' }, 2);

			//Get input channel type
			String channelType = values.Length == 2 ? values[1].ToLower() : null;
			if (null != channelType && !(channelType.Equals(Constants.TEXT_TYPE) || channelType.Equals(Constants.VOICE_TYPE)))
			{
				return null;
			}

			//If a channel mention
			IGuildChannel channel = null;
			String channelIDString = values[0].Trim(new char[] { '<', '#', '>' });
			ulong channelID = 0;
			if (UInt64.TryParse(channelIDString, out channelID))
			{
				channel = await guild.GetChannelAsync(channelID);
			}
			//Name and type
			else if (channelType != null)
			{
				IReadOnlyCollection<IGuildChannel> gottenChannels = await guild.GetChannelsAsync();
				channel = gottenChannels.FirstOrDefault(x => x.Name.Equals(values[0], StringComparison.OrdinalIgnoreCase) && x.GetType().Name.ToLower().Contains(channelType));
			}

			return channel;
		}

		//Get integer
		public static int getInteger(String inputString)
		{
			int number = 0;
			if (Int32.TryParse(inputString, out number))
			{
				return number;
			}
			return -1;
		}

		//Get server commands
		public static String[] getCommands(IGuild guild, int number)
		{
			List<PreferenceCategory> categories;
			if (!Variables.mCommandPreferences.TryGetValue(guild.Id, out categories))
			{
				return null;
			}

			List<string> commands = new List<string>();
			foreach (PreferenceSetting command in categories[number].mSettings)
			{
				commands.Add(command.mName.ToString());
			}
			return commands.ToArray();
		}

		//Load preferences
		public static void loadPreferences(IGuild guild)
		{
			List<PreferenceCategory> categories;
			if (Variables.mCommandPreferences.TryGetValue(guild.Id, out categories))
			{
				return;
			}

			categories = new List<PreferenceCategory>();
			Variables.mCommandPreferences[guild.Id] = categories;

			String path = getServerFilePath(guild.Id, Constants.PREFERENCES_FILE);
			if (!System.IO.File.Exists(path))
			{
				path = "DefaultCommandPreferences.txt";
			}

			using (System.IO.StreamReader file = new System.IO.StreamReader(path))
			{
				Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name + ": preferences for the server " + guild.Name + " have been loaded.");
				//Read the preferences document for information
				String line;
				while ((line = file.ReadLine()) != null)
				{
					//If the line is empty, do nothing
					if (String.IsNullOrWhiteSpace(line))
					{
						continue;
					}
					//If the line starts with an @ then it's a category
					if (line.StartsWith("@"))
					{
						categories.Add(new PreferenceCategory(line.Substring(1)));
					}
					//Anything else and it's a setting
					else
					{
						//Split before and after the colon, before is the setting name, after is the value
						String[] values = line.Split(new char[] { ':' }, 2);
						if (values.Length == 2)
						{
							categories[categories.Count - 1].mSettings.Add(new PreferenceSetting(values[0], values[1]));
						}
						else
						{
							Console.WriteLine("ERROR: " + line);
						}
					}
				}
			}
		}

		//Get file paths
		public static String getServerFilePath(ulong serverId, String fileName)
		{
			//Gets the appdata folder for usage, allowed to change
			String folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			//Combines the path for appdata and the preferences text file, allowed to change, but I'd recommend to keep the serverID part
			String directory = System.IO.Path.Combine(folder, "Discord_Servers", serverId.ToString());
			//This string will be similar to C:\Users\User\AppData\Roaming\ServerID
			String path = System.IO.Path.Combine(directory, fileName);
			return path;
		}

		//Load bans
		public static void loadBans(IGuild guild)
		{
			Dictionary<ulong, String> banList = null;
			if (Variables.mBanList.TryGetValue(guild.Id, out banList))
			{
				return;
			}

			banList = new Dictionary<ulong, String>();
			Variables.mBanList[guild.Id] = banList;

			String path = getServerFilePath(guild.Id, Constants.BAN_REFERENCE_FILE);
			if (!System.IO.File.Exists(path))
			{
				return;
			}

			using (System.IO.StreamReader file = new System.IO.StreamReader(path))
			{
				Console.WriteLine(String.Format("{0}: bans for the server {1} have been loaded.", System.Reflection.MethodBase.GetCurrentMethod().Name, guild.Name));
				//Read the bans document for information
				String line;
				while ((line = file.ReadLine()) != null)
				{
					//If the line is empty, do nothing
					if (String.IsNullOrWhiteSpace(line))
					{
						continue;
					}
					//Split before and after the colon, before is the userID, after is the username and discriminator
					String[] values = line.Split(new char[] { ':' }, 2);
					if (values.Length == 2)
					{
						ulong userID = getUlong(values[0]);
						if (userID == 0)
						{
							continue;
						}
						banList[userID] = values[1];
					}
					else
					{
						Console.WriteLine("ERROR: " + line);
					}
				}
			}
		}

		//Checks what the serverlog is
		public static async Task<IMessageChannel> logChannelCheck(IGuild guild, String serverOrMod)
		{
			String path = getServerFilePath(guild.Id, Constants.SERVERLOG_AND_MODLOG);
			IMessageChannel logChannel = null;
			//Check if the file exists
			if (!File.Exists(path))
			{
				//Default to 'advobot' if it doesn't exist
				if (getChannel(guild, Constants.BASE_CHANNEL_NAME) != null)
				{
					logChannel = getChannel(guild, Constants.BASE_CHANNEL_NAME) as IMessageChannel;
					return logChannel;
				}
				//If the file and the channel both don't exist then return null
				else
					return null;
			}
			else
			{
				//Read the text document and find the serverlog 
				using (StreamReader reader = new StreamReader(path))
				{
					int counter = 0;
					string line;
					while ((line = reader.ReadLine()) != null)
					{
						if (line.Contains("serverlog"))
						{
							String[] logChannelArray = line.Split(new Char[] { ':' }, 2);

							if (String.IsNullOrWhiteSpace(logChannelArray[1]) || (String.IsNullOrEmpty(logChannelArray[1])))
							{
								return null;
							}
							else
							{
								logChannel = (await guild.GetChannelAsync(Convert.ToUInt64(logChannelArray[1]))) as IMessageChannel;
								return logChannel;
							}
						}
						counter++;
					}
				}
			}
			return null;
		}

		//Save bans by server
		public static void saveBans(ulong serverID)
		{
			String path = getServerFilePath(serverID, Constants.BAN_REFERENCE_FILE);
			//Check if the location already exists
			//if (!System.IO.File.Exists(path))
			{
				System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
				using (System.IO.StreamWriter writer = new System.IO.StreamWriter(path, false))
				{
					saveBans(writer, serverID);
				}
			}
		}

		//Save bans
		public static void saveBans(TextWriter writer, ulong serverID)
		{
			//Test if the bans exist
			Dictionary<ulong, String> banList;
			if (!Variables.mBanList.TryGetValue(serverID, out banList))
			{
				return;
			}

			foreach (ulong userID in banList.Keys)
			{
				writer.WriteLine(userID.ToString() + ":" + banList[userID]);
			}
		}

		//Edit message log message
		public static async Task editMessage(IMessageChannel logChannel, String time, IGuildUser user, IMessageChannel channel, String before, String after)
		{
			before = before.Replace("`", "'");
			after = after.Replace("`", "'");

			await sendChannelMessage(logChannel, String.Format("{0} **EDIT:** `{1}#{2}` **IN** `#{3}`\n**FROM:** `{4}`\n**TO:** `{5}`",
				time, user.Username, user.Discriminator, channel.Name, before, after));
		}
	}
}
