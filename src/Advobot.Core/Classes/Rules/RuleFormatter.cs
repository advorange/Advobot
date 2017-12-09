﻿using Advobot.Core.Actions;
using Advobot.Core.Actions.Formatting;
using Advobot.Core.Classes.Attributes;
using Advobot.Core.Enums;
using Discord;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Advobot.Core.Classes.Rules
{
	public class RuleFormatter
	{
		private static Dictionary<RuleFormat, MarkDownFormat> _DefaultTitleFormats = new Dictionary<RuleFormat, MarkDownFormat>
		{
			{ default, MarkDownFormat.Bold },
			{ RuleFormat.Numbers, MarkDownFormat.Bold },
			{ RuleFormat.Dashes, MarkDownFormat.Code },
			{ RuleFormat.Bullets, MarkDownFormat.Bold },
			{ RuleFormat.Bold, MarkDownFormat.Bold | MarkDownFormat.Italics },
		};
		private static Dictionary<RuleFormat, MarkDownFormat> _DefaultRuleFormats = new Dictionary<RuleFormat, MarkDownFormat>
		{
			{ default, default },
			{ RuleFormat.Numbers, default },
			{ RuleFormat.Dashes, default },
			{ RuleFormat.Bullets, default },
			{ RuleFormat.Bold, MarkDownFormat.Bold },
		};

		private string _Rules;
		public string Rules => this._Rules;
		private List<string> _Categories = new List<string>();
		public ImmutableList<string> Categories => this._Categories.ToImmutableList();
			 
		private RuleFormat _Format;
		private MarkDownFormat _TitleFormat;
		private MarkDownFormat _RuleFormat;
		private RuleFormatOption _Options;
		private char _CharAfterNumbers;

		[CustomArgumentConstructor]
		public RuleFormatter(
			[CustomArgument] RuleFormat format = default,
			[CustomArgument] MarkDownFormat titleFormat = default,
			[CustomArgument] MarkDownFormat ruleFormat = default,
			[CustomArgument] char charAfterNumbers = '.',
			[CustomArgument(10)] params RuleFormatOption[] formatOptions)
		{
			this._Format = format == default ? RuleFormat.Numbers : format;
			this._TitleFormat = titleFormat;
			this._RuleFormat = ruleFormat;
			this._CharAfterNumbers = charAfterNumbers;
			formatOptions.ToList().ForEach(x => this._Options |= x);
		}

		public void SetRulesAndCategories(RuleHolder rules)
		{
			this._Rules = rules.ToString(this);
			this._Categories.AddRange(rules.Categories.Select((x, index) => x.ToString(this, index)));
		}
		public void SetCategory(RuleCategory category, int index) => this._Categories.Add(category.ToString(this, index));

		public string FormatName(RuleCategory category, int index)
		{
			var n = "";
			switch (this._Format)
			{
				case RuleFormat.Numbers:
				case RuleFormat.Bullets:
				case RuleFormat.Bold:
				{
					n = $"{category.Name.FormatTitle()}";
					break;
				}
				case RuleFormat.Dashes:
				{
					n = $"{index + 1} - {category.Name.FormatTitle()}";
					break;
				}
				default:
				{
					n = category.Name.FormatTitle();
					break;
				}
			}

			n = n.Trim(' ');
			if (this._Options.HasFlag(RuleFormatOption.ExtraLines))
			{
				n = n + "\n";
			}
			return AddFormattingOptions(this._TitleFormat == default ? _DefaultTitleFormats[this._Format] : this._TitleFormat, n);
		}
		public string FormatRule(Rule rule, int index, int rulesInCategory)
		{
			var r = "";
			switch (this._Format)
			{
				case RuleFormat.Numbers:
				case RuleFormat.Bold:
				{
					if (this._Options.HasFlag(RuleFormatOption.NumbersSameLength))
					{
						r = $"`{(index + 1).ToString().PadLeft(rulesInCategory.GetLengthOfNumber(), '0')}";
					}
					else
					{
						r = $"`{index + 1}`";
					}
					break;
				}
				case RuleFormat.Dashes:
				{
					r = $"-";
					break;
				}
				case RuleFormat.Bullets:
				{
					r = $"•";
					break;
				}
				default:
				{
					r = "";
					break;
				}
			}

			r = $"{r}{rule.Text}";
			r = this._CharAfterNumbers != default
				? AddCharAfterNumbers(r, this._CharAfterNumbers)
				: r;
			r = r.Trim(' ');
			if (this._Options.HasFlag(RuleFormatOption.ExtraLines))
			{
				r = r + "\n";
			}
			return AddFormattingOptions(this._RuleFormat == default ? _DefaultRuleFormats[this._Format] : this._RuleFormat, r);
		}

		private string AddCharAfterNumbers(string text, char charToAdd)
		{
			var sb = new StringBuilder();
			for (int i = 0; i < text.Length; ++i)
			{
				var c = text[i];
				sb.Append(c);

				//If the last character in a string then add a period since it's the end
				//If the next character after is not a number add a period too
				if (Char.IsNumber(c) && (i + 1 == text.Length || !Char.IsNumber(text[i + 1])))
				{
					sb.Append(charToAdd);
				}
			}
			return sb.ToString();
		}
		private string AddFormattingOptions(MarkDownFormat formattingOptions, string text)
		{
			foreach (MarkDownFormat md in Enum.GetValues(typeof(MarkDownFormat)))
			{
				if ((formattingOptions & md) != 0)
				{
					text = AddMarkDown(md, text);
				}
			}
			return text;
		}
		private string AddMarkDown(MarkDownFormat md, string text)
		{
			switch (md)
			{
				case MarkDownFormat.Bold:
				{
					return $"**{text}**";
				}
				case MarkDownFormat.Italics:
				{
					return $"*{text}*";
				}
				case MarkDownFormat.Code:
				{
					return $"`{text.EscapeBackTicks()}`";
				}
				default:
				{
					return text;
				}
			}
		}

		public async Task<IReadOnlyList<IUserMessage>> SendAsync(IMessageChannel channel)
		{
			var messages = new List<IUserMessage>();
			//If all of the rules can be sent in one message, do that.
			if (this._Rules != null && this._Rules.Length <= 2000)
			{
				messages.Add(await MessageActions.SendMessageAsync(channel, this._Rules).CAF());
				return messages.AsReadOnly();
			}

			//If not, go by category
			foreach (var category in this._Categories)
			{
				if (category == null)
				{
					continue;
				}
				else if (category.Length <= 2000)
				{
					messages.Add(await MessageActions.SendMessageAsync(channel, category).CAF());
					continue;
				}

				var sb = new StringBuilder();
				foreach (var part in category.Split('\n'))
				{
					if (sb.Length + part.Length <= 2000)
					{
						messages.Add(await MessageActions.SendMessageAsync(channel, sb.ToString()).CAF());
						sb.Clear();
					}
					sb.Append(part);
				}
				if (sb.Length > 0)
				{
					messages.Add(await MessageActions.SendMessageAsync(channel, sb.ToString()).CAF());
				}
			}
			return messages.AsReadOnly();
		}
	}
}
