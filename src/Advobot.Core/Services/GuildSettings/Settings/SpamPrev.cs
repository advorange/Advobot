﻿using Advobot.Classes;
using Advobot.Classes.Formatting;
using Advobot.Enums;
using Advobot.Utilities;
using AdvorangesUtils;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Advobot.Services.GuildSettings.Settings
{
	/// <summary>
	/// Holds information about spam prevention, such as how much is considered spam, required spam instances, and votes to kick.
	/// </summary>
	[NamedArgumentType]
	public sealed class SpamPrev : TimedPrev<SpamType>
	{
		/// <summary>
		/// The required amount of times a user must spam before they can be voted to be kicked.
		/// </summary>
		[JsonProperty]
		public int SpamInstances { get; set; }
		/// <summary>
		/// The required amount of content before a message is considered spam.
		/// </summary>
		[JsonProperty]
		public int SpamPerMessage { get; set; }

		[JsonIgnore]
		private readonly ConcurrentDictionary<ulong, SortedSet<ulong>> _Instances = new ConcurrentDictionary<ulong, SortedSet<ulong>>();

		/// <summary>
		/// Punishes a user.
		/// </summary>
		/// <param name="user"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		public async Task<bool> PunishAsync(IGuildUser user, IUserMessage message)
		{
			if (!Enabled)
			{
				return false;
			}

			var instances = _Instances.GetOrAdd(message.Author.Id, id => new SortedSet<ulong>());
			if (GetSpamCount(message) >= SpamPerMessage)
			{
				lock (instances)
				{
					instances.Add(message.Id);
				}
			}
			if (CountItemsInTimeFrame(instances, TimeInterval) >= SpamInstances)
			{
				_Instances.TryRemove(message.Author.Id, out _);
				var punishmentArgs = new PunishmentArgs()
				{
					Options = DiscordUtils.GenerateRequestOptions("Spam prevention."),
				};
				await PunishmentUtils.GiveAsync(Punishment, user.Guild, user.Id, RoleId, punishmentArgs).CAF();
				return true;
			}
			return false;
		}
		private int GetSpamCount(IUserMessage message) => Type switch
		{
			SpamType.Message => int.MaxValue,
			SpamType.LongMessage => message.Content?.Length ?? 0,
			SpamType.Link => message.Content?.Split(' ')?.Count(x => Uri.IsWellFormedUriString(x, UriKind.Absolute)) ?? 0,
			SpamType.Image => message.Attachments.Count(x => x.Height != null || x.Width != null) + message.Embeds.Count(x => x.Image != null || x.Video != null),
			SpamType.Mention => message.MentionedUserIds.Distinct().Count(),
			_ => throw new ArgumentException(nameof(Type)),
		};
		/// <inheritdoc />
		public override Task EnableAsync(IGuild guild)
		{
			Enabled = true;
			return Task.CompletedTask;
		}
		/// <inheritdoc />
		public override Task DisableAsync(IGuild guild)
		{
			Enabled = false;
			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public override IDiscordFormattableString GetFormattableString()
		{
			return new Dictionary<string, object>
			{
				{ "Enabled", Enabled },
				{ "Interval", TimeInterval },
				{ "Punishment", Punishment },
				{ "Instances", SpamInstances },
				{ "Amount", SpamPerMessage },
			}.ToDiscordFormattableStringCollection();
		}
	}
}