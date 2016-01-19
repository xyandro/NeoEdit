using System;
using System.Net;
using System.Threading;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Tools.Dialogs.SpiderTool
{
	internal partial class SpiderToolDialog
	{
		[DepProp]
		public string URL { get { return UIHelper<SpiderToolDialog>.GetPropValue<string>(this); } set { UIHelper<SpiderToolDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string OutputPath { get { return UIHelper<SpiderToolDialog>.GetPropValue<string>(this); } set { UIHelper<SpiderToolDialog>.SetPropValue(this, value); } }

		static SpiderToolDialog() { UIHelper<SpiderToolDialog>.Register(); }

		SpiderToolDialog()
		{
			InitializeComponent();
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			if ((String.IsNullOrWhiteSpace(URL)) || (String.IsNullOrWhiteSpace(OutputPath)))
				return;

			var url = URL;
			var outputPath = OutputPath;
			ProgressDialog.Run(this, "Downloading file", callback =>
			{
				var client = new WebClient();
				var notifier = new AutoResetEvent(false);
				client.DownloadProgressChanged += (s2, e2) => callback(e2.ProgressPercentage, false);
				client.DownloadFileCompleted += (s2, e2) => notifier.Set();
				client.DownloadFileAsync(new Uri(url), outputPath);
				notifier.WaitOne();
				return true;
			});

			DialogResult = true;
		}

		static public void Run(Window parent) => new SpiderToolDialog() { Owner = parent }.ShowDialog();
	}
}
