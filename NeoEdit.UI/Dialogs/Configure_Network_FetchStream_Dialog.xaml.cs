using System;
using System.IO;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Network_FetchStream_Dialog
	{
		[DepProp]
		public string Expression { get { return UIHelper<Configure_Network_FetchStream_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Network_FetchStream_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string YouTubeDLPath { get { return UIHelper<Configure_Network_FetchStream_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Network_FetchStream_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string FFmpegPath { get { return UIHelper<Configure_Network_FetchStream_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Network_FetchStream_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string OutputDirectory { get { return UIHelper<Configure_Network_FetchStream_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Network_FetchStream_Dialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static Configure_Network_FetchStream_Dialog() { UIHelper<Configure_Network_FetchStream_Dialog>.Register(); }

		Configure_Network_FetchStream_Dialog(NEVariables variables, string outputDirectory)
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

		Configuration_Network_FetchStream result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if ((OutputDirectory != null) && (!Directory.Exists(OutputDirectory)))
				throw new Exception("Invalid output directory");
			Settings.YouTubeDLPath = YouTubeDLPath;
			Settings.FFmpegPath = FFmpegPath;
			expression.AddCurrentSuggestion();
			outputDirectory.AddCurrentSuggestion();
			result = new Configuration_Network_FetchStream
			{
				Expression = Expression,
				OutputDirectory = OutputDirectory,
			};
			DialogResult = true;
		}

		public static Configuration_Network_FetchStream Run(Window parent, NEVariables variables, string outputDirectory)
		{
			var dialog = new Configure_Network_FetchStream_Dialog(variables, outputDirectory) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
