﻿using ICSharpCode.AvalonEdit;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Collections.Generic;

namespace Advobot
{
	//Create the UI
	public class BotWindow : Window
	{
		private static Grid mLayout = new Grid();

		#region Input
		private static Grid mInputLayout = new Grid();
		//(Max height has to be set here as a large number to a) not get in the way and b) not crash when resized small) I don't really like using a RTB for the input.
		private static TextBox mInputBox = new TextBox { MaxLength = 250, MaxLines = 5, MaxHeight = 1000, TextWrapping = TextWrapping.Wrap };
		private static Button mInputButton = new Button { IsEnabled = false, Content = "Enter", };
		#endregion

		#region Edit
		private static Grid mEditLayout = new Grid { Visibility = Visibility.Collapsed, };
		private static Grid mEditButtonLayout = new Grid();
		private static TextEditor mEditBox = new TextEditor { WordWrap = true, VerticalScrollBarVisibility = ScrollBarVisibility.Visible, ShowLineNumbers = true, };
		private static TextBox mEditSaveBox = new TextBox
		{
			Text = "Successfully saved the file.",
			Visibility = Visibility.Collapsed,
			TextAlignment = TextAlignment.Center,
			VerticalContentAlignment = VerticalAlignment.Center,
			IsReadOnly = true,
		};
		private static Button mEditSaveButton = new Button { Content = "Save", };
		private static Button mEditCloseButton = new Button { Content = "Close", };
		#endregion

		#region Output
		private static MenuItem mOutputContextMenuSave = new MenuItem { Header = "Save Output Log", };
		private static MenuItem mOutputContextMenuClear = new MenuItem { Header = "Clear Output Log", };
		private static ContextMenu mOutputContextMenu = new ContextMenu { ItemsSource = new[] { mOutputContextMenuSave, mOutputContextMenuClear }, };
		private static RichTextBox mOutputBox = new RichTextBox
		{
			ContextMenu = mOutputContextMenu,
			IsReadOnly = true,
			IsDocumentEnabled = true,
			VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
			Background = Brushes.White,
		};
		#endregion

		#region Menu
		private static readonly string mCmdsCmds = "Commands:".PadRight(Constants.PAD_RIGHT) + "Aliases:\n" + UICommandNames.FormatStringForUse();
		private const string mHelpSynt = "\n\nCommand Syntax:\n\t[] means required\n\t<> means optional\n\t| means or";
		private const string mHelpInf1 = "\n\nLatency:\n\tTime it takes for a command to reach the bot.\nMemory:\n\tAmount of RAM the program is using.\n\t(This is wrong most of the time.)";
		private const string mHelpInf2 = "\nThreads:\n\tWhere all the actions in the bot happen.\nShards:\n\tHold all the guilds a bot has on its client.\n\tThere is a limit of 2500 guilds per shard.";
		private const string mHelpVers = "\n\nAPI Wrapper Version: " + Constants.API_VERSION + "\nBot Version: " + Constants.BOT_VERSION + "\nGitHub Repository: ";
		private const string mHelpHelp = "\n\nNeed additional help? Join the Discord server: ";
		private static Inline mHelpFirstRun = new Run(mCmdsCmds + mHelpSynt + mHelpInf1 + mHelpInf2 + mHelpVers);
		private static Inline mHelpFirstHyperlink = UIMakeElement.MakeHyperlink(Constants.REPO, "Advobot");
		private static Inline mHelpSecondRun = new Run(mHelpHelp);
		private static Inline mHelpSecondHyperlink = UIMakeElement.MakeHyperlink(Constants.DISCORD_INV, "Here");
		private static Paragraph mHelpParagraph = new Paragraph(mHelpFirstRun);

		private static Inline mInfoFirstRun = new Run(Actions.FormatLoggedThings(true));
		private static Paragraph mInfoParagraph = new Paragraph(mInfoFirstRun);

		private static TreeView mFileTreeView = new TreeView();
		private static Paragraph mFileParagraph = new Paragraph();

		private static RichTextBox mMenuBox = new RichTextBox { IsReadOnly = true, IsDocumentEnabled = true, Visibility = Visibility.Collapsed, Background = Brushes.White, };

		private static string mLastButtonClicked;
		private static Grid mButtonLayout = new Grid();
		private static Button mHelpButton = new Button { Content = "Help", };
		private static Button mSettingsButton = new Button { Content = "Settings", };
		private static Button mInfoButton = new Button { Content = "Info", };
		private static Button mFileButton = new Button { Content = "Files", };
		#endregion

		#region Settings
		private static Grid mSettingsLayout = new Grid() { Visibility = Visibility.Collapsed };
		private const int TITLE_START_COLUMN = 5;
		private const int TITLE_COLUMN_LENGTH = 35;
		private const int TB_START_COLUMN = 40;
		private const int TB_COLUMN_LENGTH = 55;

