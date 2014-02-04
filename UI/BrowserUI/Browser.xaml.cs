using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.Records;
using NeoEdit.Records.Disk;
using NeoEdit.Records.List;
using NeoEdit.Records.Network;
using NeoEdit.Records.Registry;
using NeoEdit.UI.BinaryEditorUI;
using NeoEdit.UI.BrowserUI.Dialogs;
using NeoEdit.UI.Dialogs;
using NeoEdit.UI.Resources;

namespace NeoEdit.UI.BrowserUI
{
	public partial class Browser : Window
	{
		[DepProp]
		public Record Location
		{
			get { return uiHelper.GetPropValue<Record>(); }
			set
			{
				uiHelper.SetPropValue(value);
				var root = Location;
				while (true)
				{
					if (root is Root)
						break;
					if ((root is RecordRoot) && (root != lastRoot))
					{
						if ((root is DiskRoot) || (root is NetworkRoot))
							SetDiskView();
						if (root is ListRoot)
							SetListView();
						if (root is RegistryRoot)
							SetRegistryView();
						lastRoot = root as RecordRoot;
					}
					root = root.Parent;
				}
			}
		}
		[DepProp]
		public ObservableCollection<RecordProperty.PropertyName> Properties { get { return uiHelper.GetPropValue<ObservableCollection<RecordProperty.PropertyName>>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public RecordProperty.PropertyName SortProperty { get { return uiHelper.GetPropValue<RecordProperty.PropertyName>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool SortAscending { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }

		RecordRoot lastRoot;
		readonly UIHelper<Browser> uiHelper;

		public Browser() : this(StartLocation()) { }
		public Browser(string uri)
		{
			uiHelper = new UIHelper<Browser>(this);
			InitializeComponent();
			SetLocation(uri);
		}

		static string StartLocation()
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

			if (record.Records != null)
				SetLocation(record);
		}

		void Window_KeyDown(object sender, KeyEventArgs e)
		{
			var action = RecordAction.ActionFromAccessKey(e.Key, Keyboard.Modifiers);
			if (action.HasValue)
			{
				RunAction(action.Value);
				return;
			}

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
					files.Resort();
					break;
				case Key.Escape:
					uiHelper.InvalidBinding(locationDisplay, TextBox.TextProperty);
					files.Focus();
					break;
				case Key.Back: SetLocation(Location.Parent); break;
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
					if ((Keyboard.Modifiers & (ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift)) == ModifierKeys.None)
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
					var toFocus = files.ItemContainerGenerator.ContainerFromItem(sel) as ListViewItem;
					if (toFocus != null)
						toFocus.Focus();
				}
			}
		}

		void LocationDisplay_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Enter: SetLocation(locationDisplay.Text); break;
			}
		}

