using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.Common.NEClipboards;
using NeoEdit.Common.Transform;
using NeoEdit.Disk.Dialogs;
using NeoEdit.Disk.VCS;
using NeoEdit.GUI;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Controls.ItemGridControl;
using NeoEdit.GUI.Converters;
using NeoEdit.GUI.Dialogs;
using NeoEdit.GUI.Misc;

namespace NeoEdit.Disk
{
	class DiskItemGrid : ItemGrid<DiskItem> { }
	public class TabsControl : TabsControl<DiskWindow, DiskCommand> { }

	partial class DiskWindow
	{
		[DepProp]
		DiskItem Location { get { return UIHelper<DiskWindow>.GetPropValue<DiskItem>(this); } set { UIHelper<DiskWindow>.SetPropValue(this, value); } }
		[DepProp]
		int? ConstantList { get { return UIHelper<DiskWindow>.GetPropValue<int?>(this); } set { UIHelper<DiskWindow>.SetPropValue(this, value); } }
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

		public DiskWindow(string path = null, int? list = null, IEnumerable<string> listFiles = null)
		{
			if (listFiles == null)
				listFiles = new List<string>();
			if ((string.IsNullOrEmpty(path)) && (!list.HasValue) && (!listFiles.Any()))
				path = Directory.GetCurrentDirectory();

			filesChangedTimer = new RunOnceTimer(() => FilesChanged());

			InitializeComponent();
			ConstantList = list;

			var multiBinding = new MultiBinding { Converter = new NEExpressionConverter(), ConverterParameter = @"[0] o== null ? ([1] o== null ? ""Custom"" : FileName([1])) : $""List {[0]}""" };
			multiBinding.Bindings.Add(new Binding("ConstantList") { Source = this });
			multiBinding.Bindings.Add(new Binding("Location") { Source = this });
			SetBinding(UIHelper<TabsControl<DiskWindow, DiskCommand>>.GetProperty(a => a.TabLabel), multiBinding);

			location.GotFocus += (s, e) => location.SelectAll();
			location.LostFocus += (s, e) => { location.Text = Location?.FullName ?? ""; };
			location.PreviewKeyDown += LocationKeyDown;
			files.Accept += s => OnAccept();
			files.MouseDrag += MouseDragFiles;

			Files = new ObservableCollection<DiskItem>();
			Selected = new ObservableCollection<DiskItem>();
			Columns = new ObservableCollection<ItemGridColumn>();

			ShowColumn(a => a.Ico);
			ShowColumn(a => a.Name);
			ShowColumn(a => a.Size);
			ShowColumn(a => a.WriteTime);
			ShowColumn(a => a.Type);
			SetSort(a => a.Name);

			if (listFiles.Any())
			{
				ShowColumn(a => a.Path);
				Selected.Clear();
				foreach (var file in listFiles)
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
			else
			{
				SetLocation(path ?? "");
			}

			Loaded += (s, e) => files.Focus();
		}

		bool controlDown => (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
		bool altOnly => (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt)) == ModifierKeys.Alt;
		bool noModifiers => (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)) == ModifierKeys.None;

		void LocationKeyDown(object sender, KeyEventArgs e)
		{
			if ((e.Key == Key.V) && (controlDown))
			{
				var files = NEClipboard.Strings;
				if (files.Count != 0)
				{
					location.Text = files.First();
					location.CaretIndex = location.Text.Length;
					e.Handled = true;
				}
			}
		}

		void SetLocation(string path)
		{
			if (ConstantList.HasValue)
				return;

			var diskItem = DiskItem.Get(path);
			if (diskItem == null)
				throw new Exception("Invalid path.");
			SetLocation(diskItem);
		}

