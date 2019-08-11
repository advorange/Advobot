﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Advobot.Attributes.ParameterPreconditions.Numbers;
using AdvorangesUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Advobot.Tests.UnitTests.Attributes.ParameterPreconditions.Numbers
{
	[TestClass]
	public sealed class ChannelBitrateAttribute_Tests
		: ParameterPreconditionsTestsBase<ChannelBitrateAttribute>
	{
		[TestMethod]
		public async Task ThrowsOnNotInt_Test()
		{
			Task Task() => CheckAsync("not int");
			await Assert.ThrowsExceptionAsync<ArgumentException>(Task).CAF();
		}
		[TestMethod]
		public async Task Standard_Test()
		{
			Context.Guild.PremiumSubscriptionCount = 0;

			var expected = new Dictionary<int, bool>
			{
				{ 7, false },
				{ 8, true },
				{ 96, true },
				{ 97, false },
			};
			foreach (var kvp in expected)
			{
				var result = await CheckAsync(kvp.Key).CAF();
				Assert.AreEqual(kvp.Value, result.IsSuccess);
			}
		}
		[TestMethod]
		public async Task Tier1_Test()
		{
			Context.Guild.PremiumSubscriptionCount = 2;

			var expected = new Dictionary<int, bool>
			{
				{ 7, false },
				{ 8, true },
				{ 128, true },
				{ 129, false },
			};
			foreach (var kvp in expected)
			{
				var result = await CheckAsync(kvp.Key).CAF();
				Assert.AreEqual(kvp.Value, result.IsSuccess);
			}
		}
		[TestMethod]
		public async Task Tier2_Test()
		{
			Context.Guild.PremiumSubscriptionCount = 10;

			var expected = new Dictionary<int, bool>
			{
				{ 7, false },
				{ 8, true },
				{ 256, true },
				{ 257, false },
			};
			foreach (var kvp in expected)
			{
				var result = await CheckAsync(kvp.Key).CAF();
				Assert.AreEqual(kvp.Value, result.IsSuccess);
			}
		}
		[TestMethod]
		public async Task Tier3_Test()
		{
			Context.Guild.PremiumSubscriptionCount = 50;

			var expected = new Dictionary<int, bool>
			{
				{ 7, false },
				{ 8, true },
				{ 384, true },
				{ 385, false },
			};
			foreach (var kvp in expected)
			{
				var result = await CheckAsync(kvp.Key).CAF();
				Assert.AreEqual(kvp.Value, result.IsSuccess);
			}
		}
	}
}