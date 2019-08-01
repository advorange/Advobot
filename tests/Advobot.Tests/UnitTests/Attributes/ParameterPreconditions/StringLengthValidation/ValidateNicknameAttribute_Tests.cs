﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Advobot.Attributes.ParameterPreconditions.StringLengthValidation;
using AdvorangesUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Advobot.Tests.UnitTests.Attributes.ParameterPreconditions.StringLengthValidation
{
	[TestClass]
	public sealed class ValidateNicknameAttribute_Tests
		: ParameterPreconditionsTestsBase<ValidateNicknameAttribute>
	{
		[TestMethod]
		public async Task ThrowsOnNotString_Test()
		{
			Task Task() => CheckAsync(1);
			await Assert.ThrowsExceptionAsync<ArgumentException>(Task).CAF();
		}
		[TestMethod]
		public async Task Standard_Test()
		{
			var expected = new Dictionary<string, bool>
			{
				{ "", false },
				{ new string('a', 1), true },
				{ new string('a', 32), true },
				{ new string('a', 33), false },
			};
			foreach (var kvp in expected)
			{
				var result = await CheckAsync(kvp.Key).CAF();
				Assert.AreEqual(kvp.Value, result.IsSuccess);
			}
		}
	}
}
