﻿using System.Threading.Tasks;

using Advobot.Attributes.ParameterPreconditions.Logs;
using Advobot.Services.GuildSettings;
using Advobot.Tests.Fakes.Discord;
using Advobot.Tests.Fakes.Discord.Channels;
using Advobot.Tests.Fakes.Services.GuildSettings;

using AdvorangesUtils;

using Discord;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Advobot.Tests.Core.Attributes.ParameterPreconditions.Logs
{
	[TestClass]
	public sealed class NotServerLogAttribute_Tests
		: ParameterlessParameterPreconditions_TestsBase<NotServerLogAttribute>
	{
		private const ulong CHANNEL_ID = 1;
		private readonly ITextChannel _FakeChannel;
		private readonly IGuildSettings _Settings;

		public NotServerLogAttribute_Tests()
		{
			_Settings = new GuildSettings();
			_FakeChannel = new FakeTextChannel(new FakeGuild())
			{
				Id = CHANNEL_ID
			};

			Services = new ServiceCollection()
				.AddSingleton<IGuildSettingsFactory>(new FakeGuildSettingsFactory(_Settings))
				.BuildServiceProvider();
		}

		[TestMethod]
		public async Task FailsOnNotUlong_Test()
			=> await AssertPreconditionFailsOnInvalidType(CheckAsync("")).CAF();

		[TestMethod]
		public async Task LogExisting_Test()
		{
			_Settings.ServerLogId = CHANNEL_ID;

			var result = await CheckAsync(_FakeChannel).CAF();
			Assert.AreEqual(false, result.IsSuccess);
		}

		[TestMethod]
		public async Task LogNotExisting_Test()
		{
			var result = await CheckAsync(_FakeChannel).CAF();
			Assert.AreEqual(true, result.IsSuccess);
		}
	}
}