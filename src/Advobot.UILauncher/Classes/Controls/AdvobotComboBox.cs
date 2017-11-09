﻿using Advobot.UILauncher.Actions;
using Advobot.UILauncher.Enums;
using Advobot.UILauncher.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Advobot.UILauncher.Classes.Controls
{
	/// <summary>
	/// A <see cref="ComboBox"/> which implements some other useful properties and accepts custom colors easily.
	/// </summary>
	internal class AdvobotComboBox : ComboBox, IFontResizeValue, IAdvobotControl
	{
		private double _FRV;
		public double FontResizeValue
		{
			get => _FRV;
			set
			{
				EntityActions.SetFontResizeProperty(this, value);
				_FRV = value;
			}
		}
		private Type _SET;
		public Type SourceEnumType
		{
			get => _SET;
			set
			{
				this.ItemsSource = CreateItemsSourceOutOfEnum(value);
				_SET = value;
			}
		}
		private ObservableCollection<object> _Items = new ObservableCollection<object>();

		public AdvobotComboBox()
		{
			this.ItemsSource = _Items;
			//Sort alphabetically
			this.Items.SortDescriptions.Add(new SortDescription("Text", ListSortDirection.Ascending));
			this.VerticalContentAlignment = VerticalAlignment.Center;
			this.HorizontalContentAlignment = HorizontalAlignment.Center;
			SetResourceReferences();
		}
		public void SetResourceReferences()
		{
			this.SetResourceReference(Control.BackgroundProperty, ColorTarget.BaseBackground);
			this.SetResourceReference(Control.ForegroundProperty, ColorTarget.BaseForeground);
			this.SetResourceReference(Control.BorderBrushProperty, ColorTarget.BaseBorder);
		}

		public void AddItem(object obj)
		{
			_Items.Add(obj);
		}
		public void RemoveItem(object obj)
		{
			_Items.Remove(obj);
		}

		/// <summary>
		/// Returns the values of <see cref="CreateItemsSourceOutOfEnum(Type)"/> by passing in <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static IEnumerable<TextBox> CreateItemsSourceOutOfEnum<T>() where T : struct, IConvertible, IComparable, IFormattable
		{
			return CreateItemsSourceOutOfEnum(typeof(T));
		}
		/// <summary>
		/// Returns textboxes with the text as the enum name and the tag as the enum.
		/// </summary>
		/// <param name="enumType"></param>
		/// <returns></returns>
		public static IEnumerable<TextBox> CreateItemsSourceOutOfEnum(Type enumType)
		{
			if (!enumType.IsEnum)
			{
				throw new ArgumentException($"{nameof(enumType)} must be an enum.");
			}

			foreach (var e in Enum.GetValues(enumType))
			{
				yield return CreateItem(Enum.GetName(enumType, e), e);
			}
		}
		/// <summary>
		/// Returns textboxes with the text as the string and the tag as the string too.
		/// </summary>
		/// <param name="strings"></param>
		/// <returns></returns>
		public static IEnumerable<TextBox> CreateComboBoxSourceOutOfStrings(params string[] strings)
		{
			foreach (var s in strings)
			{
				yield return CreateItem(s, s);
			}
		}
		private static TextBox CreateItem(string text, object tag)
		{
			return new AdvobotTextBox
			{
				FontFamily = new FontFamily("Courier New"),
				Text = text,
				Tag = tag,
				IsReadOnly = true,
				IsHitTestVisible = false,
				BorderThickness = new Thickness(0),
				Background = Brushes.Transparent,
				Foreground = Brushes.Black,
			};
		}
	}
}
