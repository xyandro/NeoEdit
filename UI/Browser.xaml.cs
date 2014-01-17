using NeoEdit.Records;
using System.Windows.Input;

namespace NeoEdit.UI
{
	public partial class Browser : UIWindow
	{
		[DepProp]
		public RecordList Directory { get { return GetProp<RecordList>(); } set { SetProp(value); } }

		static Browser()
		{
			Register<Browser>();
		}

		public Browser(RecordList directory)
		{
			InitializeComponent();
			Directory = directory;
			Files.Focus();
		}

		void ClickOnItem(Record item)
		{
			if (item == null)
				return;

			var recordList = item as RecordList;
			if (recordList != null)
				Directory = recordList;
		}

		void Window_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.F5:
					Directory.Refresh();
					break;
				case Key.Escape:
					Files.Focus();
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

		void Files_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Enter:
					ClickOnItem(Files.SelectedItem as Record);
					break;
			}
		}

		RecordList GetDirectory(string uri)
		{
			var record = Root.AllRoot.GetRecord(uri);
			if (record is RecordItem)
				record = record.Parent;
			if (record is RecordList)
				return record as RecordList;
			return Directory;
		}

		void DirectoryDisplay_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Enter:
					Directory = GetDirectory(DirectoryDisplay.Text);
					Files.Focus();
					break;
			}
		}

		void Files_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			ClickOnItem(Files.SelectedItem as Record);
		}
	}
}
