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
using NeoEdit.GUI;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;
using NeoEdit.GUI.ItemGridControl;

namespace NeoEdit.Disk
{
	class DiskItemGrid : ItemGrid<DiskItem> { }

	partial class DiskWindow
	{
		[DepProp]
		DiskItem Location { get { return UIHelper<DiskWindow>.GetPropValue<DiskItem>(this); } set { UIHelper<DiskWindow>.SetPropValue(this, value); } }

		Stack<DiskItem> lastLocation = new Stack<DiskItem>();
		Stack<DiskItem> nextLocation = new Stack<DiskItem>();

		static DiskWindow()
		{
			UIHelper<DiskWindow>.Register();
			UIHelper<DiskWindow>.AddCallback(a => a.Location, (s, o, n) => s.LocationChanged(o));
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
		void LocationChanged(DiskItem last)
		{
			if ((!changingLocation) && (last != null))
			{
				lastLocation.Push(last);
				nextLocation.Clear();
			}

			if (!Location.HasChildren)
			{
				changingLocation = true;
				Location = Location.Parent;
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

			Location = item;
		}

		void DoRename(DiskItem item)
		{
			//if (!item.IsDiskItem)
			//	throw new ArgumentException("Can only rename disk files.");

			//var newName = Rename.Run(item);
			//if (newName == null)
			//	return;

			//(files.Location as DiskItem).MoveFrom(item, newName);
			//Command_View_Refresh();
			//files.Focused = files.Items.Cast<DiskItem>().Where(file => file.FullName == newName).FirstOrDefault();
			//if (files.Focused != null)
			//	files.Selected.Add(files.Focused);
		}

		void ShowColumn<T>(Expression<Func<DiskItem, T>> expression)
		{
			var prop = UIHelper<DiskItem>.GetProperty(expression);
			var type = prop.PropertyType;
			var sortAscending = (type != typeof(long?)) && (type != typeof(DateTime?));
			if (!files.Columns.Any(column => column.DepProp == prop))
				files.Columns.Add(new ItemGridColumn(prop) { SortAscending = sortAscending });
		}

		internal void Command_File_Rename()
		{
			DoRename(files.Selected.Single() as DiskItem);
		}

		internal void Command_File_Identify()
		{
			foreach (DiskItem selected in files.Selected)
				selected.Identify();
			ShowColumn(a => a.Identity);
		}

		internal void Command_File_MD5()
		{
			foreach (DiskItem selected in files.Selected)
				selected.CalcMD5();
			ShowColumn(a => a.MD5);
		}

		internal void Command_File_SHA1()
		{
			foreach (DiskItem selected in files.Selected)
				selected.CalcSHA1();
			ShowColumn(a => a.SHA1);
		}

		internal void Command_File_Delete()
		{
			if (files.Selected.Count == 0)
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
				foreach (DiskItem file in files.Selected)
					file.Delete();
			}
			Command_View_Refresh();
		}

		internal void Command_Edit_Cut()
		{
			if (files.Selected.Count != 0)
				ClipboardWindow.SetFiles(files.Selected.Cast<DiskItem>().Select(item => item.FullName), true);
		}

		internal void Command_Edit_Copy()
		{
			if (files.Selected.Count != 0)
				ClipboardWindow.SetFiles(files.Selected.Cast<DiskItem>().Select(item => item.FullName), false);
		}

		internal void Command_Edit_Paste()
		{
			//var location = files.Location as DiskItem;
			//if (!location.IsDiskItem)
			//	throw new ArgumentException("Can only paste to disk.");

			//List<string> fileList;
			//bool isCut;
			//if (!ClipboardWindow.GetFiles(out fileList, out isCut))
			//	return;

			//var items = fileList.Select(file => DiskItem.GetRoot().GetChild(file)).Cast<DiskItem>().ToList();

			//var locationFiles = files.Items.Cast<DiskItem>().Select(record => record.Name).ToList();
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

		internal void Command_Select_Directories()
		{
			files.Selected.Clear();
			foreach (var file in files.Items)
				if (file.HasChildren)
					files.Selected.Add(file);
		}

		internal void Command_Select_Files()
		{
			files.Selected.Clear();
			foreach (var file in files.Items)
				if (!file.HasChildren)
					files.Selected.Add(file);
		}

		internal void Command_Select_Remove()
		{
			files.Selected.ToList().ForEach(file => files.Items.Remove(file as DiskItem));
		}

		internal void Command_View_Refresh()
		{
			var newFiles = new ObservableCollection<DiskItem>(Location.GetChildren());
			files.SyncItems(newFiles, UIHelper<DiskItem>.GetProperty(a => a.FullName));
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			var keySet = new KeySet
			{
				{ Key.Escape, () => files.Focus() },
				{ ModifierKeys.Alt, Key.Up, () => Location = Location.Parent },
				{ ModifierKeys.Alt, Key.Left, () => SetLastLocation() },
				{ ModifierKeys.Alt, Key.Right, () => SetNextLocation() },
			};

			if (keySet.Run(e))
				e.Handled = true;
		}

		void OnAccept()
		{
			if (files.Selected.Count != 1)
				return;

			var location = files.Selected.Single() as DiskItem;
			if (!location.HasChildren)
				return;

			Location = location;
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
