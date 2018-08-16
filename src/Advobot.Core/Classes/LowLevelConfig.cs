﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Advobot.Interfaces;
using Advobot.Services.Commands;
using Advobot.Services.GuildSettings;
using Advobot.Services.HelpEntries;
using Advobot.Services.InviteList;
using Advobot.Services.Levels;
using Advobot.Services.Logging;
using Advobot.Services.Timers;
using Advobot.Utilities;
using AdvorangesSettingParser;
using AdvorangesUtils;
using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Advobot.Classes
{
	/// <summary>
	/// Low level configuration that is necessary for the bot to run. Holds the bot key, bot id, and save path.
	/// </summary>
	public class LowLevelConfig : ILowLevelConfig
	{
		/// <summary>
		/// The path leading to the bot's directory.
		/// </summary>
		[JsonProperty("SavePath")]
		public string SavePath { get; private set; }
		/// <summary>
		/// The API key for the bot.
		/// </summary>
		[JsonProperty("BotKey")]
		private string _BotKey;
		/// <inheritdoc />
		[JsonIgnore]
		public ulong BotId { get; private set; }
		/// <inheritdoc />
		[JsonIgnore]
		public int PreviousProcessId { get; private set; } = -1;
		/// <inheritdoc />
		[JsonIgnore]
		public int CurrentInstance { get; private set; } = -1;
		/// <inheritdoc />
		[JsonIgnore]
		public bool ValidatedPath { get; private set; }
		/// <inheritdoc />
		[JsonIgnore]
		public bool ValidatedKey { get; private set; }
		/// <inheritdoc />
		[JsonIgnore]
		public DirectoryInfo BaseBotDirectory => Directory.CreateDirectory(Path.Combine(SavePath, $"Discord_Servers_{BotId}"));
		/// <inheritdoc />
		[JsonIgnore]
		public string RestartArguments => $"-{nameof(PreviousProcessId)} {Process.GetCurrentProcess().Id} -{nameof(CurrentInstance)} {CurrentInstance}";

		[JsonIgnore]
		private readonly DiscordRestClient _TestClient = new DiscordRestClient();

		/// <inheritdoc />
		public bool ValidatePath(string input, bool startup)
		{
			if (ValidatedPath)
			{
				return true;
			}

			var path = input ?? SavePath;

			if (startup && !String.IsNullOrWhiteSpace(path) && Directory.Exists(path))
			{
				return ValidatedPath = true;
			}
			if (startup)
			{
				ConsoleUtils.WriteLine("Please enter a valid directory path in which to save files or say 'AppData':");
				return false;
			}
			if ("appdata".CaseInsEquals(path))
			{
				path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			}
			if (Directory.Exists(path))
			{
				ConsoleUtils.WriteLine($"Successfully set the save path as {path}");
				SavePath = path;
				Save();
				return ValidatedPath = true;
			}

			ConsoleUtils.WriteLine("Invalid directory. Please enter a valid directory:", ConsoleColor.Red);
			return false;
		}
		/// <inheritdoc />
		public async Task<bool> ValidateBotKey(string input, bool startup, Func<BaseSocketClient, IRestartArgumentProvider, Task> restartCallback)
		{
			if (ValidatedKey)
			{
				return true;
			}

			var key = input ?? _BotKey;

			if (startup && !String.IsNullOrWhiteSpace(key))
			{
				try
				{
					await _TestClient.LoginAsync(TokenType.Bot, key).CAF();
					BotId = _TestClient.CurrentUser.Id;
					return ValidatedKey = true;
				}
				catch (HttpException)
				{
					ConsoleUtils.WriteLine("The given key is no longer valid. Please enter a new valid key:");
					return false;
				}
			}
			if (startup)
			{
				ConsoleUtils.WriteLine("Please enter the bot's key:");
				return false;
			}

			try
			{
				await _TestClient.LoginAsync(TokenType.Bot, key).CAF();
				_BotKey = key;
				BotId = _TestClient.CurrentUser.Id;
				Save();
				ConsoleUtils.WriteLine("Succesfully logged in via the given bot key.");
				return ValidatedKey = true;
			}
			catch (HttpException)
			{
				ConsoleUtils.WriteLine("The given key is invalid. Please enter a valid key:", ConsoleColor.Red);
				return false;
			}
		}
		/// <inheritdoc />
		public async Task StartAsync(BaseSocketClient client)
		{
			if (!(ValidatedPath && ValidatedKey))
			{
				throw new InvalidOperationException($"Either path of key has not been validated yet.");
			}
			//Remove the bot key from being easily accessible via reflection
			client.LoggedIn += () =>
			{
				_BotKey = null;
				return Task.CompletedTask;
			};
			await ClientUtils.StartAsync(client, _BotKey);
		}
		/// <inheritdoc />
		private void Save()
		{
			IOUtils.SafeWriteAllText(GetConfigPath(CurrentInstance), IOUtils.Serialize(this));
		}
		/// <inheritdoc />
		public IServiceCollection CreateDefaultServices(IEnumerable<Assembly> commands = null)
		{
			commands = commands ?? DiscordUtils.GetCommandAssemblies();
			//I have no idea if I am providing services correctly, but it works.
			var s = new ServiceCollection();
			s.AddSingleton<DiscordShardedClient>(p =>
			{
				var settings = p.GetRequiredService<IBotSettings>();
				return new DiscordShardedClient(new DiscordSocketConfig
				{
					AlwaysDownloadUsers = settings.AlwaysDownloadUsers,
					MessageCacheSize = settings.MessageCacheSize,
					LogLevel = settings.LogLevel,
				});
			});
			s.AddSingleton<IHelpEntryService>(p => new HelpEntryService());
			s.AddSingleton<IBotSettings>(p => BotSettings.Load(this));
			s.AddSingleton<ILevelService>(p => new LevelService(Create(s, p), new LevelServiceArguments()));
			s.AddSingleton<ICommandHandlerService>(p => new CommandHandlerService(Create(s, p), commands));
			s.AddSingleton<IGuildSettingsFactory>(p => new GuildSettingsFactory<GuildSettings>(Create(s, p)));
			s.AddSingleton<ITimerService>(p => new TimerService(Create(s, p)));
			s.AddSingleton<ILogService>(p => new LogService(Create(s, p)));
			s.AddSingleton<IInviteListService>(p => new InviteListService(Create(s, p)));
			return s;
		}
		/// <summary>
		/// Creates an <see cref="IIterableServiceProvider"/> from already existing services.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="provider"></param>
		/// <returns></returns>
		private IIterableServiceProvider Create(IServiceCollection services, IServiceProvider provider)
		{
			return IterableServiceProvider.CreateFromExisting(provider, services);
		}
		/// <summary>
		/// Attempts to load the configuration with the supplied instance number otherwise uses the default initialization for config.
		/// </summary>
		/// <returns></returns>
		public static LowLevelConfig Load(string[] args)
		{
			var processId = -1;
			var instance = -1;
			//No help command because this is not intended to be used more than semi internally
			new SettingParser(false, "-", "--", "/")
			{
				//Don't bother adding descriptions because of the aforementioned removal
				new Setting<int>(new[] { nameof(PreviousProcessId), "procid" }, x => processId = x),
				new Setting<int>(new[] { nameof(CurrentInstance), "instance" }, x => instance = x)
			}.Parse(args);

			//Count how many exist with that name so they can be saved as Advobot1, Advobot2, etc.
			instance = instance == -1 ? Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length : instance;
			var config = IOUtils.DeserializeFromFile<LowLevelConfig>(GetConfigPath(instance)) ?? new LowLevelConfig();
			config.PreviousProcessId = processId;
			config.CurrentInstance = instance;
			return config;
		}
		/// <summary>
		/// Gets the path leading to the config to use for this bot.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		private static FileInfo GetConfigPath(int instance)
		{
			//Start by grabbing the entry assembly location then cutting out everything but the file name
			//Use entry so console and ui applications can have diff configs
			var currentName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
			//Add the config file into the local application data folder under Advobot
			var appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			return new FileInfo(Path.Combine(appdata, "Advobot", currentName + instance + ".config"));
		}
	}
}