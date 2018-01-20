﻿using Advobot.Core.Utilities;
using Advobot.Core.Classes.Punishments;
using Advobot.Core.Classes.UserInformation;
using Advobot.Core.Enums;
using Advobot.Core.Interfaces;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;

namespace Advobot.Core.Classes.GuildSettings
{
	/// <summary>
	/// Holds a phrase and punishment.
	/// </summary>
	public class BannedPhrase : IGuildSetting
	{
		[JsonProperty]
		public string Phrase { get; }
		[JsonProperty]
		public PunishmentType Punishment;

		public BannedPhrase(string phrase, PunishmentType punishment = default)
		{
			Phrase = phrase;
			Punishment = punishment;
		}

		/// <summary>
		/// Deletes the message then checks if the user should be punished.
		/// </summary>
		/// <param name="guildSettings"></param>
		/// <param name="message"></param>
		/// <param name="timers"></param>
		/// <returns></returns>
		public async Task PunishAsync(IGuildSettings guildSettings, IMessage message, ITimersService timers = null)
		{
			await MessageUtils.DeleteMessageAsync(message, new ModerationReason("banned phrase")).CAF();

			var user = guildSettings.BannedPhraseUsers.SingleOrDefault(x => x.User.Id == message.Author.Id);
			if (user == null)
			{
				guildSettings.BannedPhraseUsers.Add(user = new BannedPhraseUserInfo(message.Author as IGuildUser));
			}

			var count = user.IncrementValue(Punishment);
			var punishment = guildSettings.BannedPhrasePunishments.SingleOrDefault(x => x.Punishment == Punishment && x.NumberOfRemoves == count);
			if (punishment == null)
			{
				return;
			}

			var giver = new PunishmentGiver(punishment.PunishmentTime, timers);
			await giver.PunishAsync(Punishment, user.User, punishment.GetRole(guildSettings.Guild), new ModerationReason("banned phrase")).CAF();
			user.ResetValue(Punishment);
		}

		public override string ToString()
		{
			var punishmentChar = Punishment == default ? "N" : Punishment.EnumName().Substring(0, 1);
			return $"`{punishmentChar}` `{Phrase}`";
		}
		public string ToString(SocketGuild guild)
		{
			return ToString();
		}
	}
}