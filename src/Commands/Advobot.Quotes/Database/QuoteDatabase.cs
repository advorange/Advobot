﻿using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;

using Advobot.Quotes.Models;
using Advobot.Quotes.ReadOnlyModels;
using Advobot.SQLite;

using AdvorangesUtils;

namespace Advobot.Quotes.Database
{
	public sealed class QuoteDatabase : DatabaseBase<SQLiteConnection>, IQuoteDatabase
	{
		public QuoteDatabase(IConnectionStringFor<QuoteDatabase> conn) : base(conn)
		{
		}

		public Task<int> AddQuoteAsync(IReadOnlyQuote quote)
		{
			return ModifyAsync(@"
				INSERT OR IGNORE INTO Quote
				( GuildId, Name, Description )
				VALUES
				( @GuildId, @Name, @Description )
			", quote);
		}

		public Task<int> DeleteQuoteAsync(IReadOnlyQuote quote)
		{
			return ModifyAsync(@"
				DELETE FROM Quote
				WHERE GuildId = @GuildId AND Name = @Name
			", quote);
		}

		public async Task<IReadOnlyQuote?> GetQuoteAsync(ulong guildId, string name)
		{
			var param = new
			{
				GuildId = guildId.ToString(),
				Name = name,
			};
			return await GetOneAsync<Quote>(@"
				SELECT *
				FROM Quote
				WHERE GuildId = @GuildId AND Name = @Name
			", param).CAF();
		}

		public async Task<IReadOnlyList<IReadOnlyQuote>> GetQuotesAsync(ulong guildId)
		{
			var param = new { GuildId = guildId.ToString(), };
			return await GetManyAsync<Quote>(@"
				SELECT *
				FROM Quote
				WHERE GuildId = @GuildId
			", param).CAF();
		}
	}
}