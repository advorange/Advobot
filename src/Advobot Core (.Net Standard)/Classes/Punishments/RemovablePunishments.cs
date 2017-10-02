﻿using Advobot.Enums;
using Advobot.Interfaces;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Advobot.Classes.Punishments
{
	/// <summary>
	/// Punishments that will be removed after <see cref="GetTime"/> is less than <see cref="DateTime.UtcNow"/>.
	/// </summary>
	public struct RemovablePunishment : IHasTime
	{
		public readonly PunishmentType PunishmentType;
		public readonly IGuild Guild;
		public readonly IRole Role;
		public readonly ulong UserId;
		private readonly DateTime _Time;

		public RemovablePunishment(PunishmentType punishment, IGuild guild, ulong userId, int minutes)
		{
			PunishmentType = punishment;
			Guild = guild;
			Role = null;
			UserId = userId;
			_Time = DateTime.UtcNow.AddMinutes(minutes);
		}
		public RemovablePunishment(PunishmentType punishment, IGuild guild, IRole role, ulong userId, int minutes) : this(punishment, guild, userId, minutes)
		{
			Role = role;
		}

		public DateTime GetTime() => _Time;
	}

	/// <summary>
	/// Messages that will get deleted after <see cref="GetTime"/> is less than <see cref="DateTime.UtcNow"/>.
	/// </summary>
	public struct RemovableMessage : IHasTime
	{
		public readonly IReadOnlyCollection<IMessage> Messages;
		public readonly ITextChannel Channel;
		private readonly DateTime _Time;

		public RemovableMessage(int seconds, params IMessage[] messages)
		{
			Messages = messages.ToList().AsReadOnly();
			Channel = messages.FirstOrDefault().Channel as ITextChannel;
			_Time = DateTime.UtcNow.AddSeconds(seconds);
		}

		public DateTime GetTime() => _Time;
	}
}