		void Files_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			ItemClicked(files.SelectedItem as Record);
		}

		Record SyncSource, SyncTarget;
		void RunAction(RecordAction.ActionName action)
		{
			var records = files.SelectedItems.Cast<Record>().ToList();
			records = records.Where(a => a.Actions.Any(b => b == action)).ToList();

			if (!RecordAction.Get(action).IsValid(records.Count, Clipboard.Current.Objects.Count > 0))
				return;

			try
			{
				switch (action)
				{
					case RecordAction.ActionName.Rename:
						{
							var record = records.Single();
							var rename = new Rename(record);
							if (rename.ShowDialog() == true)
								record.Rename(rename.RecordName, () => Message.Show("File already exists.  Overwrite?", "Warning", Message.Options.YesNo, Message.Options.Yes, Message.Options.No) == Message.Options.Yes);
						}
						break;
					case RecordAction.ActionName.Delete:
						{
							if (Message.Show("Are you sure you want to delete these items?", "Confirm", Message.Options.YesNo, Message.Options.Yes, Message.Options.No) == Message.Options.Yes)
							{
								foreach (var record in records)
									record.Delete();
							}
						}
						break;
					case RecordAction.ActionName.Copy:
						Clipboard.Current.Set(records, Clipboard.ClipboardType.Copy);
						break;
					case RecordAction.ActionName.Cut:
						Clipboard.Current.Set(records, Clipboard.ClipboardType.Cut);
						break;
					case RecordAction.ActionName.Paste:
						Location.Paste();
						break;
					case RecordAction.ActionName.MD5:
						{
							Exception error = null;
							foreach (var record in records)
							{
								try { record.CalcMD5(); }
								catch (Exception ex) { error = ex; }
							}
							if (error != null)
								throw error;

							if (!Properties.Contains(RecordProperty.PropertyName.MD5))
								Properties.Add(RecordProperty.PropertyName.MD5);
						}
						break;
					case RecordAction.ActionName.Identify:
						{
							Exception error = null;
							foreach (var record in records)
							{
								try { record.Identify(); }
								catch (Exception ex) { error = ex; }
							}
							if (error != null)
								throw error;

							if (!Properties.Contains(RecordProperty.PropertyName.Identify))
								Properties.Add(RecordProperty.PropertyName.Identify);
						}
						break;
					case RecordAction.ActionName.SyncSource:
						SyncSource = records.Single();
						break;
					case RecordAction.ActionName.SyncTarget:
						SyncTarget = records.Single();
						break;
					case RecordAction.ActionName.Sync:
						if ((SyncSource != null) && (SyncTarget != null))
							SyncTarget.Sync(SyncSource);
						break;
					case RecordAction.ActionName.Open:
						{
							var data = records.Single().Read();
							new BinaryEditor(data);
						}
						break;
				}
			}
			catch (Exception ex) { MessageBox.Show(ex.Message, "Error"); }
		}

		public void MenuItemColumnClick(object sender, RoutedEventArgs e)
		{
			var header = ((MenuItem)sender).Header.ToString();
			var property = RecordProperty.PropertyFromMenuHeader(header);
			if (Properties.Contains(property))
				Properties.Remove(property);
			else
				Properties.Add(property);
		}

		public void MenuItemSortClick(object sender, RoutedEventArgs e)
		{
			var header = ((MenuItem)sender).Header.ToString();
			var property = RecordProperty.PropertyFromMenuHeader(header);
			if (SortProperty != property)
				SortProperty = property;
			else
				SortAscending = !SortAscending;
		}

		public void MenuItemActionClick(object sender, RoutedEventArgs e)
		{
			var header = ((MenuItem)sender).Header.ToString();
			var action = RecordAction.ActionFromMenuHeader(header);
			RunAction(action);
		}

		void SetDiskView()
		{
			Properties = new ObservableCollection<RecordProperty.PropertyName> { RecordProperty.PropertyName.Name, RecordProperty.PropertyName.Size, RecordProperty.PropertyName.WriteTime };
			SortProperty = RecordProperty.PropertyName.Name;
			SortAscending = RecordProperty.Get(RecordProperty.PropertyName.Name).DefaultAscending;
		}

		void SetListView()
		{
			Properties = new ObservableCollection<RecordProperty.PropertyName> { RecordProperty.PropertyName.Name, RecordProperty.PropertyName.Size, RecordProperty.PropertyName.WriteTime, RecordProperty.PropertyName.Path };
			SortProperty = RecordProperty.PropertyName.Name;
			SortAscending = RecordProperty.Get(RecordProperty.PropertyName.Name).DefaultAscending;
		}

		void SetRegistryView()
		{
			Properties = new ObservableCollection<RecordProperty.PropertyName> { RecordProperty.PropertyName.Name, RecordProperty.PropertyName.Type, RecordProperty.PropertyName.Data };
			SortProperty = RecordProperty.PropertyName.Name;
			SortAscending = RecordProperty.Get(RecordProperty.PropertyName.Name).DefaultAscending;
		}

		private void MenuItemViewClick(object sender, RoutedEventArgs e)
		{
			switch (((MenuItem)sender).Header.ToString())
			{
				case "_Files": SetDiskView(); break;
				case "_List": SetListView(); break;
				case "_Registry": SetRegistryView(); break;
			}
		}
	}
}
