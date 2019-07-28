﻿namespace Advobot.CommandMarking.BannedPhrases
{
	/*
	[Category(typeof(EvaluateBannedRegex)), Group(nameof(EvaluateBannedRegex)), TopLevelShortAlias(typeof(EvaluateBannedRegex))]
	[Summary("Evaluates a regex (case is ignored). " +
		"The regex are also restricted to a 5,000 tick timeout. " +
		"Once a regex receives a good score then it can be used within the bot as a banned phrase.")]
	[PermissionRequirement(null, null)]
	[DefaultEnabled(false)]
	public sealed class EvaluateBannedRegex : AdvobotModuleBase
	{
		[Command]
		public async Task Command([ValidateString(Target.Regex)] string regex, [Remainder] string testPhrase)
		{
			Regex regexOutput;
			try
			{
				regexOutput = new Regex(regex);
			}
			catch (ArgumentException e)
			{
				await MessageUtils.SendErrorMessageAsync(Context, e)).CAF();
				return;
			}

			//Test to make sure it doesn't match stuff it shouldn't
			var matchesMessage = RegexUtils.IsMatch(testPhrase, regex);
			var matchesEmpty = RegexUtils.IsMatch("", regex);
			var matchesSpace = RegexUtils.IsMatch(" ", regex);
			var matchesNewLine = RegexUtils.IsMatch(Environment.NewLine, regex);
			var randomMatchCount = 0;
			for (var i = 0; i < 10; ++i)
			{
				var r = new Random();
				var p = new StringBuilder();
				for (var j = 0; j < r.Next(1, 100); ++j)
				{
					p.Append((char)r.Next(1, 10000));
				}
				if (RegexUtils.IsMatch(p.ToString(), regex))
				{
					++randomMatchCount;
				}
			}
			var matchesRandom = randomMatchCount >= 5;
			var okToUse = matchesMessage && !(matchesEmpty || matchesSpace || matchesNewLine || matchesRandom);

			//If the regex is ok then add it to the evaluated list
			if (okToUse)
			{
				var eval = Context.GuildSettings.EvaluatedRegex;
				if (eval.Count >= 5)
				{
					eval.RemoveAt(0);
				}
				eval.Add(regex);
			}

			var embed = new EmbedWrapper
			{
				Title = regex,
				Description = $"The given regex matches the given string: `{matchesMessage}`." +
					$"The given regex matches empty strings: `{matchesEmpty}`." +
					$"The given regex matches spaces: `{matchesSpace}`." +
					$"The given regex matches new lines: `{matchesNewLine}`." +
					$"The given regex matches random strings: `{matchesRandom}`." +
					$"The given regex is `{(okToUse ? "GOOD" : "BAD")}`.",
			};
			await MessageUtils.SendMessageAsync(Context.Channel, null, embed).CAF();
		}
	}

	[Category(typeof(ModifyBannedPhrases)), Group(nameof(ModifyBannedPhrases)), TopLevelShortAlias(typeof(ModifyBannedPhrases))]
	[Summary("Banned regex and strings delete messages if they are detected in them. " +
		"Banned names ban users if they join and they have them in their name.")]
	[PermissionRequirement(null, null)]
	[DefaultEnabled(false)]
	public sealed class ModifyBannedPhrases : AdvobotSettingsSavingModuleBase<IGuildSettings>
	{
		[Group(nameof(Regex)), ShortAlias(nameof(Regex))]
		public sealed class Regex : AdvobotSettingsSavingModuleBase<IGuildSettings>
		{
			[ImplicitCommand]
			public async Task Show()
				=> await ModifyBannedPhrases.Show(Context, Context.GuildSettings.BannedPhraseRegex, nameof(Regex)).CAF();
			[ImplicitCommand]
			public async Task ShowEvaluated()
			{
				var embed = new EmbedWrapper
				{
					Title = "Evaluted Regex",
					Description = Context.GuildSettings.EvaluatedRegex.FormatNumberedList(x => x)
				};
				await MessageUtils.SendMessageAsync(Context.Channel, null, embed).CAF();
			}
			[ImplicitCommand]
			public async Task Add(uint position)
			{
				if (position > Context.GuildSettings.EvaluatedRegex.Count)
				{
					await MessageUtils.SendErrorMessageAsync(Context, "Invalid position to add at.")).CAF();
					return;
				}
				if (Context.GuildSettings.BannedPhraseRegex.Count >= Context.BotSettings.MaxBannedRegex)
				{
					var error = $"You cannot have more than `{Context.BotSettings.MaxBannedRegex}` banned regex at a time.");
					await MessageUtils.SendErrorMessageAsync(Context, error).CAF();
					return;
				}

				--position;
				var regex = Context.GuildSettings.EvaluatedRegex[(int)position];
				Context.GuildSettings.BannedPhraseRegex.Add(new BannedPhrase(regex, default));
				await MessageUtils.MakeAndDeleteSecondaryMessageAsync(Context, $"Successfully added the regex `{regex}`.").CAF();
			}
			[Group(nameof(Remove)), ShortAlias(nameof(Remove))]
			public sealed class Remove : AdvobotModuleBase
			{
				[Command, Priority(1)]
				public async Task Command(uint position)
					=> await Remove(Context, Context.GuildSettings.BannedPhraseRegex, (int)position, nameof(Regex)).CAF();
				[Command]
				public async Task Command(string text)
					=> await Remove(Context, Context.GuildSettings.BannedPhraseRegex, text, nameof(Regex)).CAF();
			}
		}
		[Group(nameof(String)), ShortAlias(nameof(String))]
		public sealed class String : AdvobotSettingsSavingModuleBase<IGuildSettings>
		{
			[ImplicitCommand]
			public async Task Show()
				=> await ModifyBannedPhrases.Show(Context, Context.GuildSettings.BannedPhraseStrings, nameof(String)).CAF();
			[ImplicitCommand]
			public async Task Add(string text)
				=> await ModifyBannedPhrases.Add(Context, Context.GuildSettings.BannedPhraseStrings, text, nameof(String), Context.BotSettings.MaxBannedStrings).CAF();
			[Group(nameof(Remove)), ShortAlias(nameof(Remove))]
			public sealed class Remove : AdvobotModuleBase
			{
				[Command, Priority(1)]
				public async Task Command(uint position)
					=> await Remove(Context, Context.GuildSettings.BannedPhraseStrings, (int)position, nameof(String)).CAF();
				[Command]
				public async Task Command(string text)
					=> await Remove(Context, Context.GuildSettings.BannedPhraseStrings, text, nameof(String)).CAF();
			}
		}
		[Group(nameof(Name)), ShortAlias(nameof(Name))]
		public sealed class Name : AdvobotSettingsSavingModuleBase<IGuildSettings>
		{
			[ImplicitCommand]
			public async Task Show()
				=> await ModifyBannedPhrases.Show(Context, Context.GuildSettings.BannedPhraseNames, nameof(Name)).CAF();
			[ImplicitCommand]
			public async Task Add(string text)
				=> await ModifyBannedPhrases.Add(Context, Context.GuildSettings.BannedPhraseNames, text, nameof(Name), Context.BotSettings.MaxBannedNames).CAF();
			[Group(nameof(Remove)), ShortAlias(nameof(Remove))]
			public sealed class Remove : AdvobotModuleBase
			{
				[Command, Priority(1)]
				public async Task Command(uint position)
					=> await Remove(Context, Context.GuildSettings.BannedPhraseNames, (int)position, nameof(Name)).CAF();
				[Command]
				public async Task Command(string text)
					=> await Remove(Context, Context.GuildSettings.BannedPhraseNames, text, nameof(Name)).CAF();
			}
		}

		private static async Task Show<T>(ICommandContext context, IEnumerable<T> list, string type) where T : BannedPhrase
		{
			var embed = new EmbedWrapper
			{
				Title = $"Banned {type}",
				Description = list.FormatNumberedList(x => x.Phrase)
			};
			await MessageUtils.SendMessageAsync(context.Channel, null, embed).CAF();
		}
		private static async Task Add<T>(AdvobotCommandContext context, ICollection<T> list, string text, string type, int max) where T : BannedPhrase
		{
			if (list.Count >= max)
			{
				var error = $"You cannot have more than `{max}` banned {type} at a time.");
				await MessageUtils.SendErrorMessageAsync(context, error).CAF();
				return;
			}

			list.Add((T)new BannedPhrase(text, default));
			await MessageUtils.MakeAndDeleteSecondaryMessageAsync(context, $"Successfully removed the {type} `{text}`.").CAF();
		}
		private static async Task Remove<T>(AdvobotCommandContext context, IList<T> list, string text, string type) where T : BannedPhrase
		{
			var phrase = list.SingleOrDefault(x => x.Phrase.CaseInsEquals(text));
			if (phrase == null)
			{
				await MessageUtils.SendErrorMessageAsync(context, $"No banned {type} matches the text `{text}`.")).CAF();
				return;
			}

			list.Remove(phrase);
			await MessageUtils.MakeAndDeleteSecondaryMessageAsync(context, $"Successfully removed the {type} `{phrase.Phrase}`.").CAF();
		}
		private static async Task Remove<T>(AdvobotCommandContext context, IList<T> list, int position, string type) where T : BannedPhrase
		{
			if (position == default || position > list.Count)
			{
				await MessageUtils.SendErrorMessageAsync(context, "Invalid position to remove at.")).CAF();
				return;
			}

			--position;
			var phrase = list[position].Phrase;
			list.RemoveAt(position);
			await MessageUtils.MakeAndDeleteSecondaryMessageAsync(context, $"Successfully removed the banned {type} `{phrase}`.").CAF();
		}
	}

	[Category(typeof(ModifyPunishmentType)), Group(nameof(ModifyPunishmentType)), TopLevelShortAlias(typeof(ModifyPunishmentType))]
	[Summary("Changes the punishment type of the input string or regex to the given type. " +
		"`Show` lists the punishments of whatever type was specified.")]
	[PermissionRequirement(null, null)]
	[DefaultEnabled(false)]
	public sealed class ModifyPunishmentType : AdvobotSettingsSavingModuleBase<IGuildSettings>
	{
		[Group(nameof(Regex)), ShortAlias(nameof(Regex))]
		public sealed class Regex : AdvobotSettingsSavingModuleBase<IGuildSettings>
		{
			[ImplicitCommand]
			public async Task Show()
				=> await ModifyPunishmentType.Show(Context, Context.GuildSettings.BannedPhraseRegex, nameof(Regex)).CAF();
			[Command, Priority(1)]
			public async Task Command(uint position, Punishment punishment)
				=> await Modify(Context, Context.GuildSettings.BannedPhraseRegex, (int)position, punishment).CAF();
			[Command]
			public async Task Command(string text, Punishment punishment)
				=> await Modify(Context, Context.GuildSettings.BannedPhraseRegex, text, nameof(Regex), punishment).CAF();
		}
		[Group(nameof(String)), ShortAlias(nameof(String))]
		public sealed class String : AdvobotSettingsSavingModuleBase<IGuildSettings>
		{
			[ImplicitCommand]
			public async Task Show()
				=> await ModifyPunishmentType.Show(Context, Context.GuildSettings.BannedPhraseStrings, nameof(String)).CAF();
			[Command, Priority(1)]
			public async Task Command(uint position, Punishment punishment)
				=> await Modify(Context, Context.GuildSettings.BannedPhraseStrings, (int)position, punishment).CAF();
			[Command]
			public async Task Command(string text, Punishment punishment)
				=> await Modify(Context, Context.GuildSettings.BannedPhraseStrings, text, nameof(String), punishment).CAF();
		}

		private static async Task Show<T>(AdvobotCommandContext context, IList<T> list, string type) where T : BannedPhrase
		{
			var embed = new EmbedWrapper
			{
				Title = $"Banned {type} Punishments",
				Description = list.FormatNumberedList(x => x.ToString())
			};
			await MessageUtils.SendMessageAsync(context.Channel, null, embed).CAF();
		}
		private static async Task Modify<T>(AdvobotCommandContext context, IList<T> list, string text, string type, Punishment punishment) where T : BannedPhrase
		{
			var phrase = list.SingleOrDefault(x => x.Phrase.CaseInsEquals(text));
			if (phrase == null)
			{
				await MessageUtils.MakeAndDeleteSecondaryMessageAsync(context, $"No banned {type} matches the text `{text}`.").CAF();
				return;
			}

			phrase.Punishment = punishment;
			var resp = $"Successfully set the punishment of {phrase.Phrase} to {phrase.Punishment}.";
			await MessageUtils.MakeAndDeleteSecondaryMessageAsync(context, resp).CAF();
		}
		private static async Task Modify<T>(AdvobotCommandContext context, IList<T> list, int position, Punishment punishment) where T : BannedPhrase
		{
			if (position == default || position > list.Count)
			{
				await MessageUtils.SendErrorMessageAsync(context, "Invalid position to modify.")).CAF();
				return;
			}

			--position;
			var phrase = list[position];
			phrase.Punishment = punishment;
			var resp = $"Successfully set the punishment of {phrase.Phrase} to {phrase.Punishment}.";
			await MessageUtils.MakeAndDeleteSecondaryMessageAsync(context, resp).CAF();
		}
	}

	[Category(typeof(ModifyBannedPhrasePunishments)), Group(nameof(ModifyBannedPhrasePunishments)), TopLevelShortAlias(typeof(ModifyBannedPhrasePunishments))]
	[Summary("Sets a punishment for when a user reaches a specified number of banned phrases said. " +
		"Each message removed adds one to the total. " +
		"Time is in minutes.")]
	[PermissionRequirement(null, null)]
	[DefaultEnabled(false)]
	[SaveGuildSettings]
	public sealed class ModifyBannedPhrasePunishments : AdvobotModuleBase
	{
		[ImplicitCommand]
		public async Task Show()
		{
			var embed = new EmbedWrapper
			{
				Title = $"Banned Phrase Punishments",
				Description = Context.GuildSettings.BannedPhrasePunishments.FormatNumberedList(x => x.ToString())
			};
			await MessageUtils.SendMessageAsync(Context.Channel, null, embed).CAF();
		}
		[Group(nameof(Add)), ShortAlias(nameof(Add))]
		public sealed class Add : AdvobotModuleBase
		{
			[Command]
			public async Task Command(Punishment punishment, uint position, [Optional] uint time)
			{
				if (position == default)
				{
					await MessageUtils.SendErrorMessageAsync(Context, "Do not use zero.")).CAF();
					return;
				}
				if (Context.GuildSettings.BannedPhrasePunishments.Any(x => x.NumberOfRemoves == position))
				{
					var error = "A punishment already exists for that number of banned phrases said.");
					await MessageUtils.SendErrorMessageAsync(Context, error).CAF();
					return;
				}
				if (Context.GuildSettings.BannedPhrasePunishments.Count >= Context.BotSettings.MaxBannedPunishments)
				{
					var error = $"You cannot have more than `{Context.BotSettings.MaxBannedPunishments}` banned phrase punishments at a time.");
					await MessageUtils.SendErrorMessageAsync(Context, error).CAF();
					return;
				}

				var p = new BannedPhrasePunishment(punishment, (int)position, (int)time);
				Context.GuildSettings.BannedPhrasePunishments.Add(p);
				var resp = $"Successfully added the following banned phrase punishment: {p}.";
				await MessageUtils.MakeAndDeleteSecondaryMessageAsync(Context, resp).CAF();
			}
			[Command]
			public async Task Command([ValidateObject(Verif.CanBeEdited)] SocketRole role, uint position, [Optional] uint time)
			{
				if (position == default)
				{
					await MessageUtils.SendErrorMessageAsync(Context, "Do not use zero.")).CAF();
					return;
				}
				if (Context.GuildSettings.BannedPhrasePunishments.Any(x => x.NumberOfRemoves == position))
				{
					var error = "A punishment already exists for that number of banned phrases said.");
					await MessageUtils.SendErrorMessageAsync(Context, error).CAF();
					return;
				}
				if (Context.GuildSettings.BannedPhrasePunishments.Count >= Context.BotSettings.MaxBannedPunishments)
				{
					var error = $"You cannot have more than `{Context.BotSettings.MaxBannedPunishments}` banned phrase punishments at a time.");
					await MessageUtils.SendErrorMessageAsync(Context, error).CAF();
					return;
				}

				var p = new BannedPhrasePunishment(role, (int)position, (int)time);
				Context.GuildSettings.BannedPhrasePunishments.Add(p);
				var resp = $"Successfully added the following banned phrase punishment: {p.ToString(Context.Guild)}.";
				await MessageUtils.MakeAndDeleteSecondaryMessageAsync(Context, resp).CAF();
			}
		}
		[ImplicitCommand]
		public async Task Remove(uint position)
		{
			var removed = Context.GuildSettings.BannedPhrasePunishments.RemoveAll(x => x.NumberOfRemoves == position);
			if (removed < 1)
			{
				await MessageUtils.SendErrorMessageAsync(Context, $"No punishment has the position `{position}`.")).CAF();
				return;
			}

			var resp = $"Successfully removed the banned phrase punishment at `{position}`.";
			await MessageUtils.MakeAndDeleteSecondaryMessageAsync(Context, resp).CAF();
		}
	}
	*/
}