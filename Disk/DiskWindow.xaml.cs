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

	partial class DiskWindow
	{
		[DepProp]
		DiskItem Location { get { return UIHelper<DiskWindow>.GetPropValue<DiskItem>(this); } set { UIHelper<DiskWindow>.SetPropValue(this, value); } }
		[DepProp]
		ObservableCollection<DiskItem> Files { get { return UIHelper<DiskWindow>.GetPropValue<ObservableCollection<DiskItem>>(this); } set { UIHelper<DiskWindow>.SetPropValue(this, value); } }
		[DepProp]
		ObservableCollection<DiskItem> Selected { get { return UIHelper<DiskWindow>.GetPropValue<ObservableCollection<DiskItem>>(this); } set { UIHelper<DiskWindow>.SetPropValue(this, value); } }
		[DepProp]
		DiskItem Focused { get { return UIHelper<DiskWindow>.GetPropValue<DiskItem>(this); } set { UIHelper<DiskWindow>.SetPropValue(this, value); } }
		[DepProp]
		ObservableCollection<ItemGridColumn> Columns { get { return UIHelper<DiskWindow>.GetPropValue<ObservableCollection<ItemGridColumn>>(this); } set { UIHelper<DiskWindow>.SetPropValue(this, value); } }
		[DepProp]
		ItemGridColumn SortColumn { get { return UIHelper<DiskWindow>.GetPropValue<ItemGridColumn>(this); } set { UIHelper<DiskWindow>.SetPropValue(this, value); } }
		[DepProp]
		bool SortAscending { get { return UIHelper<DiskWindow>.GetPropValue<bool>(this); } set { UIHelper<DiskWindow>.SetPropValue(this, value); } }
		[DepProp]
		int ColumnsChangeCount { get { return UIHelper<DiskWindow>.GetPropValue<int>(this); } set { UIHelper<DiskWindow>.SetPropValue(this, value); } }

		static DiskWindow()
		{
			UIHelper<DiskWindow>.Register();
			UIHelper<DiskWindow>.AddObservableCallback(a => a.Files, (obj, o, n) => obj.filesChangedTimer.Start());
			UIHelper<DiskWindow>.AddObservableCallback(a => a.Columns, (obj, s, e) => ++obj.ColumnsChangeCount);
		}

		RunOnceTimer filesChangedTimer;

		public DiskWindow(string path = null)
		{
			if (String.IsNullOrEmpty(path))
				path = Directory.GetCurrentDirectory();

			filesChangedTimer = new RunOnceTimer(() => FilesChanged());


			InitializeComponent();
			location.GotFocus += (s, e) => location.SelectAll();
			location.LostFocus += (s, e) => { location.Text = Location.FullName; };
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

		void LocationKeyDown(object sender, KeyEventArgs e)
		{
			if ((e.Key == Key.V) && (controlDown))
			{
				var files = ClipboardWindow.GetStrings();
				if (files != null)
				{
					location.Text = files.First();
					location.CaretIndex = location.Text.Length;
					e.Handled = true;
				}
			}
		}

		void SetLocation(string path)
		{
			var diskItem = DiskItem.Get(path);
			if (diskItem == null)
				throw new Exception("Invalid path.");
			SetLocation(diskItem);
		}

		void SetLocation(DiskItem item)
		{
			Location = item;

			DiskItem selectedFile = null;
			if (Location != null)
			{
				if (!Location.HasChildren)
				{
					selectedFile = Location;
					Location = Location.Parent;
				}
				location.Text = Location.FullName;
			}

			SyncFiles(new List<DiskItem>(Location.GetChildren()));
			files.ResetScroll();

			if (selectedFile != null)
				selectedFile = Files.FirstOrDefault(file => file.Equals(selectedFile));
			if (selectedFile != null)
			{
				Focused = selectedFile;
				Selected.Add(selectedFile);
			}

			filesChangedTimer.Stop();
		}

		void FilesChanged()
		{
			var duplicates = Files.GroupBy(file => file.FullName).SelectMany(group => group.Skip(1)).ToList();
			if (duplicates.Any())
			{
				var selected = new HashSet<string>(Selected.Select(file => file.FullName));
				var focused = Focused == null ? null : Focused.FullName;

				foreach (var dup in duplicates)
					Files.Remove(dup);

				foreach (var file in Files)
				{
					if (file.FullName == focused)
						Focused = file;
					if (selected.Contains(file.FullName))
						Selected.Add(file);
				}
			}

			files.BringFocusedIntoView();
			filesChangedTimer.Stop();
		}

		void SyncFiles(List<DiskItem> items)
		{
			var filesDict = Files.ToDictionary(file => file.FullName, file => file);
			var itemsDict = items.ToDictionary(file => file.FullName, file => file);
			filesDict.Where(pair => !itemsDict.ContainsKey(pair.Key)).ToList().ForEach(pair => Files.Remove(pair.Value));
			itemsDict.Where(pair => !filesDict.ContainsKey(pair.Key)).ToList().ForEach(pair => Files.Add(pair.Value));
			filesDict.Where(pair => itemsDict.ContainsKey(pair.Key)).ToList().ForEach(pair => pair.Value.Refresh());
		}

		void ShowColumn<T>(Expression<Func<DiskItem, T>> expression)
		{
			ShowColumn(UIHelper<DiskItem>.GetProperty(expression));
		}

		void ShowColumn(DependencyProperty prop)
		{
			if (Columns.Any(column => column.DepProp == prop))
				return;
			var props = UIHelper<DiskItem>.GetProperties().ToList();
			while ((props.Count != 0) && (props[0] != prop))
				props.RemoveAt(0);
			var index = Columns.Count;
			for (var ctr = 0; ctr < Columns.Count; ++ctr)
				if (props.Contains(Columns[ctr].DepProp))
				{
					index = ctr;
					break;
				}
			Columns.Insert(index, new ItemGridColumn(prop));
		}

		internal void Command_File_Rename()
		{
			if (Focused == null)
				return;

			if (!Focused.CanRename)
				throw new ArgumentException("Cannot rename this entry.");

			var newName = RenameDialog.Run(Focused);
			if (newName == null)
				return;

			var oldName = Focused.FullName;
			Focused.Rename(newName);
			foreach (var file in Files)
				file.Relocate(oldName, newName);
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
				selected.SetMD5();
			ShowColumn(a => a.MD5);
		}

		internal void Command_File_SHA1()
		{
			foreach (DiskItem selected in Selected)
				selected.SetSHA1();
			ShowColumn(a => a.SHA1);
		}

		internal void Command_File_Svn()
		{
			foreach (DiskItem selected in Selected)
				selected.SetSvnStatus();
			ShowColumn(a => a.SvnStatus);
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
			}.Show() != Message.OptionsEnum.Yes)
				return;

			foreach (var file in Selected)
			{
				file.Delete();
				Files.Remove(file);
				Files.Where(item => item.IsChildOf(file)).ToList().ForEach(item => Files.Remove(item));
			}
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

			//PopulateFilesFromLocation();
		}

		internal void Command_Edit_Find()
		{
			var result = FindDialog.Run();
			if (result == null)
				return;

			Selected.Clear();

			var items = Files.ToList();
			if (result.Recursive)
			{
				items = items.Where(file => file.Exists).ToList();
				var found = new HashSet<string>(items.Select(file => file.FullName));
				for (var ctr = 0; ctr < items.Count; ++ctr)
				{
					if (items[ctr].HasChildren)
						foreach (var file in items[ctr].GetChildren())
						{
							if (found.Contains(file.FullName))
								continue;
							items.Add(file);
							found.Add(file.FullName);
						}
				}

				ShowColumn(a => a.Path);
			}

			var selected = items;
			if (result.Regex != null)
			{
				if (result.FullPath)
					selected = selected.Where(file => result.Regex.IsMatch(file.FullName)).ToList();
				else
					selected = selected.Where(file => result.Regex.IsMatch(file.Name)).ToList();
			}

			if (result.MinSize.HasValue)
				selected = selected.Where(file => file.Size >= result.MinSize.Value).ToList();
			if (result.MaxSize.HasValue)
				selected = selected.Where(file => file.Size <= result.MaxSize.Value).ToList();
			if (result.StartDate.HasValue)
				selected = selected.Where(file => file.WriteTime >= result.StartDate.Value).ToList();
			if (result.EndDate.HasValue)
				selected = selected.Where(file => file.WriteTime <= result.EndDate.Value).ToList();

			if (result.Recursive)
			{
				items = selected;
				selected = new List<DiskItem>();
			}

			SyncFiles(items);
			foreach (var file in selected)
				Selected.Add(file);

			if (!Selected.Contains(Focused))
				Focused = Selected.FirstOrDefault();
		}

		bool SearchFile(DiskItem file, BinaryFindDialog.Result search)
		{
			if ((file.Type != DiskItem.DiskItemType.File) || (!file.Exists))
				return false;

			var findLen = search.Searcher.MaxLen;
			var buffer = new byte[8192];
			var used = 0;
			using (var stream = File.OpenRead(file.FullName))
				while (true)
				{
					var block = stream.Read(buffer, used, buffer.Length - used);
					if (block == 0)
						break;
					used += block;

					var result = search.Searcher.Find(buffer, 0, used, true);
					if (result.Any())
						return true;

					var keep = Math.Min(used, findLen - 1);
					Array.Copy(buffer, used - keep, buffer, 0, keep);
					used = keep;
				}

			return false;
		}

		internal void Command_Edit_BinaryFind()
		{
			if (Selected.Count == 0)
				return;
			var search = BinaryFindDialog.Run();
			if (search == null)
				return;

			var files = Selected.ToList();
			Selected.Clear();
			foreach (var file in files)
				if (SearchFile(file, search))
					Selected.Add(file);
			if (!Selected.Contains(Focused))
				Focused = Selected.FirstOrDefault();
		}

		internal void Command_Edit_TextEdit()
		{
			foreach (var file in Selected)
				Launcher.Static.LaunchTextEditor(file.FullName);
		}

		internal void Command_Edit_BinaryEdit()
		{
			foreach (var file in Selected)
				Launcher.Static.LaunchBinaryEditor(file.FullName);
		}

		internal void Command_Select_All()
		{
			Selected.Clear();
			foreach (var file in Files)
				Selected.Add(file);
		}

		internal void Command_Select_None()
		{
			Selected.Clear();
		}

		internal void Command_Select_Invert()
		{
			foreach (var file in Files)
				if (Selected.Contains(file))
					Selected.Remove(file);
				else
					Selected.Add(file);
		}

		internal void Command_Select_Directories()
		{
			Selected.Clear();
			foreach (var file in Files)
				if (file.HasChildren)
					Selected.Add(file);
			if (!Selected.Contains(Focused))
				Focused = Selected.FirstOrDefault();
		}

		internal void Command_Select_Files()
		{
			Selected.Clear();
			foreach (var file in Files)
				if (!file.HasChildren)
					Selected.Add(file);
			if (!Selected.Contains(Focused))
				Focused = Selected.FirstOrDefault();
		}

		internal void Command_Select_AddCopiedCut()
		{
			var files = ClipboardWindow.GetStrings();
			if ((files == null) || (files.Count == 0))
				return;

			Selected.Clear();
			foreach (var file in files)
			{
				var diskItem = DiskItem.Get(file);
				if (diskItem == null)
					continue;
				Files.Add(diskItem);
				Selected.Add(diskItem);
			}
			if (!Selected.Contains(Focused))
				Focused = Selected.FirstOrDefault();
		}

		internal void Command_Select_Remove()
		{
			Selected.ToList().ForEach(file => Files.Remove(file));
		}

		internal void Command_Select_RemoveWithChildren()
		{
			foreach (var sel in Selected.ToList())
			{
				Files.Remove(sel);
				Files.Where(file => file.IsChildOf(sel)).ToList().ForEach(file => Files.Remove(file));
			}
		}

		internal void ToggleColumn(DependencyProperty property)
		{
			var found = Columns.FirstOrDefault(a => a.DepProp == property);
			if (found != null)
				Columns.Remove(found);
			else
				ShowColumn(property);
		}

		internal void SetSort(DependencyProperty property)
		{
			var sortColumn = Columns.FirstOrDefault(column => column.DepProp == property);
			if (sortColumn == null)
				return;

			if (SortColumn != sortColumn)
				SortColumn = sortColumn;
			else
				SortAscending = !SortAscending;
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			var keySet = new KeySet
			{
				{ Key.Escape, () => files.Focus() },
				{ Key.Back, () => SetLocation(Location.Parent) },
			};

			if (keySet.Run(e))
				e.Handled = true;
		}

		void OnAccept()
		{
			if ((Focused == null) || (!Focused.HasChildren))
				return;

			SetLocation(Focused);
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
			var multiBinding = new MultiBinding { Converter = new NeoEdit.GUI.Common.ExpressionConverter(), ConverterParameter = @"[0] == '' ? 'Custom' : FileName([0])" };
			multiBinding.Bindings.Add(new Binding("Location") { Source = this });
			label.SetBinding(Label.ContentProperty, multiBinding);
			return label;
		}
	}
}
