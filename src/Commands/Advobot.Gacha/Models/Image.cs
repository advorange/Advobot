﻿using Advobot.Gacha.ReadOnlyModels;

namespace Advobot.Gacha.Models
{
	public class Image : IReadOnlyImage
	{
		public long CharacterId { get; set; }
		public string? Url { get; set; }

		public Image() { }
		public Image(IReadOnlyCharacter character)
		{
			CharacterId = character.CharacterId;
		}
	}
}