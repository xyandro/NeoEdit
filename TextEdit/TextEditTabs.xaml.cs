using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using NeoEdit.Common;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.NEClipboards;
using NeoEdit.Common.Transform;
using NeoEdit.GUI;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;
using NeoEdit.GUI.Misc;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	public class Tabs : Tabs<TextEditor, TextEditCommand> { }
	public class TabsWindow : TabsWindow<TextEditor, TextEditCommand> { }

	partial class TextEditTabs
	{
		static TextEditTabs() { UIHelper<TextEditTabs>.Register(); }

		public static void Create(string fileName = null, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, bool? modified = null, int line = 1, int column = 1, TextEditTabs textEditTabs = null, bool forceCreate = false)
		{
			if ((!Helpers.IsDebugBuild) && (fileName != null))
			{
				var fileInfo = new FileInfo(fileName);
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
							case Message.OptionsEnum.Yes: Launcher.Static.LaunchTextViewer(fileName); return;
							case Message.OptionsEnum.No: break;
							case Message.OptionsEnum.Cancel: return;
						}
					}
				}
			}

			var textEditor = new TextEditor(fileName, displayName, bytes, codePage, modified, line, column);
			var replaced = CreateTab(textEditor, textEditTabs, forceCreate);
			textEditor.DiffTarget = replaced?.DiffTarget;
		}

		public static TextEditTabs CreateDiff()
		{
			var textEditTabs = new TextEditTabs();
			textEditTabs.ItemTabs.Layout = TabsLayout.Grid;
			textEditTabs.ItemTabs.Columns = 2;
			return textEditTabs;
		}

		public void AddDiff(string fileName1 = null, string displayName1 = null, byte[] bytes1 = null, Coder.CodePage codePage1 = Coder.CodePage.AutoByBOM, bool? modified1 = null, int? line1 = null, int? column1 = null, string fileName2 = null, string displayName2 = null, byte[] bytes2 = null, Coder.CodePage codePage2 = Coder.CodePage.AutoByBOM, bool? modified2 = null, int? line2 = null, int? column2 = null)
		{
			var textEdit1 = new TextEditor(fileName1, displayName1, bytes1, codePage1, modified1, line1, column1);
			var textEdit2 = new TextEditor(fileName2, displayName2, bytes2, codePage2, modified2, line2, column2);
			tabs.CreateTab(textEdit1);
			tabs.CreateTab(textEdit2);
			ItemTabs.TopMost = textEdit2;
			textEdit1.DiffTarget = textEdit2;
		}

		public void AddTextEditor(string fileName = null, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, int line = 1, int column = 1, bool? modified = null) => Create(fileName, displayName, bytes, codePage, modified, line, column, this);

		TextEditTabs()
		{
			TextEditMenuItem.RegisterCommands(this, (command, multiStatus) => RunCommand(command, multiStatus));
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

		void Command_File_Copy_AllPaths() => NEClipboard.CopiedFiles = ItemTabs.Items.Select(editor => editor.FileName).Where(name => !String.IsNullOrEmpty(name)).ToList();

		void Command_File_Open_CopiedCut()
		{
			var files = NEClipboard.Strings;

			if ((files.Count > 5) && (new Message
			{
				Title = "Confirm",
				Text = $"Are you sure you want to open these {files.Count} files?",
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
			NEClipboard.Strings = data;
		}

		void Command_Edit_Paste_AllFiles()
		{
			var strs = NEClipboard.Strings;
			var active = ItemTabs.Items.Where(data => data.Active).ToList();
			if (strs.Count != active.Count)
				throw new Exception("Clipboard count and active editor count must match");
			for (var ctr = 0; ctr < strs.Count; ++ctr)
				active[ctr].Command_Edit_Paste_AllFiles(strs[ctr], shiftDown);
		}

		void Command_Diff_Diff()
		{
			var diffTargets = ItemTabs.Items.Count == 2 ? ItemTabs.Items.ToList() : ItemTabs.Items.Where(data => data.Active).ToList();
			if ((diffTargets.Count == 2) && (diffTargets[0].DiffTarget != diffTargets[1]))
			{
				if (shiftDown)
				{
					if (ItemTabs.Items.Count == 2)
						ItemTabs.Layout = TabsLayout.Grid;
					else
					{
						ItemTabs.Items.Remove(diffTargets[0]);
						ItemTabs.Items.Remove(diffTargets[1]);

						var textEditTabs = new TextEditTabs();
						textEditTabs.ItemTabs.Layout = TabsLayout.Grid;
						textEditTabs.tabs.CreateTab(diffTargets[0]);
						textEditTabs.tabs.CreateTab(diffTargets[1]);
						textEditTabs.ItemTabs.TopMost = diffTargets[0];
					}

				}
				diffTargets[0].DiffTarget = diffTargets[1];
			}
			else if (diffTargets.Any(item => item.DiffTarget != null))
				diffTargets.ForEach(item => item.DiffTarget = null);
			else
				throw new Exception("Must have two files active for diff.");
		}

		CustomGridDialog.Result Command_View_Type_Dialog() => CustomGridDialog.Run(this, ItemTabs.Columns, ItemTabs.Rows);

		void Command_View_Type(TabsLayout layout, CustomGridDialog.Result result) => ItemTabs.SetLayout(layout, result?.Columns ?? 0, result?.Rows ?? 0);

		void Command_View_ActiveTabs() => tabs.ShowActiveTabsDialog();

		void Command_View_SelectTabsWithSelections(bool hasSelections)
		{
			var topMost = ItemTabs.TopMost;
			var active = ItemTabs.Items.Where(tab => (tab.Active) && (tab.NumSelections != 0 == hasSelections)).ToList();
			ItemTabs.Items.ToList().ForEach(tab => tab.Active = false);

			if (!active.Any())
				return;

			if (!active.Contains(topMost))
				topMost = active.First();
			ItemTabs.TopMost = topMost;
			active.ForEach(tab => tab.Active = true);
		}

		void Command_View_WordList()
		{
			var type = GetType();
			byte[] data;
			using (var stream = type.Assembly.GetManifestResourceStream($"{type.Namespace}.Misc.Words.txt.gz"))
			using (var ms = new MemoryStream())
			{
				stream.CopyTo(ms);
				data = ms.ToArray();
			}

			data = Compressor.Decompress(data, Compressor.Type.GZip);
			data = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(data).Replace("\n", "\r\n"));
			AddTextEditor(bytes: data, modified: false);
		}

		string QuickMacro(int num) => $"QuickText{num}.xml";
		void Macro_Open_Quick(int quickNum) => AddTextEditor(Path.Combine(Macro<TextEditCommand>.MacroDirectory, QuickMacro(quickNum)));

		void Command_Macro_Record_Quick(int quickNum)
		{
			if (recordingMacro == null)
				Command_Macro_Record_Record();
			else
				Command_Macro_Record_StopRecording(QuickMacro(quickNum));
		}

		void Command_Macro_Append_Quick(int quickNum)
		{
			if (recordingMacro == null)
				recordingMacro = Macro<TextEditCommand>.Load(QuickMacro(quickNum), true);
			else
				Command_Macro_Record_StopRecording(QuickMacro(quickNum));
		}

		void Command_Macro_Append_Append()
		{
			ValidateNoCurrentMacro();
			recordingMacro = Macro<TextEditCommand>.Load();
		}

		void Command_Macro_Play_Quick(int quickNum) => Macro<TextEditCommand>.Load(QuickMacro(quickNum), true).Play(this, playing => macroPlaying = playing);

		void ValidateNoCurrentMacro()
		{
			if (recordingMacro == null)
				return;

			throw new Exception("Cannot start recording; recording is already in progess.");
		}

		void Command_Macro_Record_Record()
		{
			ValidateNoCurrentMacro();
			recordingMacro = new Macro<TextEditCommand>();
		}

		void Command_Macro_Record_StopRecording(string fileName = null)
		{
			if (recordingMacro == null)
			{
				new Message
				{
					Title = "Error",
					Text = $"Cannot stop recording; recording not in progess.",
					Options = Message.OptionsEnum.Ok,
				}.Show();
				return;
			}

			var macro = recordingMacro;
			recordingMacro = null;
			macro.Save(fileName, true);
		}

		void Command_Macro_Play_Play() => Macro<TextEditCommand>.Load().Play(this, playing => macroPlaying = playing);

		void Command_Macro_Play_PlayOnCopiedFiles()
		{
			var files = new Queue<string>(NEClipboard.Strings);
			var macro = Macro<TextEditCommand>.Load();
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
			var result = MacroRepeatDialog.Run(this, Macro<TextEditCommand>.ChooseMacro);
			if (result == null)
				return;

			var macro = Macro<TextEditCommand>.Load(result.Macro);
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
					if (!expression.EvaluateRow<bool>(ItemTabs.TopMost.GetVariables()))
						return;

				macro.Play(this, playing => macroPlaying = playing, startNext);
			};
			startNext();
		}

		Macro<TextEditCommand> recordingMacro;
		internal Macro<TextEditCommand> macroPlaying = null;

		internal void RunCommand(TextEditCommand command, bool? multiStatus)
		{
			if (macroPlaying != null)
				return;

			switch (command)
			{
				case TextEditCommand.Macro_Record_Quick_6: Command_Macro_Record_Quick(6); return;
				case TextEditCommand.Macro_Record_Quick_7: Command_Macro_Record_Quick(7); return;
				case TextEditCommand.Macro_Record_Quick_8: Command_Macro_Record_Quick(8); return;
				case TextEditCommand.Macro_Record_Quick_9: Command_Macro_Record_Quick(9); return;
				case TextEditCommand.Macro_Record_Quick_10: Command_Macro_Record_Quick(10); return;
				case TextEditCommand.Macro_Record_Quick_11: Command_Macro_Record_Quick(11); return;
				case TextEditCommand.Macro_Record_Quick_12: Command_Macro_Record_Quick(12); return;
				case TextEditCommand.Macro_Record_Record: Command_Macro_Record_Record(); return;
				case TextEditCommand.Macro_Record_StopRecording: Command_Macro_Record_StopRecording(); return;
				case TextEditCommand.Macro_Append_Quick_6: Command_Macro_Append_Quick(6); return;
				case TextEditCommand.Macro_Append_Quick_7: Command_Macro_Append_Quick(7); return;
				case TextEditCommand.Macro_Append_Quick_8: Command_Macro_Append_Quick(8); return;
				case TextEditCommand.Macro_Append_Quick_9: Command_Macro_Append_Quick(9); return;
				case TextEditCommand.Macro_Append_Quick_10: Command_Macro_Append_Quick(10); return;
				case TextEditCommand.Macro_Append_Quick_11: Command_Macro_Append_Quick(11); return;
				case TextEditCommand.Macro_Append_Quick_12: Command_Macro_Append_Quick(12); return;
				case TextEditCommand.Macro_Append_Append: Command_Macro_Append_Append(); return;
				case TextEditCommand.Macro_Play_Quick_6: Command_Macro_Play_Quick(6); return;
				case TextEditCommand.Macro_Play_Quick_7: Command_Macro_Play_Quick(7); return;
				case TextEditCommand.Macro_Play_Quick_8: Command_Macro_Play_Quick(8); return;
				case TextEditCommand.Macro_Play_Quick_9: Command_Macro_Play_Quick(9); return;
				case TextEditCommand.Macro_Play_Quick_10: Command_Macro_Play_Quick(10); return;
				case TextEditCommand.Macro_Play_Quick_11: Command_Macro_Play_Quick(11); return;
				case TextEditCommand.Macro_Play_Quick_12: Command_Macro_Play_Quick(12); return;
				case TextEditCommand.Macro_Play_Play: Command_Macro_Play_Play(); return;
				case TextEditCommand.Macro_Play_Repeat: Command_Macro_Play_Repeat(); return;
				case TextEditCommand.Macro_Play_PlayOnCopiedFiles: Command_Macro_Play_PlayOnCopiedFiles(); return;
			}

			var shiftDown = this.shiftDown;

			object dialogResult;
			if (!GetDialogResult(command, out dialogResult))
				return;

			if (recordingMacro != null)
				recordingMacro.AddCommand(command, shiftDown, dialogResult, multiStatus);

			HandleCommand(command, shiftDown, dialogResult, multiStatus);
		}

		internal bool GetDialogResult(TextEditCommand command, out object dialogResult)
		{
			dialogResult = null;

			switch (command)
			{
				case TextEditCommand.File_Open_Open: dialogResult = Command_File_Open_Open_Dialog(); break;
				case TextEditCommand.View_CustomGrid: dialogResult = Command_View_Type_Dialog(); break;
				case TextEditCommand.Macro_Open_Open: dialogResult = Command_File_Open_Open_Dialog(Macro<TextEditCommand>.MacroDirectory); break;
				default: return ItemTabs.TopMost == null ? true : ItemTabs.TopMost.GetDialogResult(command, out dialogResult);
			}

			return dialogResult != null;
		}

		public override bool HandleCommand(TextEditCommand command, bool shiftDown, object dialogResult, bool? multiStatus)
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
				case TextEditCommand.Diff_Diff: Command_Diff_Diff(); break;
				case TextEditCommand.View_Full: Command_View_Type(TabsLayout.Full, null); break;
				case TextEditCommand.View_Grid: Command_View_Type(TabsLayout.Grid, null); break;
				case TextEditCommand.View_CustomGrid: Command_View_Type(TabsLayout.Grid, dialogResult as CustomGridDialog.Result); break;
				case TextEditCommand.View_ActiveTabs: Command_View_ActiveTabs(); break;
				case TextEditCommand.View_SelectTabsWithSelections: Command_View_SelectTabsWithSelections(true); break;
				case TextEditCommand.View_SelectTabsWithoutSelections: Command_View_SelectTabsWithSelections(false); break;
				case TextEditCommand.View_WordList: Command_View_WordList(); break;
				case TextEditCommand.Macro_Open_Quick_6: Macro_Open_Quick(6); return true;
				case TextEditCommand.Macro_Open_Quick_7: Macro_Open_Quick(7); return true;
				case TextEditCommand.Macro_Open_Quick_8: Macro_Open_Quick(8); return true;
				case TextEditCommand.Macro_Open_Quick_9: Macro_Open_Quick(9); return true;
				case TextEditCommand.Macro_Open_Quick_10: Macro_Open_Quick(10); return true;
				case TextEditCommand.Macro_Open_Quick_11: Macro_Open_Quick(11); return true;
				case TextEditCommand.Macro_Open_Quick_12: Macro_Open_Quick(12); return true;
				case TextEditCommand.Macro_Open_Open: Command_File_Open_Open(dialogResult as OpenFileDialogResult); return true;
			}

			foreach (var textEditorItem in ItemTabs.Items.Where(item => item.Active).ToList())
				textEditorItem.HandleCommand(command, shiftDown, dialogResult, multiStatus);

			return true;
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
			var altDown = this.altDown;

			e.Handled = HandleKey(e.Key, shiftDown, controlDown, altDown);

			if ((recordingMacro != null) && (e.Handled))
				recordingMacro.AddKey(e.Key, shiftDown, controlDown, altDown);
		}

		public override bool HandleKey(Key key, bool shiftDown, bool controlDown, bool altDown)
		{
			var result = false;
			foreach (var textEditorItems in ItemTabs.Items.Where(item => item.Active).ToList())
				result = textEditorItems.HandleKey(key, shiftDown, controlDown, altDown) || result;
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

		public override bool HandleText(string text)
		{
			var result = false;
			foreach (var textEditorItems in ItemTabs.Items.Where(item => item.Active).ToList())
				result = textEditorItems.HandleText(text) || result;
			return result;
		}
	}
}
