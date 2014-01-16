using NeoEdit.Records;
using System.Windows.Input;

namespace NeoEdit.UI
{
	public partial class Browser : UIWindow
	{
		[DepProp]
		public IRecordList Directory { get { return GetProp<IRecordList>(); } set { SetProp(value); } }

		static Browser()
		{
			Register<Browser>();
		}

		public Browser(string recordUri)
		{
			InitializeComponent();
			Directory = RecordListProvider.GetRecordList(recordUri);
			Files.Focus();
		}

		void ClickOnItem(IRecord item)
		{
			if (item == null)
				return;

			var recordList = item as IRecordList;
			if (recordList != null)
				Directory = recordList;
		}

		void Files_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Enter:
					ClickOnItem(Files.SelectedItem as IRecord);
					break;
				case Key.Back:
					Directory = Directory.Parent;
					break;
			}
			switch (e.SystemKey)
			{
				case Key.D:
					if ((Keyboard.Modifiers & (ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift)) == ModifierKeys.Alt)
					{
						DirectoryDisplay.SelectAll();
						DirectoryDisplay.Focus();
					}
					break;
				case Key.Up:
					if ((Keyboard.Modifiers & (ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift)) == ModifierKeys.Alt)
						Directory = Directory.Parent;
					break;
			}
		}

		void DirectoryDisplay_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Escape:
					Files.Focus();
					break;
				case Key.Enter:
					Directory = RecordListProvider.GetRecordList(DirectoryDisplay.Text, Directory);
					Files.Focus();
					break;
			}
		}

		void Files_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			ClickOnItem(Files.SelectedItem as IRecord);
		}
	}
}
