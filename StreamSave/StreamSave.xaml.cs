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

		public StreamSaver()
		{
			InitializeComponent();
		}

		void OnSaveURL(object sender, RoutedEventArgs e)
		{
			var urls = GetURLsDialog.Run(this);
			if (urls == null)
				return;

			MultiProgressDialog.RunAsync(this, "Downloading...", urls, async (item, progress, cancelled) => await YouTubeDL.DownloadStream(StreamSaveDirectory, item, progress, cancelled));
		}

		void OnSavePlayList(object sender, RoutedEventArgs e)
		{
			var urls = GetURLsDialog.Run(this);
			if (urls == null)
				return;

			var items = MultiProgressDialog.RunAsync(this, "Getting playlist contents...", urls, async (item, progress, cancelled) => await YouTubeDL.GetPlayListItems(item, progress, cancelled)).SelectMany().ToList();
			if (!items.Any())
				return;
			MultiProgressDialog.RunAsync(this, "Downloading...", items, async (item, progress, cancelled) => await YouTubeDL.DownloadStream(StreamSaveDirectory, item, progress, cancelled));
		}

		void OnUpdateYouTubeDL(object sender, RoutedEventArgs e) => YouTubeDL.Update();
	}
}
