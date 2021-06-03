﻿using System;
using System.Threading;

using Advobot.SQLite.Relationships;

namespace Advobot.AutoMod.Models
{
	public record AutoModSettings(
		ulong GuildId,
		long Ticks,
		bool IgnoreAdmins,
		bool IgnoreHigherHierarchy
	) : IGuildChild
	{
		public bool CheckDuration => Duration != Timeout.InfiniteTimeSpan;
		public TimeSpan Duration => new(Ticks);

		public AutoModSettings() : this(default, default, IgnoreAdmins: true, IgnoreHigherHierarchy: true) { }
	}
}