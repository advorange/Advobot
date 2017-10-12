﻿using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Advobot.Classes.TypeReaders
{
	/// <summary>
	/// Returns custom arguments.
	/// </summary>
	public sealed class CustomArgumentsTypeReader<T> : TypeReader where T : class
	{
		/// <summary>
		/// Creates custom arguments from the given input.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="input"></param>
		/// <param name="services"></param>
		/// <returns></returns>
		public override Task<TypeReaderResult> Read(ICommandContext context, string input, IServiceProvider services)
		{
			return Task.FromResult(TypeReaderResult.FromSuccess(new CustomArguments<T>(input)));
		}
	}
}
