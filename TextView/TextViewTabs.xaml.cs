using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using NeoEdit.Common.NEClipboards;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;
using NeoEdit.TextView.Dialogs;

namespace NeoEdit.TextView
{
	public class TabsWindow : TabsWindow<TextViewer, TextViewCommand> { }
	public class Tabs : Tabs<TextViewer, TextViewCommand> { }

	partial class TextViewTabs
	{
		static TextViewTabs() { UIHelper<TextViewTabs>.Register(); }

		public static void Create(string filename = null, bool forceCreate = false) => ((!forceCreate ? UIHelper<TextViewTabs>.GetNewest() : null) ?? new TextViewTabs()).Add(filename);

		TextViewTabs()
		{
			TextViewMenuItem.RegisterCommands(this, (command, multiStatus) => RunCommand(command));
			InitializeComponent();
			ItemTabs = tabs;
			UIHelper.AuditMenu(menu);

			Show(); // Explicitly show because sometimes the loading file dialog will put up first and be hidden
		}

		void Command_File_NewWindow() => new TextViewTabs();

		void Command_File_Open()
		{
			var dir = ItemTabs.TopMost != null ? Path.GetDirectoryName(ItemTabs.TopMost.FileName) : null;
			var dialog = new OpenFileDialog
			{
				DefaultExt = "txt",
				Filter = "Text files|*.txt|All files|*.*",
				FilterIndex = 2,
				Multiselect = true,
				InitialDirectory = dir,
			};
			if (dialog.ShowDialog() != true)
				return;

			Add(dialog.FileNames.ToList());
		}

		void Command_File_OpenCopiedCutFiles()
		{
			var files = NEClipboard.Strings;
			if (files.Count == 0)
				return;

			if ((files.Count > 5) && (new Message
			{
				Title = "Confirm",
				Text = $"Are you sure you want to open these {files.Count} files?",
				Options = Message.OptionsEnum.YesNoCancel,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.Cancel,
			}.Show() != Message.OptionsEnum.Yes))
				return;

			Add(files);
		}

		void Command_File_Combine()
		{
			var result = CombineDialog.Run(this, false);
			if (result == null)
				return;

			TextData.CombineFiles(result.OutputFile, result.Files, () => { if (result.OpenFile) Add(result.OutputFile); });
		}

		void Command_File_Merge()
		{
			var result = CombineDialog.Run(this, true);
			if (result == null)
				return;

			TextData.MergeFiles(result.OutputFile, result.Files, () => { if (result.OpenFile) Add(result.OutputFile); });
		}

		void Command_File_Encoding()
		{
			var result = ChangeEncodingDialog.Run(this);
			if (result == null)
				return;

			TextData.SaveEncoding(result.InputFile, result.OutputFile, result.OutputCodePage);
		}

		internal void RunCommand(TextViewCommand command)
		{
			var shiftDown = this.shiftDown;

			switch (command)
			{
				case TextViewCommand.File_NewWindow: Command_File_NewWindow(); break;
				case TextViewCommand.File_Open: Command_File_Open(); break;
				case TextViewCommand.File_OpenCopiedCutFiles: Command_File_OpenCopiedCutFiles(); break;
				case TextViewCommand.File_Combine: Command_File_Combine(); break;
				case TextViewCommand.File_Merge: Command_File_Merge(); break;
				case TextViewCommand.File_Encoding: Command_File_Encoding(); break;
				case TextViewCommand.File_Exit: Close(); break;
			}

			if (ItemTabs.TopMost == null)
				return;

			switch (command)
			{
				case TextViewCommand.File_Close: Remove(ItemTabs.TopMost); break;
				case TextViewCommand.File_CopyPath: ItemTabs.TopMost.Command_File_CopyPath(); break;
				case TextViewCommand.File_Split: ItemTabs.TopMost.Command_File_Split(); break;
				case TextViewCommand.Edit_Copy: ItemTabs.TopMost.Command_Edit_Copy(); break;
			}
		}

		void Add(string fileName)
		{
			if (String.IsNullOrEmpty(fileName))
				return;
			Add(new List<string> { fileName });
		}

		void Add(List<string> fileNames)
		{
			if ((fileNames == null) || (fileNames.Count == 0))
				return;

			var start = DateTime.Now;
			TextData.ReadFiles(fileNames, (data, cancelled) => Dispatcher.Invoke(() =>
			{
				if (cancelled)
				{
					var end = DateTime.Now;
					if (((end - start).TotalSeconds < 10) || (new Message
					{
						Title = "Confirm",
						Text = "File processing cancelled.  Open the processed portion?",
						Options = Message.OptionsEnum.YesNoCancel,
						DefaultAccept = Message.OptionsEnum.Yes,
						DefaultCancel = Message.OptionsEnum.Cancel,
					}.Show() != Message.OptionsEnum.Yes))
					{
						data.Dispose();
						return;
					}
				}

				var add = new TextViewer(data);
				ItemTabs.Items.Add(add);
				ItemTabs.TopMost = add;
			}));
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Handled)
				return;

			var shiftDown = this.shiftDown;
			var controlDown = this.controlDown;
			var altDown = this.altDown;

			e.Handled = HandleKey(e.Key, shiftDown, controlDown, altDown);
		}

		public override bool HandleKey(Key key, bool shiftDown, bool controlDown, bool altDown)
		{
			if (ItemTabs.TopMost == null)
				return false;
			return ItemTabs.TopMost.HandleKey(key, shiftDown, controlDown, altDown);
		}

		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			base.OnTextInput(e);
			if (e.Handled)
				return;

			if (e.Source is MenuItem)
				return;

			e.Handled = HandleText(e.Text);
		}

		public override bool HandleText(string text)
		{
			if (ItemTabs.TopMost == null)
				return false;
			return ItemTabs.TopMost.HandleText(text);
		}
	}
}
