﻿using System;
using Avalonia;
using Avalonia.Controls;

namespace Advobot.NetCoreUI.Classes
{
	/// <summary>
	/// This is used to set the items of a drop down.
	/// </summary>
	public static class SourceEnum
	{
		public static readonly StyledProperty<Type> SourceEnumProperty =
			AvaloniaProperty.Register<DropDown, Type>("SourceEnum");

		public static void SetSourceEnum(DropDown obj, Type value)
		{
			if (!value.IsEnum)
			{
				throw new ArgumentException($"Cannot set SourceEnum to a non enum type: {value.Name}");
			}

			obj.Items = value.GetEnumValues();
			obj.SetValue(SourceEnumProperty, value);
		}
		public static Type GetSourceEnum(DropDown obj)
			=> obj.GetValue(SourceEnumProperty);
	}
}