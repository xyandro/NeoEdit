using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Rip.Dialogs
{
	partial class AddYouTubeDialog
	{
		[DepProp]
		public string URLs { get { return UIHelper<AddYouTubeDialog>.GetPropValue<string>(this); } set { UIHelper<AddYouTubeDialog>.SetPropValue(this, value); } }

		static AddYouTubeDialog() { UIHelper<AddYouTubeDialog>.Register(); }

		AddYouTubeDialog()
		{
			InitializeComponent();
		}

		List<string> result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = URLs.Split('\r', '\n').Select(str => str.Trim()).NonNullOrEmpty().ToList();
			DialogResult = true;
		}

		static public List<string> Run(Window parent)
		{
			var dialog = new AddYouTubeDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
