﻿using System;

namespace Advobot.Gacha.Models
{
	[Flags]
	public enum Gender : ulong
	{
		Male = (1U << 0),
		Female = (1U << 1),
		Other = (1U << 2),
	}
}