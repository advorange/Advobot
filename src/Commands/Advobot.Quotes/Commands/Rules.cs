﻿using System.Linq;
using System.Threading.Tasks;

using Advobot.Attributes;
using Advobot.Attributes.Preconditions.Permissions;
using Advobot.Localization;
using Advobot.Modules;
using Advobot.Quotes.Database;
using Advobot.Quotes.Formatting;
using Advobot.Quotes.Models;
using Advobot.Quotes.ParameterPreconditions;
using Advobot.Quotes.ReadOnlyModels;
using Advobot.Resources;

using AdvorangesUtils;

using Discord.Commands;

using static Advobot.Quotes.Responses.Rules;

namespace Advobot.Quotes.Commands
{
	[Category(nameof(Rules))]
	public sealed class Rules : ModuleBase
	{
		[LocalizedGroup(nameof(Groups.ModifyRuleCategories))]
		[LocalizedAlias(nameof(Aliases.ModifyRuleCategories))]
		[LocalizedSummary(nameof(Summaries.ModifyRuleCategories))]
		[Meta("29ce9d5e-59c0-4262-8922-e444a9fc0ec6")]
		[RequireGuildPermissions]
		public sealed class ModifyRuleCategories : RuleModuleBase
		{
			[LocalizedCommand(nameof(Groups.Create))]
			[LocalizedAlias(nameof(Aliases.Create))]
			public async Task<RuntimeResult> Create(
				[Remainder, Rule]
				string value)
			{
				var categories = await Db.GetCategoriesAsync(Context.Guild.Id).CAF();

				var category = new RuleCategory
				{
					GuildId = Context.Guild.Id,
					Value = value,
					Category = categories.Count + 1,
				};
				await Db.UpsertRuleCategoryAsync(category).CAF();
				return CreatedCategory(category);
			}

			[LocalizedCommand(nameof(Groups.Delete))]
			[LocalizedAlias(nameof(Aliases.Delete))]
			public async Task<RuntimeResult> Delete(IReadOnlyRuleCategory category)
			{
				await Db.DeleteRuleCategoryAsync(category).CAF();
				return DeletedCategory(category);
			}

			[LocalizedCommand(nameof(Groups.ModifyValue))]
			[LocalizedAlias(nameof(Aliases.ModifyValue))]
			public async Task<RuntimeResult> ModifyValue(
				IReadOnlyRuleCategory category,
				[Remainder, Rule]
				string value)
			{
				var copy = new RuleCategory(category)
				{
					Value = value,
				};
				await Db.UpsertRuleCategoryAsync(copy).CAF();
				return ModifiedCategoryValue(copy);
			}

			[LocalizedCommand(nameof(Groups.Swap))]
			[LocalizedAlias(nameof(Aliases.Swap))]
			public async Task<RuntimeResult> Swap(
				IReadOnlyRuleCategory categoryA,
				IReadOnlyRuleCategory categoryB)
			{
				var copyA = new RuleCategory(categoryA)
				{
					Category = categoryB.Category,
				};
				var rulesA = await Db.GetRulesAsync(categoryA).CAF();
				var copyRulesA = rulesA.Select(x => new Rule(x)
				{
					Category = copyA.Category,
				});

				var copyB = new RuleCategory(categoryB)
				{
					Category = categoryA.Category,
				};
				var rulesB = await Db.GetRulesAsync(categoryB).CAF();
				var copyRulesB = rulesB.Select(x => new Rule(x)
				{
					Category = copyB.Category,
				});

				await Db.UpsertRuleCategoryAsync(copyA).CAF();
				await Db.UpsertRuleCategoryAsync(copyB).CAF();
				await Db.UpsertRulesAsync(copyRulesA.Concat(copyRulesB)).CAF();

				// Example:
				// 1.1, 1.2, 1.3, 1.4 | 2.1, 2.2
				// 1.1, 1.2, [1.3, 1.4] | 2.1, 2.2, 2.3, 2.4
				// 1.3 and 1.4 aren't upserted; only 2 rules from 2.x overwrite 1.1 and 1.2
				// so they need to be deleted manually
				if (rulesA.Count != rulesB.Count)
				{
					var (longer, shorter) = rulesA.Count > rulesB.Count
						? (rulesA, rulesB)
						: (rulesB, rulesA);
					var needsDeleting = longer.Skip(shorter.Count);
					await Db.DeleteRulesAsync(needsDeleting).CAF();
				}

				return SwappedRuleCategories(categoryA, rulesA, categoryB, rulesB);
			}
		}

