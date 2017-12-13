﻿using Advobot.Core.Actions;
using Advobot.Core.Actions.Formatting;
using Advobot.UILauncher.Enums;
using Advobot.UILauncher.Interfaces;
using Discord.WebSocket;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Controls;

namespace Advobot.UILauncher.Classes.Controls
{
	internal class AdvobotTreeViewHeader : TreeViewItem, IAdvobotControl
	{
		private FileSystemWatcher _FSW;
		public FileSystemWatcher FileSystemWatcher => this._FSW;
		private DirectoryInfo _DI;
		public DirectoryInfo GuildDirectory => this._DI;
		private SocketGuild _G;
		public SocketGuild Guild
		{
			get => _G;
			set
			{
				this._G = value;

				//Make sure the guild currently has a directory. If not, create it
				var directories = IOActions.GetBaseBotDirectory().GetDirectories();
				var guildDir = directories.SingleOrDefault(x => x.Name == this._G.Id.ToString());
				if (!guildDir.Exists)
				{
					Directory.CreateDirectory(guildDir.FullName);
				}

				//Use the correct directory and files
				this._DI = guildDir;
				this._Files.Clear();
				foreach (var file in this._DI.GetFiles())
				{
					this._Files.Add(new AdvobotTreeViewFile(file));
				}

				//If any files get updated or deleted then modify the guild files in the treeview
				this._FSW?.Dispose();
				this._FSW = new FileSystemWatcher(this._DI.FullName);
				this._FSW.Deleted += this.OnFileChangeInGuildDirectory;
				this._FSW.Renamed += this.OnFileChangeInGuildDirectory;
				this._FSW.Created += this.OnFileChangeInGuildDirectory;
				this._FSW.EnableRaisingEvents = true;
			}
		}
		private ObservableCollection<AdvobotTreeViewFile> _Files = new ObservableCollection<AdvobotTreeViewFile>();

		public AdvobotTreeViewHeader(SocketGuild guild)
		{
			this.Header = guild.FormatGuild();
			this.Guild = guild;
			this.Tag = new CompGuild(guild);
			this.ItemsSource = this._Files;
			this.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left;
			this.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
			SetResourceReferences();
		}
		public void SetResourceReferences()
		{
			this.SetResourceReference(Control.BackgroundProperty, ColorTarget.BaseBackground);
			this.SetResourceReference(Control.ForegroundProperty, ColorTarget.BaseForeground);
		}

		private void OnFileChangeInGuildDirectory(object sender, FileSystemEventArgs e)
		{
			//Only allow basic text files to be shown
			//If someone is determined, they could get any file in here by renaming the extension
			//But they know what they're getting into if they do that, so no worries.
			if (!new[] { ".json", ".txt", ".config" }.Contains(Path.GetExtension(e.FullPath)))
			{
				return;
			}

			this.Dispatcher.Invoke(() =>
			{
				switch (e.ChangeType)
				{
					case WatcherChangeTypes.Created:
					{
						this._Files.Add(new AdvobotTreeViewFile(new FileInfo(e.FullPath)));
						break;
					}
					case WatcherChangeTypes.Deleted:
					{
						this._Files.Remove(this._Files.FirstOrDefault(x => x.FileInfo.FullName == e.FullPath));
						break;
					}
					case WatcherChangeTypes.Renamed:
					{
						var renamed = (RenamedEventArgs)e;
						this._Files.FirstOrDefault(x => x.FileInfo.FullName == renamed.OldFullPath)?.Update(renamed);
						break;
					}
				}
				this.Items.SortDescriptions.Clear();
				this.Items.SortDescriptions.Add(new SortDescription("Header", ListSortDirection.Ascending));
			});
		}
	}
}