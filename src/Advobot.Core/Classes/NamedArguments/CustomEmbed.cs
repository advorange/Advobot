﻿using System;
using System.Collections.Generic;
using Advobot.Core.Classes.Attributes;
using Advobot.Core.Classes.TypeReaders;

namespace Advobot.Core.Classes.NamedArguments
{
	/// <summary>
	/// Allows a user to make an embed through the use of <see cref="NamedArguments{T}"/>.
	/// </summary>
	public class CustomEmbed
	{
		public const string FIELD_NAME = "FieldName";
		public const string FIELD_TEXT = "FieldText";
		public const string FIELD_INLINE = "FieldInline";
		public const string SPLIT_CHAR = "^";
		private static char _SplitChar = SPLIT_CHAR[0];
		public const string FORMAT = FIELD_NAME + ":Name" + SPLIT_CHAR + FIELD_TEXT + ":Text" + SPLIT_CHAR + FIELD_INLINE + ":True|False";

		public EmbedWrapper Embed { get; }

		public CustomEmbed() : this(null, null, null, null, null, null, null, null, null, null, null) { }
		[NamedArgumentConstructor]
		public CustomEmbed(
			[NamedArgument] string title,
			[NamedArgument] string description,
			[NamedArgument] string imageUrl,
			[NamedArgument] string url,
			[NamedArgument] string thumbUrl,
			[NamedArgument] string color,
			[NamedArgument] string authorName,
			[NamedArgument] string authorIconUrl,
			[NamedArgument] string authorUrl,
			[NamedArgument] string footer,
			[NamedArgument] string footerIconUrl,
			[NamedArgument(25)] params string[] fieldInfo)
		{
			Embed = new EmbedWrapper
			{
				Title = title,
				Description = description,
				Color = ColorTypeReader.GetColor(color),
				ImageUrl = imageUrl,
				Url = url,
				ThumbnailUrl = thumbUrl
			};
			Embed.TryAddAuthor(authorName, authorUrl, authorIconUrl, out _);
			Embed.TryAddFooter(footer, footerIconUrl, out _);

			//Fields are done is a very gross way
			foreach (var f in fieldInfo)
			{
				//Split at max three since there are three parts to each field. Name, text, and inline.
				var split = f.Split(new[] { _SplitChar }, 3);
				if (split.Length < 2)
				{
					continue;
				}

				//Create a dict to store the values
				var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
				{
					{ FIELD_NAME, null },
					{ FIELD_TEXT, null },
					{ FIELD_INLINE, null }
				};
				//Get the values by the standard split by colon
				foreach (var arg in split)
				{
					var splitArg = arg.Split(new[] { ':' }, 2);
					if (splitArg.Length == 2 && dict.ContainsKey(splitArg[0]))
					{
						dict[splitArg[0]] = splitArg[1];
					}
				}

				//Fields cannot be set if the name or text is null
				if (String.IsNullOrWhiteSpace(dict[FIELD_NAME]) || String.IsNullOrWhiteSpace(dict[FIELD_TEXT]))
				{
					continue;
				}

				//Finally try to parse if the inline is a bool or not
				bool.TryParse(dict[FIELD_INLINE], out var inline);
				Embed.TryAddField(dict[FIELD_NAME], dict[FIELD_TEXT], inline, out _);
			}
		}
	}
}