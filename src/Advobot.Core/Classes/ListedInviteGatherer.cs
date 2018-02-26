﻿using Advobot.Core.Classes.Attributes;
using Advobot.Core.Classes.Settings;
using Advobot.Core.Enums;
using Advobot.Core.Interfaces;
using Advobot.Core.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace Advobot.Core.Classes
{
	public sealed class ListedInviteGatherer
	{
		private string _Code;
		private string _Name;
		private bool _HasGlobalEmotes;
		private uint? _UserCount;
		private CountTarget _UserCountTarget;
		private string[] _Keywords;

		public ListedInviteGatherer() : this(null, null, false, null, default) { }
		[NamedArgumentConstructor]
		public ListedInviteGatherer(
			[NamedArgument] string code,
			[NamedArgument] string name,
			[NamedArgument] bool hasGlobalEmotes,
			[NamedArgument] uint? userCount, 
			[NamedArgument] CountTarget userCountTarget,
			[NamedArgument] params string[] keywords)
		{
			_Code = code;
			_Name = name;
			_HasGlobalEmotes = hasGlobalEmotes;
			_UserCount = userCount;
			_UserCountTarget = userCountTarget;
			_Keywords = keywords;
		}

		public IEnumerable<ListedInvite> GatherInvites(IInviteListService inviteListService)
		{
			var invites = _Keywords.Any() ? inviteListService.GetAll(_Keywords) : inviteListService.GetAll();

			var wentIntoAny = false;
			if (_Code != null)
			{
				invites = invites.Where(x => x.Code == _Code);
				wentIntoAny = true;
			}
			if (_Name != null)
			{
				invites = invites.Where(x => x.Guild.Name.CaseInsEquals(_Name));
				wentIntoAny = true;
			}
			if (_HasGlobalEmotes)
			{
				invites = invites.Where(x => x.HasGlobalEmotes);
				wentIntoAny = true;
			}
			if (_UserCount != null)
			{
				invites = invites.GetObjectsBasedOffCount(_UserCountTarget, _UserCount, x => x.Guild.Users.Count);
				wentIntoAny = true;
			}
			return wentIntoAny ? Enumerable.Empty<ListedInvite>() : invites;
		}
	}
}