﻿using Discord.Commands;
using System;
using Advobot.Classes.Attributes;
using Advobot.Gacha.Models;
using System.Threading.Tasks;
using Discord;
using AdvorangesUtils;
using Advobot.Gacha.Database;
using Microsoft.Extensions.DependencyInjection;
using Advobot.Utilities;

namespace Advobot.Gacha.TypeReaders
{
	[TypeReaderTargetType(typeof(User))]
	public sealed class UserTypeReader : TypeReader
	{
		public bool CreateIfNotFound { get; set; }

		private readonly UserTypeReader<IUser> _UserTypeReader = new UserTypeReader<IUser>();

		//TODO: add in the ability to get users who have left the server
		public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
		{
			var result = await _UserTypeReader.ReadAsync(context, input, services).CAF();
			if (!result.IsSuccess) //Can't find a discord user with the supplied string
			{
				return result;
			}

			var user = (IUser)result.BestMatch;
			var db = services.GetRequiredService<GachaDatabase>();
			var entry = await db.GetUserAsync(context.Guild.Id, user.Id).CAF();
			if (entry != null) //Profile already exists, can return that
			{
				return TypeReaderResult.FromSuccess(user);
			}
			else if (!CreateIfNotFound) //Profile doesn't exist and this is something like checking their harem
			{
				return TypeReaderResult.FromError(CommandError.ObjectNotFound,
					$"{user.Format()} does not have a profile.");
			}
			else if (!(user is IGuildUser guildUser))
			{
				return TypeReaderResult.FromError(CommandError.ObjectNotFound,
					$"{user.Format()} is not in the guild.");
			}
			else
			{
				var newEntry = new User(guildUser);
				await db.AddUserAsync(newEntry).CAF();
				return TypeReaderResult.FromSuccess(newEntry);
			}
		}
	}
}