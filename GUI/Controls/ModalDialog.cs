using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace NeoEdit.GUI.Controls
{
	public class ModalDialog : Window
	{
		bool result = false;
		Button CancelButton = null;

		public ModalDialog()
		{
			WindowStyle = WindowStyle.ToolWindow;
		}

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

				result = value;
				Close();
			}
		}

		public new bool ShowDialog()
		{
			if (Owner == null)
				return base.ShowDialog() == true;

			CancelButton = this.FindLogicalChildren<Button>().Where(button => button.IsCancel).FirstOrDefault();
			if (CancelButton != null)
				CancelButton.Click += (s, e) =>
				{
					if (e.Handled)
						return;
					Close();
					e.Handled = true;
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
				{
					CancelButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
					e.Handled = true;
				}
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
