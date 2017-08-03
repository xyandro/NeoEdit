using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;
using NeoEdit.StreamSave.Dialogs;

namespace NeoEdit.StreamSave
{
	partial class StreamSaver
	{
		static StreamSaver() { UIHelper<StreamSaver>.Register(); }

		public StreamSaver(List<string> urls, bool isPlaylist)
		{
			InitializeComponent();

			if (urls?.Any() == true)
			{
				Loaded += (s, e) =>
				{
					if (isPlaylist)
						SavePlaylists(urls);
					else
						SaveURLs(urls);
				};
			}
		}

		void SaveURLs(List<string> urls)
		{
			var now = DateTime.Now;
			var data = urls.Select((url, index) => Tuple.Create(url, now + TimeSpan.FromSeconds(index))).ToList();
			MultiProgressDialog.RunAsync(this, "Downloading...", data, async (item, progress, cancelled) => await YouTubeDL.DownloadStream(StreamSaveDirectory, item.Item1, item.Item2, progress, cancelled));
		}

		void SavePlaylists(List<string> urls)
		{
			var items = MultiProgressDialog.RunAsync(this, "Getting playlist contents...", urls, async (item, progress, cancelled) => await YouTubeDL.GetPlayListItems(item, progress, cancelled)).SelectMany().ToList();
			if (!items.Any())
				return;
			SaveURLs(items);
		}

		void OnSaveURL(object sender, RoutedEventArgs e)
		{
			var urls = GetURLsDialog.Run(this);
			if (urls == null)
				return;

			SaveURLs(urls);
		}

		void OnSavePlayList(object sender, RoutedEventArgs e)
		{
			var urls = GetURLsDialog.Run(this);
			if (urls == null)
				return;

			SavePlaylists(urls);
		}

		void OnUpdateYouTubeDL(object sender, RoutedEventArgs e) => YouTubeDL.Update();
	}
}
