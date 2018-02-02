﻿using Advobot.Core.Classes.Attributes;
using Advobot.Core.Enums;
using Discord;
using System.Collections.Generic;
using System.Linq;

namespace Advobot.Core.Classes.NamedArguments
{
	/// <summary>
	/// Sets the search terms for invites and can gather invites matching those terms.
	/// </summary>
	public class MultipleInviteGatherer
	{
		private ulong? _UserId;
		private ulong? _ChannelId;
		private uint? _Uses;
		private CountTarget _UsesCountTarget;
		private uint? _Age;
		private CountTarget _AgeCountTarget;
		private bool _IsTemporary;
		private bool _NeverExpires;
		private bool _NoMaxUses;

		public MultipleInviteGatherer() : this(null, null, null, default, null, default, false, false, false) { }
		[NamedArgumentConstructor]
		public MultipleInviteGatherer(
			[NamedArgument] ulong? userId,
			[NamedArgument] ulong? channelId,
			[NamedArgument] uint? uses,
			[NamedArgument] CountTarget usesCountTarget,
			[NamedArgument] uint? age,
			[NamedArgument] CountTarget ageCountTarget,
			[NamedArgument] bool isTemporary,
			[NamedArgument] bool neverExpires,
			[NamedArgument] bool noMaxUses)
		{
			_UserId = userId;
			_ChannelId = channelId;
			_Uses = uses;
			_UsesCountTarget = usesCountTarget;
			_Age = age;
			_AgeCountTarget = ageCountTarget;
			_IsTemporary = isTemporary;
			_NeverExpires = neverExpires;
			_NoMaxUses = noMaxUses;
		}

		/// <summary>
		/// Gathers invites matching the supplied arguments.
		/// </summary>
		/// <param name="invites"></param>
		/// <returns></returns>
		public IEnumerable<IInviteMetadata> GatherInvites(IEnumerable<IInviteMetadata> invites)
		{
			var wentIntoAny = false;
			if (_UserId != null)
			{
				invites = invites.Where(x => x.Inviter.Id == _UserId);
				wentIntoAny = true;
			}
			if (_ChannelId != null)
			{
				invites = invites.Where(x => x.ChannelId == _ChannelId);
				wentIntoAny = true;
			}
			if (_Uses != null)
			{
				invites = invites.GetObjectsBasedOffCount(_UsesCountTarget, _Uses, x => x.Uses);
				wentIntoAny = true;
			}
			if (_Age != null)
			{
				invites = invites.GetObjectsBasedOffCount(_AgeCountTarget, _Age, x => x.MaxAge);
				wentIntoAny = true;
			}
			if (_IsTemporary)
			{
				invites = invites.Where(x => x.IsTemporary);
				wentIntoAny = true;
			}
			if (_NeverExpires)
			{
				invites = invites.Where(x => x.MaxAge == null);
				wentIntoAny = true;
			}
			if (_NoMaxUses)
			{
				invites = invites.Where(x => x.MaxUses == null);
				wentIntoAny = true;
			}
			return wentIntoAny ? Enumerable.Empty<IInviteMetadata>() : invites;
		}
	}
}