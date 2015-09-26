using System;
using System.Collections.Generic;
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
	public class TabsWindow : TabsWindow<TextEditor> { }

	public partial class TextEditTabs
	{
		static TextEditTabs() { UIHelper<TextEditTabs>.Register(); }

		public static void Create(string filename = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, bool? modified = null, int line = 1, int column = 1, TextEditTabs textEditTabs = null, bool forceCreate = false)
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

			var textEditor = new TextEditor(filename, bytes, codePage, modified, line, column);
			CreateTab(textEditor, textEditTabs, forceCreate);
		}

		public static void CreateDiff(string filename1, string filename2)
		{
			var textEdit1 = new TextEditor(filename1);
			var textEdit2 = new TextEditor(filename2);
			var textEditTabs = new TextEditTabs();
			textEditTabs.ItemTabs.Tiles = true;
			textEditTabs.tabs.CreateTab(textEdit1);
			textEditTabs.tabs.CreateTab(textEdit2);
			textEditTabs.ItemTabs.TopMost = textEdit1;
			textEdit1.DiffTarget = textEdit2;
		}

		public void AddTextEditor(string filename = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, int line = 1, int column = 1, bool? modified = null)
		{
			Create(filename, bytes, codePage, modified, line, column, this);
		}

		TextEditTabs()
		{
			TextEditMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command));
			InitializeComponent();
			ItemTabs = tabs;
			UIHelper.AuditMenu(menu);

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

		OpenFileDialogResult Command_File_Open_Open_Dialog(string initialDirectory = null)
		{
			if ((initialDirectory == null) && (ItemTabs.TopMost != null))
				initialDirectory = Path.GetDirectoryName(ItemTabs.TopMost.FileName);
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

		void Command_File_Open_Open(OpenFileDialogResult result)
		{
			foreach (var filename in result.files)
				AddTextEditor(filename);
		}

		void Command_File_Copy_AllPaths()
		{
			var names = ItemTabs.Items.Select(editor => editor.FileName).Where(name => !String.IsNullOrEmpty(name)).ToList();
			NEClipboard.Set(names, String.Join(" ", names));
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
				AddTextEditor(file);
		}

		void Command_Edit_Copy_AllFiles()
		{
			var data = new List<string>();
			foreach (var textEditorData in ItemTabs.Items)
				if (textEditorData.Active)
					data.AddRange(textEditorData.GetSelectionStrings());
			NEClipboard.Set(data, String.Join(" ", data));
		}

		void Command_Edit_Paste_AllFiles()
		{
			var strs = NEClipboard.Strings;
			var active = ItemTabs.Items.Where(data => data.Active).ToList(); ;
			if (strs.Count != active.Count)
				throw new Exception("Clipboard count and active editor count must match");
			for (var ctr = 0; ctr < strs.Count; ++ctr)
				active[ctr].Command_Edit_Paste_AllFiles(strs[ctr], shiftDown);
		}

		void Command_Edit_Diff_Diff()
		{
			List<TextEditor> diffTargets;
			if (ItemTabs.Items.Count == 2)
				diffTargets = ItemTabs.Items.ToList();
			else
				diffTargets = ItemTabs.Items.Where(data => data.Active).ToList();
			if (diffTargets.Count != 2)
				throw new Exception("Must have two files active for diff.");
			diffTargets[0].DiffTarget = diffTargets[1];
		}

		void Command_View_ActiveTabs()
		{
			tabs.ShowActiveTabsDialog();
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

			data = Compressor.Decompress(data, Compressor.Type.GZip);
			data = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(data).Replace("\n", "\r\n"));
			AddTextEditor(bytes: data, modified: false);
		}

		void Command_Macro_Open_Quick()
		{
			AddTextEditor(Path.Combine(macroDirectory, quickMacroFilename));
		}

		const string quickMacroFilename = "Quick.xml";
		void Command_Macro_Record_QuickRecord()
		{
			if (recordingMacro == null)
				Command_Macro_Record_Record();
			else
				Command_Macro_Record_StopRecording(quickMacroFilename);
		}

		void Command_Macro_Play_QuickPlay()
		{
			Command_Macro_Play_Play(quickMacroFilename);
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

			recordingMacro = new Macro();
		}

		readonly string macroDirectory = Path.Combine(Helpers.NeoEditAppData, "Macro");
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

		void Command_Macro_Play_Play(string macroFile = null)
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

		void Command_Macro_Play_PlayOnCopiedFiles()
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

		void Command_Macro_Play_Repeat()
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
				if ((ItemTabs.TopMost == null) || (--count < 0))
					return;

				if (result.RepeatType == MacroRepeatDialog.RepeatTypeEnum.Condition)
					if (!expression.EvaluateRow<bool>(ItemTabs.TopMost.GetExpressionData(expression: expression), 0))
						return;

				macro.Play(this, playing => macroPlaying = playing, startNext);
			};
			startNext();
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
				case TextEditCommand.Macro_Record_QuickRecord: Command_Macro_Record_QuickRecord(); return;
				case TextEditCommand.Macro_Record_Record: Command_Macro_Record_Record(); return;
				case TextEditCommand.Macro_Record_StopRecording: Command_Macro_Record_StopRecording(); return;
				case TextEditCommand.Macro_Play_QuickPlay: Command_Macro_Play_QuickPlay(); return;
				case TextEditCommand.Macro_Play_Play: Command_Macro_Play_Play(); return;
				case TextEditCommand.Macro_Play_Repeat: Command_Macro_Play_Repeat(); return;
				case TextEditCommand.Macro_Play_PlayOnCopiedFiles: Command_Macro_Play_PlayOnCopiedFiles(); return;
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
				case TextEditCommand.File_Open_Open: dialogResult = Command_File_Open_Open_Dialog(); break;
				case TextEditCommand.Macro_Open_Open: dialogResult = Command_File_Open_Open_Dialog(macroDirectory); break;
				default: return ItemTabs.TopMost == null ? true : ItemTabs.TopMost.GetDialogResult(command, out dialogResult);
			}

			return dialogResult != null;
		}

		internal void HandleCommand(TextEditCommand command, bool shiftDown, object dialogResult)
		{
			switch (command)
			{
				case TextEditCommand.File_New: Create(textEditTabs: this, forceCreate: shiftDown); break;
				case TextEditCommand.File_Open_Open: Command_File_Open_Open(dialogResult as OpenFileDialogResult); break;
				case TextEditCommand.File_Open_CopiedCut: Command_File_Open_CopiedCut(); break;
				case TextEditCommand.File_Copy_AllPaths: Command_File_Copy_AllPaths(); break;
				case TextEditCommand.File_Exit: Close(); break;
				case TextEditCommand.Edit_Copy_AllFiles: Command_Edit_Copy_AllFiles(); break;
				case TextEditCommand.Edit_Paste_AllFiles: Command_Edit_Paste_AllFiles(); break;
				case TextEditCommand.Edit_Diff_Diff: Command_Edit_Diff_Diff(); break;
				case TextEditCommand.View_ActiveTabs: Command_View_ActiveTabs(); break;
				case TextEditCommand.View_WordList: Command_View_WordList(); break;
				case TextEditCommand.Macro_Open_Quick: Command_Macro_Open_Quick(); return;
				case TextEditCommand.Macro_Open_Open: Command_File_Open_Open(dialogResult as OpenFileDialogResult); return;
			}

			foreach (var textEditorItem in ItemTabs.Items.Where(item => item.Active).ToList())
				textEditorItem.HandleCommand(command, shiftDown, dialogResult);
		}

		public int GetIndex(TextEditor textEditor)
		{
			var index = ItemTabs.Items.IndexOf(textEditor);
			if (index == -1)
				throw new ArgumentException("Not found");
			return index;
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
			var result = false;
			foreach (var textEditorItems in ItemTabs.Items.Where(item => item.Active).ToList())
				result = textEditorItems.HandleKey(key, shiftDown, controlDown) || result;
			return result;
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
			var result = false;
			foreach (var textEditorItems in ItemTabs.Items.Where(item => item.Active).ToList())
				result = textEditorItems.HandleText(text) || result;
			return result;
		}
	}
}
