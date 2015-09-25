using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;
using NeoEdit.TextView.Dialogs;

namespace NeoEdit.TextView
{
	public class Tabs : Tabs<TextViewer> { }

	partial class TextViewerTabs
	{
		[DepProp]
		public ObservableCollection<TextViewer> TextViewers { get { return UIHelper<TextViewerTabs>.GetPropValue<ObservableCollection<TextViewer>>(this); } set { UIHelper<TextViewerTabs>.SetPropValue(this, value); } }
		[DepProp]
		public TextViewer Active { get { return UIHelper<TextViewerTabs>.GetPropValue<TextViewer>(this); } set { UIHelper<TextViewerTabs>.SetPropValue(this, value); } }
		[DepProp]
		public bool Tiles { get { return UIHelper<TextViewerTabs>.GetPropValue<bool>(this); } set { UIHelper<TextViewerTabs>.SetPropValue(this, value); } }

		static TextViewerTabs() { UIHelper<TextViewerTabs>.Register(); }

		public static void Create(string filename = null, bool createNew = false)
		{
			((!createNew ? UIHelper<TextViewerTabs>.GetNewest() : null) ?? new TextViewerTabs()).Add(filename);
		}

		TextViewerTabs()
		{
			TextViewMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command));
			InitializeComponent();
			UIHelper.AuditMenu(menu);

			TextViewers = new ObservableCollection<TextViewer>();
			Show(); // Explicitly show because sometimes the loading file dialog will put up first and be hidden
		}

		void Command_File_NewWindow()
		{
			new TextViewerTabs();
		}

		void Command_File_Open()
		{
			var dir = Active != null ? Path.GetDirectoryName(Active.FileName) : null;
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
				Text = String.Format("Are you sure you want to open these {0} files?", files.Count),
				Options = Message.OptionsEnum.YesNoCancel,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.Cancel,
			}.Show() != Message.OptionsEnum.Yes))
				return;

			Add(files);
		}

		void Command_File_Combine()
		{
			var result = CombineDialog.Run(UIHelper.FindParent<Window>(this), false);
			if (result == null)
				return;

			TextData.CombineFiles(result.OutputFile, result.Files, () => { if (result.OpenFile) Add(result.OutputFile); });
		}

		void Command_File_Merge()
		{
			var result = CombineDialog.Run(UIHelper.FindParent<Window>(this), true);
			if (result == null)
				return;

			TextData.MergeFiles(result.OutputFile, result.Files, () => { if (result.OpenFile) Add(result.OutputFile); });
		}

		void Command_File_Encoding()
		{
			var result = ChangeEncodingDialog.Run(UIHelper.FindParent<Window>(this));
			if (result == null)
				return;

			TextData.SaveEncoding(result.InputFile, result.OutputFile, result.OutputCodePage);
		}

		bool shiftDown { get { return (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None; } }
		bool controlDown { get { return (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None; } }

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

			if (Active == null)
				return;

			switch (command)
			{
				case TextViewCommand.File_Close: Active.Dispose(); TextViewers.Remove(Active); break;
				case TextViewCommand.File_CopyPath: Active.Command_File_CopyPath(); break;
				case TextViewCommand.File_Split: Active.Command_File_Split(); break;
				case TextViewCommand.Edit_Copy: Active.Command_Edit_Copy(); break;
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
				TextViewers.Add(add);
				Active = add;
			}));
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Handled)
				return;

			var shiftDown = this.shiftDown;
			var controlDown = this.controlDown;

			e.Handled = HandleKey(e.Key, shiftDown, controlDown);
		}

		internal bool HandleKey(Key key, bool shiftDown, bool controlDown)
		{
			if (Active == null)
				return false;
			return Active.HandleKey(key, shiftDown, controlDown);
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

		internal bool HandleText(string text)
		{
			if (Active == null)
				return false;
			return Active.HandleText(text);
		}
	}
}
