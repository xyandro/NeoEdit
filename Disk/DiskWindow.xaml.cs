﻿using System;
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
		bool Recursive { get { return UIHelper<DiskWindow>.GetPropValue<bool>(this); } set { UIHelper<DiskWindow>.SetPropValue(this, value); } }
		[DepProp]
		ObservableCollection<DiskItem> Files { get { return UIHelper<DiskWindow>.GetPropValue<ObservableCollection<DiskItem>>(this); } set { UIHelper<DiskWindow>.SetPropValue(this, value); } }
		[DepProp]
		ObservableCollection<DiskItem> Selected { get { return UIHelper<DiskWindow>.GetPropValue<ObservableCollection<DiskItem>>(this); } set { UIHelper<DiskWindow>.SetPropValue(this, value); } }
		[DepProp]
		DiskItem Focused { get { return UIHelper<DiskWindow>.GetPropValue<DiskItem>(this); } set { UIHelper<DiskWindow>.SetPropValue(this, value); } }
		[DepProp]
		ObservableCollection<ItemGridColumn> Columns { get { return UIHelper<DiskWindow>.GetPropValue<ObservableCollection<ItemGridColumn>>(this); } set { UIHelper<DiskWindow>.SetPropValue(this, value); } }
		[DepProp]
		int ColumnsChangeCount { get { return UIHelper<DiskWindow>.GetPropValue<int>(this); } set { UIHelper<DiskWindow>.SetPropValue(this, value); } }

		static DiskWindow()
		{
			UIHelper<DiskWindow>.Register();
			UIHelper<DiskWindow>.AddCallback(a => a.Location, (obj, o, n) => { if (obj.Location != null) { obj.Recursive = false; obj.locationChangedTimer.Start(); } });
			UIHelper<DiskWindow>.AddCallback(a => a.Recursive, (obj, o, n) => obj.locationChangedTimer.Start());
			UIHelper<DiskWindow>.AddObservableCallback(a => a.Columns, (obj, s, e) => ++obj.ColumnsChangeCount);
		}

		RunOnceTimer locationChangedTimer;

		public DiskWindow(string path = null)
		{
			if (String.IsNullOrEmpty(path))
				path = Directory.GetCurrentDirectory();

			locationChangedTimer = new RunOnceTimer(() => LocationChanged());

			InitializeComponent();
			location.GotFocus += (s, e) => location.SelectAll();
			location.LostFocus += (s, e) => { if (Location != null) location.Text = Location.FullName; };
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
				var files = ClipboardWindow.GetFiles();
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
			var location = DiskItem.Get(path);
			if (location == null)
				throw new Exception("Invalid path.");

			Location = location;
		}

		void LocationChanged()
		{
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

			Command_View_Refresh();
			files.ResetScroll();

			if (selectedFile != null)
				selectedFile = Files.FirstOrDefault(file => file.Equals(selectedFile));
			if (selectedFile != null)
			{
				Focused = selectedFile;
				Selected.Add(selectedFile);
			}

			locationChangedTimer.Stop();
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

			var newName = Rename.Run(Focused);
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

			//Command_View_Refresh();
		}

		internal void Command_Edit_Find()
		{
			ShowColumn(a => a.Path);
			Recursive = true;
			locationChangedTimer.Start();
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
			Location = null;
			Selected.ToList().ForEach(file => Files.Remove(file));
		}

		internal void Command_View_Refresh()
		{
			List<DiskItem> items;

			if (Location == null)
				items = Files.Where(file => file.Exists).ToList();
			else
				items = new List<DiskItem> { Location };

			var found = new HashSet<string>(items.Select(file => file.FullName));
			if ((Location != null) || (Recursive))
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
					if (!Recursive)
					{
						items.RemoveAt(ctr);
						break;
					}
				}

			var filesDict = Files.ToDictionary(file => file.FullName, file => file);
			var itemsDict = items.ToDictionary(file => file.FullName, file => file);
			filesDict.Where(pair => !itemsDict.ContainsKey(pair.Key)).ToList().ForEach(pair => Files.Remove(pair.Value));
			itemsDict.Where(pair => !filesDict.ContainsKey(pair.Key)).ToList().ForEach(pair => Files.Add(pair.Value));
			filesDict.Where(pair => itemsDict.ContainsKey(pair.Key)).ToList().ForEach(pair => pair.Value.Refresh());
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
				{ ModifierKeys.Alt, Key.Up, () => Location = new DiskItem(location.Text).Parent },
			};

			if (keySet.Run(e))
				e.Handled = true;
		}

		void OnAccept()
		{
			if ((Focused == null) || (!Focused.HasChildren))
				return;

			Recursive = false;
			Location = Focused;
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
