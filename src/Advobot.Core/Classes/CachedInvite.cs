﻿using System.Threading;

namespace Advobot.Core.Classes
{
	/// <summary>
	/// Holds the code and uses of an invite. Used in <see cref="Utilities.InviteUtils.GetInviteUserJoinedOnAsync(Interfaces.IGuildSettings, Discord.IGuildUser)"/>.
	/// </summary>
	public class CachedInvite
	{
		public string Code { get; }
		private int _Uses;
		public int Uses => _Uses;

		public CachedInvite(string code, int uses)
		{
			Code = code;
			_Uses = uses;
		}

		public void IncrementUses()
		{
			Interlocked.Increment(ref _Uses);
		}
	}
}
