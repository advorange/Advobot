﻿using System.Collections.Generic;
using System.Linq;
using Advobot.Modules;
using Advobot.Services.InviteList;
using Advobot.Utilities;
using AdvorangesUtils;
using Discord;

namespace Advobot.Settings.Responses
{
	public sealed class GuildList : CommandResponses
	{
		private const int _GLength = 25;
		private const int _ULength = 35;
		private const int _MLength = 14;

		private static readonly string _GHeader = "Guild Name".PadRight(_GLength);
		private static readonly string _UHeader = "Url".PadRight(_ULength);
		private static readonly string _MHeader = "Member Count".PadRight(_MLength);
		private static readonly string _EHeader = "Global Emotes";
		private static readonly string _Header = _GHeader + _UHeader + _MHeader + _EHeader;

		private GuildList() { }

		public static AdvobotResult CreatedListing(IInviteMetadata invite, IReadOnlyCollection<string> keywords)
			=> Success(Default.FormatInterpolated($"Successfully created a listed invite from {invite} with the keywords {keywords}."));
		public static AdvobotResult DeletedListing()
			=> Success("Successfully deleted the listed invite.");
		public static AdvobotResult Bumped()
			=> Success("Successfully bumped the listed invite.");
		public static AdvobotResult NoInviteMatch()
			=> Failure("Failed to find an invite with the supplied options.").WithTime(DefaultTime);
		public static AdvobotResult InviteMatches(IEnumerable<IListedInvite> invites)
		{
			var formatted = invites.Join(x =>
			{
				var n = x.GuildName.PadRight(_GLength).Substring(0, _GLength);
				var u = x.Url.PadRight(_ULength);
				var m = x.GuildMemberCount.ToString().PadRight(_MLength);
				var e = x.HasGlobalEmotes ? "Yes" : "";
				return $"{n}{u}{m}{e}";
			}, "\n");
			var str = $"{_Header}\n{formatted}";
			return Success(BigBlock.FormatInterpolated($"{str}"));
		}
		public static AdvobotResult TooManyMatches()
			=> Failure("Failed to find a suitable invite; too many were found.").WithTime(DefaultTime);
	}
}
