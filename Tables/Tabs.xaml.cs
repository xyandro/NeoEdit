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
	public class TabsWindow : TabsWindow<TableEditor> { }

	partial class TablesTabs
	{
		[DepProp]
		public ObservableCollection<TableEditor> TableEditors { get { return UIHelper<TablesTabs>.GetPropValue<ObservableCollection<TableEditor>>(this); } set { UIHelper<TablesTabs>.SetPropValue(this, value); } }
		[DepProp]
		public TableEditor TopMost { get { return UIHelper<TablesTabs>.GetPropValue<TableEditor>(this); } set { UIHelper<TablesTabs>.SetPropValue(this, value); } }
		[DepProp]
		public bool Tiles { get { return UIHelper<TablesTabs>.GetPropValue<bool>(this); } set { UIHelper<TablesTabs>.SetPropValue(this, value); } }

		static TablesTabs() { UIHelper<TablesTabs>.Register(); }

		public static void Create(string fileName = null, TablesTabs tableEditTabs = null, bool forceCreate = false)
		{
			CreateTab(new TableEditor(fileName), tableEditTabs, forceCreate);
		}

		TablesTabs()
		{
			TablesMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command));
			InitializeComponent();
			ItemTabs = tabs;
			UIHelper.AuditMenu(menu);

			TableEditors = new ObservableCollection<TableEditor>();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			var answer = Message.OptionsEnum.None;
			var active = TopMost;
			foreach (var tableEditor in TableEditors)
			{
				TopMost = tableEditor;
				if (!tableEditor.CanClose(ref answer))
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
				initialDirectory = Path.GetDirectoryName(TopMost.FileName);
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
				Create(filename, this);
		}

		internal bool GetDialogResult(TablesCommand command, out object dialogResult)
		{
			dialogResult = null;

			switch (command)
			{
				case TablesCommand.File_Open: dialogResult = Command_File_Open_Dialog(); break;
				default: return TopMost == null ? true : TopMost.GetDialogResult(command, out dialogResult);
			}

			return dialogResult != null;
		}

		internal void HandleCommand(TablesCommand command, bool shiftDown, object dialogResult)
		{
			switch (command)
			{
				case TablesCommand.File_New: Create(tableEditTabs: this, forceCreate: shiftDown); break;
				case TablesCommand.File_Open: Command_File_Open(dialogResult as OpenFileDialogResult); break;
				case TablesCommand.File_Exit: Close(); break;
			}

			foreach (var textEditorItem in TableEditors.Where(item => item.Active).ToList())
				textEditorItem.HandleCommand(command, shiftDown, dialogResult);
		}

		internal void Remove(TableEditor tableEditor, bool closeIfLast = false)
		{
			TableEditors.Remove(tableEditor);
			tableEditor.Closed();
			if ((closeIfLast) && (TableEditors.Count == 0))
				Close();
		}
	}
}
