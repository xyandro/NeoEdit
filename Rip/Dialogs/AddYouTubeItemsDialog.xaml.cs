using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Rip.Dialogs
{
	partial class AddYouTubeItemsDialog
	{
		[DepProp]
		string URLs { get { return UIHelper<AddYouTubeItemsDialog>.GetPropValue<string>(this); } set { UIHelper<AddYouTubeItemsDialog>.SetPropValue(this, value); } }

		static AddYouTubeItemsDialog() { UIHelper<AddYouTubeItemsDialog>.Register(); }

		AddYouTubeItemsDialog() { InitializeComponent(); }

		List<string> result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = URLs.Split('\r', '\n').Select(str => str.Trim()).NonNullOrEmpty().ToList();
			DialogResult = true;
		}

		static public List<string> Run(Window parent)
		{
			var dialog = new AddYouTubeItemsDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
