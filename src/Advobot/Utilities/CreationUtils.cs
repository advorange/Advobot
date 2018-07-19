﻿using Advobot.Classes;
using Advobot.Classes.Attributes;
using Advobot.Classes.TypeReaders;
using Advobot.Interfaces;
using Advobot.Services.GuildSettings;
using Advobot.Services.InviteList;
using Advobot.Services.Logging;
using Advobot.Services.Timers;
using AdvorangesUtils;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Advobot.Utilities
{
	/// <summary>
	/// Actions creating the services for the main <see cref="IServiceProvider"/>.
	/// </summary>
	public static class CreationUtils
	{
		/// <summary>
		/// Creates services the bot uses. The explicit implementations will always be the same; if wanting to customize
		/// them do not use this method.
		/// </summary>
		/// <typeparam name="TBotSettings"></typeparam>
		/// <typeparam name="TGuildSettings"></typeparam>
		/// <param name="commands">The assemblies holding commands.</param>
		/// <returns>The service provider which holds all the services.</returns>
		public static IServiceProvider CreateDefaultServiceProvider<TBotSettings, TGuildSettings>(IEnumerable<Assembly> commands)
			where TBotSettings : IBotSettings, new()
			where TGuildSettings : IGuildSettings, new()
		{
			//I have no idea if I am providing services correctly, but it works.
			var helpEntryHolder = new HelpEntryHolder(commands);
			return new DefaultServiceProviderFactory().CreateServiceProvider(new ServiceCollection()
				.AddSingleton<CommandService>(provider => CreateCommandService(provider, commands))
				.AddSingleton<HelpEntryHolder>(helpEntryHolder)
				.AddSingleton<IBotSettings>(provider => CreateBotSettings<TBotSettings>())
				.AddSingleton<IDiscordClient>(provider => CreateDiscordClient(provider))
				.AddSingleton<IGuildSettingsService>(provider => new GuildSettingsService<TGuildSettings>(provider))
				.AddSingleton<ITimersService>(provider => new TimersService(provider))
				.AddSingleton<ILogService>(provider => new LogService(provider))
				.AddSingleton<IInviteListService>(provider => new InviteListService(provider)));
		}
		/// <summary>
		/// Creates the <see cref="CommandService"/> for the bot. Add in typereaders and modules.
		/// </summary>
		/// <returns></returns>
		internal static CommandService CreateCommandService(IServiceProvider provider, IEnumerable<Assembly> commandAssemblies)
		{
			var cmds = new CommandService(new CommandServiceConfig
			{
				CaseSensitiveCommands = false,
				ThrowOnError = false,
			});

			cmds.AddTypeReader<IInvite>(new InviteTypeReader());
			cmds.AddTypeReader<IBan>(new BanTypeReader());
			cmds.AddTypeReader<IWebhook>(new WebhookTypeReader());
			cmds.AddTypeReader<Emote>(new EmoteTypeReader());
			cmds.AddTypeReader<GuildEmote>(new GuildEmoteTypeReader());
			cmds.AddTypeReader<Color>(new ColorTypeReader());
			cmds.AddTypeReader<Uri>(new UriTypeReader());
			cmds.AddTypeReader<ModerationReason>(new ModerationReasonTypeReader());
			cmds.AddTypeReader<RuleCategory>(new RuleCategoryTypeReader());

			//Add in generic custom argument type readers
			var customArgumentsClasses = Assembly.GetAssembly(typeof(NamedArguments<>)).GetTypes()
				.Where(t => t.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
				.Any(c => c.GetCustomAttribute<NamedArgumentConstructorAttribute>() != null));
			foreach (var c in customArgumentsClasses)
			{
				var t = typeof(NamedArguments<>).MakeGenericType(c);
				var tr = (TypeReader)Activator.CreateInstance(typeof(NamedArgumentsTypeReader<>).MakeGenericType(c));
				cmds.AddTypeReader(t, tr);
			}

			//Add in commands
			Task.Run(async () =>
			{
				foreach (var assembly in commandAssemblies)
				{
					await cmds.AddModulesAsync(assembly, provider).CAF();
				}
				ConsoleUtils.DebugWrite("Successfully added every command assembly.");
			});

			return cmds;
		}
		/// <summary>
		/// Returns <see cref="DiscordSocketClient"/> if shard count in <paramref name="provider"/> is 1. Else returns <see cref="DiscordShardedClient"/>.
		/// </summary>
		/// <param name="provider">The settings to initialize the client with.</param>
		/// <returns>A discord client.</returns>
		internal static IDiscordClient CreateDiscordClient(IServiceProvider provider)
		{
			var botSettings = provider.GetRequiredService<IBotSettings>();
			var config = new DiscordSocketConfig
			{
				AlwaysDownloadUsers = botSettings.AlwaysDownloadUsers,
				MessageCacheSize = botSettings.MessageCacheCount,
				LogLevel = botSettings.LogLevel,
				TotalShards = botSettings.ShardCount
			};
			return botSettings.ShardCount > 1 ? new DiscordShardedClient(config) : (IDiscordClient)new DiscordSocketClient(config);
		}
		/// <summary>
		/// Creates settings that the bot uses.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		internal static IBotSettings CreateBotSettings<T>() where T : IBotSettings, new()
		{
			return IOUtils.DeserializeFromFile<IBotSettings, T>(FileUtils.GetBotSettingsFile());
		}
	}
}