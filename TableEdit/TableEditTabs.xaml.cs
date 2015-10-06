using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Microsoft.Win32;
using NeoEdit.Common.NEClipboards;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;
using NeoEdit.GUI.Misc;

namespace NeoEdit.TableEdit
{
	public class Tabs : Tabs<TableEditor, TableEditCommand> { }
	public class TabsWindow : TabsWindow<TableEditor, TableEditCommand> { }

	partial class TableEditTabs
	{
		static TableEditTabs() { UIHelper<TableEditTabs>.Register(); }

		public static void Create(string fileName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, bool? modified = null, TableEditTabs tableEditTabs = null, bool forceCreate = false)
		{
			CreateTab(new TableEditor(fileName, bytes, codePage, modified: modified), tableEditTabs, forceCreate);
		}

		public static object Create(object tableViewer, List<DbDataReader> readers)
		{
			if (!readers.Any())
				return tableViewer;

			var tableEditTabs = tableViewer as TableEditTabs;
			if ((tableEditTabs != null) && (!tableEditTabs.IsLoaded))
				tableEditTabs = null;
			if (tableEditTabs == null)
				tableEditTabs = new TableEditTabs();

			tableEditTabs.ItemTabs.RemoveAll();
			tableEditTabs.ItemTabs.Tiles = true;

			foreach (var reader in readers)
				CreateTab(new TableEditor(reader: reader), tableEditTabs);

			return tableEditTabs;
		}

