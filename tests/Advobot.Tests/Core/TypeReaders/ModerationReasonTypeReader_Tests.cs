﻿using System;
using System.Threading.Tasks;

using Advobot.Classes;
using Advobot.Tests.TestBases;
using Advobot.TypeReaders;

using AdvorangesUtils;

using Discord.Commands;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Advobot.Tests.Core.TypeReaders
{
	[TestClass]
	public sealed class ModerationReasonTypeReader_Tests : TypeReaderTestsBase
	{
		protected override TypeReader Instance { get; } = new ModerationReasonTypeReader();
		protected override string? NotExisting => null;

		[TestMethod]
		public async Task Valid_Test()
		{
			var result = await ReadAsync("asdf").CAF();
			Assert.IsTrue(result.IsSuccess);
			Assert.IsInstanceOfType(result.BestMatch, typeof(ModerationReason));
		}

		[TestMethod]
		public async Task ValidWithTime_Test()
		{
			var result = await ReadAsync("asdf time:5 kapow").CAF();
			Assert.IsTrue(result.IsSuccess);
			Assert.IsInstanceOfType(result.BestMatch, typeof(ModerationReason));
			var cast = (ModerationReason)result.BestMatch;
			Assert.AreEqual(TimeSpan.FromMinutes(5), cast.Time);
		}
	}
}