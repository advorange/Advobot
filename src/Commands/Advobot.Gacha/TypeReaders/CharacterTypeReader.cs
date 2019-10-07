﻿using System;
using System.Threading.Tasks;

using Advobot.Attributes;
using Advobot.Gacha.Database;
using Advobot.Gacha.ReadOnlyModels;

using AdvorangesUtils;

using Discord.Commands;

using Microsoft.Extensions.DependencyInjection;

namespace Advobot.Gacha.TypeReaders
{
	[TypeReaderTargetType(typeof(IReadOnlyCharacter))]
	public sealed class CharacterTypeReader : TypeReader
	{
		public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
		{
			var db = services.GetRequiredService<GachaDatabase>();
			var id = int.Parse(input);
			return TypeReaderResult.FromSuccess(await db.GetCharacterAsync(id).CAF());
		}
	}
}