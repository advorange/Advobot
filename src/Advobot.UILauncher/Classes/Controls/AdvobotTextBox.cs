﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Advobot.Core.Utilities;
using Advobot.UILauncher.Enums;
using Advobot.UILauncher.Interfaces;
using Advobot.UILauncher.Utilities;
using Discord;

namespace Advobot.UILauncher.Classes.Controls
{
	/// <summary>
	/// A <see cref="TextBox"/> which implements some other useful properties and accepts custom colors easily.
	/// </summary>
	internal class AdvobotTextBox : TextBox, IFontResizeValue, IAdvobotControl
	{
		public static readonly DependencyProperty FontResizeValueProperty = DependencyProperty.Register("FontResizeValue", typeof(double), typeof(AdvobotTextBox), new PropertyMetadata(ElementUtils.SetFontResizeProperty));
		public double FontResizeValue
		{
			get => (double)GetValue(FontResizeValueProperty);
			set => SetValue(FontResizeValueProperty, value);
		}

		public AdvobotTextBox()
		{
			SetResourceReferences();
		}

		public void SetResourceReferences()
		{
			SetResourceReference(BackgroundProperty, ColorTarget.BaseBackground);
			SetResourceReference(ForegroundProperty, ColorTarget.BaseForeground);
			SetResourceReference(BorderBrushProperty, ColorTarget.BaseBorder);
		}

		public static AdvobotTextBox CreateComboBoxItem(string text, object tag)
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
				Foreground = Brushes.Black
			};
		}

		public static AdvobotTextBox CreateUserBox(IUser user)
		{
			var name = user.Username.AllCharsWithinLimit() ? user.Username : "Non-Standard Name";
			var text = $"'{name}#{user.Discriminator}' ({user.Id})";
			return CreateComboBoxItem(text, user.Id);
		}
	}
}
