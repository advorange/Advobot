﻿using System;
using System.IO;
using System.Threading.Tasks;
using Advobot.Databases;
using Advobot.Settings;
using Discord.WebSocket;

namespace Advobot.Tests.Fakes.Services
{
	public sealed class FakeLowLevelConfig : ILowLevelConfig
	{
		public ulong BotId => ulong.MinValue;
		public int PreviousProcessId => -1;
		public int CurrentInstance => int.MaxValue;
		public DatabaseType DatabaseType => DatabaseType.LiteDB;
		public string DatabaseConnectionString => "";
		public bool ValidatedPath => true;
		public bool ValidatedKey => true;
		public string RestartArguments => "";
		public DirectoryInfo BaseBotDirectory => null;

		public Task StartAsync(BaseSocketClient client)
			=> Task.CompletedTask;
		public Task<bool> ValidateBotKey(string input, bool startup, Func<BaseSocketClient, IRestartArgumentProvider, Task> restartCallback)
			=> Task.FromResult(true);
		public bool ValidatePath(string input, bool startup)
			=> true;
	}
}