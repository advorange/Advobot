﻿using Advobot.Interfaces;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;

namespace Advobot.Classes.Rules
{
	public class RuleHolder : ISetting
	{
		public IReadOnlyList<RuleCategory> Categories => _Categories.Values.ToList().AsReadOnly();
		private Dictionary<int, RuleCategory> _Categories = new Dictionary<int, RuleCategory>();

		public void AddOrUpdateCategory(int pos, RuleCategory category)
		{
			if (_Categories.ContainsKey(pos))
			{
				_Categories[pos] = category;
			}
			else
			{
				_Categories.Add(pos, category);
			}
		}
		public bool RemoveCategory(int pos)
		{
			if (_Categories.ContainsKey(pos))
			{
				return _Categories.Remove(pos);
			}
			return false;
		}
		public bool RemoveCategory(RuleCategory category)
		{
			if (_Categories.ContainsValue(category))
			{
				return _Categories.Remove(_Categories.SingleOrDefault(x => x.Value == category).Key);
			}
			return false;
		}

		public override string ToString()
		{
			return new RuleFormatter().FormatRules(this);
		}
		public string ToString(SocketGuild guild)
		{
			return ToString();
		}
	}
}
