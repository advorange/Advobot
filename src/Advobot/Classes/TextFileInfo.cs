﻿using AdvorangesUtils;

namespace Advobot.Classes
{
	/// <summary>
	/// Holds information about what how to send a text file through Discord.
	/// </summary>
	public class TextFileInfo
	{
		/// <summary>
		/// The name of the text file. This may have invalid characters for file names in it, but Discord will just remove those.
		/// </summary>
		public string Name
		{
			get => _Name == null ? null : $"{_Name}_{Formatting.ToSaving()}.txt";
			set => _Name = value?.FormatTitle()?.Replace(' ', '_')?.TrimEnd('_');
		}
		/// <summary>
		/// The text of the text file.
		/// </summary>
		public string Text
		{
			get => _Text;
			set => _Text = value?.Trim();
		}

		private string _Name;
		private string _Text;

		/// <summary>
		/// Creates an instance of textfileinfo.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="text"></param>
		public TextFileInfo(string name = null, string text = null)
		{
			Name = name;
			Text = text;
		}
	}
}