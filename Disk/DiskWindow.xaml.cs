using System;
using System.Collections.Generic;
using System.Linq;
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
	partial class DiskWindow
	{
		[DepProp]
		string Location { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }

		static DiskWindow() { UIHelper<DiskWindow>.Register(); }

		readonly UIHelper<DiskWindow> uiHelper;
		public DiskWindow(string path)
		{
			uiHelper = new UIHelper<DiskWindow>(this);
			InitializeComponent();
			UIHelper<DiskWindow>.AddCallback(files, ItemGridTree.LocationProperty, () => Location = files.Location.FullName);
			location.GotFocus += (s, e) => location.SelectAll();
			location.LostFocus += (s, e) => UIHelper<DiskWindow>.InvalidateBinding(location, TextBox.TextProperty);
			location.PreviewKeyDown += LocationKeyDown;

			var keep = new List<string> { "Name", "Size", "WriteTime" };
			foreach (var prop in DiskItem.StaticGetDepProps())
			{
				if (!keep.Contains(prop.Name))
					continue;
				files.Columns.Add(new ItemGridColumn(prop));
			}
			files.SortColumn = files.TextInputColumn = files.Columns.First(col => col.Header == "Name");
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
					Location = files.First();
					location.CaretIndex = Location.Length;
					e.Handled = true;
				}
			}
		}

		bool SetLocation(string location)
		{
			var item = DiskItem.GetRoot().GetChild(DiskItem.Simplify(location));
			if (item == null)
			{
				MessageBox.Show("Invalid path.", "Error");
				return false;
			}

			files.Location = item;
			return true;
		}

		void DoRename(DiskItem item)
		{
			if (!item.IsDiskItem)
				throw new ArgumentException("Can only rename disk files.");

			var newName = Rename.Run(item);
			if (newName == null)
				return;

			(files.Location as DiskItem).MoveFrom(item, newName);
			Command_View_Refresh();
			files.Focused = files.Items.Cast<DiskItem>().Where(file => file.FullName == newName).FirstOrDefault();
			if (files.Focused != null)
				files.Selected.Add(files.Focused);
		}

		void ShowColumn(string col)
		{
			if (!files.Columns.Any(column => column.Header == col))
				files.Columns.Add(new ItemGridColumn(DiskItem.StaticGetDepProp(col)));
		}

		internal void Command_File_Rename()
		{
			DoRename(files.Selected.Single() as DiskItem);
		}

		internal void Command_File_Identify()
		{
			foreach (DiskItem selected in files.Selected)
				selected.Identify();
			ShowColumn("Identity");
		}

		internal void Command_File_MD5()
		{
			foreach (DiskItem selected in files.Selected)
				selected.CalcMD5();
			ShowColumn("MD5");
		}

		internal void Command_File_SHA1()
		{
			foreach (DiskItem selected in files.Selected)
				selected.CalcSHA1();
			ShowColumn("SHA1");
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
			var location = files.Location as DiskItem;
			if (!location.IsDiskItem)
				throw new ArgumentException("Can only pastae to disk.");

			List<string> fileList;
			bool isCut;
			if (!ClipboardWindow.GetFiles(out fileList, out isCut))
				return;

			var items = fileList.Select(file => DiskItem.GetRoot().GetChild(file)).Cast<DiskItem>().ToList();

			var locationFiles = files.Items.Cast<DiskItem>().Select(record => record.Name).ToList();
			var paths = items.Select(item => item.Path).GroupBy(path => path).Select(path => path.Key).ToList();
			var canRename = (paths.Count == 1) && (paths[0] == location.FullName);
			if ((isCut) || (!canRename))
			{
				var names = items.Select(record => record.Name).ToList();
				var exists = locationFiles.Any(name => names.Contains(name));
				if (exists)
					throw new Exception("Destination already exists.");
			}

			foreach (var item in items)
			{
				if (isCut)
				{
					location.MoveFrom(item);
					continue;
				}

				var name = item.NameWoExtension;
				string newName;
				for (var num = 1; ; ++num)
				{
					var extra = num == 1 ? "" : String.Format(" ({0})", num);
					newName = name + extra + item.Extension;
					if (locationFiles.Contains(newName))
						continue;
					break;
				}

				location.CopyFrom(item, newName);
			}

			Command_View_Refresh();
		}

		internal void Command_View_Refresh()
		{
			files.Refresh();
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			var keySet = new KeySet
					{
						{ Key.Escape, () => files.Focus() },
					};

			if (keySet.Run(e))
				e.Handled = true;
		}

		void location_KeyDown(object sender, KeyEventArgs e)
		{
			e.Handled = true;
			switch (e.Key)
			{
				case Key.Enter:
					if (SetLocation(location.Text))
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
