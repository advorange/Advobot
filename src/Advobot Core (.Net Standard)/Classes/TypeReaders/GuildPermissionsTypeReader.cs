﻿using Advobot.Actions;
using Advobot.Classes.Permissions;
using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Advobot.Classes.TypeReaders
{
	/// <summary>
	/// Attempts to get a ulong representing guild permissions.
	/// </summary>
	public sealed class GuildPermissionsTypeReader : TypeReader
	{
		/// <summary>
		/// Checks for valid ulong first, then checks permission names.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="input"></param>
		/// <param name="services"></param>
		/// <returns></returns>
		public override Task<TypeReaderResult> Read(ICommandContext context, string input, IServiceProvider services)
		{
			//Check numbers first
			if (ulong.TryParse(input, out ulong rawValue))
			{
				return Task.FromResult(TypeReaderResult.FromSuccess(rawValue));
			}
			//Then check permission names
			else if (!GuildPerms.TryGetValidGuildPermissionNamesFromInputString(input, out var validPerms, out var invalidPerms))
			{
				var str = $"Invalid permission{GetActions.GetPlural(invalidPerms.Count())} provided: `{String.Join("`, `", invalidPerms)}`.";
				return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, str));
			}
			else
			{
				return Task.FromResult(TypeReaderResult.FromSuccess(GuildPerms.ConvertToValue(validPerms)));
			}
		}
	}
}
