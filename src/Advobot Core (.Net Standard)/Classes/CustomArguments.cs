﻿using Advobot.Classes.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Advobot.Classes
{
	public class CustomArguments<T> where T : class
	{
		public static ImmutableList<string> ArgNames { get; }

		private static ConstructorInfo _Constructor;
		private static bool _HasParams;
		private static int _ParamsLength;
		private static string _ParamsName;

		private Dictionary<string, string> _Args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		private List<string> _ParamArgs = new List<string>();

		static CustomArguments()
		{
			_Constructor = typeof(T).GetConstructors(BindingFlags.Public | BindingFlags.Instance)
				.Single(x => x.GetCustomAttribute<CustomArgumentConstructorAttribute>() != null);

			//Make sure no invalid types have CustomArgumentAttribute
			var argNames = new List<string>();
			foreach (var p in _Constructor.GetParameters())
			{
				if (p.GetCustomAttribute<CustomArgumentAttribute>() != null)
				{
					var t = p.ParameterType.IsArray ? p.ParameterType.GetElementType() : p.ParameterType;
					if (!t.IsPrimitive && t != typeof(string))
					{
						throw new ArgumentException($"Do not use {nameof(CustomArgumentAttribute)} on non primitive arguments.");
					}
					else if (p.GetCustomAttribute<ParamArrayAttribute>() != null)
					{
						var split = p.Name.Split(new[] { '_' }, 2);
						if (split.Length != 2 || !int.TryParse(split[1], out _ParamsLength))
						{
							throw new ArgumentException($"When marking a params parameter with {nameof(CustomArgumentAttribute)}" +
								$"it must be in the format NAME_x where x is the length of allowed args.");
						}

						_HasParams = true;
						_ParamsName = split[0];
						argNames.Add(_ParamsName);
					}
					else
					{
						argNames.Add(p.Name);
					}
				}
			}
			ArgNames = argNames.ToImmutableList();
		}

		public CustomArguments(string input)
		{
			ArgNames.ForEach(x => _Args.Add(x, null));

			//Split except when in quotes
			var unseperatedArgs = input.Split('"').Select((element, index) =>
			{
				return index % 2 == 0
					? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
					: new[] { element };
			}).SelectMany(x => x).Where(x => !String.IsNullOrWhiteSpace(x));

			//If the _Args dictionary has the first half of the split, set it to the second half
			//Otherwise don't do anything with it since it's not important
			foreach (var arg in unseperatedArgs)
			{
				var split = arg.Split(new[] { ':' }, 2);
				if (split.Length == 2 && _HasParams && _ParamsName.CaseInsEquals(split[0]) && _ParamArgs.Count() < _ParamsLength)
				{
					_ParamArgs.Add(split[1]);
				}
				else if (split.Length == 2 && _Args.ContainsKey(split[0]))
				{
					_Args[split[0]] = split[1];
				}
			}
		}

		public T CreateObject(params object[] additionalArgs)
		{
			var additionalArgCounter = 0;
			var parameters = _Constructor.GetParameters().Select(x =>
			{
				var t = x.ParameterType;
				//Check params first otherwise will go into the middle else if
				if (x.GetCustomAttribute<ParamArrayAttribute>() != null)
				{
					return _ParamArgs.ToArray();
				}
				//Checking against the attribute again in case arguments have duplicate names
				else if (x.GetCustomAttribute<CustomArgumentAttribute>() != null && _Args.TryGetValue(x.Name, out string value))
				{
					return Convert.ChangeType(value, t, CultureInfo.InvariantCulture);
				}
				else if (additionalArgCounter < additionalArgs.Length)
				{
					return additionalArgs[++additionalArgCounter - 1];
				}

				return t.IsValueType ? Activator.CreateInstance(t) : null;
			}).ToArray();

			return (T)Activator.CreateInstance(typeof(T), parameters);
		}
	}
}
