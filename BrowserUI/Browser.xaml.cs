﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.BinaryEditorUI;
using NeoEdit.BrowserUI.Dialogs;
using NeoEdit.Common;
using NeoEdit.Dialogs;
using NeoEdit.Records;
using NeoEdit.Records.Disk;
using NeoEdit.Records.List;
using NeoEdit.Records.Registry;

namespace NeoEdit.BrowserUI
{
	public partial class Browser : Window
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
				}

				Refresh();
			}
		}
		[DepProp]
		public List<Record> Records { get { return uiHelper.GetPropValue<List<Record>>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public ObservableCollection<RecordProperty.PropertyName> Properties { get { return uiHelper.GetPropValue<ObservableCollection<RecordProperty.PropertyName>>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public RecordProperty.PropertyName SortProperty { get { return uiHelper.GetPropValue<RecordProperty.PropertyName>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool SortAscending { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }

		static Browser() { UIHelper<Browser>.Register(); }

		readonly UIHelper<Browser> uiHelper;
		public Browser() : this(StartLocation()) { }
		public Browser(string uri)
		{
			uiHelper = new UIHelper<Browser>(this);
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
			Records = Location.Records.ToList();
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
						ItemClicked(files.SelectedItem as Record);
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
									DefaultYes = Message.OptionsEnum.Yes,
									DefaultNo = Message.OptionsEnum.No,
								}.Show() != Message.OptionsEnum.Yes)
									break;
								existingRecord.Delete();
							}
							record.Rename(rename.RecordName);
						}
					}
					break;
				case RecordAction.ActionName.Delete:
					{
						if (new Message
						{
							Title = "Confirm",
							Text = "Are you sure you want to delete these items?",
							Options = Message.OptionsEnum.YesNo,
							DefaultYes = Message.OptionsEnum.Yes,
							DefaultNo = Message.OptionsEnum.No,
						}.Show() == Message.OptionsEnum.Yes)
						{
							foreach (var record in records)
								record.Delete();
						}
					}
					break;
				case RecordAction.ActionName.Copy:
					Clipboard.Current.Set(records, false);
					break;
				case RecordAction.ActionName.Cut:
					Clipboard.Current.Set(records, true);
					break;
				case RecordAction.ActionName.Paste:
					{
						bool isCut;
						if (!Clipboard.Current.GetRecords(out records, out isCut))
							break;

						if (isCut)
						{
							var names = records.Select(record => record.Name).ToList();
							var exists = Location.Records.Any(record => names.Contains(record.Name));
							if (exists)
								throw new Exception("Destination already exists.");
						}

						foreach (var child in records)
						{
							if (isCut)
							{
								child.Move(Location);
							}

							//var name = child[RecordProperty.PropertyName.NameWoExtension] as string;
							//var ext = child[RecordProperty.PropertyName.Extension] as string;
							//string newName;
							//for (var num = 1; ; ++num)
							//{
							//	var extra = num == 1 ? "" : String.Format(" ({0})", num);
							//	newName = Path.Combine(child.FullName, name + extra + ext);
							//	if ((File.Exists(newName)) || (Directory.Exists(newName)))
							//	{
							//		if (isCut)
							//			throw new Exception("Destination already exists.");
							//		continue;
							//	}
							//	break;
							//}

							//if (isCut)
							//{
							//	if (child is DiskFile)
							//		File.Move(child.FullName, newName);
							//	else if (child is DiskDir)
							//		Directory.Move(child.FullName, newName);
							//}
							//else
							//{
							//	if (child is DiskFile)
							//		File.Copy(child.FullName, newName);
							//	else if (child is DiskDir)
							//		CopyDir(child.FullName, newName);
							//}
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

		void MenuItemViewClick(object sender, RoutedEventArgs e)
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
