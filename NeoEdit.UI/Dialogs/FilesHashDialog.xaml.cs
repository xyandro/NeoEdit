using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
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

		Configuration_Files_Hash result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Files_Hash { HashType = HashType, HMACKey = HMACKey };
			DialogResult = true;
		}

		public static Configuration_Files_Hash Run(Window parent)
		{
			var dialog = new FilesHashDialog() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
