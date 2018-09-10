﻿using System;
using System.Collections.Generic;
using System.Linq;
using Advobot.Classes;
using Advobot.Interfaces;
using Discord.WebSocket;

namespace Advobot.Services.Levels
{
	/// <summary>
	/// Holds the experience a user has.
	/// </summary>
	internal sealed class UserExperienceInformation : DatabaseEntry, IUserExperienceInformation
	{
		private const int _MESSAGE_AMOUNT = 10;

		/// <inheritdoc />
		public ulong UserId { get; set; }
		/// <inheritdoc />
		public int MessageCount { get; set; }
		/// <summary>
		/// The experience this user has in each guild, then each individual channel.
		/// </summary>
		public Dictionary<ulong, Dictionary<ulong, int>> Experience { get; set; } = new Dictionary<ulong, Dictionary<ulong, int>>();
		/// <summary>
		/// The past few messages a user has sent (hashes) so spam can be given less XP.
		/// </summary>
		public List<MessageHash> MessageHashes { get; set; } = new List<MessageHash>(_MESSAGE_AMOUNT);

		/// <summary>
		/// Creates an instance of <see cref="UserExperienceInformation"/>.
		/// </summary>
		public UserExperienceInformation() : base(TimeSpan.FromSeconds(0)) { }
		/// <summary>
		/// Creates an instance of <see cref="UserExperienceInformation"/> with the supplied user id.
		/// </summary>
		/// <param name="userId"></param>
		public UserExperienceInformation(ulong userId) : this()
		{
			UserId = userId;
		}

		/// <inheritdoc />
		public void AddExperience(IGuildSettings settings, SocketUserMessage message, int experience)
		{
			if (message.Author.Id != UserId)
			{
				return;
			}

			var xp = CalculateExperience(message, experience);
			GetChannels((SocketTextChannel)message.Channel)[message.Channel.Id] += xp;
			Time = DateTime.UtcNow;
			++MessageCount;

			//Hash the message content to not possibly keep sensitive info in the bot's database
			MessageHashes.Add(new MessageHash(message, xp));
			if (MessageHashes.Count > _MESSAGE_AMOUNT)
			{
				MessageHashes.RemoveAt(0);
			}
		}
		/// <inheritdoc />
		public void RemoveExperience(SocketUserMessage message, int xp)
		{
			if (message.Author.Id != UserId)
			{
				return;
			}

			GetChannels((SocketTextChannel)message.Channel)[message.Channel.Id] -= xp;
			--MessageCount;
		}
		/// <summary>
		/// Calculates what xp to give from the passed in xp.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="experience"></param>
		/// <returns></returns>
		private int CalculateExperience(SocketUserMessage message, int experience)
		{
			var rng = new Random();
			//Be within 20% of the base value
			var xp = (double)experience * rng.Next(80, 120) / 100.0;
			//Message length adds up to 10% increase capping out at 50 characters (any higher = same)
			//Reason: Some people just spam short messages for xp and this incentivizes longer messages which indicates better convos
			var msgLengthFactor = 1 + Math.Min(message.Content.Length, 50) / 50.0 * .1;
			//Attachments/embeds remove up to 5% of the xp capping out at 5 attachments/embeds
			//Reason: Marginally disincentivizes lots of images which discourage conversation
			var attachmentFactor = 1 - Math.Min((message.Attachments.Count + message.Embeds.Count) * .01, .05);
			//Any messages with the same hash are considered spam and remove up to 60% of the xp
			//Reason: Disincentivizes spamming which greatly discourages conversation
			//The first punishes for spam that was said before and during the last message. This only takes off up to 30% of the xp.
			//The second punishes for spam that is the same as the last message sent. This takes off up to 60% of the xp.
			var spamFactor = rng.Next(0, 2) != 0
				? 1 - Math.Min((MessageHashes.Count - MessageHashes.Select(x => x.Hash).Distinct().Count()) * .1, .3)
				: 1 - Math.Min((MessageHashes.Count(x => x.Hash == MessageHashes.Last().Hash) - 1) * .1, .6);
			return (int)Math.Round(xp * msgLengthFactor * attachmentFactor * spamFactor);
		}
		/// <summary>
		/// Gets the dictionary holding xp for the guild and makes sure the channel has a value in it.
		/// </summary>
		/// <param name="channel"></param>
		/// <returns></returns>
		private Dictionary<ulong, int> GetChannels(SocketTextChannel channel)
		{
			if (!Experience.TryGetValue(channel.Guild.Id, out var channels))
			{
				Experience.Add(channel.Guild.Id, channels = new Dictionary<ulong, int>());
			}
			if (!channels.TryGetValue(channel.Id, out var _))
			{
				channels.Add(channel.Id, 0);
			}
			return channels;
		}
		/// <summary>
		/// Attempts to get the message info out of the 10 most recent message information stored.
		/// </summary>
		/// <param name="messageId"></param>
		/// <returns></returns>
		public MessageHash RemoveMessageHash(ulong messageId)
		{
			var hash = MessageHashes.SingleOrDefault(x => x.MessageId == messageId);
			MessageHashes.Remove(hash);
			return hash;
		}
		/// <inheritdoc />
		public int GetExperience()
			=> Experience.Sum(g => g.Value.Sum(c => c.Value));
		/// <inheritdoc />
		public int GetExperience(SocketGuild guild)
			=> Experience.TryGetValue(guild.Id, out var channels) ? channels.Values.Sum() : 0;
		/// <inheritdoc />
		public int GetExperience(SocketTextChannel channel)
			=> Experience.TryGetValue(channel.Guild.Id, out var channels) && channels.TryGetValue(channel.Id, out var xp) ? xp : 0;
	}
}