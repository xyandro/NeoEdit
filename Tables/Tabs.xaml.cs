using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Microsoft.Win32;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.Tables
{
	public class Tabs : Tabs<TableEditor> { }

	partial class TablesTabs
	{
		[DepProp]
		public ObservableCollection<Tabs.ItemData> TableEditors { get { return UIHelper<TablesTabs>.GetPropValue<ObservableCollection<Tabs.ItemData>>(this); } set { UIHelper<TablesTabs>.SetPropValue(this, value); } }
		[DepProp]
		public Tabs.ItemData TopMost { get { return UIHelper<TablesTabs>.GetPropValue<Tabs.ItemData>(this); } set { UIHelper<TablesTabs>.SetPropValue(this, value); } }
		[DepProp]
		public bool Tiles { get { return UIHelper<TablesTabs>.GetPropValue<bool>(this); } set { UIHelper<TablesTabs>.SetPropValue(this, value); } }

		static TablesTabs() { UIHelper<TablesTabs>.Register(); }

		public static void Create(string fileName = null, bool createNew = false, TablesTabs tableEditTabs = null)
		{
			var tableEditor = new TableEditor(fileName);

			if ((tableEditTabs == null) && (!createNew))
				tableEditTabs = UIHelper<TablesTabs>.GetNewest();

			if (tableEditTabs == null)
				tableEditTabs = new TablesTabs();

			tableEditTabs.Activate();
			tableEditTabs.Add(tableEditor);
		}

		TablesTabs()
		{
			TablesMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command));
			InitializeComponent();
			UIHelper.AuditMenu(menu);

			TableEditors = new ObservableCollection<Tabs.ItemData>();
			AllowDrop = true;
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			var answer = Message.OptionsEnum.None;
			var active = TopMost;
			foreach (var tableEditor in TableEditors)
			{
				TopMost = tableEditor;
				if (!tableEditor.Item.CanClose(ref answer))
				{
					e.Cancel = true;
					return;
				}
			}
			TopMost = active;
			base.OnClosing(e);
		}

		bool shiftDown { get { return (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None; } }
		bool controlDown { get { return (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None; } }

		internal void RunCommand(TablesCommand command)
		{
			var shiftDown = this.shiftDown;

			object dialogResult;
			if (!GetDialogResult(command, out dialogResult))
				return;

			HandleCommand(command, shiftDown, dialogResult);
		}

		class OpenFileDialogResult
		{
			public List<string> files { get; set; }
		}

		OpenFileDialogResult Command_File_Open_Dialog(string initialDirectory = null)
		{
			if ((initialDirectory == null) && (TopMost != null))
				initialDirectory = Path.GetDirectoryName(TopMost.Item.FileName);
			var dialog = new OpenFileDialog
			{
				DefaultExt = "tsv",
				Filter = "Table files|*.tsv;*.csv|All files|*.*",
				FilterIndex = 1,
				Multiselect = true,
				InitialDirectory = initialDirectory,
			};
			if (dialog.ShowDialog() != true)
				return null;

			return new OpenFileDialogResult { files = dialog.FileNames.ToList() };
		}

		void Command_File_Open(OpenFileDialogResult result)
		{
			foreach (var filename in result.files)
				Create(filename, false, this);
		}

		internal bool GetDialogResult(TablesCommand command, out object dialogResult)
		{
			dialogResult = null;

			switch (command)
			{
				case TablesCommand.File_Open: dialogResult = Command_File_Open_Dialog(); break;
				default: return TopMost == null ? true : TopMost.Item.GetDialogResult(command, out dialogResult);
			}

			return dialogResult != null;
		}

		internal void HandleCommand(TablesCommand command, bool shiftDown, object dialogResult)
		{
			switch (command)
			{
				case TablesCommand.File_New: Create(createNew: shiftDown, tableEditTabs: shiftDown ? null : this); break;
				case TablesCommand.File_Open: Command_File_Open(dialogResult as OpenFileDialogResult); break;
				case TablesCommand.File_Exit: Close(); break;
			}

			foreach (var textEditorItem in TableEditors.Where(item => item.Active).ToList())
				textEditorItem.Item.HandleCommand(command, shiftDown, dialogResult);
		}

		void Add(TableEditor tableEditor)
		{
			var item = new Tabs.ItemData(tableEditor);
			if ((!tableEditor.Empty()) && (TopMost != null) && (TopMost.Item.Empty()))
				TableEditors[TableEditors.IndexOf(TopMost)] = item;
			else
				TableEditors.Add(item);
			TopMost = item;
		}

		internal void Remove(TableEditor tableEditor, bool closeIfLast = false)
		{
			TableEditors.Remove(TableEditors.Single(item => item.Item == tableEditor));
			tableEditor.Closed();
			if ((closeIfLast) && (TableEditors.Count == 0))
				Close();
		}
	}
}
