using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using NeoEdit.Records;
using NeoEdit.Records.List;

namespace NeoEdit.UI.Windows
{
	public partial class Browser : Window
	{
		[DepProp]
		public RecordList Directory { get { return uiHelper.GetPropValue<RecordList>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public ObservableCollection<Property.PropertyType> Properties { get { return uiHelper.GetPropValue<ObservableCollection<Property.PropertyType>>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public Property.PropertyType SortProperty { get { return uiHelper.GetPropValue<Property.PropertyType>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool SortAscending { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }

		readonly UIHelper<Browser> uiHelper;

		public Browser() : this(GetCurrentDirectory()) { }
		public Browser(RecordList directory)
		{
			uiHelper = new UIHelper<Browser>(this);
			InitializeComponent();
			SetDirectory(directory);
			uiHelper.AddObservableCallback(a => a.Properties, () => BindingOperations.GetBindingExpression(columns, MenuItem.ItemsSourceProperty).UpdateTarget());
			Properties = new ObservableCollection<Property.PropertyType> { Property.PropertyType.Name, Property.PropertyType.Size, Property.PropertyType.WriteTime };
			Files.Focus();
		}

		static RecordList GetCurrentDirectory()
		{
			return Root.AllRoot.GetRecord(System.IO.Directory.GetCurrentDirectory()) as RecordList;
		}

		List<RecordList> previousDirectory = new List<RecordList>();
		List<RecordList> nextDirectory = new List<RecordList>();
		void SetDirectory(RecordList directory)
		{
			if (directory == Directory)
				return;

			if (Directory != null)
				previousDirectory.Add(Directory);
			nextDirectory.Clear();
			Directory = directory;
		}

		void SetPreviousDirectory()
		{
			if (previousDirectory.Count == 0)
				return;

			nextDirectory.Add(Directory);
			Directory = previousDirectory[previousDirectory.Count - 1];
			previousDirectory.RemoveAt(previousDirectory.Count - 1);
		}

		void SetNextDirectory()
		{
			if (nextDirectory.Count == 0)
				return;

			previousDirectory.Add(Directory);
			Directory = nextDirectory[nextDirectory.Count - 1];
			nextDirectory.RemoveAt(nextDirectory.Count - 1);
		}

		void ClickOnItem(Record item)
		{
			if (item == null)
				return;

			var recordList = item as RecordList;
			if (recordList != null)
				SetDirectory(recordList);
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
								SetDirectory(list);
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
					SetDirectory(Directory.Parent);
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
						SetDirectory(Directory.Parent);
					break;
				case Key.Left:
					if ((Keyboard.Modifiers & (ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift)) == ModifierKeys.Alt)
						SetPreviousDirectory();
					break;
				case Key.Right:
					if ((Keyboard.Modifiers & (ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift)) == ModifierKeys.Alt)
						SetNextDirectory();
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

			SetDirectory(record as RecordList);
			Files.Focus();

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

		private void MenuItemColumnClick(object sender, RoutedEventArgs e)
		{
			var header = ((MenuItem)sender).Header.ToString();
			var property = Property.PropertyFromMenuHeader(header);
			if (Properties.Contains(property))
				Properties.Remove(property);
			else
				Properties.Add(property);
		}

		private void MenuItemSortClick(object sender, RoutedEventArgs e)
		{
			var header = ((MenuItem)sender).Header.ToString();
			var property = Property.PropertyFromMenuHeader(header);
			if (SortProperty != property)
				SortProperty = property;
			else
				SortAscending = !SortAscending;
		}
	}
}
