using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;
using NeoEdit.Rip.Dialogs;

namespace NeoEdit.Rip
{
	partial class Ripper
	{
		[DepProp]
		ObservableCollection<RipItem> RipItems { get { return UIHelper<Ripper>.GetPropValue<ObservableCollection<RipItem>>(this); } set { UIHelper<Ripper>.SetPropValue(this, value); } }
		[DepProp]
		string OutputDirectory { get { return UIHelper<Ripper>.GetPropValue<string>(this); } set { UIHelper<Ripper>.SetPropValue(this, value); } }

		YouTube youTube = new YouTube();
		static Ripper() { UIHelper<Ripper>.Register(); }

		public Ripper()
		{
			RipMenuItem.RegisterCommands(this, (command, multiStatus) => RunCommand(command));
			InitializeComponent();
			RipItems = new ObservableCollection<RipItem>();
			OutputDirectory = Directory.GetCurrentDirectory();
		}

		protected override void OnClosed(EventArgs e)
		{
			youTube.Dispose();
			base.OnClosed(e);
		}

		void RunCommand(RipCommand command)
		{
			switch (command)
			{
				case RipCommand.File_Exit: Close(); break;
				case RipCommand.Add_CD: Command_Add_CD(); break;
				case RipCommand.Add_YouTube: Command_Add_YouTube(); break;
			}
		}

		void Command_Add_CD()
		{
			using (var drive = AddCDDialog.Run(this))
			{
				if (drive == null)
					return;

				foreach (var track in drive.GetTracks())
					RipItems.Add(track);
			}
		}

		void Command_Add_YouTube()
		{
			var urls = AddYouTubeDialog.Run(this);
			if (urls == null)
				return;

			var playlists = urls.Where(YouTube.IsPlaylist).ToList();
			var playlistItems = MultiProgressDialog.RunAsync(this, "Getting playlist contents...", playlists.Select(url => YouTube.GetPlaylistID(url)), async (id, progress, token) => await youTube.GetPlaylistVideoIDs(id, progress, token));
			var playlistsMap = playlists.ToDictionary(playlistItems);
			urls = urls.SelectMany(url => playlistsMap.ContainsKey(url) ? playlistsMap[url] : new List<string> { YouTube.GetVideoID(url) }).Distinct().ToList();

			var youTubeItems = MultiProgressDialog.RunAsync(this, "Getting video data...", urls, async (id, progress, token) => new YouTubeItem(youTube, await youTube.GetBestVideo(id, progress, token))).NonNull().ToList();
			foreach (var youTubeItem in youTubeItems)
				RipItems.Add(youTubeItem);
		}

		void OnRemoveClick(object sender = null, RoutedEventArgs e = null) => ripItems.SelectedItems.Cast<RipItem>().ToList().ForEach(item => RipItems.Remove(item));

		void OnSelectExistingClick(object sender, RoutedEventArgs e)
		{
			ripItems.UnselectAll();
			RipItems.Where(ripItem => File.Exists(ripItem.GetFileName(OutputDirectory))).ForEach(ripItem => ripItems.SelectedItems.Add(ripItem));
		}

		void OnGoClick(object sender = null, RoutedEventArgs e = null)
		{
			var directory = OutputDirectory;
			MultiProgressDialog.RunAsync(this, "Ripping...", RipItems, async (item, progress, cancelled) => await item.Run(progress, cancelled, directory));
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Delete: OnRemoveClick(); break;
			}
			base.OnPreviewKeyDown(e);
		}
	}
}
