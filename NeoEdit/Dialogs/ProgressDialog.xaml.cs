using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class ProgressDialog
	{
		[DepProp]
		public string Text { get { return UIHelper<ProgressDialog>.GetPropValue<string>(this); } set { UIHelper<ProgressDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int? Percent { get { return UIHelper<ProgressDialog>.GetPropValue<int?>(this); } set { UIHelper<ProgressDialog>.SetPropValue(this, value); } }

		static ProgressDialog() { UIHelper<ProgressDialog>.Register(); }

		readonly BackgroundWorker worker;
		object result;
		ProgressDialog(string text, Func<Func<bool>, Action<int>, object> action)
		{
			InitializeComponent();
			Text = text;

			worker = new BackgroundWorker { WorkerSupportsCancellation = true, WorkerReportsProgress = true };
			worker.DoWork += (s, e) => e.Result = action(() => worker.CancellationPending, percent => worker.ReportProgress(percent));
			worker.ProgressChanged += (s, e) => Percent = e.ProgressPercentage;
			worker.RunWorkerCompleted += (s, e) =>
			{
				DialogResult = true;
				if (e.Error != null)
					throw e.Error;

				result = e.Result;
			};
			worker.RunWorkerAsync();
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnPreviewKeyDown(e);
			if (e.Key == Key.Escape)
				worker.CancelAsync();
		}

		void OnCancel(object sender, RoutedEventArgs e) => worker.CancelAsync();

		static public object Run(Window parent, string text, Func<Func<bool>, Action<int>, object> action)
		{
			var dialog = new ProgressDialog(text, action) { Owner = parent };
			dialog.ShowDialog();
			return dialog.result;
		}

		static public void Run(Window parent, string text, Action<Func<bool>, Action<int>> action) => Run(parent, text, (canceled, progress) => { action(canceled, progress); return null; });
	}
}
