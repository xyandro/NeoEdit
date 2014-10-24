using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using NeoEdit.Disk.Dialogs;
using NeoEdit.GUI;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;
using NeoEdit.GUI.ItemGridControl;

namespace NeoEdit.Disk
{
	class DiskItemGrid : ItemGrid<DiskItem> { }

	class FilesLocation
	{
		public readonly DiskItem item;
		public readonly bool recursive;

		public FilesLocation(DiskItem item, bool recursive = false)
		{
			this.item = item;
			this.recursive = recursive;
		}

		public override string ToString()
		{
			return item.ToString();
		}
	}

	partial class DiskWindow
	{
		[DepProp]
		FilesLocation Location { get { return UIHelper<DiskWindow>.GetPropValue<FilesLocation>(this); } set { UIHelper<DiskWindow>.SetPropValue(this, value); } }
		[DepProp]
		ObservableCollection<DiskItem> Files { get { return UIHelper<DiskWindow>.GetPropValue<ObservableCollection<DiskItem>>(this); } set { UIHelper<DiskWindow>.SetPropValue(this, value); } }
		[DepProp]
		ObservableCollection<DiskItem> Selected { get { return UIHelper<DiskWindow>.GetPropValue<ObservableCollection<DiskItem>>(this); } set { UIHelper<DiskWindow>.SetPropValue(this, value); } }
		[DepProp]
		ObservableCollection<ItemGridColumn> Columns { get { return UIHelper<DiskWindow>.GetPropValue<ObservableCollection<ItemGridColumn>>(this); } set { UIHelper<DiskWindow>.SetPropValue(this, value); } }
		[DepProp]
		int ColumnsChangeCount { get { return UIHelper<DiskWindow>.GetPropValue<int>(this); } set { UIHelper<DiskWindow>.SetPropValue(this, value); } }

		Stack<FilesLocation> lastLocation = new Stack<FilesLocation>();
		Stack<FilesLocation> nextLocation = new Stack<FilesLocation>();

		static DiskWindow()
		{
			UIHelper<DiskWindow>.Register();
			UIHelper<DiskWindow>.AddCallback(a => a.Location, (s, o, n) => s.LocationChanged(o));
			UIHelper<DiskWindow>.AddObservableCallback(a => a.Columns, (obj, s, e) => ++obj.ColumnsChangeCount);
		}

		public DiskWindow(string path = null)
		{
			if (String.IsNullOrEmpty(path))
				path = Directory.GetCurrentDirectory();

			InitializeComponent();
			location.GotFocus += (s, e) => location.SelectAll();
			location.LostFocus += (s, e) => UIHelper<DiskWindow>.InvalidateBinding(location, TextBox.TextProperty);
			location.PreviewKeyDown += LocationKeyDown;
			files.Accept += (s, e) => OnAccept();

			Files = new ObservableCollection<DiskItem>();
			Selected = new ObservableCollection<DiskItem>();
			Columns = new ObservableCollection<ItemGridColumn>();

			ShowColumn(a => a.Name);
			ShowColumn(a => a.Size);
			ShowColumn(a => a.WriteTime);
			SetLocation(path ?? "");

			Loaded += (s, e) => files.Focus();
		}

		bool controlDown { get { return (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control; } }
		bool altOnly { get { return (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt)) == ModifierKeys.Alt; } }
		bool noModifiers { get { return (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)) == ModifierKeys.None; } }

		bool changingLocation = false;
		void LocationChanged(FilesLocation last)
		{
			if ((!changingLocation) && (last != null))
			{
				lastLocation.Push(last);
				nextLocation.Clear();
			}

			if (!Location.item.HasChildren)
			{
				changingLocation = true;
				Location = new FilesLocation(Location.item.Parent);
				changingLocation = false;
				return;
			}

			files.ResetScroll();
			Command_View_Refresh();
		}

