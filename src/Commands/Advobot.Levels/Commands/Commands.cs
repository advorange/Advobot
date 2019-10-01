﻿using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Advobot.Attributes;
using Advobot.Levels.Database;
using Advobot.Levels.Utilities;

using AdvorangesUtils;

using Discord;
using Discord.Commands;

namespace Advobot.Levels.Commands
{
	[Category(nameof(Levels))]
	public sealed class Levels : ModuleBase
	{
		[Group(nameof(Level)), ModuleInitialismAlias(typeof(Level))]
		[Summary("temp")]
		[Meta("bebda6ba-6fbf-4278-94e0-408dcdc77d3c", IsEnabled = true)]
		public sealed class Level : LevelModuleBase
		{
			[Command]
			public Task<RuntimeResult> Command()
				=> Command(new SearchArgs(Context.User.Id, Context.Guild.Id));

			[Command]
			public Task<RuntimeResult> Command(IGuildUser user)
				=> Command(new SearchArgs(user.Id, user.Guild.Id));

			[Command]
			public async Task<RuntimeResult> Command(SearchArgs args)
			{
				var rank = await Service.GetRankAsync(args).CAF();
				var level = Service.CalculateLevel(rank.Experience);
				var user = Context.Client.GetUser(rank.GetUserId());
				return Responses.Levels.Level(args, rank, level, user);
			}
		}

		[Group(nameof(Top)), ModuleInitialismAlias(typeof(Top))]
		[Summary("temp")]
		[Meta("649ec476-4043-48b0-9802-62a9288d007b", IsEnabled = true)]
		public sealed class Top : LevelModuleBase
		{
			public const int PAGE_LENGTH = 15;

			private ulong ChannelId => Context.Channel.Id;
			private ulong GuildId => Context.Guild.Id;

			[Command(nameof(Channel))]
			public Task<RuntimeResult> Channel([Optional] int page)
				=> Command(new SearchArgs(guildId: GuildId, channelId: ChannelId), page);

			[Command(nameof(Global))]
			public Task<RuntimeResult> Global([Optional] int page)
				=> Command(new SearchArgs(), page);

			[Command]
			public Task<RuntimeResult> Guild([Optional] int page)
				=> Command(new SearchArgs(guildId: GuildId), page);

			private async Task<RuntimeResult> Command(ISearchArgs args, int page)
			{
				var offset = PAGE_LENGTH * page;
				var ranks = await Service.GetRanksAsync(args, offset, PAGE_LENGTH).CAF();
				return Responses.Levels.Top(args, ranks, x =>
				{
					var level = Service.CalculateLevel(x.Experience);
					var user = Context.Client.GetUser(x.GetUserId());
					return (level, user);
				});
			}
		}
	}
}