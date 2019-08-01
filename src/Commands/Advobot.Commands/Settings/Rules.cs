﻿using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Advobot.Attributes;
using Advobot.Attributes.ParameterPreconditions.NumberValidation;
using Advobot.Attributes.ParameterPreconditions.StringLengthValidation;
using Advobot.Attributes.Preconditions.Permissions;
using Advobot.Commands.Localization;
using Advobot.Commands.Resources;
using Advobot.Formatting.Rules;
using Advobot.Modules;
using Advobot.Services.GuildSettings;
using AdvorangesUtils;
using Discord;
using Discord.Commands;

namespace Advobot.Commands.Settings
{
	public sealed class Rules : ModuleBase
	{
		[Group(nameof(ModifyRuleCategories)), ModuleInitialismAlias(typeof(ModifyRuleCategories))]
		[LocalizedSummary(nameof(Summaries.ModifyRuleCategories))]
		[UserPermissionRequirement(GuildPermission.Administrator)]
		[EnabledByDefault(false)]
		public sealed class ModifyRuleCategories : SettingsModule<IGuildSettings>
		{
			protected override IGuildSettings Settings => Context.Settings;

			[ImplicitCommand, ImplicitAlias]
			public Task<RuntimeResult> Create(
				[ValidateRuleCategory(ErrorOnCategoryExisting = true)] string name)
			{
				Settings.Rules.Categories.Add(name, new List<string>());
				return Responses.Rules.CreatedCategory(name);
			}
			[ImplicitCommand, ImplicitAlias]
			public Task<RuntimeResult> ModifyName(
				[ValidateRuleCategory] string category,
				[ValidateRuleCategory(ErrorOnCategoryExisting = true)] string newName)
			{
				var temp = Settings.Rules.Categories[category];
				Settings.Rules.Categories.Remove(category);
				Settings.Rules.Categories.Add(newName, temp);
				return Responses.Rules.ModifiedCategoryName(category, newName);
			}
			[ImplicitCommand, ImplicitAlias]
			public Task<RuntimeResult> Delete([ValidateRuleCategory] string category)
			{
				Settings.Rules.Categories.Remove(category);
				return Responses.Rules.DeletedCategory(category);
			}
		}

		[Group(nameof(ModifyRules)), ModuleInitialismAlias(typeof(ModifyRules))]
		[LocalizedSummary(nameof(Summaries.ModifyRules))]
		[UserPermissionRequirement(GuildPermission.Administrator)]
		[EnabledByDefault(false)]
		public sealed class ModifyRules : SettingsModule<IGuildSettings>
		{
			protected override IGuildSettings Settings => Context.Settings;

			[ImplicitCommand, ImplicitAlias]
			public Task<RuntimeResult> Add(
				[ValidateRuleCategory] string category,
				[ValidateRule] string rule)
			{
				if (Settings.Rules.Categories[category].CaseInsContains(rule))
				{
					return Responses.Rules.RuleAlreadyExists();
				}

				Settings.Rules.Categories[category].Add(rule);
				return Responses.Rules.AddedRule(category);
			}
			[ImplicitCommand, ImplicitAlias]
			public Task<RuntimeResult> Insert(
				[ValidateRuleCategory] string category,
				[ValidatePositiveNumber] int position,
				[ValidateRule] string rule)
			{
				var index = position - 1;
				if (Settings.Rules.Categories[category].Count > index)
				{
					return Responses.Rules.InvalidRuleInsert(position);
				}

				Settings.Rules.Categories[category].Insert(index, rule);
				return Responses.Rules.InsertedRule(category, position);
			}
			[ImplicitCommand, ImplicitAlias]
			public Task<RuntimeResult> Remove(
				[ValidateRuleCategory] string category,
				[ValidatePositiveNumber] int position)
			{
				var index = position - 1;
				if (Settings.Rules.Categories[category].Count > index)
				{
					return Responses.Rules.InvalidRuleRemove(position);
				}

				Settings.Rules.Categories[category].RemoveAt(index);
				return Responses.Rules.RemovedRule(category, position);
			}
		}

		[Group(nameof(PrintOutRules)), ModuleInitialismAlias(typeof(PrintOutRules))]
		[LocalizedSummary(nameof(Summaries.PrintOutRules))]
		[UserPermissionRequirement(PermissionRequirementAttribute.GenericPerms)]
		[EnabledByDefault(false)]
		public sealed class PrintOutRules : AdvobotModuleBase
		{
			[Command]
			public Task<RuntimeResult> Command(
				[ValidateRuleCategory] string? category,
				[Optional, Remainder] RuleFormatter? args)
				=> AdvobotResult.FromReasonSegments(Context.Settings.Rules.GetParts(args ?? new RuleFormatter(), category));
			[Command]
			public Task<RuntimeResult> Command([Optional, Remainder] RuleFormatter? args)
				=> Command(null, args);
		}
	}
}
