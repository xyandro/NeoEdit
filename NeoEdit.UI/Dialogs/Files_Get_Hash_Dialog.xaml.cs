using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Files_Get_Hash_Dialog
	{
		[DepProp]
		Hasher.Type HashType { get { return UIHelper<Files_Get_Hash_Dialog>.GetPropValue<Hasher.Type>(this); } set { UIHelper<Files_Get_Hash_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		byte[] HMACKey { get { return UIHelper<Files_Get_Hash_Dialog>.GetPropValue<byte[]>(this); } set { UIHelper<Files_Get_Hash_Dialog>.SetPropValue(this, value); } }

		static Files_Get_Hash_Dialog() { UIHelper<Files_Get_Hash_Dialog>.Register(); }

		Files_Get_Hash_Dialog()
		{
			InitializeComponent();

			hashType.ItemsSource = Enum.GetValues(typeof(Hasher.Type)).Cast<Hasher.Type>().Where(type => type != Hasher.Type.None).ToList();
			HashType = Hasher.Type.SHA1;
		}

		Configuration_Files_Get_Hash result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Files_Get_Hash { HashType = HashType, HMACKey = HMACKey };
			DialogResult = true;
		}

		public static Configuration_Files_Get_Hash Run(Window parent)
		{
			var dialog = new Files_Get_Hash_Dialog() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
