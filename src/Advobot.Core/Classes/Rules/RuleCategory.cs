﻿using Advobot.Core.Actions.Formatting;
using Advobot.Core.Interfaces;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;

namespace Advobot.Core.Classes.Rules
{
	public class RuleCategory : ISetting
	{
		[JsonProperty]
		public string Name { get; private set; }
		[JsonProperty("Rules")]
		private List<Rule> _Rules = new List<Rule>();
		[JsonIgnore]
		public IReadOnlyList<Rule> Rules => _Rules.AsReadOnly();

		public RuleCategory(string name)
		{
			Name = name;
		}

		public void AddRule(Rule rule)
		{
			_Rules.Add(rule);
		}
		public bool RemoveRule(int index)
		{
			if (index >= 0 && index < _Rules.Count)
			{
				_Rules.RemoveAt(index);
				return true;
			}
			return false;
		}
		public bool RemoveRule(Rule rule)
		{
			return _Rules.Remove(rule);
		}
		public void ChangeName(string name)
		{
			Name = name;
		}
		public void ChangeRule(int index, string text)
		{
			if (index >= 0 && index < Rules.Count)
			{
				_Rules[index].ChangeText(text);
			}
		}

		public override string ToString()
		{
			return ToString(new RuleFormatter(), 0).ToString();
		}
		public string ToString(RuleFormatter formatter, int index)
		{
			var sb = new StringBuilder();
			sb.AppendLineFeed(formatter.FormatName(this, 0));
			for (int r = 0; r < Rules.Count; ++r)
			{
				sb.AppendLineFeed(Rules[r].ToString(formatter, r, Rules.Count));
			}
			return sb.ToString();
		}
		public string ToString(SocketGuild guild)
		{
			return ToString();
		}
	}
}