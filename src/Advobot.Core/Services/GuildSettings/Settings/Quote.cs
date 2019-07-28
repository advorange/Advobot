﻿using System;
using Advobot.Classes.Formatting;
using Advobot.Interfaces;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Advobot.Services.GuildSettings.Settings
{
	/// <summary>
	/// Holds a name and description.
	/// </summary>
	public sealed class Quote : IGuildFormattable, INameable
	{
		/// <summary>
		/// The name of the quote.
		/// </summary>
		[JsonProperty("Name")]
		public string Name { get; set; }
		/// <summary>
		/// The description of the quote.
		/// </summary>
		[JsonProperty("Description")]
		public string Description { get; set; }

		/// <summary>
		/// Creates an instance of <see cref="Quote"/>.
		/// </summary>
		public Quote() : this("", "") { }
		/// <summary>
		/// Creates an instance of <see cref="Quote"/>.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="description"></param>
		public Quote(string name, string description)
		{
			Name = name ?? throw new ArgumentException(name, nameof(name));
			Description = description ?? throw new ArgumentException(description, nameof(description));
		}

		/// <inheritdoc />
		public IDiscordFormattableString GetFormattableString()
		{
			return new Dictionary<string, object>
			{
				{ "Name", Name },
				{ "Description", Description },
			}.ToDiscordFormattableStringCollection();
		}
	}
}