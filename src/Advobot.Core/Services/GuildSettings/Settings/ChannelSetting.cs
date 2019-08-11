﻿using System;

namespace Advobot.Services.GuildSettings.Settings
{
	/// <summary>
	/// Specifies what external settings to use on a channel.
	/// </summary>
	[Flags]
	public enum ChannelSetting : uint
	{
		/// <summary>
		/// Indicates that the channel only accepts messages with images in them
		/// </summary>
		ImageOnly = (1U << 0),
	}
}