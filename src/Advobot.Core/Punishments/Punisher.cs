﻿using System;
using System.Threading.Tasks;

using Advobot.Utilities;

using AdvorangesUtils;

using Discord;

namespace Advobot.Punishments
{
	internal sealed class Punisher : IPunisher
	{
		private readonly AsyncEvent<Func<IPunishmentContext, Task>> _PunishmentGiven
			= new AsyncEvent<Func<IPunishmentContext, Task>>();
		private readonly AsyncEvent<Func<IPunishmentContext, Task>> _PunishmentRemoved
			= new AsyncEvent<Func<IPunishmentContext, Task>>();

		/// <inheritdoc />
		public event Func<IPunishmentContext, Task> PunishmentGiven
		{
			add => _PunishmentGiven.Add(value);
			remove => _PunishmentGiven.Remove(value);
		}

		/// <inheritdoc />
		public event Func<IPunishmentContext, Task> PunishmentRemoved
		{
			add => _PunishmentRemoved.Add(value);
			remove => _PunishmentRemoved.Remove(value);
		}

		/// <inheritdoc />
		public async Task HandleAsync(IPunishmentContext context)
		{
			await context.ExecuteAsync().CAF();
			await (context.IsGive ? _PunishmentGiven : _PunishmentRemoved).InvokeAsync(context).CAF();
		}
	}
}