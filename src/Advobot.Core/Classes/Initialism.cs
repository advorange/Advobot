﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Advobot.Core.Utilities;

namespace Advobot.Core.Classes
{
	/// <summary>
	/// Creates an initialism out of the passed in name. Keeps track of the parts and original.
	/// </summary>
	public sealed class Initialism
	{
		private static Dictionary<string, string> _ShortenedPhrases = new Dictionary<string, string>
		{
			{ "clear", "clr" }
		};

		public string Original { get; }
		public string Edited { get; private set; }
		public ImmutableList<string> Parts { get; }
		public ImmutableList<string> Aliases => _OtherAliases.Concat(new[] { Edited }).ToImmutableList();
		private string[] _OtherAliases;

		public Initialism(string name, string[] otherAliases, bool topLevel)
		{
			var edittingName = name;
			var parts = new List<StringBuilder>();
			var initialism = new StringBuilder();

			if (topLevel)
			{
				foreach (var kvp in _ShortenedPhrases)
				{
					edittingName = edittingName.CaseInsReplace(kvp.Key, kvp.Value.ToUpper());
				}

				if (name.EndsWith("s"))
				{
					edittingName = edittingName.Substring(0, edittingName.Length - 1) + "S";
				}
			}

			foreach (var c in edittingName)
			{
				if (Char.IsUpper(c))
				{
					initialism.Append(c);
					//ToString HAS to be called here or else it uses the capacity int constructor
					parts.Add(new StringBuilder(c.ToString()));
				}
				else
				{
					parts[parts.Count - 1].Append(c);
				}
			}

			Original = name;
			Parts = parts.Select(x => x.ToString()).ToImmutableList();
			Edited = initialism.ToString().ToLower();
			_OtherAliases = otherAliases;
		}

		public void AppendToInitialismByPart(int index, int length)
		{
			var newInitialism = new StringBuilder();
			for (var i = 0; i < Parts.Count; ++i)
			{
				var p = Parts[i];
				var l = i == index ? length : 1;
				newInitialism.Append(p.Substring(0, l));
			}
			Edited = newInitialism.ToString().ToLower();
		}
	}
}
