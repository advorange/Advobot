﻿using System;
using System.Resources;
using Discord.Commands;

namespace Advobot.Localization
{
	/// <summary>
	/// Used for a localized group.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public abstract class LocalizedGroupBaseAttribute : GroupAttribute, ILocalized
	{
		/// <summary>
		/// The name of the group to use for localization.
		/// </summary>
		public string Name { get; }
		/// <inheritdoc />
		public ResourceManager ResourceManager { get; }

		/// <summary>
		/// Creates an instance of <see cref="LocalizedGroupBaseAttribute"/>.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="resources"></param>
		public LocalizedGroupBaseAttribute(string name, ResourceManager resources)
			: base(resources.GetString(name))
		{
			Name = name;
			ResourceManager = resources;
		}
	}
}
