﻿using System;

namespace Advobot.Attributes.ParameterPreconditions.Numbers
{
	/// <summary>
	/// Validates the guild afk timer in seconds allowing specified valid values.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
	public class GuildAfkTimeAttribute : IntParameterPreconditionAttribute
	{
		/// <inheritdoc />
		public override string NumberType => "afk time";

		/// <summary>
		/// Creates an instance of <see cref="GuildAfkTimeAttribute"/>.
		/// </summary>
		public GuildAfkTimeAttribute() : base(new[] { 60, 300, 900, 1800, 3600 }) { }
	}
}