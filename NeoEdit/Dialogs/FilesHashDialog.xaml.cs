using System;
using System.Linq;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Models;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;

namespace NeoEdit.Program.Dialogs
{
	partial class FilesHashDialog
	{
		[DepProp]
		Hasher.Type HashType { get { return UIHelper<FilesHashDialog>.GetPropValue<Hasher.Type>(this); } set { UIHelper<FilesHashDialog>.SetPropValue(this, value); } }
		[DepProp]
		byte[] HMACKey { get { return UIHelper<FilesHashDialog>.GetPropValue<byte[]>(this); } set { UIHelper<FilesHashDialog>.SetPropValue(this, value); } }

		static FilesHashDialog() { UIHelper<FilesHashDialog>.Register(); }

		FilesHashDialog()
		{
			InitializeComponent();

			hashType.ItemsSource = Enum.GetValues(typeof(Hasher.Type)).Cast<Hasher.Type>().Where(type => type != Hasher.Type.None).ToList();
			HashType = Hasher.Type.SHA1;
		}

		FilesHashDialogResult result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new FilesHashDialogResult { HashType = HashType, HMACKey = HMACKey };
			DialogResult = true;
		}

		public static FilesHashDialogResult Run(Window parent)
		{
			var dialog = new FilesHashDialog() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
