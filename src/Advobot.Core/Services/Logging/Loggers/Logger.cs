﻿using System;
using System.Threading.Tasks;

using Advobot.Classes;
using Advobot.Services.BotSettings;
using Advobot.Services.GuildSettings;
using Advobot.Services.Logging.Interfaces;
using Advobot.Services.Logging.LogCounters;
using Advobot.Utilities;

using AdvorangesUtils;

using Discord;

namespace Advobot.Services.Logging.Loggers
{
	internal abstract class Logger : ILogger
	{
		protected IBotSettings BotSettings { get; }

		protected IGuildSettingsFactory GuildSettingsFactory { get; }

		public event EventHandler<LogCounterIncrementEventArgs> LogCounterIncrement;

		protected Logger(IBotSettings botSettings, IGuildSettingsFactory settingsFactory)
		{
			BotSettings = botSettings;
			GuildSettingsFactory = settingsFactory;
		}

		protected async Task HandleAsync(
			IGuildUser user,
			LoggingContextArgs<IUserLoggingContext> args)
		{
			var context = await LoggingContext.CreateAsync(user, GuildSettingsFactory).CAF();
			await HandleAsync(context, args).CAF();
		}

		protected async Task HandleAsync(
			IMessage message,
			LoggingContextArgs<IMessageLoggingContext> args)
		{
			var context = await LoggingContext.CreateAsync(message, GuildSettingsFactory).CAF();
			await HandleAsync(context, args).CAF();
		}

		protected void NotifyLogCounterIncrement(string name, int count)
							=> LogCounterIncrement?.Invoke(this, new LogCounterIncrementEventArgs(name, count));

		protected Task ReplyAsync(
			ITextChannel? channel,
			string content = "",
			EmbedWrapper? embedWrapper = null,
			TextFileInfo? textFile = null)
		{
			if (channel == null)
			{
				return Task.CompletedTask;
			}
			return MessageUtils.SendMessageAsync(channel, content, embedWrapper, textFile);
		}

		private async Task HandleAsync<T>(T? context, LoggingContextArgs<T> args)
			where T : class, ILoggingContext
		{
			if (context == null || BotSettings.Pause)
			{
				return;
			}
			if (context.CanLog(args.Action))
			{
				NotifyLogCounterIncrement(args.LogCounterName, 1);
				foreach (var task in args.WhenCanLog)
				{
					await task.Invoke(context).CAF();
				}
			}
			foreach (var task in args.AnyTime)
			{
				await task.Invoke(context).CAF();
			}
		}
	}
}