		TableEditTabs()
		{
			TableEditMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command));
			InitializeComponent();
			ItemTabs = tabs;
			UIHelper.AuditMenu(menu);
		}

		const string quickMacroFilename = "QuickTable.xml";
		void Command_Macro_Record_QuickRecord()
		{
			if (recordingMacro == null)
				Command_Macro_Record_Record();
			else
				Command_Macro_Record_StopRecording(quickMacroFilename);
		}

		void Command_Macro_Record_Record()
		{
			if (recordingMacro != null)
			{
				new Message
				{
					Title = "Error",
					Text = String.Format("Cannot start recording; recording is already in progess."),
					Options = Message.OptionsEnum.Ok,
				}.Show();
				return;
			}

			recordingMacro = new Macro<TableEditCommand>();
		}

		void Command_Macro_Record_StopRecording(string fileName = null)
		{
			if (recordingMacro == null)
			{
				new Message
				{
					Title = "Error",
					Text = String.Format("Cannot stop recording; recording not in progess."),
					Options = Message.OptionsEnum.Ok,
				}.Show();
				return;
			}

			var macro = recordingMacro;
			recordingMacro = null;
			macro.Save(fileName, true);
		}

		void Command_Macro_Play_QuickPlay()
		{
			Macro<TableEditCommand>.Load(quickMacroFilename, true).Play(this, playing => macroPlaying = playing);
		}

		void Command_Macro_Play_Play(string macroFile = null)
		{
			Macro<TableEditCommand>.Load().Play(this, playing => macroPlaying = playing);
		}

		Macro<TableEditCommand> recordingMacro = null, macroPlaying = null;

		void RunCommand(TableEditCommand command)
		{
			if (macroPlaying != null)
				return;

			switch (command)
			{
				case TableEditCommand.Macro_Record_QuickRecord: Command_Macro_Record_QuickRecord(); return;
				case TableEditCommand.Macro_Record_Record: Command_Macro_Record_Record(); return;
				case TableEditCommand.Macro_Record_StopRecording: Command_Macro_Record_StopRecording(); return;
				case TableEditCommand.Macro_Play_QuickPlay: Command_Macro_Play_QuickPlay(); return;
				case TableEditCommand.Macro_Play_Play: Command_Macro_Play_Play(); return;
			}

			var shiftDown = this.shiftDown;

			object dialogResult;
			if (!GetDialogResult(command, out dialogResult))
				return;

			if (recordingMacro != null)
				recordingMacro.AddCommand(command, shiftDown, dialogResult);

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

		void Command_File_Open_Open(OpenFileDialogResult result)
		{
			foreach (var filename in result.files)
				Create(filename, tableEditTabs: this);
		}

		void Command_File_Open_CopiedCut()
		{
			var files = NEClipboard.Strings;

			if ((files.Count > 5) && (new Message
			{
				Title = "Confirm",
				Text = String.Format("Are you sure you want to open these {0} files?", files.Count),
				Options = Message.OptionsEnum.YesNoCancel,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.Cancel,
			}.Show() != Message.OptionsEnum.Yes))
				return;

			foreach (var item in ItemTabs.Items)
				item.Active = false;
			foreach (var file in files)
				Create(file, tableEditTabs: this);
		}

		void Command_View_ActiveTabs()
		{
			tabs.ShowActiveTabsDialog();
		}

		bool GetDialogResult(TableEditCommand command, out object dialogResult)
		{
			dialogResult = null;

			switch (command)
			{
				case TableEditCommand.File_Open_Open: dialogResult = Command_File_Open_Dialog(); break;
				default: return ItemTabs.TopMost == null ? true : ItemTabs.TopMost.GetDialogResult(command, out dialogResult);
			}

			return dialogResult != null;
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (macroPlaying != null)
			{
				if (e.Key == Key.Escape)
					macroPlaying.Stop();
				return;
			}

			base.OnKeyDown(e);
			if (e.Handled)
				return;

			var topMost = ItemTabs.TopMost;
			if (topMost == null)
				return;

			if (!topMost.IsEditing)
			{
				switch (e.Key)
				{
					case Key.F2:
						topMost.StartEdit(false);
						e.Handled = true;
						return;
				}
			}
			else
			{
				switch (e.Key)
				{
					case Key.Enter:
					case Key.Escape:
						var result = topMost.EndEdit(e.Key == Key.Enter);
						if (result != null)
							DoHandleText(result);
						e.Handled = true;
						return;
				}
			}

			if (topMost.IsEditing)
				return;

			var shiftDown = this.shiftDown;
			var controlDown = this.controlDown;
			var altDown = this.altDown;

			var key = e.Key == Key.System ? e.SystemKey : e.Key;
			e.Handled = HandleKey(key, shiftDown, controlDown, altDown);

			if ((recordingMacro != null) && (e.Handled))
				recordingMacro.AddKey(key, shiftDown, controlDown, altDown);
		}

		bool DoHandleText(string text)
		{
			if (recordingMacro != null)
				recordingMacro.AddText(text);
			return HandleText(text);
		}

		public override bool HandleText(string text)
		{
			var result = false;
			foreach (var tableEditorItems in ItemTabs.Items.Where(item => item.Active).ToList())
				result = tableEditorItems.HandleText(text) || result;
			return result;
		}

		public override bool HandleKey(Key key, bool shiftDown, bool controlDown, bool altDown)
		{
			var result = false;
			foreach (var tableEditorItems in ItemTabs.Items.Where(item => item.Active).ToList())
				result = tableEditorItems.HandleKey(key, shiftDown, controlDown, altDown) || result;
			return result;
		}

		public override bool HandleCommand(TableEditCommand command, bool shiftDown, object dialogResult)
		{
			switch (command)
			{
				case TableEditCommand.File_New: Create(tableEditTabs: this, forceCreate: shiftDown); break;
				case TableEditCommand.File_Open_Open: Command_File_Open_Open(dialogResult as OpenFileDialogResult); break;
				case TableEditCommand.File_Open_CopiedCut: Command_File_Open_CopiedCut(); break;
				case TableEditCommand.File_Exit: Close(); break;
			}

			foreach (var textEditorItem in ItemTabs.Items.Where(item => item.Active).ToList())
				textEditorItem.HandleCommand(command, shiftDown, dialogResult);

			return true;
		}
	}
}
