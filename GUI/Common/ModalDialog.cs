using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace NeoEdit.GUI.Common
{
	public class ModalDialog : Window
	{
		bool result = false;
		Button CancelButton = null;

		protected new bool DialogResult
		{
			get { return result; }
			set
			{
				if (Owner == null)
				{
					base.DialogResult = value;
					return;
				}

				this.result = value;
				Close();
			}
		}

		public new bool ShowDialog()
		{
			WindowStyle = WindowStyle.ToolWindow;

			if (Owner == null)
				return base.ShowDialog() == true;

			CancelButton = this.FindLogicalChildren<Button>().Where(button => button.IsCancel).FirstOrDefault();
			if (CancelButton != null)
				CancelButton.Click += (s, e) =>
				{
					if (e.Handled)
						return;
					Close();
				};

			var parentHwnd = new WindowInteropHelper(Owner).Handle;
			var frame = new DispatcherFrame();

			Closing += (s, e) =>
			{
				EnableWindow(parentHwnd, true);
				frame.Continue = false;
			};
			KeyDown += (s, e) =>
			{
				if (e.Key != Key.Escape)
					return;
				if (CancelButton == null)
					Close();
				else
					CancelButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
			};

			EnableWindow(parentHwnd, false);
			Show();
			Dispatcher.PushFrame(frame);
			return result;
		}

		[DllImport("user32.dll")]
		static extern bool EnableWindow(IntPtr hWnd, bool bEnable);
	}
}
