﻿using System;
using System.Globalization;
using Advobot.NetCoreUI.Classes.AbstractUI.Colors;

namespace Advobot.NetCoreUI.Classes.AbstractUI.Converters
{
	/// <summary>
	/// Converts a <typeparamref name="TBrush"/> to a string and vice versa.
	/// </summary>
	/// <typeparam name="TBrush"></typeparam>
	/// <typeparam name="TBrushFactory"></typeparam>
	public abstract class ColorConverter<TBrush, TBrushFactory>
		where TBrushFactory : BrushFactory<TBrush>, new()
	{
		private static readonly TBrushFactory _Factory = new TBrushFactory();

		/// <summary>
		/// Converts a <typeparamref name="TBrush"/> to a string.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="culture"></param>
		/// <returns></returns>
		public object Convert(object value, Type _1, object _2, CultureInfo _3)
			=> value is TBrush brush ? _Factory.FormatBrush(brush)
				: throw new InvalidOperationException("Invalid brush supplied for converting.");
		/// <summary>
		/// Converts a string to a <typeparamref name="TBrush"/>.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <param name="parameter"></param>
		/// <param name="culture"></param>
		/// <returns></returns>
		public object ConvertBack(object value, Type _1, object _2, CultureInfo _3)
			=> value is string str && _Factory.CreateBrush(str) is TBrush brush ? brush
				: throw new InvalidOperationException("Brush cannot be null when converting back.");
	}
}