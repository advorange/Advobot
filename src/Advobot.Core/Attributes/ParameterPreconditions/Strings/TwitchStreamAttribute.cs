﻿using System;
using System.Threading.Tasks;

using Advobot.Utilities;

using AdvorangesUtils;

using Discord;
using Discord.Commands;

namespace Advobot.Attributes.ParameterPreconditions.Strings
{
	/// <summary>
	/// Validates the Twitch stream name by making sure it is between 4 and 25 characters and matches a Regex for Twitch usernames.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
	public sealed class TwitchStreamAttribute : StringRangeParameterPreconditionAttribute
	{
		/// <inheritdoc />
		public override string StringType => "Twitch stream name";

		/// <summary>
		/// Creates an instance of <see cref="TwitchStreamAttribute"/>.
		/// </summary>
		public TwitchStreamAttribute() : base(4, 25) { }

		/// <inheritdoc />
		protected override async Task<PreconditionResult> CheckPermissionsAsync(
			ICommandContext context,
			ParameterInfo parameter,
			IGuildUser invoker,
			string value,
			IServiceProvider services)
		{
			var result = await base.CheckPermissionsAsync(context, parameter, invoker, value, services).CAF();
			if (!result.IsSuccess)
			{
				return result;
			}

			if (RegexUtils.IsValidTwitchName(value))
			{
				return this.FromSuccess();
			}
			return PreconditionResult.FromError("Invalid Twitch username supplied.");
		}
	}
}