		[LocalizedGroup(nameof(Groups.ModifyRules))]
		[LocalizedAlias(nameof(Aliases.ModifyRules))]
		[LocalizedSummary(nameof(Summaries.ModifyRules))]
		[Meta("2808540d-9dd7-4c4a-bd87-b6bd83c37cd5")]
		[RequireGuildPermissions]
		public sealed class ModifyRules : RuleModuleBase
		{
			[LocalizedCommand(nameof(Groups.Create))]
			[LocalizedAlias(nameof(Aliases.Create))]
			public async Task<RuntimeResult> Create(
				IReadOnlyRuleCategory category,
				[Remainder, Rule]
				string value)
			{
				var rules = await Db.GetRulesAsync(category).CAF();

				var rule = new Rule()
				{
					GuildId = Context.Guild.Id,
					Category = category.Category,
					Value = value,
					Position = rules.Count + 1,
				};
				await Db.UpsertRuleAsync(rule).CAF();
				return AddedRule(category);
			}

			[LocalizedCommand(nameof(Groups.Delete))]
			[LocalizedAlias(nameof(Aliases.Delete))]
			public async Task<RuntimeResult> Delete(IReadOnlyRule rule)
			{
				await Db.DeleteRuleAsync(rule).CAF();
				return RemovedRule(rule);
			}

			[LocalizedCommand(nameof(Groups.ModifyValue))]
			[LocalizedAlias(nameof(Aliases.ModifyValue))]
			public async Task<RuntimeResult> ModifyValue(
				IReadOnlyRule rule,
				[Remainder, Rule]
				string value)
			{
				var copy = new Rule(rule)
				{
					Value = value,
				};
				await Db.UpsertRuleAsync(copy).CAF();
				return ModifiedRuleValue(copy);
			}

			[LocalizedCommand(nameof(Groups.Swap))]
			[LocalizedAlias(nameof(Aliases.Swap))]
			public async Task<RuntimeResult> Swap(IReadOnlyRule ruleA, IReadOnlyRule ruleB)
			{
				var copyA = new Rule(ruleA)
				{
					Position = ruleB.Position,
				};
				var copyB = new Rule(ruleB)
				{
					Position = ruleA.Position,
				};

				await Db.UpsertRulesAsync(new[] { copyA, copyB }).CAF();
				return SwappedRules(ruleA, ruleB);
			}
		}

		[LocalizedGroup(nameof(Groups.PrintOutRules))]
		[LocalizedAlias(nameof(Aliases.PrintOutRules))]
		[LocalizedSummary(nameof(Summaries.PrintOutRules))]
		[Meta("9ae48ca4-68a3-468f-8a6c-2cffd4483deb")]
		[RequireGuildPermissions]
		public sealed class PrintOutRules : RuleModuleBase
		{
			[Command]
			public async Task<RuntimeResult> Command(RuleFormatter? args = null)
			{
				args ??= new RuleFormatter();

				var dict = await Db.GetRuleDictionaryAsync(Context.Guild.Id).CAF();
				return AdvobotResult.Success(args.Format(dict));
			}

			[Command]
			public async Task<RuntimeResult> Command(
				IReadOnlyRuleCategory category,
				RuleFormatter? args = null)
			{
				args ??= new RuleFormatter();

				var rules = await Db.GetRulesAsync(category).CAF();
				return AdvobotResult.Success(args.Format(category, rules));
			}
		}
	}
}