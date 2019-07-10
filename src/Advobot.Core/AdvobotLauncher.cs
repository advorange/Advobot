﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Advobot.Classes;
using Advobot.Classes.DatabaseWrappers;
using Advobot.Classes.DatabaseWrappers.LiteDB;
using Advobot.Classes.DatabaseWrappers.MongoDB;
using Advobot.Classes.ImageResizing;
using Advobot.Interfaces;
using Advobot.Services.BotSettings;
using Advobot.Services.Commands;
using Advobot.Services.GuildSettings;
using Advobot.Services.HelpEntries;
using Advobot.Services.InviteList;
using Advobot.Services.Levels;
using Advobot.Services.Logging;
using Advobot.Services.Timers;
using Advobot.Utilities;
using AdvorangesUtils;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Advobot
{
	/// <summary>
	/// Puts the similarities from launching the console application and the .Net Core UI application into one.
	/// </summary>
	public sealed class AdvobotLauncher
	{
		private readonly ILowLevelConfig _Config;
		private IServiceCollection? _Services;

		/// <summary>
		/// Creates an instance of <see cref="AdvobotLauncher"/>.
		/// </summary>
		/// <param name="config"></param>
		public AdvobotLauncher(ILowLevelConfig config)
		{
			AppDomain.CurrentDomain.UnhandledException += (sender, e) => IOUtils.LogUncaughtException(e.ExceptionObject);
			Console.Title = "Advobot";
			ConsoleUtils.PrintingFlags = 0
				| ConsolePrintingFlags.Print
				| ConsolePrintingFlags.LogTime
				| ConsolePrintingFlags.LogCaller
				| ConsolePrintingFlags.RemoveDuplicateNewLines;

			_Config = config;
			ConsoleUtils.DebugWrite($"Args: {_Config.CurrentInstance}|{_Config.PreviousProcessId}", "Launcher Arguments");
		}

		/// <summary>
		/// Waits until the old process is killed. This is blocking.
		/// </summary>
		public void WaitUntilOldProcessKilled()
		{
			//Wait until the old process is killed
			if (_Config.PreviousProcessId != -1)
			{
				try
				{
					while (Process.GetProcessById(_Config.PreviousProcessId) != null)
					{
						Thread.Sleep(25);
					}
				}
				catch (ArgumentException) { }
			}
		}
		/// <summary>
		/// Gets the path and bot key from user input if they're not already stored in file.
		/// </summary>
		/// <returns></returns>
		public async Task GetPathAndKeyAsync()
		{
            //Get the save path
            var startup = true;
            while (!_Config.ValidatedPath)
            {
                startup = _Config.ValidatePath(startup ? null : Console.ReadLine(), startup);
            }

            //Get the bot key
            startup = true;
            while (!_Config.ValidatedKey)
            {
                startup = await _Config.ValidateBotKey(startup ? null : Console.ReadLine(), startup, ClientUtils.RestartBotAsync).CAF();
            }
        }
		/// <summary>
		/// Returns the default services for the bot if both the path and key have been set.
		/// </summary>
		/// <returns></returns>
		public IServiceCollection GetDefaultServices()
		{
			if (!(_Config.ValidatedPath && _Config.ValidatedKey))
			{
				throw new InvalidOperationException("Attempted to start the bot before the path and key have been set.");
			}
			return _Services ?? (_Services = CreateDefaultServices(_Config));
		}
		/// <summary>
		/// Creates a provider and initializes all of its singletons.
		/// </summary>
		/// <param name="services"></param>
		/// <returns></returns>
		public IServiceProvider CreateProvider(IServiceCollection services)
		{
			var provider = services.BuildServiceProvider();
			foreach (var service in services.Where(x => x.Lifetime == ServiceLifetime.Singleton))
			{
				var instance = provider.GetRequiredService(service.ServiceType);
				if (instance is IUsesDatabase usesDb)
				{
					usesDb.Start();
				}
			}
			return provider;
		}
		private static IServiceCollection CreateDefaultServices(ILowLevelConfig config)
		{
			//I have no idea if I am providing services correctly, but it works.
			var botSettings = BotSettings.Load(config);
			var commands = new CommandService(new CommandServiceConfig
			{
				CaseSensitiveCommands = false,
				ThrowOnError = false,
				LogLevel = botSettings.LogLevel,
			});
			var discordClient = new DiscordShardedClient(new DiscordSocketConfig
			{
				AlwaysDownloadUsers = botSettings.AlwaysDownloadUsers,
				MessageCacheSize = botSettings.MessageCacheSize,
				LogLevel = botSettings.LogLevel,
			});
			//TODO: replace with a different downloader client?
			var httpClient = new HttpClient(new HttpClientHandler
			{
				AllowAutoRedirect = true,
				Credentials = CredentialCache.DefaultCredentials,
				Proxy = new WebProxy(),
				AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
			});

			var s = new ServiceCollection()
				.AddSingleton(commands)
				.AddSingleton(discordClient)
				.AddSingleton(httpClient)
				.AddSingleton<BaseSocketClient>(discordClient)
				.AddSingleton<IDiscordClient>(discordClient)
				.AddSingleton<IBotSettings>(botSettings)
				.AddSingleton<IBotDirectoryAccessor>(botSettings)
				.AddSingleton<IHelpEntryService, HelpEntryService>()
				.AddSingleton<ICommandHandlerService, CommandHandlerService>()
				.AddSingleton<IGuildSettingsFactory, GuildSettingsFactory>()
				.AddSingleton<ILogService, LogService>()
				.AddSingleton<ILevelService, LevelService>()
				.AddSingleton<ITimerService, TimerService>()
				.AddSingleton<IInviteListService, InviteListService>()
				.AddSingleton<IImageResizer, ImageResizer>();

			switch (config.DatabaseType)
			{
				//-DatabaseType LiteDB (or no arguments supplied at all)
				case DatabaseType.LiteDB:
					s.AddSingleton<IDatabaseWrapperFactory, LiteDBWrapperFactory>();
					break;
				//-DatabaseType MongoDB -DatabaseConnectionString "mongodb://localhost:27017"
				case DatabaseType.MongoDB:
					s.AddSingleton<IDatabaseWrapperFactory, MongoDBWrapperFactory>();
					s.AddSingleton<IMongoClient>(_ => new MongoClient(config.DatabaseConnectionString));
					break;
			}
			return s;
		}
		/// <summary>
		/// Creates the service provider and starts the Discord bot.
		/// </summary>
		/// <returns></returns>
		public Task StartAsync(IServiceProvider provider)
		{
			if (!(_Config.ValidatedPath && _Config.ValidatedKey))
			{
				throw new InvalidOperationException("Attempted to start the bot before the path and key have been set.");
			}
			return _Config.StartAsync(provider.GetRequiredService<BaseSocketClient>());
		}
		/// <summary>
		/// Starts an instance of Advobot with one method call.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public static async Task<IServiceProvider> NoConfigurationStart(string[] args)
		{
			var launcher = new AdvobotLauncher(LowLevelConfig.Load(args));
			await launcher.GetPathAndKeyAsync().CAF();
			var services = launcher.CreateProvider(launcher.GetDefaultServices());
			var commandHandler = services.GetRequiredService<ICommandHandlerService>();
			await commandHandler.AddCommandsAsync(DiscordUtils.GetCommandAssemblies());
			await launcher.StartAsync(services).CAF();
			return services;
		}
	}
}