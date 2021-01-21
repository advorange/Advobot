﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Advobot.Attributes;
using Advobot.Classes.CloseWords;
using Advobot.Quotes.Database;
using Advobot.Quotes.ReadOnlyModels;
using Advobot.Utilities;

using AdvorangesUtils;

using Discord.Commands;

using Microsoft.Extensions.DependencyInjection;

namespace Advobot.TypeReaders
{
	[TypeReaderTargetType(typeof(IReadOnlyList<IReadOnlyQuote>))]
	public sealed class CloseQuoteTypeReader : TypeReader
	{
		public override async Task<TypeReaderResult> ReadAsync(
			ICommandContext context,
			string input,
			IServiceProvider services)
		{
			var db = services.GetRequiredService<IQuoteDatabase>();
			var quotes = await db.GetQuotesAsync(context.Guild.Id).CAF();
			var matches = new CloseWords<IReadOnlyQuote>(quotes, x => x.Name)
				.FindMatches(input)
				.Select(x => x.Value)
				.ToArray();
			return TypeReaderUtils.MultipleValidResults(matches, "quotes", input);
		}
	}
}