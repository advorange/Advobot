﻿using Advobot.Interfaces;
using Advobot.Utilities;
using AdvorangesUtils;
using Discord;
using Discord.WebSocket;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Advobot.Classes
{
	/// <summary>
	/// Does an action on all the input users until either no more users remain or the cancel token has been canceled.
	/// </summary>
	public class MultiUserAction
	{
		private static ConcurrentDictionary<ulong, CancellationTokenSource> _CancelTokens = new ConcurrentDictionary<ulong, CancellationTokenSource>();

		private CancellationTokenSource _CancelToken;
		private AdvobotSocketCommandContext _Context;
		private ITimersService _Timers;
		private List<IGuildUser> _Users;

		/// <summary>
		/// Creates an instance of multi user action and cancels all previous instances.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="timers"></param>
		/// <param name="users"></param>
		public MultiUserAction(AdvobotSocketCommandContext context, ITimersService timers, IEnumerable<IGuildUser> users)
		{
			_CancelToken = new CancellationTokenSource();
			_CancelTokens.AddOrUpdate(context.Guild.Id, _CancelToken, (oldKey, oldValue) =>
			{
				oldValue.Cancel();
				return _CancelToken;
			});
			_Context = context;
			_Timers = timers;
			_Users = users.ToList();
		}

		/// <summary>
		/// Take a role from multiple users.
		/// </summary>
		/// <param name="role"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		public async Task TakeRolesAsync(SocketRole role, RequestOptions options)
		{
			var presentTense = $"take the role `{role.Format()}` from";
			var pastTense = $"took the role `{role.Format()} from";
			await DoActionAsync(nameof(TakeRolesAsync), role, presentTense, pastTense, options).CAF();
		}
		/// <summary>
		/// Give a role to multiple users.
		/// </summary>
		/// <param name="role"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		public async Task GiveRolesAsync(SocketRole role, RequestOptions options)
		{
			var presentTense = $"give the role `{role.Format()}` to";
			var pastTense = $"gave the role `{role.Format()} to";
			await DoActionAsync(nameof(GiveRolesAsync), role, presentTense, pastTense, options).CAF();
		}
		/// <summary>
		/// Modify the nickname of multiple users.
		/// </summary>
		/// <param name="replace"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		public async Task ModifyNicknamesAsync(string replace, RequestOptions options)
		{
			var presentTense = "nickname";
			var pastTense = "nicknamed";
			await DoActionAsync(nameof(ModifyNicknamesAsync), replace, presentTense, pastTense, options).CAF();
		}
		/// <summary>
		/// Move multiple users.
		/// </summary>
		/// <param name="outputChannel"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		public async Task MoveUsersAsync(SocketVoiceChannel outputChannel, RequestOptions options)
		{
			var presentTense = "move";
			var pastTense = "moved";
			await DoActionAsync(nameof(MoveUsersAsync), outputChannel, presentTense, pastTense, options).CAF();
		}

		private async Task DoActionAsync(string action, object obj, string presentTense, string pastTense, RequestOptions options)
		{
			var text = $"Attempting to {presentTense} `{_Users.Count}` users.";
			var msg = await MessageUtils.SendMessageAsync(_Context.Channel, text).CAF();

			var successCount = 0;
			for (var i = 0; i < _Users.Count; ++i)
			{
				if (_CancelToken.IsCancellationRequested)
				{
					break;
				}

				if (i % 10 == 0)
				{
					var amtLeft = _Users.Count - i;
					var time = (int)(amtLeft * 1.2);
					var newText = $"Attempting to {presentTense} `{amtLeft}` people. ETA on completion: `{time}`.";
					await msg.ModifyAsync(x => x.Content = newText).CAF();
				}

				++successCount;
				var user = _Users[i];
				await user.ModifyAsync(x =>
				{
					switch (action)
					{
						case nameof(GiveRolesAsync):
							x.RoleIds = Optional.Create(x.RoleIds.Value.Concat(new[] { ((IRole)obj).Id }).Distinct());
							return;
						case nameof(TakeRolesAsync):
							x.RoleIds = Optional.Create(x.RoleIds.Value.Except(new[] { ((IRole)obj).Id }));
							return;
						case nameof(ModifyNicknamesAsync):
							x.Nickname = obj as string ?? user.Username;
							return;
						case nameof(MoveUsersAsync):
							x.Channel = Optional.Create((IVoiceChannel)obj);
							return;
					}
				}, options);
			}

			await MessageUtils.DeleteMessageAsync(msg, options).CAF();
			var response = $"Successfully {pastTense} `{successCount}` users.";
			await MessageUtils.MakeAndDeleteSecondaryMessageAsync((SocketTextChannel)_Context.Channel, _Context.Message, response, _Timers).CAF();
		}
	}
}