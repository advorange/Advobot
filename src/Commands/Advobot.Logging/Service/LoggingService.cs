﻿using System;
using System.Threading.Tasks;

using Advobot.Logging.Database;
using Advobot.Services.BotSettings;
using Advobot.Services.Commands;
using Advobot.Services.Time;

using AdvorangesUtils;

using Discord;
using Discord.WebSocket;

namespace Advobot.Logging.Service
{
	public sealed class LoggingService
	{
		private readonly ClientLogger _ClientLogger;
		private readonly CommandHandlerLogger _CommandHandlerLogger;
		private readonly ILoggingDatabase _Db;
		private readonly MessageLogger _MessageLogger;
		private readonly UserLogger _UserLogger;

		public LoggingService(
			ILoggingDatabase db,
			BaseSocketClient client,
			ICommandHandlerService commandHandler,
			IBotSettings botSettings,
			ITime time)
		{
			_Db = db;

			_ClientLogger = new ClientLogger(client);
			client.GuildAvailable += _ClientLogger.OnGuildAvailable;
			client.GuildUnavailable += _ClientLogger.OnGuildUnavailable;
			client.JoinedGuild += _ClientLogger.OnJoinedGuild;
			client.LeftGuild += _ClientLogger.OnLeftGuild;
			client.Log += OnLogMessageSent;

			_CommandHandlerLogger = new CommandHandlerLogger(_Db, botSettings);
			commandHandler.CommandInvoked += _CommandHandlerLogger.OnCommandInvoked;
			commandHandler.Ready += _CommandHandlerLogger.OnReady;
			commandHandler.Log += OnLogMessageSent;

			_MessageLogger = new MessageLogger(_Db);
			client.MessageDeleted += _MessageLogger.OnMessageDeleted;
			client.MessagesBulkDeleted += _MessageLogger.OnMessagesBulkDeleted;
			client.MessageReceived += _MessageLogger.OnMessageReceived;
			client.MessageUpdated += _MessageLogger.OnMessageUpdated;

			_UserLogger = new UserLogger(_Db, client, time);
			client.UserJoined += _UserLogger.OnUserJoined;
			client.UserLeft += _UserLogger.OnUserLeft;
			client.UserUpdated += _UserLogger.OnUserUpdated;
		}

		private Task OnLogMessageSent(LogMessage message)
		{
			if (!string.IsNullOrWhiteSpace(message.Message))
			{
				ConsoleUtils.WriteLine(message.Message, name: message.Source);
			}

			if (message.Exception is GatewayReconnectException)
			{
				ConsoleUtils.WriteLine("Gateway reconnection requested.", ConsoleColor.Yellow, message.Source);
			}
			else
			{
				message.Exception?.Write();
			}

			return Task.CompletedTask;
		}
	}
}