using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;
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

			var youTubeItems = ProgressDialog.Run(this, "Getting YouTube data...", (cancelled, progress) =>
			{
				var playlists = urls.Where(url => url.Contains("playlist")).ToDictionary(url => url, url => default(List<string>));
				playlists = Task.WhenAll(playlists.Keys.Select(url => youTube.GetPlaylistVideoIDs(url).ContinueWith(task => new { url, task.Result }))).Result.ToDictionary(obj => obj.url, obj => obj.Result);
				urls = urls.SelectMany(url => playlists.ContainsKey(url) ? playlists[url] : new List<string> { url }).ToList();

				var result = new List<YouTubeItem>();
				const int NumRunning = 10;
				var running = new List<Task<YouTubeVideo>>();
				var total = urls.Count;
				while (true)
				{
					progress(result.Count * 100 / total);
					if (cancelled())
						throw new Exception("Cancelled");

					while ((running.Count < NumRunning) && (urls.Count != 0))
					{
						var url = urls[0];
						urls.RemoveAt(0);
						running.Add(youTube.GetBestVideo(url));
					}
					if (running.Count == 0)
						break;

					var finished = Task.WhenAny(running).Result;
					running.Remove(finished);
					result.Add(new YouTubeItem(youTube, finished.Result));
				}
				return result;
			}) as List<YouTubeItem>;
			if (youTubeItems == null)
				return;
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
			foreach (var ripItem in RipItems)
				ProgressDialog.Run(this, ripItem.ToString(), (cancelled, progress) => ripItem.Run(cancelled, progress, directory));
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
