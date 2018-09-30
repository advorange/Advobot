﻿using System;
using System.Linq;
using System.Threading.Tasks;
using AdvorangesUtils;
using Discord.Commands;
using Discord.WebSocket;

namespace Advobot.Classes.TypeReaders
{
	/// <summary>
	/// Finds a channel based on position and type.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class ChannelPositionTypeReader<T> : PositionTypeReader<T> where T : SocketGuildChannel
	{
		/// <inheritdoc />
		public override string ObjectType => "channel";

		/// <inheritdoc />
		public override async Task<T[]> GetObjectsWithPosition(ICommandContext context, int position)
		{
			var channels = await context.Guild.GetChannelsAsync().CAF();
			return channels.OfType<T>().Where(x => x.Position == position).ToArray();
		}
	}

	/// <summary>
	/// Finds a role based on position.
	/// </summary>
	public class RolePositionTypeReader : PositionTypeReader<SocketRole>
	{
		/// <inheritdoc />
		public override string ObjectType => "role";

		/// <inheritdoc />
		public override Task<SocketRole[]> GetObjectsWithPosition(ICommandContext context, int position)
		{
			var channels = context.Guild.Roles;
			return Task.FromResult(channels.OfType<SocketRole>().Where(x => x.Position == position).ToArray());
		}
	}

	/// <summary>
	/// Parses something from a position.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class PositionTypeReader<T> : TypeReader
	{
		/// <summary>
		/// The type to find from a position.
		/// </summary>
		public abstract string ObjectType { get; }

		/// <inheritdoc />
		public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
		{
			if (!int.TryParse(input, out var position))
			{
				return TypeReaderResult.FromError(CommandError.ParseFailed, "Failed to parse the position.");
			}

			var samePos = await GetObjectsWithPosition(context, position).CAF();
			if (!samePos.Any())
			{
				return TypeReaderResult.FromError(CommandError.ObjectNotFound, $"There is no {ObjectType} with the supplied position.");
			}
			if (samePos.Length > 1)
			{
				return TypeReaderResult.FromError(CommandError.MultipleMatches, $"Multiple {ObjectType}s have the supplied position.");
			}
			return TypeReaderResult.FromSuccess(samePos[0]);
		}
		/// <summary>
		/// Gets objects with the supplied position.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="position"></param>
		/// <returns></returns>
		public abstract Task<T[]> GetObjectsWithPosition(ICommandContext context, int position);
	}
}