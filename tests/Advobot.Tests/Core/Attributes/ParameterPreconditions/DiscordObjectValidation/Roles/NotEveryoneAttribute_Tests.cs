﻿using System.Threading.Tasks;

using Advobot.Attributes.ParameterPreconditions.DiscordObjectValidation.Roles;
using Advobot.Tests.Fakes.Discord;
using Advobot.Tests.TestBases;

using AdvorangesUtils;

using Discord.Commands;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Advobot.Tests.Core.Attributes.ParameterPreconditions.DiscordObjectValidation.Roles
{
	[TestClass]
	public sealed class NotEveryoneAttribute_Tests : ParameterPreconditionTestsBase
	{
		protected override ParameterPreconditionAttribute Instance { get; }
			= new NotEveryoneAttribute();

		[TestMethod]
		public async Task RoleIsEveryone_Test()
		{
			var result = await CheckPermissionsAsync(Context.Guild.FakeEveryoneRole).CAF();
			Assert.IsFalse(result.IsSuccess);
		}

		[TestMethod]
		public async Task RoleIsNotEveryone_Test()
		{
			var result = await CheckPermissionsAsync(new FakeRole(Context.Guild)
			{
				Id = 73,
			}).CAF();
			Assert.IsTrue(result.IsSuccess);
		}
	}
}