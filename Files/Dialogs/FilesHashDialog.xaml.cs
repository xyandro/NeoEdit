using System;
using System.Linq;
using System.Windows;
using NeoEdit.Transform;
using NeoEdit.Controls;

namespace NeoEdit.Dialogs
{
	partial class FilesHashDialog
	{
		public class Result
		{
			public Hasher.Type HashType { get; set; }
			public byte[] HMACKey { get; set; }
		}

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

		Result result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { HashType = HashType, HMACKey = HMACKey };
			DialogResult = true;
		}

		public static Result Run(Window parent)
		{
			var dialog = new FilesHashDialog() { Owner = parent };
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
