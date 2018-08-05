﻿using System;
using System.Threading.Tasks;
using Advobot.Enums;
using AdvorangesUtils;
using Discord;
using Discord.WebSocket;

namespace Advobot.Classes
{
	/// <summary>
	/// Punishments that will be removed after the time has passed.
	/// </summary>
	public sealed class RemovablePunishment : DatabaseEntry
	{
		/// <summary>
		/// The type of punishment that was given.
		/// </summary>
		public Punishment PunishmentType { get; set; }
		/// <summary>
		/// The id of the guild the punishment was given on.
		/// </summary>
		public ulong GuildId { get; set; }
		/// <summary>
		/// The id of the user the punishment was given to.
		/// </summary>
		public ulong UserId { get; set; }
		/// <summary>
		/// The id of the role given (only applicable if <see cref="PunishmentType"/> is <see cref="Punishment.RoleMute"/>).
		/// </summary>
		public ulong RoleId { get; set; }

		/// <summary>
		/// Creates an instance of <see cref="RemovablePunishment"/>. Parameterless constructor is used for the database.
		/// </summary>
		public RemovablePunishment() : base(default) { }
		/// <summary>
		/// Creates an instance of <see cref="RemovablePunishment"/>.
		/// </summary>
		/// <param name="time"></param>
		/// <param name="punishment"></param>
		/// <param name="guild"></param>
		/// <param name="user"></param>
		public RemovablePunishment(TimeSpan time, Punishment punishment, IGuild guild, IUser user) : base(time)
		{
			PunishmentType = punishment;
			GuildId = guild.Id;
			UserId = user.Id;
			RoleId = 0;
		}
		/// <summary>
		/// Creates an instance of <see cref="RemovablePunishment"/>.
		/// </summary>
		/// <param name="time"></param>
		/// <param name="guild"></param>
		/// <param name="user"></param>
		/// <param name="role"></param>
		public RemovablePunishment(TimeSpan time, IRole role, IGuild guild, IUser user) : this(time, Punishment.RoleMute, guild, user)
		{
			RoleId = role.Id;
		}

		/// <summary>
		/// Removes the punishment from the user.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="punisher"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		public async Task RemoveAsync(DiscordShardedClient client, Punisher punisher, RequestOptions options)
		{
			if (!(client.GetGuild(GuildId) is SocketGuild guild))
			{
				return;
			}

			switch (PunishmentType)
			{
				case Punishment.Ban:
					await punisher.UnbanAsync(guild, UserId, options).CAF();
					return;
				case Punishment.Deafen:
					await punisher.UndeafenAsync(guild.GetUser(UserId), options).CAF();
					return;
				case Punishment.VoiceMute:
					await punisher.UnvoicemuteAsync(guild.GetUser(UserId), options).CAF();
					return;
				case Punishment.RoleMute:
					await punisher.UnrolemuteAsync(guild.GetUser(UserId), guild.GetRole(RoleId), options).CAF();
					return;
			}
		}
	}
}