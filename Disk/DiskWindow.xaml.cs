using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.Disk.Dialogs;
using NeoEdit.GUI;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;
using NeoEdit.GUI.ItemGridControl;

namespace NeoEdit.Disk
{
	public partial class DiskWindow : Window
	{
		public static RoutedCommand Command_File_Rename = new RoutedCommand();
		public static RoutedCommand Command_File_Identify = new RoutedCommand();
		public static RoutedCommand Command_File_MD5 = new RoutedCommand();
		public static RoutedCommand Command_File_SHA1 = new RoutedCommand();
		public static RoutedCommand Command_File_Delete = new RoutedCommand();
		public static RoutedCommand Command_Edit_Cut = new RoutedCommand();
		public static RoutedCommand Command_Edit_Copy = new RoutedCommand();
		public static RoutedCommand Command_Edit_Paste = new RoutedCommand();
		public static RoutedCommand Command_View_Refresh = new RoutedCommand();

		[DepProp]
		string Location { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }

		static DiskWindow() { UIHelper<DiskWindow>.Register(); }

		readonly UIHelper<DiskWindow> uiHelper;
		public DiskWindow(string path = null)
		{
			if (path == null)
				path = Directory.GetCurrentDirectory();

			Transparency.MakeTransparent(this);
			uiHelper = new UIHelper<DiskWindow>(this);
			InitializeComponent();
			uiHelper.AddCallback(ItemGridTree.LocationProperty, files, () => Location = files.Location.FullName);
			location.GotFocus += (s, e) => location.SelectAll();
			location.LostFocus += (s, e) => uiHelper.InvalidateBinding(location, TextBox.TextProperty);

			var keep = new List<string> { "Name", "Size", "WriteTime" };
			foreach (var prop in DiskItem.StaticGetDepProps())
			{
				if (!keep.Contains(prop.Name))
					continue;
				files.Columns.Add(new ItemGridColumn(prop));
			}
			files.SortColumn = files.TextInputColumn = files.Columns.First(col => col.Header == "Name");
			SetLocation(path ?? "");
		}

		bool altOnly { get { return (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt)) == ModifierKeys.Alt; } }
		bool noModifiers { get { return (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)) == ModifierKeys.None; } }

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

		void Refresh()
		{
			files.Refresh();
		}

		void DoRename(DiskItem item)
		{
			if (!item.IsDiskItem)
				throw new ArgumentException("Can only rename disk files.");

			var newName = Rename.Run(item);
			if (newName == null)
				return;

			(files.Location as DiskItem).MoveFrom(item, newName);
			Refresh();
			files.Focused = files.Items.Cast<DiskItem>().Where(file => file.FullName == newName).FirstOrDefault();
			if (files.Focused != null)
				files.Selected.Add(files.Focused);
		}

		void DoPaste()
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

			Refresh();
		}

		void Command_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (e.Command == Command_File_Rename)
				DoRename(files.Selected.Single() as DiskItem);
			else if (e.Command == Command_File_Identify)
			{
				if (!files.Columns.Any(column => column.Header == "Identity"))
					files.Columns.Add(new ItemGridColumn(DiskItem.StaticGetDepProp("Identity")));

				foreach (DiskItem selected in files.Selected)
					selected.Identify();
			}
			else if (e.Command == Command_File_MD5)
			{
				if (!files.Columns.Any(column => column.Header == "MD5"))
					files.Columns.Add(new ItemGridColumn(DiskItem.StaticGetDepProp("MD5")));

				foreach (DiskItem selected in files.Selected)
					selected.CalcMD5();
			}
			else if (e.Command == Command_File_SHA1)
			{
				if (!files.Columns.Any(column => column.Header == "SHA1"))
					files.Columns.Add(new ItemGridColumn(DiskItem.StaticGetDepProp("SHA1")));

				foreach (DiskItem selected in files.Selected)
					selected.CalcSHA1();
			}
			else if (e.Command == Command_File_Delete)
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
				Refresh();
			}
			else if (e.Command == Command_Edit_Cut)
			{
				if (files.Selected.Count != 0)
					ClipboardWindow.SetFiles(files.Selected.Cast<DiskItem>().Select(item => item.FullName), true);
			}
			else if (e.Command == Command_Edit_Copy)
			{
				if (files.Selected.Count != 0)
					ClipboardWindow.SetFiles(files.Selected.Cast<DiskItem>().Select(item => item.FullName), false);
			}
			else if (e.Command == Command_Edit_Paste)
				DoPaste();
			else if (e.Command == Command_View_Refresh)
				Refresh();
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
	}
}
