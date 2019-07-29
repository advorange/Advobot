﻿using System;
using System.Threading.Tasks;
using Advobot.Attributes;
using Advobot.Attributes.ParameterPreconditions.StringLengthValidation;
using Advobot.Attributes.Preconditions;
using Advobot.Modules;
using Advobot.Services.ImageResizing;
using Advobot.Utilities;
using AdvorangesUtils;
using Discord;
using Discord.Commands;

namespace Advobot.CommandMarking
{
	public sealed class Client : ModuleBase
	{
		[Group(nameof(ModifyBotName)), ModuleInitialismAlias(typeof(ModifyBotName))]
		[Summary("Changes the bot's name to the given name.")]
		[RequireBotOwner]
		[EnabledByDefault(true)]
		public sealed class ModifyBotName : AdvobotModuleBase
		{
			[Command]
			public async Task<RuntimeResult> Command([Remainder, ValidateUsername] string name)
			{
				await Context.Client.CurrentUser.ModifyAsync(x => x.Username = name).CAF();
				return Responses.Snowflakes.ModifiedName(Context.Client.CurrentUser, name);
			}
		}

		[Group(nameof(ModifyBotIcon)), ModuleInitialismAlias(typeof(ModifyBotIcon))]
		[Summary("Changes the bot's icon to the given image. " +
			"The image must be smaller than 2.5MB.")]
		[RequireBotOwner]
		[EnabledByDefault(true)]
		public sealed class ModifyBotIcon : ImageResizerModule
		{
			[Command]
			public Task<RuntimeResult> Command(Uri url)
			{
				var position = Enqueue(new IconCreationContext(Context, url, default, "Bot Icon",
					(ctx, ms) => ctx.Client.CurrentUser.ModifyAsync(x => x.Avatar = new Image(ms), ctx.GenerateRequestOptions())));
				return Responses.Snowflakes.EnqueuedIcon(Context.Client.CurrentUser, position);
			}
			[ImplicitCommand, ImplicitAlias]
			public async Task<RuntimeResult> Remove()
			{
				await Context.Client.CurrentUser.ModifyAsync(x => x.Avatar = new Image()).CAF();
				return Responses.Snowflakes.RemovedIcon(Context.Client.CurrentUser);
			}
		}

		[Group(nameof(DisconnectBot)), ModuleInitialismAlias(new[] { "runescapeservers" }, typeof(DisconnectBot))]
		[Summary("Turns the bot off.")]
		[RequireBotOwner]
		[EnabledByDefault(true)]
		public sealed class DisconnectBot : AdvobotModuleBase
		{
			[Command(RunMode = RunMode.Async)]
			public Task Command()
				=> Context.Client.DisconnectBotAsync();
		}

		[Group(nameof(RestartBot)), ModuleInitialismAlias(typeof(RestartBot))]
		[Summary("Restarts the bot.")]
		[RequireBotOwner]
		[EnabledByDefault(true)]
		public sealed class RestartBot : AdvobotModuleBase
		{
			[Command(RunMode = RunMode.Async)]
			public Task Command()
				=> Context.Client.RestartBotAsync(BotSettings);
		}
	}
}