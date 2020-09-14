﻿using System.Collections.Generic;
using System.Linq;

using Advobot.Classes;
using Advobot.Modules;
using Advobot.Quotes.ReadOnlyModels;
using Advobot.Utilities;

using AdvorangesUtils;

using static Advobot.Resources.Responses;

namespace Advobot.Quotes.Responses
{
	public sealed class Quotes : AdvobotResult
	{
		private Quotes() : base(null, "")
		{
		}

		public static AdvobotResult AddedQuote(IReadOnlyQuote quote)
		{
			return Success(QuotesAddedQuote.Format(
				quote.Name.WithBlock()
			));
		}

		public static AdvobotResult Quote(IReadOnlyQuote quote)
		{
			if (quote.Description != null)
			{
				return Success(quote.Description);
			}
			return Failure(QuotesRemovedQuote.Format(
				quote.Name.WithBlock()
			));
		}

		public static AdvobotResult RemovedQuote(IReadOnlyQuote quote)
		{
			return Success(QuotesRemovedQuote.Format(
				quote.Name.WithBlock()
			));
		}

		public static AdvobotResult ShowQuotes(IEnumerable<IReadOnlyQuote> quotes)
		{
			return Success(new EmbedWrapper
			{
				Title = VariableQuotes,
				Description = quotes.Join(x => x.Name).WithBigBlock().Value,
			});
		}
	}
}