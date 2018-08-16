﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Advobot.Interfaces;
using Advobot.NetFrameworkUI.Classes.Controls;
using Advobot.NetFrameworkUI.Utilities;
using AdvorangesUtils;

namespace Advobot.NetFrameworkUI.Windows
{
	/// <summary>
	/// Interaction logic for OutputSearchWindow.xaml
	/// </summary>
	internal partial class OutputSearchWindow : ModalWindow
	{
		public OutputSearchWindow() : this(null, null) { }
		public OutputSearchWindow(Window mainWindow, IBotSettings settings) : base(mainWindow, settings)
		{
			InitializeComponent();
			OutputNamesComboBox.ItemsSource = ConsoleUtils.WrittenLines.Keys;
		}

		private void Search(object sender, RoutedEventArgs e)
		{
			ConsoleSearchOutput.Clear();
			foreach (var line in ConsoleUtils.WrittenLines[(string)OutputNamesComboBox.SelectedItem])
			{
				ConsoleSearchOutput.AppendText($"{line}{Environment.NewLine}");
			}
		}
		private void SaveWithCtrlS(object sender, KeyEventArgs e)
		{
			if (SavingUtils.IsCtrlS(e))
			{
				Save(sender, e);
			}
		}
		private void Save(object sender, RoutedEventArgs e)
		{
			if (ConsoleSearchOutput.Text.Length > 0)
			{
				var response = SavingUtils.SaveFile(Settings, ConsoleSearchOutput);
				ToolTipUtils.EnableTimedToolTip(Layout, response.GetReason());
			}
		}
		private void MoveToolTip(object sender, MouseEventArgs e)
		{
			var fe = (FrameworkElement)sender;
			var tt = (ToolTip)fe.ToolTip;
			var pos = e.GetPosition(fe);
			tt.HorizontalOffset = pos.X + 10;
			tt.VerticalOffset = pos.Y + 10;
		}
	}
}