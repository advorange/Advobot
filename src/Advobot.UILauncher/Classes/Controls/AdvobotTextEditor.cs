﻿using Advobot.UILauncher.Actions;
using Advobot.UILauncher.Enums;
using Advobot.UILauncher.Interfaces;
using ICSharpCode.AvalonEdit;
using System.Windows.Controls;

namespace Advobot.UILauncher.Classes.Controls
{
	/// <summary>
	/// A <see cref="TextEditor"/> which implements some other useful properties and accepts custom colors easily.
	/// </summary>
	internal class AdvobotTextEditor : TextEditor, IFontResizeValue, IAdvobotControl
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

		public AdvobotTextEditor()
		{
			SetResourceReferences();
		}

		public void SetResourceReferences()
		{
			this.SetResourceReference(Control.BackgroundProperty, ColorTarget.BaseBackground);
			this.SetResourceReference(Control.ForegroundProperty, ColorTarget.BaseForeground);
			this.SetResourceReference(Control.BorderBrushProperty, ColorTarget.BaseBorder);
		}
	}
}