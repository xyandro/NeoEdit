﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Xml.Linq;
using Microsoft.Win32;
using NeoEdit.Common;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;
using NeoEdit.GUI.Misc;

namespace NeoEdit.Tables
{
	public class Tabs : Tabs<TableEditor, TablesCommand> { }
	public class TabsWindow : TabsWindow<TableEditor, TablesCommand> { }

	partial class TablesTabs
	{
		static TablesTabs() { UIHelper<TablesTabs>.Register(); }

		public static void Create(string fileName = null, TablesTabs tablesTabs = null, bool forceCreate = false)
		{
			CreateTab(new TableEditor(fileName), tablesTabs, forceCreate);
		}

		TablesTabs()
		{
			TablesMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command));
			InitializeComponent();
			ItemTabs = tabs;
			UIHelper.AuditMenu(menu);
		}

		void RunCommand(TablesCommand command)
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

		void Command_File_Open_Open(OpenFileDialogResult result)
		{
			foreach (var filename in result.files)
				Create(filename, this);
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
				Create(file, this);
		}

		void Command_View_ActiveTabs()
		{
			tabs.ShowActiveTabsDialog();
		}

		bool GetDialogResult(TablesCommand command, out object dialogResult)
		{
			dialogResult = null;

			switch (command)
			{
				case TablesCommand.File_Open_Open: dialogResult = Command_File_Open_Dialog(); break;
				default: return ItemTabs.TopMost == null ? true : ItemTabs.TopMost.GetDialogResult(command, out dialogResult);
			}

			return dialogResult != null;
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
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

			e.Handled = HandleKey(e.Key, shiftDown, controlDown, altDown);
		}

		bool DoHandleText(string text)
		{
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

		public override bool HandleCommand(TablesCommand command, bool shiftDown, object dialogResult)
		{
			switch (command)
			{
				case TablesCommand.File_New: Create(tablesTabs: this, forceCreate: shiftDown); break;
				case TablesCommand.File_Open_Open: Command_File_Open_Open(dialogResult as OpenFileDialogResult); break;
				case TablesCommand.File_Open_CopiedCut: Command_File_Open_CopiedCut(); break;
				case TablesCommand.File_Exit: Close(); break;
			}

			foreach (var textEditorItem in ItemTabs.Items.Where(item => item.Active).ToList())
				textEditorItem.HandleCommand(command, shiftDown, dialogResult);

			return true;
		}
	}
}
