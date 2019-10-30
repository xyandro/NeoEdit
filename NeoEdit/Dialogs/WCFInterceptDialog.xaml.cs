using System;
using System.Windows.Threading;

namespace NeoEdit.Program.Dialogs
{
	partial class WCFInterceptDialog
	{
		Action end;

		WCFInterceptDialog(Action<Action> start, Action end)
		{
			InitializeComponent();
			try
			{
				start(() => Dispatcher.BeginInvoke((Action)Close, DispatcherPriority.ApplicationIdle));
				this.end = end;
			}
			catch
			{
				Close();
				throw;
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			end?.Invoke();
		}

		public static void Run(Action<Action> start, Action end) => new WCFInterceptDialog(start, end).ShowDialog();
	}
}
