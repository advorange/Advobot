﻿using System;
using System.IO;

using Advobot.Gacha.Database;

namespace Advobot.Tests
{
	public sealed class SQLiteTestDatabaseFactory : IDatabaseStarter
	{
		private readonly string _ConnectionString;

		public SQLiteTestDatabaseFactory()
		{
			var file = Path.Combine(Environment.CurrentDirectory, "Database", "GachaTest.db");
			Directory.CreateDirectory(Path.GetDirectoryName(file));
			if (File.Exists(file))
			{
				File.Delete(file);
				using var _ = File.Create(file);
			}
			_ConnectionString = $"Data Source={file}";
		}

		public string GetConnectionString()
			=> _ConnectionString;

		public bool IsDatabaseCreated()
			=> false;
	}
}