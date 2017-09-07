﻿using Advobot.Classes;
using Advobot.Enums;
using System.Collections.Generic;
using System.Linq;
using Advobot.Interfaces;

namespace Advobot.Actions
{
	public static class BannedPhraseActions
	{
		/// <summary>
		/// Modifies <see cref="IGuildSettings.BannedPhraseStrings"/>. If <paramref name="add"/> is true then <paramref name="inputPhrases"/> get added,
		/// otherwise they get removed.
		/// </summary>
		/// <param name="guildSettings"></param>
		/// <param name="inputPhrases"></param>
		/// <param name="add"></param>
		/// <param name="success"></param>
		/// <param name="failure"></param>
		public static void ModifyBannedStrings(IGuildSettings guildSettings, IEnumerable<string> inputPhrases, bool add, out List<string> success, out List<string> failure)
		{
			if (add)
			{
				AddBannedPhrases(guildSettings.BannedPhraseStrings, inputPhrases, out success, out failure);
			}
			else
			{
				RemoveBannedPhrases(guildSettings.BannedPhraseStrings, inputPhrases, out success, out failure);
			}
		}
		/// <summary>
		/// Modifies <see cref="IGuildSettings.BannedPhraseRegex"/>. If <paramref name="add"/> is true then <paramref name="inputPhrases"/> get added,
		/// otherwise they get removed.
		/// </summary>
		/// <param name="inputPhrases"></param>
		/// <param name="add"></param>
		/// <param name="guildSettings"></param>
		/// <param name="success"></param>
		/// <param name="failure"></param>
		public static void ModifyBannedRegex(IEnumerable<string> inputPhrases, bool add, IGuildSettings guildSettings, out List<string> success, out List<string> failure)
		{
			if (add)
			{
				AddBannedPhrases(guildSettings.BannedPhraseRegex, inputPhrases, out success, out failure);
			}
			else
			{
				RemoveBannedPhrases(guildSettings.BannedPhraseRegex, inputPhrases, out success, out failure);
			}
		}

		/// <summary>
		/// Adds nonduplicate strings to the list of banned phrases.
		/// </summary>
		/// <param name="bannedPhrases"></param>
		/// <param name="inputPhrases"></param>
		/// <param name="success"></param>
		/// <param name="failure"></param>
		private static void AddBannedPhrases(List<BannedPhrase> bannedPhrases, IEnumerable<string> inputPhrases, out List<string> success, out List<string> failure)
		{
			success = new List<string>();
			failure = new List<string>();

			foreach (var str in inputPhrases)
			{
				//Don't add duplicate words
				if (!bannedPhrases.Any(x => x.Phrase.CaseInsEquals(str)))
				{
					success.Add(str);
					bannedPhrases.Add(new BannedPhrase(str, default(PunishmentType)));
				}
				else
				{
					failure.Add(str);
				}
			}
		}
		/// <summary>
		/// Removes banned phrases by position or matching text.
		/// </summary>
		/// <param name="bannedPhrases"></param>
		/// <param name="inputPhrases"></param>
		/// <param name="success"></param>
		/// <param name="failure"></param>
		private static void RemoveBannedPhrases(List<BannedPhrase> bannedPhrases, IEnumerable<string> inputPhrases, out List<string> success, out List<string> failure)
		{
			success = new List<string>();
			failure = new List<string>();

			var positions = new List<int>();
			foreach (var potentialPosition in inputPhrases)
			{
				if (int.TryParse(potentialPosition, out int temp) && temp < bannedPhrases.Count)
				{
					positions.Add(temp);
				}
			}

			//Removing by index
			if (positions.Any())
			{
				//Put them in descending order so as to not delete low values before high ones
				foreach (var position in positions.OrderByDescending(x => x))
				{
					if (bannedPhrases.Count - 1 <= position)
					{
						success.Add(bannedPhrases[position]?.Phrase ?? "null");
						bannedPhrases.RemoveAt(position);
					}
					else
					{
						failure.Add("String at position " + position);
					}
				}
				return;
			}

			//Removing by text matching
			foreach (var str in inputPhrases)
			{
				var temp = bannedPhrases.FirstOrDefault(x => x.Phrase.Equals(str));
				if (temp != null)
				{
					success.Add(str);
					bannedPhrases.Remove(temp);
				}
				else
				{
					failure.Add(str);
				}
			}
		}
	}
}