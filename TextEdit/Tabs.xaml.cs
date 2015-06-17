﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using Microsoft.Win32;
using NeoEdit.Common;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Transform;
using NeoEdit.GUI;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	public class Tabs : Tabs<TextEditor> { }

	public partial class TextEditTabs
	{
		[DepProp]
		public ObservableCollection<TextEditor> TextEditors { get { return UIHelper<TextEditTabs>.GetPropValue<ObservableCollection<TextEditor>>(this); } set { UIHelper<TextEditTabs>.SetPropValue(this, value); } }
		[DepProp]
		public TextEditor Active { get { return UIHelper<TextEditTabs>.GetPropValue<TextEditor>(this); } set { UIHelper<TextEditTabs>.SetPropValue(this, value); } }
		[DepProp]
		public Tabs.ViewType View { get { return UIHelper<TextEditTabs>.GetPropValue<Tabs.ViewType>(this); } set { UIHelper<TextEditTabs>.SetPropValue(this, value); } }

		static TextEditTabs() { UIHelper<TextEditTabs>.Register(); }

		public static void Create(string filename = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, bool? modified = null, int line = 1, int column = 1, bool createNew = false, TextEditTabs textEditTabs = null)
		{
			if ((!Helpers.IsDebugBuild) && (filename != null))
			{
				var fileInfo = new FileInfo(filename);
				if (fileInfo.Exists)
				{
					if (fileInfo.Length > 52428800) // 50 MB
					{
						switch (new Message
						{
							Title = "Confirm",
							Text = "The file you are trying to open is very large.  Would you like to open it in the text viewer instead?",
							Options = Message.OptionsEnum.YesNoCancel,
							DefaultAccept = Message.OptionsEnum.Yes,
							DefaultCancel = Message.OptionsEnum.Cancel,
						}.Show())
						{
							case Message.OptionsEnum.Yes: Launcher.Static.LaunchTextViewer(filename); return;
							case Message.OptionsEnum.No: break;
							case Message.OptionsEnum.Cancel: return;
						}
					}
				}
			}

			if ((textEditTabs == null) && (!createNew))
				textEditTabs = UIHelper<TextEditTabs>.GetNewest();

			if (textEditTabs == null)
				textEditTabs = new TextEditTabs();

			textEditTabs.Activate();
			textEditTabs.Add(new TextEditor(filename, bytes, codePage, modified, line, column));
		}

		public void AddTextEditor(string filename = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, int line = 1, int column = 1, bool? modified = null)
		{
			Create(filename, bytes, codePage, modified, line, column, false, this);
		}

		TextEditTabs()
		{
			TextEditMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command));
			InitializeComponent();
			UIHelper.AuditMenu(menu);

			TextEditors = new ObservableCollection<TextEditor>();
			AllowDrop = true;
			Drop += TextEditTabs_Drop;
		}

		void TextEditTabs_Drop(object sender, System.Windows.DragEventArgs e)
		{
			var fileList = e.Data.GetData("FileDrop") as string[];
			if (fileList == null)
				return;
			foreach (var file in fileList)
				Create(file);
			e.Handled = true;
		}

		class OpenFileDialogResult
		{
			public List<string> files { get; set; }
		}

		OpenFileDialogResult Command_File_Open_Dialog(string initialDirectory = null)
		{
			if ((initialDirectory == null) && (Active != null))
				initialDirectory = Path.GetDirectoryName(Active.FileName);
			var dialog = new OpenFileDialog
			{
				DefaultExt = "txt",
				Filter = "Text files|*.txt|All files|*.*",
				FilterIndex = 2,
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
				AddTextEditor(filename);
		}

		void Command_File_OpenCopiedCutFiles()
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

			foreach (var file in files)
				AddTextEditor(file);
		}

		void Command_View_WordList()
		{
			var type = GetType();
			byte[] data;
			using (var stream = type.Assembly.GetManifestResourceStream(type.Namespace + ".Misc.Words.txt.gz"))
			using (var ms = new MemoryStream())
			{
				stream.CopyTo(ms);
				data = ms.ToArray();
			}

			data = Compression.Decompress(Compression.Type.GZip, data);
			data = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(data).Replace("\n", "\r\n"));
			AddTextEditor(bytes: data, modified: false);
		}

		const string quickMacroFilename = "Quick.xml";
		void Command_Macro_QuickRecord()
		{
			if (recordingMacro == null)
				Command_Macro_Record();
			else
				Command_Macro_StopRecording(quickMacroFilename);
		}

		void Command_Macro_QuickPlay()
		{
			Command_Macro_Play(quickMacroFilename);
		}

		void Command_Macro_Record()
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

			recordingMacro = new Macro();
		}

		string macroDirectory = Path.Combine(Path.GetDirectoryName(typeof(TextEditTabs).Assembly.Location), "Macro");
		void Command_Macro_StopRecording(string fileName = null)
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

			Directory.CreateDirectory(macroDirectory);
			if (fileName == null)
			{
				var dialog = new SaveFileDialog
				{
					DefaultExt = "xml",
					Filter = "Macro files|*.xml|All files|*.*",
					FileName = "Macro.xml",
					InitialDirectory = macroDirectory,
				};
				if (dialog.ShowDialog() != true)
					return;

				fileName = dialog.FileName;
			}
			else
				fileName = Path.Combine(macroDirectory, fileName);

			XMLConverter.ToXML(macro).Save(fileName);
		}

		string ChooseMacro()
		{
			var dialog = new OpenFileDialog
			{
				DefaultExt = "xml",
				Filter = "Macro files|*.xml|All files|*.*",
				InitialDirectory = macroDirectory,
			};
			if (dialog.ShowDialog() != true)
				return null;
			return dialog.FileName;
		}

		void Command_Macro_Play(string macroFile = null)
		{
			if (macroFile == null)
			{
				macroFile = ChooseMacro();
				if (macroFile == null)
					return;
			}
			else
				macroFile = Path.Combine(macroDirectory, macroFile);

			XMLConverter.FromXML<Macro>(XElement.Load(macroFile)).Play(this, playing => macroPlaying = playing);
		}

		void Command_Macro_PlayOnCopiedFiles()
		{
			var files = new Queue<string>(NEClipboard.Strings);
			var macroFile = ChooseMacro();
			if (macroFile == null)
				return;

			var macro = XMLConverter.FromXML<Macro>(XElement.Load(macroFile));
			Action startNext = null;
			startNext = () =>
			{
				if (!files.Any())
					return;
				AddTextEditor(files.Dequeue());
				macro.Play(this, playing => macroPlaying = playing, startNext);
			};
			startNext();
		}

		void Command_Macro_Repeat()
		{
			var result = MacroRepeatDialog.Run(this, ChooseMacro);
			if (result == null)
				return;

			var macro = XMLConverter.FromXML<Macro>(XElement.Load(result.Macro));
			var expression = new NEExpression(result.Expression);
			var count = int.MaxValue;
			if (result.RepeatType == MacroRepeatDialog.RepeatTypeEnum.Number)
				count = expression.Evaluate<int>();

			Action startNext = null;
			startNext = () =>
			{
				if ((Active == null) || (--count < 0))
					return;

				if (result.RepeatType == MacroRepeatDialog.RepeatTypeEnum.Condition)
					if (!expression.EvaluateRow<bool>(Active.GetExpressionData(expression: expression), 0))
						return;

				macro.Play(this, playing => macroPlaying = playing, startNext);
			};
			startNext();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			var answer = Message.OptionsEnum.None;
			var active = Active;
			foreach (var textEditor in TextEditors)
			{
				Active = textEditor;
				if (!textEditor.CanClose(ref answer))
				{
					e.Cancel = true;
					return;
				}
			}
			Active = active;
			base.OnClosing(e);
		}

		bool shiftDown { get { return (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None; } }
		bool controlDown { get { return (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None; } }

		Macro recordingMacro;
		internal Macro macroPlaying = null;

		internal void RunCommand(TextEditCommand command)
		{
			if (macroPlaying != null)
				return;

			switch (command)
			{
				case TextEditCommand.Macro_QuickRecord: Command_Macro_QuickRecord(); return;
				case TextEditCommand.Macro_QuickPlay: Command_Macro_QuickPlay(); return;
				case TextEditCommand.Macro_Record: Command_Macro_Record(); return;
				case TextEditCommand.Macro_StopRecording: Command_Macro_StopRecording(); return;
				case TextEditCommand.Macro_Play: Command_Macro_Play(); return;
				case TextEditCommand.Macro_PlayOnCopiedFiles: Command_Macro_PlayOnCopiedFiles(); return;
				case TextEditCommand.Macro_Repeat: Command_Macro_Repeat(); return;
			}

			var shiftDown = this.shiftDown;

			object dialogResult;
			if (!GetDialogResult(command, out dialogResult))
				return;

			if (recordingMacro != null)
				recordingMacro.AddCommand(command, shiftDown, dialogResult);

			HandleCommand(command, shiftDown, dialogResult);
		}

		internal bool GetDialogResult(TextEditCommand command, out object dialogResult)
		{
			dialogResult = null;

			switch (command)
			{
				case TextEditCommand.File_Open: dialogResult = Command_File_Open_Dialog(); break;
				case TextEditCommand.Macro_Open: dialogResult = Command_File_Open_Dialog(macroDirectory); break;
				default: return Active == null ? true : Active.GetDialogResult(command, out dialogResult);
			}

			return dialogResult != null;
		}

		internal void HandleCommand(TextEditCommand command, bool shiftDown, object dialogResult)
		{
			switch (command)
			{
				case TextEditCommand.File_New: Create(createNew: shiftDown, textEditTabs: shiftDown ? null : this); break;
				case TextEditCommand.File_Open: Command_File_Open(dialogResult as OpenFileDialogResult); break;
				case TextEditCommand.File_OpenCopiedCutFiles: Command_File_OpenCopiedCutFiles(); break;
				case TextEditCommand.File_Exit: Close(); break;
				case TextEditCommand.Macro_Open: Command_File_Open(dialogResult as OpenFileDialogResult); return;
				case TextEditCommand.View_Tiles: View = View == Tabs.ViewType.Tiles ? Tabs.ViewType.Tabs : Tabs.ViewType.Tiles; break;
				case TextEditCommand.View_WordList: Command_View_WordList(); break;
			}

			if (Active != null)
				Active.HandleCommand(command, shiftDown, dialogResult);
		}

		void Add(TextEditor textEditor)
		{
			if ((!textEditor.Empty()) && (Active != null) && (Active.Empty()))
				TextEditors[TextEditors.IndexOf(Active)] = textEditor;
			else
				TextEditors.Add(textEditor);
			Active = textEditor;
		}

		internal void Remove(TextEditor textEditor, bool closeIfLast = false)
		{
			TextEditors.Remove(textEditor);
			if ((closeIfLast) && (TextEditors.Count == 0))
				Close();
		}

		Label GetLabel(TextEditor textEditor)
		{
			var label = textEditor.GetLabel();
			label.Drop += TextEditTabs_Drop;
			return label;
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

			var shiftDown = this.shiftDown;
			var controlDown = this.controlDown;

			e.Handled = HandleKey(e.Key, shiftDown, controlDown);

			if ((recordingMacro != null) && (e.Handled))
				recordingMacro.AddKey(e.Key, shiftDown, controlDown);
		}

		internal bool HandleKey(Key key, bool shiftDown, bool controlDown)
		{
			if (Active == null)
				return false;
			return Active.HandleKey(key, shiftDown, controlDown);
		}

		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			if (macroPlaying != null)
				return;

			base.OnTextInput(e);
			if (e.Handled)
				return;

			if (e.Source is MenuItem)
				return;

			e.Handled = HandleText(e.Text);

			if ((recordingMacro != null) && (e.Handled))
				recordingMacro.AddText(e.Text);
		}

		internal bool HandleText(string text)
		{
			if (Active == null)
				return false;
			return Active.HandleText(text);
		}
	}
}
