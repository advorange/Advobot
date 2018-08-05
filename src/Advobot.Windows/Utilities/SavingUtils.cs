﻿using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Advobot.Classes;
using Advobot.Interfaces;
using Advobot.Utilities;
using Advobot.Windows.Classes.Controls;
using Advobot.Windows.Enums;
using AdvorangesUtils;
using Discord;
using ICSharpCode.AvalonEdit;
using Newtonsoft.Json;

namespace Advobot.Windows.Utilities
{
	internal static class SavingUtils
	{
		/// <summary>
		/// Saves the text of <paramref name="editor"/> to file.
		/// </summary>
		/// <param name="config"></param>
		/// <param name="editor"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static ToolTipReason SaveFile(LowLevelConfig config, TextEditor editor, Type type = null)
		{
			return SaveFile(config, editor, editor.Text, type);
		}
		/// <summary>
		/// Saves the text of <paramref name="tb"/> to file.
		/// </summary>
		/// <param name="config"></param>
		/// <param name="tb"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static ToolTipReason SaveFile(LowLevelConfig config, TextBox tb, Type type = null)
		{
			return SaveFile(config, tb, tb.Text, type);
		}
		/// <summary>
		/// Attempts to save a file and returns a value indicating the result.
		/// </summary>
		/// <param name="config"></param>
		/// <param name="control"></param>
		/// <param name="text"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		private static ToolTipReason SaveFile(LowLevelConfig config, Control control, string text, Type type)
		{
			//If no valid tag just save to a new file with its name being the control's name
			var tag = control.Tag ?? CreateFileInfo(config, control);
			if (!(tag is FileInfo fi))
			{
				return ToolTipReason.InvalidFilePath;
			}
			if (type != null)
			{
				try
				{
					var throwaway = JsonConvert.DeserializeObject(text, type);
				}
				catch (JsonReaderException jre)
				{
					jre.Write();
					return ToolTipReason.FileSavingFailure;
				}
			}

			try
			{
				FileUtils.SafeWriteAllText(fi, text);
				return ToolTipReason.FileSavingSuccess;
			}
			catch
			{
				return ToolTipReason.FileSavingFailure;
			}
		}
		/// <summary>
		/// Creates a <see cref="FileInfo"/> based off of <paramref name="control"/> name.
		/// </summary>
		/// <param name="config"></param>
		/// <param name="control"></param>
		/// <returns></returns>
		private static FileInfo CreateFileInfo(LowLevelConfig config, Control control)
		{
			var baseDir = FileUtils.GetBaseBotDirectory(config).FullName;
			var fileName = $"{control.Name}_{AdvorangesUtils.Formatting.ToSaving()}.txt";
			return new FileInfo(Path.Combine(baseDir, fileName));
		}
		/// <summary>
		/// Saves every setting that is a child of <paramref name="parent"/>.
		/// </summary>
		/// <param name="config"></param>
		/// <param name="parent"></param>
		/// <param name="botSettings"></param>
		public static void SaveSettings(LowLevelConfig config, Grid parent, IBotSettings botSettings)
		{
			foreach (var child in parent.GetChildren().OfType<FrameworkElement>())
			{
				var result = SaveSetting(child, botSettings);
				if (result.HasValue && result.Value)
				{
					ConsoleUtils.WriteLine($"Successfully updated {child.Name}.");
				}
				if (result.HasValue && !result.Value)
				{
					ConsoleUtils.WriteLine($"Failed to save {child.Name}.");
				}
			}
			botSettings.SaveSettings(config);
		}
		private static bool? SaveSetting(FrameworkElement ele, IBotSettings botSettings)
		{
			//Go through children and not the actual object
			switch (ele)
			{
				case Grid g:
					var children = g.Children.OfType<FrameworkElement>();
					var results = children.Select(x => SaveSetting(x, botSettings)).Where(x => x != null).Cast<bool>();
					return results.Any() ? !results.Any(x => !x) : (bool?)null;
				case Viewbox vb:
					return vb.Child is FrameworkElement vbc ? SaveSetting(vbc, botSettings) : null;
			}

			object value = null;
			if (!(ele.Tag is string settingName))
			{
				return null;
			}
			switch (ele)
			{
				case AdvobotNumberBox nb:
					value = nb.StoredValue;
					break;
				case TextBox tb:
					var text = tb.Text;
					switch (settingName)
					{
						case nameof(IBotSettings.Prefix):
							if (String.IsNullOrWhiteSpace(text))
							{
								return false;
							}
							value = text;
							break;
						case nameof(IBotSettings.Game):
							value = text ?? "";
							break;
						case nameof(IBotSettings.Stream):
							if (!RegexUtils.IsValidTwitchName(text))
							{
								return false;
							}
							value = text;
							break;
					}
					break;
				case CheckBox cb:
					value = cb.IsChecked.Value;
					break;
				case ComboBox cmb:
					switch (settingName)
					{
						case nameof(IBotSettings.LogLevel):
							if (cmb.SelectedItem is TextBox cmbtb && cmbtb.Tag is LogSeverity ls)
							{
								value = ls;
								break;
							}
							return null;
						case nameof(IBotSettings.TrustedUsers):
							var updated = cmb.Items.OfType<TextBox>().Select(x => x?.Tag as ulong?).Where(x => x != null).Cast<ulong>();
							if (botSettings.TrustedUsers.Except(updated).Any() || updated.Except(botSettings.TrustedUsers).Any())
							{
								value = updated.ToList();
								break;
							}
							return null;
					}
					break;
				default:
					throw new ArgumentException("invalid object when attempting to save settings", ele.Name ?? ele.GetType().Name);
			}
			if (value == null)
			{
				return null;
			}

			var property = typeof(IBotSettings).GetProperty(settingName);
			if (value.GetType() != property.PropertyType)
			{
				return false;
			}
			else if (property.GetValue(botSettings).Equals(value))
			{
				return null;
			}

			property.SetValue(botSettings, value);
			return true;
		}
		/// <summary>
		/// Returns true if the key from <paramref name="e"/> is <see cref="Key.S"/> and control is pressed.
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		public static bool IsCtrlS(KeyEventArgs e)
		{
			return e.Key == Key.S && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control);
		}
		/// <summary>
		/// Attempts to get a text file from a path.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="text"></param>
		/// <param name="fileInfo"></param>
		/// <returns></returns>
		public static bool TryGetFileText(string path, out string text, out FileInfo fileInfo)
		{
			text = null;
			fileInfo = null;
			if (File.Exists(path))
			{
				try
				{
					using (var reader = new StreamReader(path))
					{
						text = reader.ReadToEnd();
						fileInfo = new FileInfo(path);
					}
					return true;
				}
				catch (Exception e)
				{
					e.Write();
					return false;
				}
			}

			ConsoleUtils.WriteLine("Unable to bring up the file.");
			return false;
		}
	}
}
