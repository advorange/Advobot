﻿using Advobot.Interfaces;
using Advobot.NonSavedClasses;
using Discord;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Advobot
{
	namespace Actions
	{
		public static class InviteActions
		{
			public static async Task<IReadOnlyCollection<IInviteMetadata>> GetInvites(IGuild guild)
			{
				if (guild == null)
					return new List<IInviteMetadata>();

				var currUser = await guild.GetCurrentUserAsync();
				if (!currUser.GuildPermissions.ManageGuild)
					return new List<IInviteMetadata>();

				return await guild.GetInvitesAsync();
			}
			public static async Task<BotInvite> GetInviteUserJoinedOn(IGuildSettings guildSettings, IGuild guild)
			{
				var curInvs = await GetInvites(guild);
				if (!curInvs.Any())
					return null;

				//Find the first invite where the bot invite has the same code as the current invite but different use counts
				var joinInv = guildSettings.Invites.FirstOrDefault(bI => curInvs.Any(cI => cI.Code == bI.Code && cI.Uses != bI.Uses));
				//If the invite is null, take that as meaning there are new invites on the guild
				if (joinInv == null)
				{
					//Get the new invites on the guild by finding which guild invites aren't on the bot invites list
					var newInvs = curInvs.Where(cI => !guildSettings.Invites.Select(bI => bI.Code).Contains(cI.Code));
					//If there's only one, then use that as the current inv. If there's more than one then there's no way to know what invite it was on
					if (guild.Features.CaseInsContains(Constants.VANITY_URL) && (!newInvs.Any() || newInvs.All(x => x.Uses == 0)))
					{
						joinInv = new BotInvite(guild.Id, "Vanity URL", 0);
					}
					else if (newInvs.Count() == 1)
					{
						var newInv = newInvs.First();
						joinInv = new BotInvite(newInv.GuildId, newInv.Code, newInv.Uses);
					}
					guildSettings.Invites.AddRange(newInvs.Select(x => new BotInvite(x.GuildId, x.Code, x.Uses)));
				}
				else
				{
					//Increment the invite the bot is holding if a curInv was found so as to match with the current invite uses count
					joinInv.IncreaseUses();
				}
				return joinInv;
			}
		}
	}
}