		private static CheckBox mAlwaysDownloadUsers = new CheckBox() { Content = "Always Download Users", IsChecked = Variables.BotInfo.AlwaysDownloadUsers, Tag = SettingOnBot.AlwaysDownloadUsers };
		private static Viewbox mAlwaysDownloadUsersVB = new Viewbox() { Child = mAlwaysDownloadUsers, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center };

		private static TextBox mPrefixTitle = new TextBox() { Text = "Prefix:", IsReadOnly = true, BorderThickness = new Thickness(0), VerticalAlignment = VerticalAlignment.Center };
		private static TextBox mPrefixSetting = new TextBox() { Text = Variables.BotInfo.Prefix, Tag = SettingOnBot.Prefix, MaxLength = 10 };

		private static TextBox mBotOwnerTitle = new TextBox() { Text = "Bot Owner:", IsReadOnly = true, BorderThickness = new Thickness(0), VerticalAlignment = VerticalAlignment.Center };
		private static TextBox mBotOwnerSetting = new TextBox() { Text = Variables.BotInfo.BotOwner.ToString(), Tag = SettingOnBot.BotOwner, MaxLength = 18 };

		private static TextBox mGameTitle = new TextBox() { Text = "Game:", IsReadOnly = true, BorderThickness = new Thickness(0), VerticalAlignment = VerticalAlignment.Center };
		private static TextBox mGameSetting = new TextBox() { Text = Variables.BotInfo.Game, Tag = SettingOnBot.Game, MaxLength = 50 };

		private static TextBox mStreamTitle = new TextBox() { Text = "Stream:", IsReadOnly = true, BorderThickness = new Thickness(0), VerticalAlignment = VerticalAlignment.Center };
		private static TextBox mStreamSetting = new TextBox() { Text = Variables.BotInfo.Stream, Tag = SettingOnBot.Stream, MaxLength = 50 };

		private static TextBox mShardTitle = new TextBox() { Text = "Shard Count:", IsReadOnly = true, BorderThickness = new Thickness(0), VerticalAlignment = VerticalAlignment.Center };
		private static TextBox mShardSetting = new TextBox() { Text = Variables.BotInfo.ShardCount.ToString(), Tag = SettingOnBot.ShardCount, MaxLength = 3 };

		private static TextBox mMessageCacheTitle = new TextBox() { Text = "Message Cache:", IsReadOnly = true, BorderThickness = new Thickness(0), VerticalAlignment = VerticalAlignment.Center };
		private static TextBox mMessageCacheSetting = new TextBox() { Text = Variables.BotInfo.MessageCacheSize.ToString(), Tag = SettingOnBot.MessageCacheSize, MaxLength = 6 };

		private static TextBox[] mTitleBoxes = new[] { mPrefixTitle, mBotOwnerTitle, mGameTitle, mStreamTitle, mShardTitle, mMessageCacheTitle };
		private static TextBox[] mSettingBoxes = new[] { mPrefixSetting, mBotOwnerSetting, mGameSetting, mStreamSetting, mShardSetting, mMessageCacheSetting };

		private static Button mSettingsSaveButton = new Button() { Content = "Save Settings" };

		//TODO: LogLevel and TrustedUsers
		#endregion

		#region System Info
		private static Grid mSysInfoLayout = new Grid();
		private static TextBox mSysInfoUnder = new TextBox();
		private static TextBox mLatency = new TextBox { IsReadOnly = true, BorderThickness = new Thickness(0, .5, 0, .5), };
		private static TextBox mMemory = new TextBox { IsReadOnly = true, BorderThickness = new Thickness(0, .5, 0, .5), };
		private static TextBox mThreads = new TextBox { IsReadOnly = true, BorderThickness = new Thickness(0, .5, 0, .5), };
		private static TextBox mShards = new TextBox { IsReadOnly = true, BorderThickness = new Thickness(0, .5, 0, .5), };
		private static TextBox mPrefix = new TextBox { IsReadOnly = true, BorderThickness = new Thickness(0, .5, 0, .5), };
		private static Viewbox mLatencyView = new Viewbox { Child = mLatency, };
		private static Viewbox mMemoryView = new Viewbox { Child = mMemory, };
		private static Viewbox mThreadsView = new Viewbox { Child = mThreads, };
		private static Viewbox mShardsView = new Viewbox { Child = mShards, };
		private static Viewbox mPrefixView = new Viewbox { Child = mPrefix, HorizontalAlignment = HorizontalAlignment.Stretch, };
		#endregion

