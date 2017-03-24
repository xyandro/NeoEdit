using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;

namespace NeoEdit.StreamSave.Dialogs
{
	partial class GetURLsDialog
	{
		[DepProp]
		public string URLs { get { return UIHelper<GetURLsDialog>.GetPropValue<string>(this); } set { UIHelper<GetURLsDialog>.SetPropValue(this, value); } }

		static GetURLsDialog() { UIHelper<GetURLsDialog>.Register(); }

		GetURLsDialog()
		{
			InitializeComponent();
		}

		void OkClick(object sender, RoutedEventArgs e) => DialogResult = true;

		static public List<string> Run(Window parent)
		{
			var dialog = new GetURLsDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.URLs.Split('\r', '\n').NonNullOrEmpty().ToList() : null;
		}
	}
}
