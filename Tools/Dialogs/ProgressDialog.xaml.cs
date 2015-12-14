using System;
using System.ComponentModel;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Tools.Dialogs
{
	partial class ProgressDialog
	{
		[DepProp]
		public string DialogTitle { get { return UIHelper<ProgressDialog>.GetPropValue<string>(this); } set { UIHelper<ProgressDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int Progress { get { return UIHelper<ProgressDialog>.GetPropValue<int>(this); } set { UIHelper<ProgressDialog>.SetPropValue(this, value); } }

		static ProgressDialog() { UIHelper<ProgressDialog>.Register(); }

		BackgroundWorker worker;
		ProgressDialog(string title)
		{
			InitializeComponent();
			DialogTitle = title;
		}

		void DoWork<T>(Func<Func<int, bool, bool>, T> work)
		{
			worker = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
			worker.ProgressChanged += (s, e) => Progress = e.ProgressPercentage;
			worker.DoWork += (s, e) =>
			{
				Result = work((percent, finished) =>
				{
					worker.ReportProgress(percent);
					if (finished)
						worker.CancelAsync();
					if (worker.CancellationPending)
						e.Cancel = true;
					return e.Cancel;
				});
			};
			worker.RunWorkerCompleted += (s, e) => DialogResult = !e.Cancelled;
			worker.RunWorkerAsync();
		}

		object Result { get; set; }

		void CancelClick(object sender, RoutedEventArgs e) => worker.CancelAsync();

		public static T Run<T>(Window owner, string title, Func<Func<int, bool, bool>, T> work)
		{
			var dialog = new ProgressDialog(title) { Owner = owner };
			dialog.DoWork(work);
			dialog.ShowDialog();
			return (T)dialog.Result;
		}
	}
}