		#region Misc
		private static ToolTip mMemHoverInfo = new ToolTip { Content = "This is not guaranteed to be 100% correct.", };
		private static ToolTip mSaveToolTip = new ToolTip() { Content = "Successfully saved the file." };
		private static Binding mInputBinding = UILayoutModification.CreateBinding(.275);
		private static Binding mFirstMenuBinding = UILayoutModification.CreateBinding(.0157);
		private static Binding mSecondMenuBinding = UILayoutModification.CreateBinding(.0195);
		#endregion

		public BotWindow()
		{
			FontFamily = new FontFamily("Courier New");
			InitializeComponent();
		}
		private void InitializeComponent()
		{
			//Main layout
			UILayoutModification.AddRows(mLayout, 100);
			UILayoutModification.AddCols(mLayout, 4);

			//Output
			UILayoutModification.AddItemAndSetPositionsAndSpans(mLayout, mOutputBox, 0, 87, 0, 4);
			UILayoutModification.AddItemAndSetPositionsAndSpans(mLayout, mMenuBox, 0, 90, 3, 1);

			//Settings
			UILayoutModification.AddItemAndSetPositionsAndSpans(mLayout, mSettingsLayout, 0, 87, 3, 1, 250, 100);
			UILayoutModification.AddItemAndSetPositionsAndSpans(mSettingsLayout, mAlwaysDownloadUsersVB, 0, 10, 5, 90);
			for (int i = 0; i < mTitleBoxes.Length; i++)
			{
				UILayoutModification.AddItemAndSetPositionsAndSpans(mSettingsLayout, mTitleBoxes[i], (i * 10) + 10, 10, TITLE_START_COLUMN, TITLE_COLUMN_LENGTH);
				UILayoutModification.AddItemAndSetPositionsAndSpans(mSettingsLayout, mSettingBoxes[i], (i * 10) + 10, 10, TB_START_COLUMN, TB_COLUMN_LENGTH);
			}
			UILayoutModification.AddItemAndSetPositionsAndSpans(mSettingsLayout, mSettingsSaveButton, 240, 10, 0, 100);

			//System Info
			UILayoutModification.AddItemAndSetPositionsAndSpans(mLayout, mSysInfoLayout, 87, 3, 0, 3, 0, 5);
			UILayoutModification.AddItemAndSetPositionsAndSpans(mSysInfoLayout, mSysInfoUnder, 0, 1, 0, 5);
			UILayoutModification.AddItemAndSetPositionsAndSpans(mSysInfoLayout, mLatencyView, 0, 1, 0, 1);
			UILayoutModification.AddItemAndSetPositionsAndSpans(mSysInfoLayout, mMemoryView, 0, 1, 1, 1);
			UILayoutModification.AddItemAndSetPositionsAndSpans(mSysInfoLayout, mThreadsView, 0, 1, 2, 1);
			UILayoutModification.AddItemAndSetPositionsAndSpans(mSysInfoLayout, mShardsView, 0, 1, 3, 1);
			UILayoutModification.AddItemAndSetPositionsAndSpans(mSysInfoLayout, mPrefixView, 0, 1, 4, 1);

			//Input
			UILayoutModification.AddItemAndSetPositionsAndSpans(mLayout, mInputLayout, 90, 10, 0, 3, 1, 10);
			UILayoutModification.AddBinding(mInputBox, FontSizeProperty, mInputBinding);
			UILayoutModification.AddItemAndSetPositionsAndSpans(mInputLayout, mInputBox, 0, 1, 0, 9);
			UILayoutModification.AddItemAndSetPositionsAndSpans(mInputLayout, mInputButton, 0, 1, 9, 1);

			//Buttons
			UILayoutModification.AddItemAndSetPositionsAndSpans(mLayout, mButtonLayout, 87, 13, 3, 1, 1, 4);
			UILayoutModification.AddItemAndSetPositionsAndSpans(mButtonLayout, mHelpButton, 0, 1, 0, 1);
			UILayoutModification.AddItemAndSetPositionsAndSpans(mButtonLayout, mSettingsButton, 0, 1, 1, 1);
			UILayoutModification.AddItemAndSetPositionsAndSpans(mButtonLayout, mInfoButton, 0, 1, 2, 1);
			UILayoutModification.AddItemAndSetPositionsAndSpans(mButtonLayout, mFileButton, 0, 1, 3, 1);

			//Edit
			UILayoutModification.AddItemAndSetPositionsAndSpans(mLayout, mEditLayout, 0, 100, 0, 4, 100, 4);
			UILayoutModification.AddItemAndSetPositionsAndSpans(mEditLayout, mEditBox, 0, 100, 0, 3);
			UILayoutModification.AddBinding(mEditBox, FontSizeProperty, mSecondMenuBinding);
			UILayoutModification.AddItemAndSetPositionsAndSpans(mEditLayout, mEditSaveBox, 84, 3, 3, 1);
			UILayoutModification.AddBinding(mEditSaveBox, FontSizeProperty, mSecondMenuBinding);
			UILayoutModification.AddItemAndSetPositionsAndSpans(mEditLayout, mEditButtonLayout, 87, 13, 3, 1, 1, 2);
			UILayoutModification.AddItemAndSetPositionsAndSpans(mEditButtonLayout, mEditSaveButton, 0, 1, 0, 1);
			UILayoutModification.AddItemAndSetPositionsAndSpans(mEditButtonLayout, mEditCloseButton, 0, 1, 1, 1);

			//Paragraphs
			mHelpParagraph.Inlines.AddRange(new[] { mHelpFirstHyperlink, mHelpSecondRun, mHelpSecondHyperlink });
			mFileParagraph.Inlines.Add(mFileTreeView);

			//Events
			mInputBox.KeyUp += AcceptInput;
			mMemory.MouseEnter += ModifyMemHoverInfo;
			mMemory.MouseLeave += ModifyMemHoverInfo;
			mInputButton.Click += AcceptInput;
			mOutputContextMenuSave.Click += SaveOutput;
			mOutputContextMenuClear.Click += ClearOutput;
			mHelpButton.Click += BringUpMenu;
			mSettingsButton.Click += BringUpMenu;
			mInfoButton.Click += BringUpMenu;
			mFileButton.Click += BringUpMenu;
			mEditCloseButton.Click += CloseEditScreen;
			mEditSaveButton.Click += SaveEditScreen;
			mSettingsSaveButton.Click += SaveSettings;

			//Set this panel as the content for this window.
			Content = mLayout;
			//Actually run the application
			RunApplication();
		}
		private void RunApplication()
		{
			//Make console output show on the output text block and box
			Console.SetOut(new UITextBoxStreamWriter(mOutputBox));

			//Validate path/botkey after the UI has launched to have them logged
			Task.Run(async () =>
			{
				//Check if valid path at startup
				Variables.GotPath = Actions.ValidatePath(Properties.Settings.Default.Path, true);
				//Check if valid key at startup
				Variables.GotKey = Variables.GotPath && await Actions.ValidateBotKey(Variables.Client, Properties.Settings.Default.BotKey, true);
				//Try to start the bot
				Actions.MaybeStartBot();
			});

			//Make sure the system information stays updated
			UpdateSystemInformation();
		}
		private void UpdateSystemInformation()
		{
			var timer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 500) };
			timer.Tick += (sender, e) =>
			{
				var client = Variables.Client;
				mLatency.Text = String.Format("Latency: {0}ms", client.GetLatency());
				mMemory.Text = String.Format("Memory: {0}MB", Actions.GetMemory().ToString("0.00"));
				mThreads.Text = String.Format("Threads: {0}", Process.GetCurrentProcess().Threads.Count);
				mShards.Text = String.Format("Guilds: {0}", client.GetGuilds().Count);
				mPrefix.Text = String.Format("Members: {0}", client.GetGuilds().SelectMany(x => x.Users).Select(x => x.Id).Distinct().Count());
				mInfoParagraph.Inlines.Clear();
				mInfoParagraph.Inlines.Add(new Run(Actions.FormatLoggedThings(true) + "\n\nCharacter Count: ~540,000\nLine Count: ~15,500"));
			};
			timer.Start();
		}

		private void AcceptInput(object sender, KeyEventArgs e)
		{
			var text = mInputBox.Text;
			if (String.IsNullOrWhiteSpace(text))
			{
				mInputButton.IsEnabled = false;
				return;
			}
			else
			{
				if (e.Key.Equals(Key.Enter) || e.Key.Equals(Key.Return))
				{
					UICommandHandler.GatherInput();
				}
				else
				{
					mInputButton.IsEnabled = true;
				}
			}
		}
		private void AcceptInput(object sender, RoutedEventArgs e)
		{
			UICommandHandler.GatherInput();
		}
		private void SaveOutput(object sender, RoutedEventArgs e)
		{
			//Make sure the path is valid
			var path = Actions.GetBaseBotDirectory("Output_Log_" + DateTime.UtcNow.ToString("MM-dd_HH-mm-ss") + Constants.GENERAL_FILE_EXTENSION);
			if (path == null)
			{
				Actions.WriteLine("Unable to save the output log.");
				return;
			}

			//Save the file
			using (FileStream stream = new FileStream(path, FileMode.Create))
			{
				new TextRange(mOutputBox.Document.ContentStart, mOutputBox.Document.ContentEnd).Save(stream, DataFormats.Text, true);
			}

			//Write to the console telling the user that the console log was successfully saved
			Actions.WriteLine("Successfully saved the output log.");
		}
		private void ClearOutput(object sender, RoutedEventArgs e)
		{
			var result = MessageBox.Show("Are you sure you want to clear the output window?", Variables.BotName, MessageBoxButton.OKCancel);

			switch (result)
			{
				case MessageBoxResult.OK:
				{
					mOutputBox.Document.Blocks.Clear();
					break;
				}
			}
		}
		private void BringUpMenu(object sender, RoutedEventArgs e)
		{
			//Make sure everything is loaded first
			if (!Variables.Loaded)
				return;
			//Get the button's name
			var name = (sender as Button).Content.ToString();
			//Remove the current blocks in the document
			mMenuBox.Document.Blocks.Clear();
			mMenuBox.Visibility = Visibility.Collapsed;
			mSettingsLayout.Visibility = Visibility.Collapsed;
			//Disable the rtb if the most recent button clicked is clicked again
			if (Actions.CaseInsEquals(name, mLastButtonClicked))
			{
				UILayoutModification.SetColAndSpan(mOutputBox, 0, 4);
				mLastButtonClicked = null;
			}
			else
			{
				//Resize the regular output window
				UILayoutModification.SetColAndSpan(mOutputBox, 0, 3);
				//Keep track of the last button clicked
				mLastButtonClicked = name;

				//Show the text for help
				if (Actions.CaseInsEquals(name, mHelpButton.Content.ToString()))
				{
					//Make the secondary output visible
					mMenuBox.Visibility = Visibility.Visible;
					mMenuBox.SetBinding(FontSizeProperty, mFirstMenuBinding);
					mMenuBox.Document.Blocks.Add(mHelpParagraph);
				}
				//Show the text for settings
				else if (Actions.CaseInsEquals(name, mSettingsButton.Content.ToString()))
				{
					//Make the settings layout visible
					mSettingsLayout.Visibility = Visibility.Visible;
				}
				//Show the text for info
				else if (Actions.CaseInsEquals(name, mInfoButton.Content.ToString()))
				{
					//Make the secondary output visible
					mMenuBox.Visibility = Visibility.Visible;
					mMenuBox.SetBinding(FontSizeProperty, mSecondMenuBinding);
					mMenuBox.Document.Blocks.Add(mInfoParagraph);
				}
				//Show the text for settings
				else if (Actions.CaseInsEquals(name, mFileButton.Content.ToString()))
				{
					//Make the secondary output visible
					mMenuBox.Visibility = Visibility.Visible;
					mFileParagraph = UIMakeElement.MakeGuildTreeView(mFileParagraph);
					mMenuBox.SetBinding(FontSizeProperty, mSecondMenuBinding);
					mMenuBox.Document.Blocks.Add(mFileParagraph);
				}
			}
		}
		private void ModifyMemHoverInfo(object sender, RoutedEventArgs e)
		{
			UILayoutModification.ToggleToolTip(mMemHoverInfo);
		}
		private void CloseEditScreen(object sender, RoutedEventArgs e)
		{
			var result = MessageBox.Show("Are you sure you want to close the edit window?", Variables.BotName, MessageBoxButton.OKCancel);

			switch (result)
			{
				case MessageBoxResult.OK:
				{
					mEditLayout.Visibility = Visibility.Collapsed;
					break;
				}
			}
		}
		private void SaveEditScreen(object sender, RoutedEventArgs e)
		{
			var fileLocation = mEditBox.Tag.ToString();
			if (String.IsNullOrWhiteSpace(fileLocation) || !File.Exists(fileLocation))
			{
				MessageBox.Show("Unable to gather the path for this file.", Variables.BotName);
			}
			else
			{
				var fileAndExtension = fileLocation.Substring(fileLocation.LastIndexOf('\\') + 1);
				if (fileAndExtension.Equals(Constants.GUILD_INFO_LOCATION))
				{
					//Make sure the guild info stays valid
					try
					{
						var throwaway = Newtonsoft.Json.JsonConvert.DeserializeObject<BotGuildInfo>(mEditBox.Text);
					}
					catch (Exception exc)
					{
						Actions.ExceptionToConsole(exc);
						MessageBox.Show("Failed to save the file.", Variables.BotName);
						return;
					}
				}

				//Save the file and give a notification
				using (var writer = new StreamWriter(fileLocation))
				{
					writer.WriteLine(mEditBox.Text);
				}
				UILayoutModification.ToggleAndUntoggleUIEle(mEditSaveBox);
			}
		}
		private void SaveSettings(object sender, RoutedEventArgs e)
		{
			var botInfo = Variables.BotInfo;
			var success = new List<string>();
			var changed = new List<string>();

			//Go through each setting and update them
			foreach (var tb in mSettingBoxes)
			{
				var setting = (SettingOnBot)tb.Tag;
				if (setting == default(SettingOnBot))
					continue;

				var text = tb.Text;
				if (Actions.CaseInsEquals(botInfo.GetSetting(setting), text))
					continue;

				var name = Enum.GetName(typeof(SettingOnBot), setting);
				changed.Add(name);
				switch (setting)
				{
					case SettingOnBot.Prefix:
					{
						botInfo.SetPrefix(text);
						success.Add(name);
						break;
					}
					case SettingOnBot.BotOwner:
					{
						if (ulong.TryParse(text, out ulong id))
						{
							botInfo.SetBotOwner(id);
							success.Add(name);
						}
						break;
					}
					case SettingOnBot.Game:
					{
						botInfo.SetGame(text);
						success.Add(name);
						break;
					}
					case SettingOnBot.Stream:
					{
						botInfo.SetStream(text);
						success.Add(name);
						break;
					}
					case SettingOnBot.ShardCount:
					{
						if (int.TryParse(text, out int shardCount))
						{
							botInfo.SetShardCount(shardCount);
							success.Add(name);
						}
						break;
					}
					case SettingOnBot.MessageCacheSize:
					{
						if (int.TryParse(text, out int cacheSize))
						{
							botInfo.SetCacheSize(cacheSize);
							success.Add(name);
						}
						break;
					}
				}
			}
			var failure = changed.Except(success);

			if (mAlwaysDownloadUsers.IsChecked.Value != botInfo.AlwaysDownloadUsers)
			{
				botInfo.SetAlwaysDownloadUsers(!botInfo.AlwaysDownloadUsers);
				success.Add(Enum.GetName(typeof(SettingOnBot), SettingOnBot.AlwaysDownloadUsers));
			}

			//Notify what was saved
			if (success.Any())
			{
				Actions.WriteLine(String.Format("Successfully saved: {0}", String.Join(", ", success)));
				Actions.UpdateGame().Forget();
				Actions.SaveBotInfo();
			}
			if (failure.Any())
			{
				Actions.WriteLine(String.Format("Failed to save: {0}", String.Join(", ", failure)));
			}
		}
		public static void TreeViewDoubleClick(object sender, RoutedEventArgs e)
		{
			//Get the double clicked item
			var treeItem = sender as TreeViewItem;
			if (treeItem == null)
				return;
			//Get the path from the tag
			var fileLocation = treeItem.Tag.ToString();
			if (fileLocation == null)
				return;
			//Print out all the info in that file
			var data = "";
			using (var reader = new StreamReader(fileLocation))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					data += line + Environment.NewLine;
				}
			}
			//Change the text in the bot and make it visible
			mEditBox.Text = data;
			mEditBox.Tag = fileLocation;
			mEditLayout.Visibility = Visibility.Visible;
		}

		public static RichTextBox Output { get { return mOutputBox; } }
		public static RichTextBox Menu { get { return mMenuBox; } }
		public static TextBox Input { get { return mInputBox; } }
		public static Button InputButton { get { return mInputButton; } }
	}

	//Modify the UI
	public class UILayoutModification
	{
		public static void AddRows(Grid grid, int amount)
		{
			for (int i = 0; i < amount; i++)
			{
				grid.RowDefinitions.Add(new RowDefinition());
			}
		}

		public static void AddCols(Grid grid, int amount)
		{
			for (int i = 0; i < amount; i++)
			{
				grid.ColumnDefinitions.Add(new ColumnDefinition());
			}
		}

		public static void SetRowAndSpan(UIElement item, int start = 0, int length = 1)
		{
			Grid.SetRow(item, start < 0 ? 0 : start);
			Grid.SetRowSpan(item, length < 1 ? 1 : length);
		}

		public static void SetColAndSpan(UIElement item, int start = 0, int length = 1)
		{
			Grid.SetColumn(item, start < 0 ? 0 : start);
			Grid.SetColumnSpan(item, length < 1 ? 1 : length);
		}

		public static void AddItemAndSetPositionsAndSpans(Panel parent, UIElement child, int rowStart, int rowLength, int columnStart, int columnLength, int setRows = 0, int setColumns = 0)
		{
			if (child is Grid)
			{
				AddRows(child as Grid, setRows);
				AddCols(child as Grid, setColumns);
			}
			parent.Children.Add(child);
			SetRowAndSpan(child, rowStart, rowLength);
			SetColAndSpan(child, columnStart, columnLength);
		}

		public static Binding CreateBinding(double val)
		{
			return new Binding
			{
				Path = new PropertyPath("ActualHeight"),
				RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Grid), 1),
				Converter = new UIFontResizer(val),
			};
		}

		public static void AddBinding(Control element, DependencyProperty dproperty, Binding binding)
		{
			element.SetBinding(dproperty, binding);
		}

		public static void ToggleToolTip(ToolTip ttip)
		{
			ttip.IsOpen = !ttip.IsOpen;
		}

		public static void ToggleUIElement(UIElement ele)
		{
			ele.Visibility = ele.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
		}

		public static void ToggleAndUntoggleUIEle(UIElement ele)
		{
			ele.Dispatcher.InvokeAsync(async () =>
			{
				ToggleUIElement(ele);
				await Task.Delay(2500);
				ToggleUIElement(ele);
			});
		}

		public static void AddHyperlink(RichTextBox output, string link, string name, string beforeText = null, string afterText = null)
		{
			//Create the hyperlink
			var hyperlink = UIMakeElement.MakeHyperlink(link, name);
			if (hyperlink == null)
			{
				return;
			}
			//Check if the paragraph is valid
			var para = BotWindow.Output.Document.Blocks.LastBlock as Paragraph;
			if (para == null)
			{
				Actions.WriteLine(link);
				return;
			}
			//Format the text before the hyperlink
			if (String.IsNullOrWhiteSpace(beforeText))
			{
				para.Inlines.Add(new Run(DateTime.Now.ToString("HH:mm:ss") + ": "));
			}
			else
			{
				para.Inlines.Add(new Run(beforeText));
			}
			//Add in the hyperlink
			para.Inlines.Add(hyperlink);
			//Format the text after the hyperlink
			if (String.IsNullOrWhiteSpace(beforeText))
			{
				para.Inlines.Add(new Run("\r"));
			}
			else
			{
				para.Inlines.Add(new Run(afterText));
			}
			//Add the paragraph to the ouput
			output.Document.Blocks.Add(para);
		}
	}

	//Make certain elements
	public class UIMakeElement
	{
		public static Hyperlink MakeHyperlink(string link, string name)
		{
			//Make sure the input is a valid link
			if (!Actions.ValidateURL(link))
			{
				Actions.WriteLine(Actions.ERROR("Invalid URL."));
				return null;
			}
			//Create the hyperlink
			var hyperlink = new Hyperlink(new Run(name))
			{
				NavigateUri = new Uri(link),
				IsEnabled = true,
			};
			//Make it work when clicked
			hyperlink.RequestNavigate += (sender, e) =>
			{
				Process.Start(e.Uri.ToString());
				e.Handled = true;
			};
			return hyperlink;
		}

		public static Paragraph MakeGuildTreeView(Paragraph input)
		{
			//Get the directory
			var directory = Actions.GetBaseBotDirectory();
			if (directory == null || !Directory.Exists(directory))
				return input;

			//Create the treeview
			var treeView = new TreeView() { BorderThickness = new Thickness(0) };

			//Format the treeviewitems
			Directory.GetDirectories(directory).ToList().ForEach(guildDir =>
			{
				//Separate the ID from the rest of the directory
				var strID = guildDir.Substring(guildDir.LastIndexOf('\\') + 1);
				//Make sure the ID is valid
				if (!ulong.TryParse(strID, out ulong ID))
					return;

				string header;
				try
				{
					header = String.Format("({0}) {1}", strID, Variables.Client.GetGuild(ID).Name);
				}
				catch
				{
					//This means that the guild is currently not using the bot. Don't delete the directory in case they ever do come back to using the bot.
					return;
				}

				//Get all of the files
				var listOfFiles = new List<TreeViewItem>();
				Directory.GetFiles(guildDir).ToList().ForEach(file =>
				{
					var fileAndExtension = Path.GetFileName(file);
					if (!Constants.VALID_GUILD_FILES.Contains(fileAndExtension))
						return;

					var fileItem = new TreeViewItem() { Header = fileAndExtension, Tag = file };
					fileItem.MouseDoubleClick += BotWindow.TreeViewDoubleClick;
					listOfFiles.Add(fileItem);
				});

				//If no items then don't bother adding in the guild to the treeview
				if (!listOfFiles.Any())
					return;

				//Create the guild item
				var guildItem = new TreeViewItem { Header = header };
				listOfFiles.ForEach(x =>
				{
					guildItem.Items.Add(x);
				});
				
				treeView.Items.Add(guildItem);
			});

			input.Inlines.Clear();
			input.Inlines.Add(treeView);
			return input;
		}
	}

	//New class to handle commands
	public class UICommandHandler
	{
		public static void GatherInput()
		{
			//Get the current text
			var text = BotWindow.Input.Text.Trim(new[] { '\r', '\n' });
			BotWindow.Input.Text = "";
			BotWindow.InputButton.IsEnabled = false;
			//Write it out to the ghetto console
			Console.WriteLine(text);
			//Do an action with the text
			if (!Variables.GotPath || !Variables.GotKey)
			{
				Task.Run(async () =>
				{
					//Get the input
					if (!Variables.GotPath)
					{
						Variables.GotPath = Actions.ValidatePath(text);
						Variables.GotKey = Variables.GotPath && await Actions.ValidateBotKey(Variables.Client, Properties.Settings.Default.BotKey, true);
					}
					else if (!Variables.GotKey)
					{
						Variables.GotKey = await Actions.ValidateBotKey(Variables.Client, text);
					}
					Actions.MaybeStartBot();
				});
			}
			else
			{
				HandleCommand(text);
			}
		}

		public static void HandleCommand(string input)
		{
			//Check if it's a global bot command done through the console
			if (Actions.CaseInsStartsWith(input, Variables.BotInfo.Prefix))
			{
				//Remove the prefix
				input = input.Substring(Variables.BotInfo.Prefix.Length);
				//Split the input
				var inputArray = input.Split(new[] { ' ' }, 2);
				//Get the command
				var cmd = inputArray[0];
				//Get the args
				var args = inputArray.Length > 1 ? inputArray[1] : null;
				//Find the command with the given name
				if (FindCommand(cmd, args))
					return;
				//If no command, give an error message
				Actions.WriteLine("No command could be found with that name.");
			}
		}

		public static bool FindCommand(string cmd, string args)
		{
			//Find what command it belongs to
			if (UICommandNames.GetNameAndAliases(UICommandEnum.Pause).CaseInsContains(cmd))
			{
				UICommands.PAUSE(args);
			}
			else if (UICommandNames.GetNameAndAliases(UICommandEnum.ListGuilds).CaseInsContains(cmd))
			{
				UICommands.UIListGuilds();
			}
			else if (Actions.CaseInsEquals(cmd, "test"))
			{
				UICommands.UITest();
			}
			else
			{
				return false;
			}
			return true;
		}
	}

	//Commands the bot can do through the 'console'
	public class UICommands
	{
		public static void PAUSE(string input)
		{
			if (Variables.Pause)
			{
				Variables.Pause = false;
				Actions.WriteLine("Successfully unpaused the bot.");
			}
			else
			{
				Variables.Pause = true;
				Actions.WriteLine("Successfully paused the bot.");
			}
		}

		public static void UIListGuilds()
		{
			var guilds = Variables.Client.GetGuilds().ToList();
			var countWidth = guilds.Count.ToString().Length;
			var count = 1;
			guilds.ForEach(x =>
			{
				Actions.WriteLine(String.Format("{0}. {1} Owner: {2}", count++.ToString().PadLeft(countWidth, '0'), x.FormatGuild(), x.Owner.FormatUser()));
			});
		}

		public static void UITest()
		{
#if DEBUG
			var programLoc = System.Reflection.Assembly.GetExecutingAssembly().Location;
			var newPath = Path.GetFullPath(Path.Combine(programLoc, @"..\..\..\"));
			var totalChars = 0;
			var totalLines = 0;
			foreach (var file in Directory.GetFiles(newPath))
			{
				if (Actions.CaseInsEquals(Path.GetExtension(file), ".cs"))
				{
					totalChars += File.ReadAllText(file).Length;
					totalLines += File.ReadAllLines(file).Count();
				}
			}
			Actions.WriteLine(String.Format("Current Totals:{0}\t\t\t Chars: {1}{0}\t\t\t Lines: {2}", Environment.NewLine, totalChars, totalLines));
#endif
		}
	}

	//Write the console output into the UI
	public class UITextBoxStreamWriter : TextWriter 
	{
		private TextBoxBase mOutput;
		private bool mIgnoreNewLines;
		private string mCurrentLineText;

		public UITextBoxStreamWriter(TextBoxBase output)
		{
			mOutput = output;
			mIgnoreNewLines = output is RichTextBox;
		}

		public override void Write(char value)
		{
			if (value.Equals('\n'))
			{
				Write(mCurrentLineText);
				mCurrentLineText = null;
			}
			else
			{
				mCurrentLineText += value;
			}
		}

		public override void Write(string value)
		{
			if (mIgnoreNewLines && value.Equals('\n'))
				return;

			mOutput.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
			{
				mOutput.AppendText(value);
			}));
		}

		public override Encoding Encoding
		{
			get { return Encoding.UTF8; }
		}
	}

	//Resize font
	public class UIFontResizer : IValueConverter
	{
		double convertFactor;
		public UIFontResizer(double convertFactor)
		{
			this.convertFactor = convertFactor;
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Math.Max((int)(System.Convert.ToDouble(value) * convertFactor), -1);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
