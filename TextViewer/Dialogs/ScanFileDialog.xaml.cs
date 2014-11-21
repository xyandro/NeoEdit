using System;
using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextView.Dialogs
{
	partial class ScanFileDialog
	{
		[DepProp]
		public string FileName { get { return UIHelper<ScanFileDialog>.GetPropValue<string>(this); } set { UIHelper<ScanFileDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int Progress { get { return UIHelper<ScanFileDialog>.GetPropValue<int>(this); } set { UIHelper<ScanFileDialog>.SetPropValue(this, value); } }

		static ScanFileDialog() { UIHelper<ScanFileDialog>.Register(); }

		readonly Action cancel;
		bool canClose = false;
		public ScanFileDialog(string fileName, Action _cancel)
		{
			InitializeComponent();
			cancel = _cancel;
			FileName = fileName;
		}

		public new void Close()
		{
			canClose = true;
			base.Close();
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			base.OnClosing(e);
			e.Cancel = !canClose;
		}

		public void SetProgress(int progress)
		{
			Progress = progress;
		}

		private void CancelClick(object sender, RoutedEventArgs e)
		{
			cancel();
		}
	}
}