		void LocationKeyDown(object sender, KeyEventArgs e)
		{
			if ((e.Key == Key.V) && (controlDown))
			{
				var files = ClipboardWindow.GetFiles();
				if (files != null)
				{
					location.Text = files.First();
					location.CaretIndex = location.Text.Length;
					e.Handled = true;
				}
			}
		}

		void SetLocation(string location)
		{
			var item = DiskItem.Get(location);
			if (item == null)
				throw new Exception("Invalid path.");

			Location = new FilesLocation(item);
		}

		void DoRename(DiskItem item)
		{
			if ((item.Type != DiskItem.DiskItemType.File) && (item.Type != DiskItem.DiskItemType.Directory))
				throw new ArgumentException("Cannot rename this entry.");

			var newName = Rename.Run(item);
			if (newName == null)
				return;

			Location.item.MoveFrom(item, newName);
			Command_View_Refresh();
			files.Focused = Files.Cast<DiskItem>().Where(file => file.Name == newName).FirstOrDefault();
			if (files.Focused != null)
				Selected.Add(files.Focused);
		}

		void ShowColumn<T>(Expression<Func<DiskItem, T>> expression)
		{
			ShowColumn(UIHelper<DiskItem>.GetProperty(expression));
		}

		void ShowColumn(DependencyProperty prop)
		{
			var type = prop.PropertyType;
			var sortAscending = (type != typeof(long?)) && (type != typeof(DateTime?));
			if (!Columns.Any(column => column.DepProp == prop))
				Columns.Add(new ItemGridColumn(prop) { SortAscending = sortAscending });
		}

		internal void Command_File_Rename()
		{
			DoRename(Selected.Single());
		}

		internal void Command_File_Identify()
		{
			foreach (DiskItem selected in Selected)
				selected.Identify();
			ShowColumn(a => a.Identity);
		}

		internal void Command_File_MD5()
		{
			foreach (DiskItem selected in Selected)
				selected.CalcMD5();
			ShowColumn(a => a.MD5);
		}

		internal void Command_File_SHA1()
		{
			foreach (DiskItem selected in Selected)
				selected.CalcSHA1();
			ShowColumn(a => a.SHA1);
		}

		internal void Command_File_Delete()
		{
			if (Selected.Count == 0)
				return;

			if (new Message
			{
				Title = "Confirm",
				Text = "Are you sure you want to delete these items?",
				Options = Message.OptionsEnum.YesNo,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.No,
			}.Show() == Message.OptionsEnum.Yes)
			{
				foreach (DiskItem file in Selected)
					file.Delete();
			}
			Command_View_Refresh();
		}

		internal void Command_Edit_Cut()
		{
			if (Selected.Count != 0)
				ClipboardWindow.SetFiles(Selected.Cast<DiskItem>().Select(item => item.FullName), true);
		}

		internal void Command_Edit_Copy()
		{
			if (Selected.Count != 0)
				ClipboardWindow.SetFiles(Selected.Cast<DiskItem>().Select(item => item.FullName), false);
		}

		internal void Command_Edit_Paste()
		{
			//var location = files.Location;
			//if (!location.IsDiskItem)
			//	throw new ArgumentException("Can only paste to disk.");

			//List<string> fileList;
			//bool isCut;
			//if (!ClipboardWindow.GetFiles(out fileList, out isCut))
			//	return;

			//var items = fileList.Select(file => DiskItem.GetRoot().GetChild(file)).Cast<DiskItem>().ToList();

			//var locationFiles = Files.Cast<DiskItem>().Select(record => record.Name).ToList();
			//var paths = items.Select(item => item.Path).GroupBy(path => path).Select(path => path.Key).ToList();
			//var canRename = (paths.Count == 1) && (paths[0] == location.FullName);
			//if ((isCut) || (!canRename))
			//{
			//	var names = items.Select(record => record.Name).ToList();
			//	var exists = locationFiles.Any(name => names.Contains(name));
			//	if (exists)
			//		throw new Exception("Destination already exists.");
			//}

			//foreach (var item in items)
			//{
			//	if (isCut)
			//	{
			//		location.MoveFrom(item);
			//		continue;
			//	}

			//	var name = item.NameWoExtension;
			//	string newName;
			//	for (var num = 1; ; ++num)
			//	{
			//		var extra = num == 1 ? "" : String.Format(" ({0})", num);
			//		newName = name + extra + item.Extension;
			//		if (locationFiles.Contains(newName))
			//			continue;
			//		break;
			//	}

			//	location.CopyFrom(item, newName);
			//}

			//Command_View_Refresh();
		}

