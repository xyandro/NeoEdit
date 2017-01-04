using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Rip.Dialogs
{
	partial class AddYouTubeDialog
	{
		class VideoItemData : INotifyPropertyChanged
		{
			public event PropertyChangedEventHandler PropertyChanged;
			void SetPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

			public string ID { get; set; }
			public string Title { get; set; }
			public List<YouTubeVideo> Videos { get; set; }
			public YouTubeVideo Best => Videos.Where(video => enabled[video]).FirstOrDefault();

			readonly Dictionary<YouTubeVideo, bool> enabled;

			public VideoItemData(string id, IEnumerable<YouTubeVideo> videos)
			{
				ID = id;
				Title = videos.Select(video => video.Title).NonNullOrWhiteSpace().FirstOrDefault();
				Videos = videos.ToList();
				enabled = Videos.ToDictionary(video => video, video => (video.Video != null) && (video.Audio != null));
			}

			public bool Enabled(YouTubeVideo video) => enabled[video];
			public void Enable(YouTubeVideo video, bool isEnabled = true)
			{
				enabled[video] = isEnabled;
				SetPropertyChanged(nameof(Best));
			}

			public void Reset() => enabled.Where(pair => !pair.Value).Select(pair => pair.Key).ToList().ForEach(key => Enable(key));
		}

		readonly YouTube youTube;

		[DepProp]
		ObservableCollection<VideoItemData> VideoItems { get { return UIHelper<AddYouTubeDialog>.GetPropValue<ObservableCollection<VideoItemData>>(this); } set { UIHelper<AddYouTubeDialog>.SetPropValue(this, value); } }
		[DepProp]
		Dictionary<string, string> Extensions { get { return UIHelper<AddYouTubeDialog>.GetPropValue<Dictionary<string, string>>(this); } set { UIHelper<AddYouTubeDialog>.SetPropValue(this, value); } }
		[DepProp]
		Dictionary<string, string> Resolutions { get { return UIHelper<AddYouTubeDialog>.GetPropValue<Dictionary<string, string>>(this); } set { UIHelper<AddYouTubeDialog>.SetPropValue(this, value); } }
		[DepProp]
		Dictionary<string, string> Audios { get { return UIHelper<AddYouTubeDialog>.GetPropValue<Dictionary<string, string>>(this); } set { UIHelper<AddYouTubeDialog>.SetPropValue(this, value); } }
		[DepProp]
		Dictionary<string, string> Videos { get { return UIHelper<AddYouTubeDialog>.GetPropValue<Dictionary<string, string>>(this); } set { UIHelper<AddYouTubeDialog>.SetPropValue(this, value); } }
		[DepProp]
		Dictionary<string, int?> AudioBitRates { get { return UIHelper<AddYouTubeDialog>.GetPropValue<Dictionary<string, int?>>(this); } set { UIHelper<AddYouTubeDialog>.SetPropValue(this, value); } }
		[DepProp]
		Dictionary<string, bool> Is3Ds { get { return UIHelper<AddYouTubeDialog>.GetPropValue<Dictionary<string, bool>>(this); } set { UIHelper<AddYouTubeDialog>.SetPropValue(this, value); } }
		[DepProp]
		Dictionary<string, YouTubeVideo.AdaptiveKindEnum> AdaptiveKinds { get { return UIHelper<AddYouTubeDialog>.GetPropValue<Dictionary<string, YouTubeVideo.AdaptiveKindEnum>>(this); } set { UIHelper<AddYouTubeDialog>.SetPropValue(this, value); } }

		static AddYouTubeDialog() { UIHelper<AddYouTubeDialog>.Register(); }

		AddYouTubeDialog(YouTube youTube)
		{
			this.youTube = youTube;
			InitializeComponent();

			VideoItems = new ObservableCollection<VideoItemData>();

			changing = true;
			Extensions = YouTubeVideo.Extensions.ToDictionary(x => x == null ? "<NULL>" : $"{x}");
			extensions.SelectAll();

			Resolutions = YouTubeVideo.Resolutions.ToDictionary(x => x == null ? "<NULL>" : $"{x}");
			resolutions.SelectAll();

			Audios = YouTubeVideo.Audios.ToDictionary(x => x == null ? "<NULL>" : $"{x}");
			foreach (var audio in Audios.Where(x => x.Value != null))
				audios.SelectedItems.Add(audio);

			Videos = YouTubeVideo.Videos.ToDictionary(x => x == null ? "<NULL>" : $"{x}");
			foreach (var video in Videos.Where(x => x.Value != null))
				videos.SelectedItems.Add(video);

			AudioBitRates = YouTubeVideo.AudioBitRates.ToDictionary(x => x == null ? "<NULL>" : $"{x}");
			audioBitRates.SelectAll();

			Is3Ds = YouTubeVideo.Is3Ds.ToDictionary(x => $"{x}");
			is3Ds.SelectAll();

			AdaptiveKinds = YouTubeVideo.AdaptiveKinds.ToDictionary(x => $"{x}");
			adaptiveKinds.SelectAll();

			Loaded += AddClick;
			changing = false;
		}

		void AddClick(object sender, RoutedEventArgs e)
		{
			var urls = AddYouTubeItemsDialog.Run(this);
			if (urls == null)
				return;

			var playlistUrls = urls.Distinct().Where(YouTube.IsPlaylist).ToList();
			var playlistIDs = playlistUrls.Select(url => YouTube.GetPlaylistID(url)).Distinct().ToList();
			var playlistItems = playlistIDs.ToDictionary(MultiProgressDialog.RunAsync(this, "Getting playlist contents...", playlistIDs, async (id, progress, token) => await youTube.GetPlaylistVideoIDs(id, progress, token)));
			var playlistsMap = playlistUrls.ToDictionary(url => url, url => playlistItems[YouTube.GetPlaylistID(url)]);
			urls = urls.SelectMany(url => playlistsMap.ContainsKey(url) ? playlistsMap[url] : new List<string> { YouTube.GetVideoID(url) }).Distinct().ToList();

			var items = MultiProgressDialog.RunAsync(this, "Getting video data...", urls, async (id, progress, token) => new VideoItemData(id, await youTube.GetVideos(id, progress, token))).NonNull().ToList();
			foreach (var item in items)
			{
				VideoItems.Add(item);
				videoItems.SelectedItems.Add(item);
			}
		}

		void RemoveClick(object sender = null, RoutedEventArgs e = null) => videoItems.SelectedItems.Cast<VideoItemData>().ToList().ForEach(item => VideoItems.Remove(item));

		void InvertClick(object sender, RoutedEventArgs e)
		{
			var newItems = VideoItems.Except(videoItems.SelectedItems.Cast<VideoItemData>()).ToList();
			videoItems.SelectedItems.Clear();
			foreach (var item in newItems)
				videoItems.SelectedItems.Add(item);
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Delete: RemoveClick(); break;
			}
			base.OnPreviewKeyDown(e);
		}

		bool changing = false;
		void OnVideoItemsSelectionChanged(object sender, SelectionChangedEventArgs e) => OnRefresh();

		void OnParamsSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (changing)
				return;

			var extensionsHash = new HashSet<string>(extensions.SelectedItems.OfType<KeyValuePair<string, string>>().Select(pair => pair.Value));
			var resolutionsHash = new HashSet<string>(resolutions.SelectedItems.OfType<KeyValuePair<string, string>>().Select(pair => pair.Value));
			var audiosHash = new HashSet<string>(audios.SelectedItems.OfType<KeyValuePair<string, string>>().Select(pair => pair.Value));
			var videosHash = new HashSet<string>(videos.SelectedItems.OfType<KeyValuePair<string, string>>().Select(pair => pair.Value));
			var audioBitRatesHash = new HashSet<int?>(audioBitRates.SelectedItems.OfType<KeyValuePair<string, int?>>().Select(pair => pair.Value));
			var is3DsHash = new HashSet<bool>(is3Ds.SelectedItems.OfType<KeyValuePair<string, bool>>().Select(pair => pair.Value));
			var adaptiveKindsHash = new HashSet<YouTubeVideo.AdaptiveKindEnum>(adaptiveKinds.SelectedItems.OfType<KeyValuePair<string, YouTubeVideo.AdaptiveKindEnum>>().Select(pair => pair.Value));

			foreach (var videoItem in videoItems.SelectedItems.OfType<VideoItemData>())
				foreach (var video in videoItem.Videos)
					videoItem.Enable(video, extensionsHash.Contains(video.Extension) && resolutionsHash.Contains(video.Resolution) && audiosHash.Contains(video.Audio) && videosHash.Contains(video.Video) && audioBitRatesHash.Contains(video.AudioBitRate) && is3DsHash.Contains(video.Is3D) && adaptiveKindsHash.Contains(video.AdaptiveKind));
		}

		void OnRefresh(object sender = null, RoutedEventArgs e = null)
		{
			changing = true;
			var selected = videoItems.SelectedItems.Cast<VideoItemData>().SelectMany(videoItem => videoItem.Videos.Where(video => videoItem.Enabled(video))).ToList();

			extensions.UnselectAll();
			var extensionHash = new HashSet<string>(selected.Select(video => video.Extension).Distinct());
			foreach (var item in Extensions.Where(pair => extensionHash.Contains(pair.Value)))
				extensions.SelectedItems.Add(item);

			resolutions.UnselectAll();
			var resolutionHash = new HashSet<string>(selected.Select(video => video.Resolution).Distinct());
			foreach (var item in Resolutions.Where(pair => resolutionHash.Contains(pair.Value)))
				resolutions.SelectedItems.Add(item);

			audios.UnselectAll();
			var audioHash = new HashSet<string>(selected.Select(video => video.Audio).Distinct());
			foreach (var item in Audios.Where(pair => audioHash.Contains(pair.Value)))
				audios.SelectedItems.Add(item);

			videos.UnselectAll();
			var videoHash = new HashSet<string>(selected.Select(video => video.Video).Distinct());
			foreach (var item in Videos.Where(pair => videoHash.Contains(pair.Value)))
				videos.SelectedItems.Add(item);

			audioBitRates.UnselectAll();
			var audioBitRateHash = new HashSet<int?>(selected.Select(video => video.AudioBitRate).Distinct());
			foreach (var item in AudioBitRates.Where(pair => audioBitRateHash.Contains(pair.Value)))
				audioBitRates.SelectedItems.Add(item);

			is3Ds.UnselectAll();
			var is3DHash = new HashSet<bool>(selected.Select(video => video.Is3D).Distinct());
			foreach (var item in Is3Ds.Where(pair => is3DHash.Contains(pair.Value)))
				is3Ds.SelectedItems.Add(item);

			adaptiveKinds.UnselectAll();
			var adaptiveKindHash = new HashSet<YouTubeVideo.AdaptiveKindEnum>(selected.Select(video => video.AdaptiveKind).Distinct());
			foreach (var item in AdaptiveKinds.Where(pair => adaptiveKindHash.Contains(pair.Value)))
				adaptiveKinds.SelectedItems.Add(item);
			changing = false;
		}

		void OnReset(object sender, RoutedEventArgs e)
		{
			VideoItems.ForEach(videoItem => videoItem.Reset());
			OnRefresh();
		}

		List<YouTubeItem> result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = VideoItems.Select(videoItem => videoItem.Best).NonNull().Select(video => new YouTubeItem(youTube, video)).ToList();
			DialogResult = true;
		}

		static public List<YouTubeItem> Run(Window parent, YouTube youTube)
		{
			var dialog = new AddYouTubeDialog(youTube) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
