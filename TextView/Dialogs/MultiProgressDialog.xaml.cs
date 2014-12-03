using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextView.Dialogs
{
	partial class MultiProgressDialog
	{
		public delegate void ProgressDelegate(int num, long done, long total = -1);
		public delegate bool CancelDelegate(bool forceCancel = false);

		[DepProp]
		public string Status { get { return UIHelper<MultiProgressDialog>.GetPropValue<string>(this); } set { UIHelper<MultiProgressDialog>.SetPropValue(this, value); } }

		static MultiProgressDialog() { UIHelper<MultiProgressDialog>.Register(); }

		readonly BackgroundWorker worker;
		readonly List<MultiProgressDialogProgress> progressBars = new List<MultiProgressDialogProgress>();
		readonly List<int> progress;
		bool canClose = false;
		MultiProgressDialog(string status, List<string> names, Action<ProgressDelegate, CancelDelegate> work, Action finished = null)
		{
			InitializeComponent();
			Status = status;
			progress = names.Select(name => 0).ToList();
			SetupGrid(names);

			worker = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
			worker.DoWork += (s, e) =>
			{
				work((child, done, total) => SetProgress(child, done, total), forceCancel =>
				{
					if (forceCancel)
						worker.CancelAsync();
					if (worker.CancellationPending)
						e.Cancel = true;
					return e.Cancel;
				});
			};
			worker.ProgressChanged += (s, e) =>
			{
				for (var ctr = 0; ctr < progressBars.Count; ++ctr)
					progressBars[ctr].Progress = progress[ctr];
			};
			worker.RunWorkerCompleted += (s, e) =>
			{
				canClose = true;
				Close();
				if (e.Error != null)
					throw new Exception(String.Format("Background task failed: ", e.Error.Message), e.Error);
				if ((!e.Cancelled) && (finished != null))
					finished();
			};
			worker.RunWorkerAsync();
		}

		void SetProgress(int child, long done, long total)
		{
			if (total != -1)
			{
				if (total == 0)
					done = 0;
				else
					done = done * 100 / total;
			}
			progress[child] = (int)done;
			worker.ReportProgress(0);
		}

		void SetupGrid(List<string> names)
		{
			var count = names.Count;
			childrenProgress.Columns = Math.Max(1, Math.Min((int)Math.Ceiling(Math.Sqrt(count - 1)), 8));

			for (var ctr = 0; ctr < count; ++ctr)
			{
				var progressBar = new MultiProgressDialogProgress(names[ctr]);
				progressBars.Add(progressBar);

				if (ctr == 0)
					parentProgress.Children.Add(progressBar);
				else
					childrenProgress.Children.Add(progressBar);
			}
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
			e.Cancel = !canClose;
		}

		private void CancelClick(object sender, RoutedEventArgs e)
		{
			worker.CancelAsync();
		}

		public static void Run(string status, List<string> names, Action<ProgressDelegate, CancelDelegate> work, Action finished = null)
		{
			new MultiProgressDialog(status, names, work, finished).Show();
		}
	}
}
