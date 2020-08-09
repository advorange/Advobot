﻿using System.Threading.Tasks;

using Advobot.Attributes.ParameterPreconditions.Numbers;
using Advobot.Services.GuildSettings;
using Advobot.Services.GuildSettings.Settings;
using Advobot.Tests.Fakes.Services.GuildSettings;
using Advobot.Tests.TestBases;

using AdvorangesUtils;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Advobot.Tests.Core.Attributes.ParameterPreconditions.Numbers
{
	[TestClass]
	public sealed class SelfRoleGroupAttribute_Tests
		: ParameterlessParameterPreconditions_TestsBase<SelfRoleGroupAttribute>
	{
		private readonly IGuildSettings _Settings;

		public SelfRoleGroupAttribute_Tests()
		{
			_Settings = new GuildSettings();

			Services = new ServiceCollection()
				.AddSingleton<IGuildSettingsFactory>(new FakeGuildSettingsFactory(_Settings))
				.BuildServiceProvider();
		}

		[TestMethod]
		public async Task FailsOnNotInt_Test()
			=> await AssertPreconditionFailsOnInvalidType(CheckAsync("")).CAF();

		[TestMethod]
		public async Task GroupExisting_Test()
		{
			_Settings.SelfAssignableGroups.Add(new SelfAssignableRoles(1));

			var result = await CheckAsync(1).CAF();
			Assert.IsFalse(result.IsSuccess);
		}

		[TestMethod]
		public async Task GroupNotExisting_Test()
		{
			var result = await CheckAsync(1).CAF();
			Assert.IsTrue(result.IsSuccess);
		}
	}
}