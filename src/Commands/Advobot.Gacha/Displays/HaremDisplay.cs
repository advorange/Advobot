﻿using Advobot.Gacha.Database;
using Advobot.Gacha.Models;
using AdvorangesUtils;
using Discord;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;

namespace Advobot.Gacha.Displays
{
	/// <summary>
	/// Displays a list of all the characters someone has claimed.
	/// </summary>
	public class HaremDisplay : PaginatedDisplay
	{
		private readonly User _User;
		private readonly Marriage? _Primary;

		public HaremDisplay(
			BaseSocketClient client,
			GachaDatabase db,
			User user) : base(client, db, user.Marriages.Count, Constants.CharactersPerPage)
		{
			_User = user;
			_Primary = _User.Marriages.FirstOrDefault();

			foreach (var marriage in _User.Marriages)
			{
				if (marriage.IsPrimaryMarriage)
				{
					_Primary = marriage;
				}
			}
		}

		protected override Task<Embed> GenerateEmbedAsync()
			=> Task.FromResult(GenerateEmbed());
		protected override Task<string> GenerateTextAsync()
			=> Task.FromResult("");
		private Embed GenerateEmbed()
		{
			var values = GetPageValues(_User.Marriages);
			var description = values.Select(x => x.Character.Name).Join("\n");

			return new EmbedBuilder
			{
				Description = description,
				ThumbnailUrl = _Primary?.Image?.Url,
				Author = new EmbedAuthorBuilder
				{
					Name = "Placeholder Name",
					IconUrl = "https://cdn.discordapp.com/attachments/367092372636434443/597957769038921758/image0-4-1.jpg",
				},
				Footer = GeneratePaginationFooter(),
			}.Build();
		}
	}
}
