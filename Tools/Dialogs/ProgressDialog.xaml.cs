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
		ProgressDialog(string title, Action<Func<int, bool, bool>> work)
		{
			InitializeComponent();
			DialogTitle = title;

			worker = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
			worker.ProgressChanged += (s, e) => Progress = e.ProgressPercentage;
			worker.DoWork += (s, e) =>
			{
				work((percent, finished) =>
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

		void CancelClick(object sender, RoutedEventArgs e) => worker.CancelAsync();

		public static void Run(Window owner, string title, Action<Func<int, bool, bool>> work) => new ProgressDialog(title, work) { Owner = owner }.ShowDialog();
	}
}
