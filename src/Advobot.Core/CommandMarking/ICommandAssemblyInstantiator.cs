﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Advobot.CommandMarking
{
	/// <summary>
	/// Specifies how to instantiate the command assembly.
	/// </summary>
	public interface ICommandAssemblyInstantiator
	{
		/// <summary>
		/// Adds some services to <paramref name="services"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <returns></returns>
		Task AddServicesAsync(IServiceCollection services);
		/// <summary>
		/// Configures the services and makes sure they are set up correctly.
		/// </summary>
		/// <param name="services"></param>
		/// <returns></returns>
		Task ConfigureServicesAsync(IServiceProvider services);
	}
}