		internal void Command_Edit_Find()
		{
			Location = new FilesLocation(Location.item, true);
		}

		internal void Command_Select_Directories()
		{
			Selected.Clear();
			foreach (var file in Files)
				if (file.HasChildren)
					Selected.Add(file);
		}

		internal void Command_Select_Files()
		{
			Selected.Clear();
			foreach (var file in Files)
				if (!file.HasChildren)
					Selected.Add(file);
		}

		internal void Command_Select_Remove()
		{
			Selected.ToList().ForEach(file => Files.Remove(file));
		}

		internal void Command_View_Refresh()
		{
			var recursive = Location.recursive;
			if (recursive)
				ShowColumn(a => a.Path);
			var newFiles = new ObservableCollection<DiskItem>();
			var locations = new List<DiskItem> { Location.item };
			for (var ctr = 0; ctr < locations.Count; ++ctr)
			{
				var children = locations[ctr].GetChildren();
				foreach (var child in children)
				{
					if ((recursive) && (child.HasChildren))
						locations.Add(child);
					newFiles.Add(child);
				}
			}
			files.SyncItems(newFiles, UIHelper<DiskItem>.GetProperty(a => a.FullName));
		}

		internal void ToggleColumn(DependencyProperty property)
		{
			var found = Columns.FirstOrDefault(a => a.DepProp == property);
			if (found != null)
			{
				Columns.Remove(found);
				return;
			}

			ShowColumn(property);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			var keySet = new KeySet
			{
				{ Key.Escape, () => files.Focus() },
				{ ModifierKeys.Alt, Key.Up, () => Location = new FilesLocation(Location.item.Parent) },
				{ ModifierKeys.Alt, Key.Left, () => SetLastLocation() },
				{ ModifierKeys.Alt, Key.Right, () => SetNextLocation() },
			};

			if (keySet.Run(e))
				e.Handled = true;
		}

		void OnAccept()
		{
			if (Selected.Count != 1)
				return;

			var location = Selected.Single();
			if (!location.HasChildren)
				return;

			Location = new FilesLocation(location);
		}

		void SetLastLocation()
		{
			if (!lastLocation.Any())
				return;

			changingLocation = true;
			nextLocation.Push(Location);
			Location = lastLocation.Pop();
			changingLocation = false;
		}

		void SetNextLocation()
		{
			if (!nextLocation.Any())
				return;

			changingLocation = true;
			lastLocation.Push(Location);
			Location = nextLocation.Pop();
			changingLocation = false;
		}

		void location_KeyDown(object sender, KeyEventArgs e)
		{
			e.Handled = true;
			switch (e.Key)
			{
				case Key.Enter:
					SetLocation(location.Text);
					files.Focus();
					break;
				default: e.Handled = false; break;
			}
		}

		internal Label GetLabel()
		{
			var label = new Label { Padding = new Thickness(10, 2, 10, 2) };
			var multiBinding = new MultiBinding { Converter = new NeoEdit.GUI.Common.ExpressionConverter(), ConverterParameter = @"FileName([0])" };
			multiBinding.Bindings.Add(new Binding("Location") { Source = this });
			label.SetBinding(Label.ContentProperty, multiBinding);
			return label;
		}
	}
}