		void SetLocation(DiskItem item)
		{
			if (ConstantList.HasValue)
				return;

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
				var focused = Focused?.FullName;

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

		void MouseDragFiles()
		{
			var paths = Selected.Select(diskItem => diskItem.FullName).ToArray();
			DragDrop.DoDragDrop(this, new DataObject(DataFormats.FileDrop, paths), DragDropEffects.Copy);
		}

		void SyncFiles(List<DiskItem> items)
		{
			var filesDict = Files.ToDictionary(file => file.FullName, file => file);
			var itemsDict = items.ToDictionary(file => file.FullName, file => file);
			filesDict.Where(pair => !itemsDict.ContainsKey(pair.Key)).ToList().ForEach(pair => Files.Remove(pair.Value));
			itemsDict.Where(pair => !filesDict.ContainsKey(pair.Key)).ToList().ForEach(pair => Files.Add(pair.Value));
			filesDict.Where(pair => itemsDict.ContainsKey(pair.Key)).ToList().ForEach(pair => pair.Value.Refresh());
		}

		void ShowColumn<T>(Expression<Func<DiskItem, T>> expression) => ShowColumn(UIHelper<DiskItem>.GetProperty(expression));

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

			var newName = RenameDialog.Run(WindowParent, Focused);
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

		internal void Command_File_Hash()
		{
			var result = HashDialog.Run(WindowParent);
			if (result == null)
				return;

			foreach (DiskItem selected in Selected)
				selected.SetHash(result.HashType, result.HMACKey);
			ShowColumn(a => a.Hash);
		}

		internal void Command_File_VCS()
		{
			foreach (DiskItem selected in Selected)
				selected.SetVCSStatus();
			ShowColumn(a => a.VCSStatus);
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

		internal void Command_Edit_CutCopy(bool isCut)
		{
			if (Selected.Count == 0)
				return;
			var files = Selected.Cast<DiskItem>().Select(item => item.FullName).ToList();
			if (isCut)
				NEClipboard.CutFiles = files;
			else
				NEClipboard.CopiedFiles = files;
		}

		internal void Command_Edit_Paste()
		{
			//var location = files.Location;
			//if (!location.IsDiskItem)
			//	throw new ArgumentException("Can only paste to disk.");

			//List<string> fileList;
			//bool isCut;
			//if (!NEClipboard.GetFiles(out fileList, out isCut))
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
			//		var extra = num == 1 ? "" : $" ({num})";
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
			var result = FindDialog.Run(WindowParent);
			if (result == null)
				return;

			Selected.Clear();

			var items = Files.ToList();
			if (result.Recursive)
			{
				var list = items.Where(file => file.Exists).ToList();
				items.Clear();
				var found = new HashSet<string>();
				for (var ctr = 0; ctr < list.Count; ++ctr)
				{
					var file = list[ctr];

					if (found.Contains(file.FullName))
						continue;
					found.Add(file.FullName);

					items.Add(file);

					if (file.HasChildren)
					{
						var add = true;
						if ((result.VCSStatus != VersionControlStatus.Unknown) && (result.VCSStatus != VersionControlStatus.All))
						{
							file.SetVCSStatus();
							if ((file.VCSStatus == VersionControlStatus.DoNotTrack) || (file.VCSStatus == VersionControlStatus.Ignored))
								add = false;
						}

						if (add)
							list.AddRange(file.GetChildren());
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

			if ((result.VCSStatus != VersionControlStatus.Unknown) && (result.VCSStatus != VersionControlStatus.All))
			{
				foreach (var file in selected)
					file.SetVCSStatus();

				selected = selected.Where(file => result.VCSStatus.HasFlag(file.VCSStatus)).ToList();

				ShowColumn(a => a.VCSStatus);
			}

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

		bool BinarySearchFile(DiskItem file, FindBinaryDialog.Result search)
		{
			if ((file.FileType != DiskItem.DiskItemType.File) || (!file.Exists))
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

		bool TextSearchFile(DiskItem file, FindTextDialog.Result search)
		{
			if ((file.FileType != DiskItem.DiskItemType.File) || (!file.Exists))
				return false;

			var data = new TextData(Coder.BytesToString(File.ReadAllBytes(file.FullName), Common.Transform.Coder.CodePage.AutoByBOM, true));
			var start = data.GetOffset(0, 0);
			return data.RegexMatches(search.Regex, start, data.NumChars - start, search.IncludeEndings, false, true).Any();
		}

		internal void Command_Edit_FindBinary()
		{
			if (Selected.Count == 0)
				return;
			var search = FindBinaryDialog.Run(WindowParent);
			if (search == null)
				return;

			var files = Selected.ToList();
			Selected.Clear();
			foreach (var file in files)
				if (BinarySearchFile(file, search))
					Selected.Add(file);
			if (!Selected.Contains(Focused))
				Focused = Selected.FirstOrDefault();
		}

		internal void Command_Edit_FindText()
		{
			if (Selected.Count == 0)
				return;
			var search = FindTextDialog.Run(WindowParent, FindTextDialog.FindTextType.Single);
			if (search == null)
				return;

			var files = Selected.ToList();
			Selected.Clear();
			foreach (var file in files)
				if (TextSearchFile(file, search))
					Selected.Add(file);
			if (!Selected.Contains(Focused))
				Focused = Selected.FirstOrDefault();
		}

		internal void Command_Edit_ToList(int list)
		{
			var toList = DiskTabs.GetList(list);

			foreach (var item in Selected)
			{
				toList.Files.Add(item);
				toList.Selected.Add(item);
			}
			if (!Selected.Contains(Focused))
				Focused = Selected.FirstOrDefault();
		}

		internal void Command_Edit_TextEdit()
		{
			foreach (var file in Selected)
				Launcher.Static.LaunchTextEditor(file.FullName);
		}

		internal void Command_Edit_HexEdit()
		{
			foreach (var file in Selected)
				Launcher.Static.LaunchHexEditor(file.FullName);
		}

		internal void Command_Select_All()
		{
			Selected.Clear();
			foreach (var file in Files)
				Selected.Add(file);
		}

		internal void Command_Select_None() => Selected.Clear();

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

		Dictionary<string, List<DiskItem>> GetFilesByHash()
		{
			var selected = Selected.Where(item => item.FileType == DiskItem.DiskItemType.File).ToList();
			Selected.Clear();
			foreach (var item in selected)
				Selected.Add(item);

			if (selected.All(item => !string.IsNullOrWhiteSpace(item.Hash)))
				return selected.GroupBy(item => item.Hash).ToDictionary(group => group.Key, group => group.ToList());
			Command_File_Hash();
			return selected.GroupBy(item => item.Hash).ToDictionary(group => group.Key, group => group.ToList());
		}

		internal void Command_Select_Unique()
		{
			var byHash = GetFilesByHash();
			Selected.Clear();
			foreach (var pair in byHash)
				Selected.Add(pair.Value.First());
			if (!Selected.Contains(Focused))
				Focused = Selected.FirstOrDefault();
		}

		internal void Command_Select_Duplicates()
		{
			var byHash = GetFilesByHash();
			Selected.Clear();
			foreach (var pair in byHash)
				foreach (var item in pair.Value.Skip(1))
					Selected.Add(item);
			if (!Selected.Contains(Focused))
				Focused = Selected.FirstOrDefault();
		}

		internal void Command_Select_AddCopiedCut()
		{
			var files = NEClipboard.Strings;
			if (files.Count == 0)
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

		internal void Command_Select_Remove() => Selected.ToList().ForEach(file => Files.Remove(file));

		internal void Command_Select_RemoveWithChildren()
		{
			foreach (var sel in Selected.ToList())
			{
				Files.Remove(sel);
				Files.Where(file => file.IsChildOf(sel)).ToList().ForEach(file => Files.Remove(file));
			}
		}

		internal void Command_View_DiskUsage()
		{
			var files = Selected.Count != 0 ? Selected : Files;
			var usage = files.Select(file => new Tuple<string, string, long>(file.Name, file.FullName, file.DiskUsage())).OrderByDescending(tuple => tuple.Item3).ToList();
			new Chart(usage);
		}

		internal void ToggleColumn(DependencyProperty property)
		{
			var found = Columns.FirstOrDefault(a => a.DepProp == property);
			if (found != null)
				Columns.Remove(found);
			else
				ShowColumn(property);
		}

		void SetSort<T>(Expression<Func<DiskItem, T>> expression) => SetSort(UIHelper<DiskItem>.GetProperty(expression));

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
	}
}
