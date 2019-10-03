﻿using System.Collections.Generic;
using System.Threading.Tasks;

using Advobot.Invites.Database;
using Advobot.Invites.Models;
using Advobot.Invites.ReadOnlyModels;
using Advobot.Services.Time;

using AdvorangesUtils;

using Discord;

namespace Advobot.Invites.Service
{
	public sealed class InviteListService : IInviteListService
	{
		private readonly InviteDatabase _Db;
		private readonly ITime _Time;

		public InviteListService(
			InviteDatabase db,
			ITime time)
		{
			_Db = db;
			_Time = time;
		}

		public Task AddInviteAsync(IInviteMetadata invite)
		{
			var listedInvite = new ListedInvite(invite, _Time.UtcNow);
			return _Db.AddInviteAsync(listedInvite);
		}

		public Task AddKeywordAsync(IGuild guild, string word)
		{
			var keyword = new Keyword(guild, word);
			return _Db.AddKeywordAsync(keyword);
		}

		public async Task<bool> BumpAsync(IGuild guild)
		{
			var listedInvite = await _Db.GetInviteAsync(guild.Id).CAF();
			var invites = await guild.GetInvitesAsync().CAF();
			if (invites.TryGetFirst(x => x.Id == listedInvite.Code, out var invite))
			{
				var newListedInvite = new ListedInvite(invite, _Time.UtcNow);
				await _Db.UpdateInviteAsync(listedInvite).CAF();
				return true;
			}

			await RemoveInviteAsync(guild.Id).CAF();
			return false;
		}

		public Task<IReadOnlyList<IReadOnlyListedInvite>> GetAllAsync()
			=> _Db.GetInvitesAsync();

		public Task<IReadOnlyList<IReadOnlyListedInvite>> GetAllAsync(
			IEnumerable<string> keywords)
			=> _Db.GetInvitesAsync(keywords);

		public Task<IReadOnlyListedInvite?> GetAsync(ulong guildId)
			=> _Db.GetInviteAsync(guildId);

		public Task RemoveInviteAsync(ulong guildId)
			=> _Db.DeleteInviteAsync(guildId);

		public Task RemoveKeywordAsync(ulong guildId, string word)
			=> _Db.DeleteKeywordAsync(guildId, word);
	}
}