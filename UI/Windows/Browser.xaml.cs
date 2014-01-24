using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.Records;
using NeoEdit.Records.List;

namespace NeoEdit.UI.Windows
{
	public partial class Browser : Window
	{
		[DepProp]
		public Record Location { get { return uiHelper.GetPropValue<Record>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public ObservableCollection<Property.PropertyType> Properties { get { return uiHelper.GetPropValue<ObservableCollection<Property.PropertyType>>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public Property.PropertyType SortProperty { get { return uiHelper.GetPropValue<Property.PropertyType>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool SortAscending { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }

		readonly UIHelper<Browser> uiHelper;

		public Browser() : this(GetCurrentLocation()) { }
		public Browser(string uri)
		{
			uiHelper = new UIHelper<Browser>(this);
			InitializeComponent();
			SetLocation(uri);
			uiHelper.AddObservableCallback(a => a.Properties, () => uiHelper.InvalidBinding(columns, MenuItem.ItemsSourceProperty));
			Properties = new ObservableCollection<Property.PropertyType> { Property.PropertyType.Name, Property.PropertyType.Size, Property.PropertyType.WriteTime };
		}

		static string GetCurrentLocation()
		{
			return System.IO.Directory.GetCurrentDirectory();
		}

		List<Record> previousLocation = new List<Record>();
		List<Record> nextLocation = new List<Record>();
		void SetLocation(Record location)
		{
			if (location == Location)
				return;

			if (Location != null)
				previousLocation.Add(Location);
			nextLocation.Clear();
			Location = location;
		}

		void SetPreviousLocation()
		{
			if (previousLocation.Count == 0)
				return;

			nextLocation.Add(Location);
			Location = previousLocation[previousLocation.Count - 1];
			previousLocation.RemoveAt(previousLocation.Count - 1);
		}

		void SetNextLocation()
		{
			if (nextLocation.Count == 0)
				return;

			previousLocation.Add(Location);
			Location = nextLocation[nextLocation.Count - 1];
			nextLocation.RemoveAt(nextLocation.Count - 1);
		}

		void ItemClicked(Record record)
		{
			if (record == null)
				return;

			if (!record.IsFile)
				SetLocation(record);
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
								SetLocation(list);
								break;
							case ModifierKeys.Control | ModifierKeys.Shift:
								foreach (var record in Location.Records)
									list.Add(record);
								break;
						}
					}
					break;
				case Key.F5:
					Location.Refresh();
					break;
				case Key.Escape:
					uiHelper.InvalidBinding(locationDisplay, TextBox.TextProperty);
					files.Focus();
					break;
				case Key.Back:
					SetLocation(Location.Parent);
					break;
				case Key.System:
					switch (e.SystemKey)
					{
						case Key.D:
							if ((Keyboard.Modifiers & (ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift)) == ModifierKeys.Alt)
							{
								locationDisplay.SelectAll();
								locationDisplay.Focus();
							}
							break;
						case Key.Up:
							if ((Keyboard.Modifiers & (ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift)) == ModifierKeys.Alt)
								SetLocation(Location.Parent);
							break;
						case Key.Left:
							if ((Keyboard.Modifiers & (ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift)) == ModifierKeys.Alt)
								SetPreviousLocation();
							break;
						case Key.Right:
							if ((Keyboard.Modifiers & (ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift)) == ModifierKeys.Alt)
								SetNextLocation();
							break;
					}
					break;
			}
		}

		void Files_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Enter:
					ItemClicked(files.SelectedItem as Record);
					break;
			}
		}

		void SetLocation(string uri)
		{
			string select = null;
			var record = Root.AllRoot.GetRecord(uri);
			if (record == null)
				return;
			if (record.IsFile)
			{
				select = record.FullName;
				record = record.Parent;
			}

			SetLocation(record);
			files.Focus();

			if (select != null)
			{
				var sel = Location.Records.FirstOrDefault(a => a.FullName == select);
				if (sel != null)
				{
					files.SelectedItem = sel;
					files.ScrollIntoView(sel);
					(files.ItemContainerGenerator.ContainerFromItem(sel) as ListViewItem).Focus();
				}
			}
		}

		void LocationDisplay_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Enter:
					SetLocation(locationDisplay.Text);
					break;
			}
		}

		void Files_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			ItemClicked(files.SelectedItem as Record);
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
