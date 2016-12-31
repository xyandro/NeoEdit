using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Rip.Dialogs
{
	partial class AddYouTubeDialog
	{
		internal class Result
		{
			public List<string> URLs;
			public HashSet<string> Extensions;
			public HashSet<string> Resolutions;
			public HashSet<string> Audios;
			public HashSet<string> Videos;
			public HashSet<bool> Is3Ds;
			public HashSet<YouTubeVideo.AdaptiveKindEnum> AdaptiveKinds;
		}

		[DepProp]
		public string URLs { get { return UIHelper<AddYouTubeDialog>.GetPropValue<string>(this); } set { UIHelper<AddYouTubeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public List<string> Extensions { get { return UIHelper<AddYouTubeDialog>.GetPropValue<List<string>>(this); } set { UIHelper<AddYouTubeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public List<string> Resolutions { get { return UIHelper<AddYouTubeDialog>.GetPropValue<List<string>>(this); } set { UIHelper<AddYouTubeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public List<string> Audios { get { return UIHelper<AddYouTubeDialog>.GetPropValue<List<string>>(this); } set { UIHelper<AddYouTubeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public List<string> Videos { get { return UIHelper<AddYouTubeDialog>.GetPropValue<List<string>>(this); } set { UIHelper<AddYouTubeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public List<bool> Is3Ds { get { return UIHelper<AddYouTubeDialog>.GetPropValue<List<bool>>(this); } set { UIHelper<AddYouTubeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public List<YouTubeVideo.AdaptiveKindEnum> AdaptiveKinds { get { return UIHelper<AddYouTubeDialog>.GetPropValue<List<YouTubeVideo.AdaptiveKindEnum>>(this); } set { UIHelper<AddYouTubeDialog>.SetPropValue(this, value); } }

		static AddYouTubeDialog() { UIHelper<AddYouTubeDialog>.Register(); }

		AddYouTubeDialog()
		{
			InitializeComponent();

			Extensions = YouTubeVideo.Extensions;
			extensions.SelectAll();

			Resolutions = YouTubeVideo.Resolutions;
			resolutions.SelectAll();

			Audios = YouTubeVideo.Audios;
			foreach (var audio in Audios.NonNull())
				audios.SelectedItems.Add(audio);

			Videos = YouTubeVideo.Videos;
			foreach (var video in Videos.NonNull())
				videos.SelectedItems.Add(video);

			Is3Ds = YouTubeVideo.Is3Ds;
			is3Ds.SelectAll();

			AdaptiveKinds = YouTubeVideo.AdaptiveKinds;
			adaptiveKinds.SelectAll();

		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result
			{
				URLs = URLs.Split('\r', '\n').Select(str => str.Trim()).NonNullOrEmpty().ToList(),
				Extensions = new HashSet<string>(extensions.SelectedItems.Cast<string>()),
				Resolutions = new HashSet<string>(resolutions.SelectedItems.Cast<string>()),
				Audios = new HashSet<string>(audios.SelectedItems.Cast<string>()),
				Videos = new HashSet<string>(videos.SelectedItems.Cast<string>()),
				Is3Ds = new HashSet<bool>(is3Ds.SelectedItems.Cast<bool>()),
				AdaptiveKinds = new HashSet<YouTubeVideo.AdaptiveKindEnum>(adaptiveKinds.SelectedItems.Cast<YouTubeVideo.AdaptiveKindEnum>()),
			};
			DialogResult = true;
		}

		static public Result Run(Window parent)
		{
			var dialog = new AddYouTubeDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
