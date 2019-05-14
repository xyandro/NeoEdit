using System;
using System.IO;
using System.Windows;
using NeoEdit.Expressions;
using NeoEdit.Controls;

namespace NeoEdit.Dialogs
{
	partial class NetworkFetchStreamDialog
	{
		internal class Result
		{
			public string Expression { get; set; }
			public string OutputDirectory { get; set; }
		}

		[DepProp]
		public string Expression { get { return UIHelper<NetworkFetchStreamDialog>.GetPropValue<string>(this); } set { UIHelper<NetworkFetchStreamDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string YouTubeDLPath { get { return UIHelper<NetworkFetchStreamDialog>.GetPropValue<string>(this); } set { UIHelper<NetworkFetchStreamDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string FFmpegPath { get { return UIHelper<NetworkFetchStreamDialog>.GetPropValue<string>(this); } set { UIHelper<NetworkFetchStreamDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string OutputDirectory { get { return UIHelper<NetworkFetchStreamDialog>.GetPropValue<string>(this); } set { UIHelper<NetworkFetchStreamDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static NetworkFetchStreamDialog() { UIHelper<NetworkFetchStreamDialog>.Register(); }

		NetworkFetchStreamDialog(NEVariables variables, string outputDirectory)
		{
			Variables = variables;
			InitializeComponent();
			OutputDirectory = outputDirectory;

			if (OutputDirectory == null)
				outputDirectoryRow.Height = new GridLength(0);
			Expression = "x";
			YouTubeDLPath = Settings.YouTubeDLPath;
			FFmpegPath = Settings.FFmpegPath;
		}

		void OnUpdateYouTubeDL(object sender, RoutedEventArgs e) => YouTubeDL.Update();

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if ((OutputDirectory != null) && (!Directory.Exists(OutputDirectory)))
				throw new Exception("Invalid output directory");
			Settings.YouTubeDLPath = YouTubeDLPath;
			Settings.FFmpegPath = FFmpegPath;
			expression.AddCurrentSuggestion();
			outputDirectory.AddCurrentSuggestion();
			result = new Result
			{
				Expression = Expression,
				OutputDirectory = OutputDirectory,
			};
			DialogResult = true;
		}

		static public Result Run(Tabs parent, NEVariables variables, string outputDirectory)
		{
			var dialog = new NetworkFetchStreamDialog(variables, outputDirectory) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
