using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Tables
{
	public class Tabs : Tabs<TableEditor> { }
	public class TabsWindow : TabsWindow<TableEditor> { }

	partial class TablesTabs
	{
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
		}

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
			if ((initialDirectory == null) && (ItemTabs.TopMost != null))
				initialDirectory = Path.GetDirectoryName(ItemTabs.TopMost.FileName);
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
				default: return ItemTabs.TopMost == null ? true : ItemTabs.TopMost.GetDialogResult(command, out dialogResult);
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

			foreach (var textEditorItem in ItemTabs.Items.Where(item => item.Active).ToList())
				textEditorItem.HandleCommand(command, shiftDown, dialogResult);
		}
	}
}
