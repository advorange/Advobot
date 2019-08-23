﻿using System;
using System.Collections.Generic;
using Advobot.Services.HelpEntries;

namespace Advobot.Tests.Fakes.Services.HelpEntries
{
	public sealed class FakeHelpEntry : IModuleHelpEntry
	{
		public bool AbleToBeToggled { get; set; }
		public bool EnabledByDefault { get; set; }
		public string Id { get; set; } = Guid.NewGuid().ToString();
		public string Category { get; set; } = "";
		public IReadOnlyList<string> Aliases { get; set; } = Array.Empty<string>();
		public IReadOnlyList<IPrecondition> Preconditions { get; set; } = Array.Empty<IPrecondition>();
		public IReadOnlyList<ICommandHelpEntry> Commands { get; set; } = Array.Empty<ICommandHelpEntry>();
		public string Name { get; set; } = "";
		public string Summary { get; set; } = "";
	}
}
