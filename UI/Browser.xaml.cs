using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.Records;
using NeoEdit.Records.List;

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
				case Key.D1:
				case Key.D2:
				case Key.D3:
				case Key.D4:
				case Key.D5:
					{
						var list = Root.AllRoot.GetRecord("List " + e.Key.ToString().Substring(1)) as ListDir;
						switch (Keyboard.Modifiers & (ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift))
						{
							case ModifierKeys.Control:
								Directory = list;
								break;
							case ModifierKeys.Control | ModifierKeys.Shift:
								foreach (var record in Directory.Records)
									list.Add(record);
								break;
						}
					}
					break;
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

		void SetDirectory(string uri)
		{
			string select = null;
			var record = Root.AllRoot.GetRecord(uri);
			if (record is RecordItem)
			{
				select = record.FullName;
				record = record.Parent;
			}
			if (!(record is RecordList))
				return;
			Directory = record as RecordList;
			if (select != null)
			{
				var sel = Directory.Records.FirstOrDefault(a => a.FullName == select);
				if (sel != null)
				{
					Files.SelectedItem = sel;
					Files.ScrollIntoView(sel);
					(Files.ItemContainerGenerator.ContainerFromItem(sel) as ListViewItem).Focus();
				}
			}
		}

		void DirectoryDisplay_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Enter:
					SetDirectory(DirectoryDisplay.Text);
					break;
			}
		}

		void Files_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			ClickOnItem(Files.SelectedItem as Record);
		}
	}
}
