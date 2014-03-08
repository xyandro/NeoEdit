using System.Windows;
using NeoEdit.GUI.Common;
using NeoEdit.Records;

namespace NeoEdit.GUI.Browser.Dialogs
{
	public partial class Rename : Window
	{
		[DepProp]
		public string RecordName { get { return uiHelper.GetPropValue<string>(); } private set { uiHelper.SetPropValue(value); } }

		static Rename() { UIHelper<Rename>.Register(); }

		readonly UIHelper<Rename> uiHelper;
		public Rename(Record record)
		{
			uiHelper = new UIHelper<Rename>(this);
			InitializeComponent();

			RecordName = record.Name;
			var highlightEnd = (record[RecordProperty.PropertyName.NameWoExtension] as string).Length;

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
