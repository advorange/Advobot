﻿using Advobot.Classes;
using Advobot.Modules;
using Advobot.Utilities;

using AdvorangesUtils;

using Discord;
using Discord.WebSocket;

namespace Advobot.Settings.Responses
{
	public sealed class GuildSettings : CommandResponses
	{
		private GuildSettings()
		{
		}

		public static AdvobotResult DisplayJson(ISettingsBase settings)
		{
			return Success(new TextFileInfo
			{
				Name = "Settings_JSON",
				Text = IOUtils.Serialize(settings),
			});
		}

		public static AdvobotResult DisplayNames(ISettingsBase settings)
		{
			return Success(new EmbedWrapper
			{
				Title = Title.FormatInterpolated($"{settings.GetType().Name}"),
				Description = Default.FormatInterpolated($"{settings.GetSettingNames()}"),
			});
		}

		public static AdvobotResult DisplaySetting(BaseSocketClient client, SocketGuild guild, ISettingsBase settings, string name)
		{
			var description = settings.FormatSetting(name).ToString(client, guild, Default);
			if (description.Length <= EmbedBuilder.MaxDescriptionLength)
			{
				return Success(new EmbedWrapper
				{
					Title = name,
					Description = description,
				});
			}
			return Success(new TextFileInfo
			{
				Name = name,
				Text = description,
			});
		}

		public static AdvobotResult DisplaySettings(BaseSocketClient client, SocketGuild guild, ISettingsBase settings)
		{
			return Success(new TextFileInfo
			{
				Name = settings.GetType().Name.FormatTitle().Replace(' ', '_'),
				Text = settings.Format().ToString(client, guild, Default),
			});
		}

		public static AdvobotResult Reset(string name)
			=> Success(Default.FormatInterpolated($"Successfully reset the setting {name}"));

		public static AdvobotResult ResetAll()
			=> Success($"Successfully reset all settings.");
	}
}