﻿using Advobot.Core.Enums;
using Advobot.Core.Interfaces;
using Discord;
using System;

namespace Advobot.Core.Classes.Punishments
{
	/// <summary>
	/// Punishments that will be removed after <see cref="GetTime"/> is less than <see cref="DateTime.UtcNow"/>.
	/// </summary>
	public struct RemovablePunishment : ITime
	{
		public PunishmentType PunishmentType { get; }
		public IGuild Guild { get; }
		public IUser User { get; }
		public IRole Role { get; }
		public DateTime Time { get; }

		public RemovablePunishment(PunishmentType punishment, IGuild guild, IUser user, int minutes)
		{
			PunishmentType = punishment;
			Guild = guild;
			User = user;
			Role = null;
			Time = DateTime.UtcNow.AddMinutes(minutes);
		}
		public RemovablePunishment(PunishmentType punishment, IGuild guild, IUser user, IRole role, int minutes) : this(punishment, guild, user, minutes)
		{
			Role = role;
		}
	}
}
