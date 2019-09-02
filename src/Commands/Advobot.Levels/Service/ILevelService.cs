﻿using System.Threading.Tasks;

using Advobot.Levels.Database;
using Advobot.Levels.Metadata;

namespace Advobot.Levels.Service
{
	/// <summary>
	/// Abstraction for giving experience and rewards for chatting.
	/// </summary>
	public interface ILevelService
	{
		int CalculateLevel(int experience);

		Task<Rank> GetRankAsync(ISearchArgs args);

		Task<int> GetXpAsync(ISearchArgs args);
	}
}