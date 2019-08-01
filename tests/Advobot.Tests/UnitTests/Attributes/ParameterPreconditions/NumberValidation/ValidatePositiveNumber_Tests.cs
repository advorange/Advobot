﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Advobot.Attributes.ParameterPreconditions.NumberValidation;
using AdvorangesUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Advobot.Tests.UnitTests.Attributes.ParameterPreconditions.NumberValidation
{
	[TestClass]
	public sealed class ValidatePositiveNumber_Tests
		: ParameterPreconditionsTestsBase<ValidatePositiveNumber>
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
			var expected = new Dictionary<int, bool>
			{
				{ -1, false },
				{ 1, true },
				{ int.MaxValue, true },
			};
			foreach (var kvp in expected)
			{
				var result = await CheckAsync(kvp.Key).CAF();
				Assert.AreEqual(kvp.Value, result.IsSuccess);
			}
		}
	}
}
