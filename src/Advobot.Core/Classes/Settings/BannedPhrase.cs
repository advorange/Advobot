﻿using Advobot.Core.Classes.Punishments;
using Advobot.Core.Classes.UserInformation;
using Advobot.Core.Enums;
using Advobot.Core.Interfaces;
using Advobot.Core.Utilities;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;

namespace Advobot.Core.Classes.Settings
{
	/// <summary>
	/// Holds a phrase and punishment.
	/// </summary>
	public class BannedPhrase : IGuildSetting
	{
		private static RequestOptions _Options = ClientUtils.CreateRequestOptions("banned phrase");

		[JsonProperty]
		public string Phrase { get; }
		[JsonProperty]
		public Punishment Punishment;

		public BannedPhrase(string phrase, Punishment punishment = default)
		{
			Phrase = phrase;
			Punishment = punishment;
		}

		/// <summary>
		/// Deletes the message then checks if the user should be punished.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="guild"
		/// <param name="info"></param>
		/// <param name="timers"></param>
		/// <returns></returns>
		public async Task PunishAsync(IGuildSettings settings, SocketGuild guild, BannedPhraseUserInfo info, ITimersService timers)
		{
			var count = info.Increment(Punishment);
			var punishment = settings.BannedPhrasePunishments.SingleOrDefault(x => x.Punishment == Punishment && x.NumberOfRemoves == count);
			if (punishment == null)
			{
				return;
			}

			await new PunishmentGiver(punishment.Time, timers).PunishAsync(Punishment, guild, info.UserId, punishment.RoleId, _Options).CAF();
			info.Reset(Punishment);
		}

		public override string ToString()
		{
			return $"`{(Punishment == default ? 'N' : Punishment.ToString()[0])}` `{Phrase}`";
		}
		public string ToString(SocketGuild guild)
		{
			return ToString();
		}
	}
}