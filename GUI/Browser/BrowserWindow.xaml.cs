using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.GUI.BinaryEditor;
using NeoEdit.GUI.Browser.Dialogs;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;
using NeoEdit.Records;
using NeoEdit.Records.Disks;
using NeoEdit.Records.Lists;
using NeoEdit.Records.Processes;
using NeoEdit.Records.Registries;

namespace NeoEdit.GUI.Browser
{
	public partial class BrowserWindow : Window
	{
		Type lastRootType;
		[DepProp]
		public Record Location
		{
			get { return uiHelper.GetPropValue<Record>(); }
			set
			{
				uiHelper.SetPropValue(value);

				if (value.GetRootType() != lastRootType)
				{
					lastRootType = value.GetRootType();
					if (lastRootType == typeof(DiskRecord))
						SetDiskView();
					if (lastRootType == typeof(ListRecord))
						SetListView();
					if (lastRootType == typeof(RegistryRecord))
						SetRegistryView();
					if (lastRootType == typeof(ProcessRecord))
						SetProcessView();
				}

				Refresh();
			}
		}
		[DepProp]
		List<GUIRecord> Records { get { return uiHelper.GetPropValue<List<GUIRecord>>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		ObservableCollection<RecordProperty.PropertyName> Properties { get { return uiHelper.GetPropValue<ObservableCollection<RecordProperty.PropertyName>>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public RecordProperty.PropertyName SortProperty { get { return uiHelper.GetPropValue<RecordProperty.PropertyName>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool SortAscending { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }

		static BrowserWindow() { UIHelper<BrowserWindow>.Register(); }

		readonly UIHelper<BrowserWindow> uiHelper;
		public BrowserWindow() : this(StartLocation()) { }
		public BrowserWindow(string uri)
		{
			uiHelper = new UIHelper<BrowserWindow>(this);
			InitializeComponent();

			locationDisplay.LostFocus += (s, e) => uiHelper.InvalidateBinding(locationDisplay, TextBox.TextProperty);
			uiHelper.AddCallback(a => a.SortProperty, (o, n) => SortAscending = RecordProperty.Get(SortProperty).DefaultAscending);

			Loaded += (s, e) => SetLocation(uri);
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

		void Refresh()
		{
			var selected = files.SelectedItems.Cast<GUIRecord>().Select(guiRecord => guiRecord.record.FullName).ToList();
			Records = Location.Records.Select(record => new GUIRecord(record)).ToList();
			Records.Where(guiRecord => selected.Contains(guiRecord.record.FullName)).ToList().ForEach(record => files.SelectedItems.Add(record));
		}

		bool altOnly { get { return (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)) == ModifierKeys.Alt; } }

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
						var list = new ListRoot()[e.Key - Key.D1 + 1];
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
					Refresh();
					break;
				case Key.Escape:
					files.Focus();
					break;
				case Key.Back: SetLocation(Location.Parent); break;
				case Key.System:
					switch (e.SystemKey)
					{
						case Key.D:
							if (altOnly)
							{
								locationDisplay.SelectAll();
								locationDisplay.Focus();
							}
							break;
						case Key.Up:
							if (altOnly)
								SetLocation(Location.Parent);
							break;
						case Key.Left:
							if (altOnly)
								SetPreviousLocation();
							break;
						case Key.Right:
							if (altOnly)
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
						ItemClicked((files.SelectedItem as GUIRecord).record);
					break;
			}
		}

		void SetLocation(string uri)
		{
			string select = null;
			var record = new Root().GetRecord(uri);
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
				var sel = Records.FirstOrDefault(guiRecord => guiRecord.record.FullName == select);
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
			ItemClicked((files.SelectedItem as GUIRecord).record);
		}

		Record syncSource, syncTarget;
		SyncParams syncParams = new SyncParams();
		void RunAction(RecordAction.ActionName action)
		{
			var records = files.SelectedItems.Cast<GUIRecord>().Select(guiRecord => guiRecord.record).ToList();
			records = records.Where(a => a.Actions.Any(b => b == action)).ToList();

			if (!RecordAction.Get(action).IsValid(records.Count))
				return;

			switch (action)
			{
				case RecordAction.ActionName.Rename:
					{
						var record = records.Single();
						var rename = new Rename(record);
						if (rename.ShowDialog() == true)
						{
							var existingRecord = new Root().GetRecord(Path.Combine((string)record[RecordProperty.PropertyName.Path], rename.RecordName));
							if (existingRecord != null)
							{
								if (new Message
								{
									Title = "Warning",
									Text = "File already exists.  Overwrite?",
									Options = Message.OptionsEnum.YesNo,
									DefaultAccept = Message.OptionsEnum.Yes,
									DefaultCancel = Message.OptionsEnum.No,
								}.Show() != Message.OptionsEnum.Yes)
									break;
								existingRecord.Delete();
							}
							Location.MoveFrom(record, rename.RecordName);
						}
						Refresh();
					}
					break;
				case RecordAction.ActionName.Delete:
					{
						if (new Message
						{
							Title = "Confirm",
							Text = "Are you sure you want to delete these items?",
							Options = Message.OptionsEnum.YesNo,
							DefaultAccept = Message.OptionsEnum.Yes,
							DefaultCancel = Message.OptionsEnum.No,
						}.Show() == Message.OptionsEnum.Yes)
						{
							foreach (var record in records)
								record.Delete();
						}
						Refresh();
					}
					break;
				case RecordAction.ActionName.Suspend:
					foreach (var record in records)
						record.Suspend();
					break;
				case RecordAction.ActionName.Resume:
					foreach (var record in records)
						record.Resume();
					break;
				case RecordAction.ActionName.Copy:
					ClipboardWindow.Set(records, false);
					break;
				case RecordAction.ActionName.Cut:
					ClipboardWindow.Set(records, true);
					break;
				case RecordAction.ActionName.Paste:
					{
						bool isCut;
						if (!ClipboardWindow.GetRecords(out records, out isCut))
							break;

						var locationFiles = Location.Records.Select(record => record.Name).ToList();
						var paths = records.Select(record => record[RecordProperty.PropertyName.Path] as string).GroupBy(path => path).Select(path => path.Key).ToList();
						var canRename = (paths.Count == 1) && (paths[0] == Location.FullName);
						if ((isCut) || (!canRename))
						{
							var names = records.Select(record => record.Name).ToList();
							var exists = locationFiles.Any(name => names.Contains(name));
							if (exists)
								throw new Exception("Destination already exists.");
						}

						foreach (var child in records)
						{
							if (isCut)
							{
								Location.MoveFrom(child);
								continue;
							}

							var name = child[RecordProperty.PropertyName.NameWoExtension] as string;
							var ext = child[RecordProperty.PropertyName.Extension] as string;
							string newName;
							for (var num = 1; ; ++num)
							{
								var extra = num == 1 ? "" : String.Format(" ({0})", num);
								newName = name + extra + ext;
								if (locationFiles.Contains(newName))
									continue;
								break;
							}

							Location.CopyFrom(child, newName);
						}

						Refresh();
					}
					break;
				case RecordAction.ActionName.MD5:
					{
						foreach (var record in records)
							record.CalcMD5();

						if (!Properties.Contains(RecordProperty.PropertyName.MD5))
							Properties.Add(RecordProperty.PropertyName.MD5);
					}
					break;
				case RecordAction.ActionName.Identify:
					{
						foreach (var record in records)
							record.Identify();

						if (!Properties.Contains(RecordProperty.PropertyName.Identify))
							Properties.Add(RecordProperty.PropertyName.Identify);
					}
					break;
				case RecordAction.ActionName.SyncSource:
					syncSource = records.Single();
					break;
				case RecordAction.ActionName.SyncTarget:
					syncTarget = records.Single();
					break;
				case RecordAction.ActionName.Sync:
					if ((syncSource == null) || (syncTarget == null))
						break;

					if (new EditSyncParams(syncParams).ShowDialog() != true)
						break;

					var log = new Log();
					syncTarget.SyncFrom(syncSource, syncParams, msg => log.AddMessage(msg));
					Refresh();
					break;
				case RecordAction.ActionName.Open:
					new BinaryEditorWindow(records.Single());
					break;
				case RecordAction.ActionName.View:
					foreach (var record in records)
						new ViewImage { FileName = record.FullName };
					break;
			}
		}

		internal void MenuItemColumnClick(object sender, RoutedEventArgs e)
		{
			var header = ((MenuItem)sender).Header.ToString();
			var property = RecordProperty.PropertyFromMenuHeader(header);
			if (Properties.Contains(property))
				Properties.Remove(property);
			else
				Properties.Add(property);
		}

		internal void MenuItemSortClick(object sender, RoutedEventArgs e)
		{
			var header = ((MenuItem)sender).Header.ToString();
			var property = RecordProperty.PropertyFromMenuHeader(header);
			if (SortProperty != property)
				SortProperty = property;
			else
				SortAscending = !SortAscending;
		}

		internal void MenuItemActionClick(object sender, RoutedEventArgs e)
		{
			var header = ((MenuItem)sender).Header.ToString();
			var action = RecordAction.ActionFromMenuHeader(header);
			RunAction(action);
		}

		void SetDiskView()
		{
			Properties = new ObservableCollection<RecordProperty.PropertyName> { RecordProperty.PropertyName.Name, RecordProperty.PropertyName.Size, RecordProperty.PropertyName.WriteTime };
			SortProperty = RecordProperty.PropertyName.Name;
		}

		void SetListView()
		{
			Properties = new ObservableCollection<RecordProperty.PropertyName> { RecordProperty.PropertyName.Name, RecordProperty.PropertyName.Size, RecordProperty.PropertyName.WriteTime, RecordProperty.PropertyName.Path };
			SortProperty = RecordProperty.PropertyName.Name;
		}

		void SetRegistryView()
		{
			Properties = new ObservableCollection<RecordProperty.PropertyName> { RecordProperty.PropertyName.Name, RecordProperty.PropertyName.Type, RecordProperty.PropertyName.Data };
			SortProperty = RecordProperty.PropertyName.Name;
		}

		void SetProcessView()
		{
			Properties = new ObservableCollection<RecordProperty.PropertyName> { RecordProperty.PropertyName.ID, RecordProperty.PropertyName.Name, RecordProperty.PropertyName.CPU, RecordProperty.PropertyName.Size, RecordProperty.PropertyName.Data };
			SortProperty = RecordProperty.PropertyName.Name;
		}

		void MenuItemViewClick(object sender, RoutedEventArgs e)
		{
			switch (((MenuItem)sender).Header.ToString())
			{
				case "_Files": SetLocation(new DiskRoot()); break;
				case "_List": SetLocation(new ListRoot()); break;
				case "_Registry": SetLocation(new RegistryRoot()); break;
				case "_Processes": SetLocation(new ProcessRoot()); break;
			}
		}
	}
}
