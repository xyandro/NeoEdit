using System.IO;
using System.Windows;
using NeoEdit.Records;
using NeoEdit.Records.Disk;

namespace NeoEdit.UI.Dialogs
{
	/// <summary>
	/// Interaction logic for Rename.xaml
	/// </summary>
	public partial class Rename : Window
	{
		[DepProp]
		public string RecordName { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }

		readonly UIHelper<Rename> uiHelper;
		public Rename(Record record)
		{
			uiHelper = new UIHelper<Rename>(this);
			InitializeComponent();

			RecordName = record.Name;
			var highlightEnd = RecordName.Length;
			if (record is DiskRecord)
				highlightEnd -= Path.GetExtension(record.FullName).Length;

			name.Focus();
			name.CaretIndex = highlightEnd;
			name.Select(0, highlightEnd);
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}
	}
}
