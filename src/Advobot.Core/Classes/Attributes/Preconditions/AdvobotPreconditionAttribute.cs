﻿using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace Advobot.Classes.Attributes.Preconditions
{
	/// <summary>
	/// Functionally same as <see cref="PreconditionResult"/> except automatically sets the group as the type name.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public abstract class AdvobotPreconditionAttribute : PreconditionAttribute
	{
		/// <summary>
		/// Indicates this precondition should be visible in the help command.
		/// </summary>
		public abstract bool Visible { get; }

		/// <summary>
		/// Creates an instance of <see cref="AdvobotPreconditionAttribute"/> with the Group as the type name.
		/// </summary>
		public AdvobotPreconditionAttribute()
		{
			Group = GetType().Name;
		}

		/// <inheritdoc />
		public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
			=> CheckPermissionsAsync((AdvobotCommandContext)context, command, services);
		/// <summary>
		/// Checks if the command can be executed.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="command"></param>
		/// <param name="services"></param>
		/// <returns></returns>
		public abstract Task<PreconditionResult> CheckPermissionsAsync(AdvobotCommandContext context, CommandInfo command, IServiceProvider services);
	}
}