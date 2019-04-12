using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using NeoEdit.Common;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.NEClipboards;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;
using NeoEdit.GUI.Misc;
using NeoEdit.TextEdit.Content;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	public class Tabs : Tabs<TextEditor, TextEditCommand> { }
	public class TabsWindow : TabsWindow<TextEditor, TextEditCommand> { }

	partial class TextEditTabs
	{
		[DepProp]
		public string ActiveCountText { get { return UIHelper<TextEditTabs>.GetPropValue<string>(this); } private set { UIHelper<TextEditTabs>.SetPropValue(this, value); } }
		[DepProp]
		public string InactiveCountText { get { return UIHelper<TextEditTabs>.GetPropValue<string>(this); } private set { UIHelper<TextEditTabs>.SetPropValue(this, value); } }
		[DepProp]
		public string TotalCountText { get { return UIHelper<TextEditTabs>.GetPropValue<string>(this); } private set { UIHelper<TextEditTabs>.SetPropValue(this, value); } }
		[DepProp]
		public string ClipboardCountText { get { return UIHelper<TextEditTabs>.GetPropValue<string>(this); } private set { UIHelper<TextEditTabs>.SetPropValue(this, value); } }

		static TextEditTabs() { UIHelper<TextEditTabs>.Register(); }

		public static Window Create(string fileName = null, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, Parser.ParserType contentType = Parser.ParserType.None, bool? modified = null, int line = 1, int column = 1, TextEditTabs textEditTabs = null, bool forceCreate = false, string shutdownEvent = null)
		{
			fileName = fileName?.Trim('"');
			var textEditor = new TextEditor(fileName, displayName, bytes, codePage, contentType, modified, line, column, new ShutdownData(shutdownEvent, 1));
			var replaced = CreateTab(textEditor, textEditTabs, forceCreate);
			textEditor.DiffTarget = replaced.Item1?.DiffTarget;
			return replaced.Item2;
		}

		public Window AddDiff(string fileName1 = null, string displayName1 = null, byte[] bytes1 = null, Coder.CodePage codePage1 = Coder.CodePage.AutoByBOM, Parser.ParserType contentType1 = Parser.ParserType.None, bool? modified1 = null, int? line1 = null, int? column1 = null, string fileName2 = null, string displayName2 = null, byte[] bytes2 = null, Coder.CodePage codePage2 = Coder.CodePage.AutoByBOM, Parser.ParserType contentType2 = Parser.ParserType.None, bool? modified2 = null, int? line2 = null, int? column2 = null, string shutdownEvent = null)
		{
			var shutdownData = new ShutdownData(shutdownEvent, 2);
			var textEdit1 = new TextEditor(fileName1, displayName1, bytes1, codePage1, contentType1, modified1, line1, column1, shutdownData);
			var textEdit2 = new TextEditor(fileName2, displayName2, bytes2, codePage2, contentType2, modified2, line2, column2, shutdownData);
			if (textEdit1.ContentType == Parser.ParserType.None)
				textEdit1.ContentType = textEdit2.ContentType;
			if (textEdit2.ContentType == Parser.ParserType.None)
				textEdit2.ContentType = textEdit1.ContentType;
			tabs.CreateTab(textEdit1);
			tabs.CreateTab(textEdit2);
			ItemTabs.TopMost = textEdit2;
			textEdit1.DiffTarget = textEdit2;
			ItemTabs.Layout = TabsLayout.Grid;
			if (ItemTabs.Items.Count > 2)
				ItemTabs.Columns = 2;
			return this;
		}

		public void AddTextEditor(string fileName = null, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, Parser.ParserType contentType = Parser.ParserType.None, int line = 1, int column = 1, bool? modified = null) => Create(fileName, displayName, bytes, codePage, contentType, modified, line, column, this);

		readonly RunOnceTimer doActivatedTimer, countsTimer, clipboardTimer;
		public TextEditTabs()
		{
			TextEditMenuItem.RegisterCommands(this, (command, multiStatus) => RunCommand(command, multiStatus));
			InitializeComponent();
			ItemTabs = tabs;
			UIHelper.AuditMenu(menu);

			AllowDrop = true;
			Drop += OnDrop;
			doActivatedTimer = new RunOnceTimer(() => DoActivated());
			countsTimer = new RunOnceTimer(() => UpdateStatusBarText());
			clipboardTimer = new RunOnceTimer(() => UpdateClipboards());
			ItemTabs.TabsChanged += ItemTabs_TabsChanged;
			NEClipboard.ClipboardChanged += () => UpdateClipboards();
			Activated += OnActivated;
		}

		void ItemTabs_TabsChanged()
		{
			ItemTabs.Items.ForEach(item => item.InvalidateCanvas());
			clipboardTimer.Start();
		}

		void UpdateStatusBarText()
		{
			Func<int, string, string> plural = (count, item) => $"{count:n0} {item}{(count == 1 ? "" : "s")}";

			ActiveCountText = $"{plural(ItemTabs.Items.Where(item => item.Active).Count(), "file")}, {plural(ItemTabs.Items.Where(item => item.Active).Sum(item => item.NumSelections), "selection")}";
			InactiveCountText = $"{plural(ItemTabs.Items.Where(item => !item.Active).Count(), "file")}, {plural(ItemTabs.Items.Where(item => !item.Active).Sum(item => item.NumSelections), "selection")}";
			TotalCountText = $"{plural(ItemTabs.Items.Count, "file")}, {plural(ItemTabs.Items.Sum(item => item.NumSelections), "selection")}";
			ClipboardCountText = $"{plural(NEClipboard.Current.Count, "file")}, {plural(NEClipboard.Current.ChildCount, "selection")}";
		}

		void UpdateClipboards()
		{
			ItemTabs.Items.ForEach(item => item.Clipboard = new List<string>());

			var activeTabs = ItemTabs.Items.Where(item => item.Active).ToList();

			if (NEClipboard.Current.Count == activeTabs.Count)
				NEClipboard.Current.Zip(activeTabs, (cb, tab) => new { cb, tab }).ForEach(obj => obj.tab.Clipboard = obj.cb.Strings);
			else if (NEClipboard.Current.ChildCount == activeTabs.Count)
				NEClipboard.Current.Strings.Zip(activeTabs, (str, tab) => new { str, tab }).ForEach(obj => obj.tab.Clipboard = new List<string> { obj.str });
			else
			{
				var strs = NEClipboard.Current.Strings;
				activeTabs.ForEach(tab => tab.Clipboard = strs);
			}

			UpdateStatusBarText();
		}

		void OnDrop(object sender, DragEventArgs e)
		{
			var fileList = e.Data.GetData("FileDrop") as string[];
			if (fileList == null)
				return;
			fileList.ForEach(file => Create(file));
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

		void Command_File_Shell_Integrate()
		{
			using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Default))
			using (var starKey = baseKey.OpenSubKey("*"))
			using (var shellKey = starKey.OpenSubKey("shell", true))
			using (var neoEditKey = shellKey.CreateSubKey("Open with NeoEdit Text Editor"))
			using (var commandKey = neoEditKey.CreateSubKey("command"))
				commandKey.SetValue("", $@"""{Assembly.GetEntryAssembly().Location}"" -text ""%1""");
		}

		void Command_File_Shell_Unintegrate()
		{
			using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Default))
			using (var starKey = baseKey.OpenSubKey("*"))
			using (var shellKey = starKey.OpenSubKey("shell", true))
				shellKey.DeleteSubKeyTree("Open with NeoEdit Text Editor");
		}

		void Command_File_Open_CopiedCut()
		{
			var files = NEClipboard.Current.Strings;

			if ((files.Count > 5) && (new Message(this)
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

		void Command_Diff_Diff()
		{
			var diffTargets = ItemTabs.Items.Count == 2 ? ItemTabs.Items.ToList() : ItemTabs.Items.Where(data => data.Active).ToList();
			if (diffTargets.Any(item => item.DiffTarget != null))
			{
				diffTargets.ForEach(item => item.DiffTarget = null);
				return;
			}

			if ((diffTargets.Count == 0) || (diffTargets.Count % 2 != 0))
				throw new Exception("Must have even number of files active for diff.");

			if (shiftDown)
			{
				if (!ItemTabs.Items.Except(diffTargets).Any())
					ItemTabs.Layout = TabsLayout.Grid;
				else
				{
					diffTargets.ForEach(diffTarget => ItemTabs.Items.Remove(diffTarget));

					var textEditTabs = new TextEditTabs();
					textEditTabs.ItemTabs.Layout = TabsLayout.Grid;
					diffTargets.ForEach(diffTarget => textEditTabs.tabs.CreateTab(diffTarget));
					textEditTabs.ItemTabs.TopMost = diffTargets[0];
				}
			}

			diffTargets.Batch(2).ForEach(batch => batch[0].DiffTarget = batch[1]);
		}

		void Command_Diff_Select_LeftRightBothTabs(bool? left)
		{
			var topMost = ItemTabs.TopMost;
			var active = ItemTabs.Items.Where(item => (item.Active) && (item.DiffTarget != null)).SelectMany(item => new List<TextEditor> { item, item.DiffTarget }).Distinct().Where(item => (!left.HasValue) || ((ItemTabs.GetIndex(item) < ItemTabs.GetIndex(item.DiffTarget)) == left)).ToList();
			ItemTabs.Items.ForEach(item => item.Active = false);

			if (!active.Any())
				return;

			if (!active.Contains(topMost))
				topMost = active.First();
			ItemTabs.TopMost = topMost;
			active.ForEach(item => item.Active = true);
		}

		CustomGridDialog.Result Command_View_Type_Dialog() => CustomGridDialog.Run(this, ItemTabs.Columns, ItemTabs.Rows);

		void Command_View_Type(TabsLayout layout, CustomGridDialog.Result result) => ItemTabs.SetLayout(layout, result?.Columns, result?.Rows);

		void Command_View_ActiveTabs() => tabs.ShowActiveTabsDialog();

		void Command_View_FontSize() => FontSizeDialog.Run(this);

		void Command_View_SelectTabsWithSelections(bool hasSelections)
		{
			var topMost = ItemTabs.TopMost;
			var active = ItemTabs.Items.Where(tab => (tab.Active) && (tab.HasSelections == hasSelections)).ToList();
			ItemTabs.Items.ToList().ForEach(tab => tab.Active = false);

			if (!active.Any())
				return;

			if (!active.Contains(topMost))
				topMost = active.First();
			ItemTabs.TopMost = topMost;
			active.ForEach(tab => tab.Active = true);
		}

		void Command_View_Select_TabsWithSelectionsToTop()
		{
			var topMost = ItemTabs.TopMost;
			var active = ItemTabs.Items.Where(tab => tab.Active).ToList();
			var hasSelections = active.Where(tab => tab.HasSelections).ToList();
			if ((!active.Any()) || (!hasSelections.Any()))
				return;

			ItemTabs.MoveToTop(hasSelections);
			if (!active.Contains(topMost))
				topMost = active.First();
			ItemTabs.TopMost = topMost;
			active.ForEach(tab => tab.Active = true);
		}

		void Command_View_CloseTabsWithSelections(bool hasSelections)
		{
			var topMost = ItemTabs.TopMost;
			var active = ItemTabs.Items.Where(tab => (tab.Active) && (tab.HasSelections != hasSelections)).ToList();

			var answer = new AnswerResult();
			var closeTabs = ItemTabs.Items.Where(tab => (tab.Active) && (tab.HasSelections == hasSelections)).ToList();
			if (!closeTabs.All(tab => tab.CanClose(answer)))
				return;
			closeTabs.ForEach(tab => Remove(tab));

			if (!active.Any())
				return;

			if (!active.Contains(topMost))
				topMost = active.First();
			ItemTabs.TopMost = topMost;
			active.ForEach(tab => tab.Active = true);
		}

		void Command_View_Close_ActiveTabs(bool active)
		{
			var answer = new AnswerResult();
			var closeTabs = ItemTabs.Items.Where(tab => tab.Active == active).ToList();
			if (!closeTabs.All(tab => tab.CanClose(answer)))
				return;
			closeTabs.ForEach(tab => Remove(tab));
		}

		void Command_View_NewWindow()
		{
			var active = ItemTabs.Items.Where(tab => tab.Active).ToList();
			active.ForEach(tab => ItemTabs.Items.Remove(tab));

			var newWindow = new TextEditTabs();
			newWindow.ItemTabs.Layout = ItemTabs.Layout;
			newWindow.ItemTabs.Columns = ItemTabs.Columns;
			newWindow.ItemTabs.Rows = ItemTabs.Rows;
			active.ForEach(tab => newWindow.ItemTabs.Items.Add(tab));
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
				new Message(this)
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
			var files = new Queue<string>(NEClipboard.Current.Strings);
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
			var result = MacroPlayRepeatDialog.Run(this, Macro<TextEditCommand>.ChooseMacro);
			if (result == null)
				return;

			var macro = Macro<TextEditCommand>.Load(result.Macro);
			var expression = new NEExpression(result.Expression);
			var count = int.MaxValue;
			if (result.RepeatType == MacroPlayRepeatDialog.RepeatTypeEnum.Number)
				count = expression.Evaluate<int>();

			Action startNext = null;
			startNext = () =>
			{
				if ((ItemTabs.TopMost == null) || (--count < 0))
					return;

				if (result.RepeatType == MacroPlayRepeatDialog.RepeatTypeEnum.Condition)
					if (!expression.Evaluate<bool>(ItemTabs.TopMost.GetVariables()))
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
				case TextEditCommand.Macro_Record_Quick_1: Command_Macro_Record_Quick(1); return;
				case TextEditCommand.Macro_Record_Quick_2: Command_Macro_Record_Quick(2); return;
				case TextEditCommand.Macro_Record_Quick_3: Command_Macro_Record_Quick(3); return;
				case TextEditCommand.Macro_Record_Quick_4: Command_Macro_Record_Quick(4); return;
				case TextEditCommand.Macro_Record_Quick_5: Command_Macro_Record_Quick(5); return;
				case TextEditCommand.Macro_Record_Quick_6: Command_Macro_Record_Quick(6); return;
				case TextEditCommand.Macro_Record_Quick_7: Command_Macro_Record_Quick(7); return;
				case TextEditCommand.Macro_Record_Quick_8: Command_Macro_Record_Quick(8); return;
				case TextEditCommand.Macro_Record_Quick_9: Command_Macro_Record_Quick(9); return;
				case TextEditCommand.Macro_Record_Quick_10: Command_Macro_Record_Quick(10); return;
				case TextEditCommand.Macro_Record_Quick_11: Command_Macro_Record_Quick(11); return;
				case TextEditCommand.Macro_Record_Quick_12: Command_Macro_Record_Quick(12); return;
				case TextEditCommand.Macro_Record_Record: Command_Macro_Record_Record(); return;
				case TextEditCommand.Macro_Record_StopRecording: Command_Macro_Record_StopRecording(); return;
				case TextEditCommand.Macro_Append_Quick_1: Command_Macro_Append_Quick(1); return;
				case TextEditCommand.Macro_Append_Quick_2: Command_Macro_Append_Quick(2); return;
				case TextEditCommand.Macro_Append_Quick_3: Command_Macro_Append_Quick(3); return;
				case TextEditCommand.Macro_Append_Quick_4: Command_Macro_Append_Quick(4); return;
				case TextEditCommand.Macro_Append_Quick_5: Command_Macro_Append_Quick(5); return;
				case TextEditCommand.Macro_Append_Quick_6: Command_Macro_Append_Quick(6); return;
				case TextEditCommand.Macro_Append_Quick_7: Command_Macro_Append_Quick(7); return;
				case TextEditCommand.Macro_Append_Quick_8: Command_Macro_Append_Quick(8); return;
				case TextEditCommand.Macro_Append_Quick_9: Command_Macro_Append_Quick(9); return;
				case TextEditCommand.Macro_Append_Quick_10: Command_Macro_Append_Quick(10); return;
				case TextEditCommand.Macro_Append_Quick_11: Command_Macro_Append_Quick(11); return;
				case TextEditCommand.Macro_Append_Quick_12: Command_Macro_Append_Quick(12); return;
				case TextEditCommand.Macro_Append_Append: Command_Macro_Append_Append(); return;
				case TextEditCommand.Macro_Play_Quick_1: Command_Macro_Play_Quick(1); return;
				case TextEditCommand.Macro_Play_Quick_2: Command_Macro_Play_Quick(2); return;
				case TextEditCommand.Macro_Play_Quick_3: Command_Macro_Play_Quick(3); return;
				case TextEditCommand.Macro_Play_Quick_4: Command_Macro_Play_Quick(4); return;
				case TextEditCommand.Macro_Play_Quick_5: Command_Macro_Play_Quick(5); return;
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
			if (!GetDialogResult(command, out dialogResult, multiStatus))
				return;

			if (recordingMacro != null)
				recordingMacro.AddCommand(command, shiftDown, dialogResult, multiStatus);

			HandleCommand(command, shiftDown, dialogResult, multiStatus);
		}

		NEClipboard newClipboard;
		public void AddClipboardStrings(IEnumerable<string> strings, bool? isCut = null)
		{
			newClipboard = newClipboard ?? new NEClipboard();
			newClipboard.Add(NEClipboardList.Create(strings));
			newClipboard.IsCut = isCut;
		}

		internal bool GetDialogResult(TextEditCommand command, out object dialogResult, bool? multiStatus)
		{
			dialogResult = null;

			switch (command)
			{
				case TextEditCommand.File_Open_Open: dialogResult = Command_File_Open_Open_Dialog(); break;
				case TextEditCommand.View_CustomGrid: dialogResult = Command_View_Type_Dialog(); break;
				case TextEditCommand.Macro_Open_Open: dialogResult = Command_File_Open_Open_Dialog(Macro<TextEditCommand>.MacroDirectory); break;
				default: return ItemTabs.TopMost == null ? true : ItemTabs.TopMost.GetDialogResult(command, out dialogResult, multiStatus);
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
				case TextEditCommand.File_Shell_Integrate: Command_File_Shell_Integrate(); break;
				case TextEditCommand.File_Shell_Unintegrate: Command_File_Shell_Unintegrate(); break;
				case TextEditCommand.File_Exit: Close(); break;
				case TextEditCommand.Diff_Diff: Command_Diff_Diff(); break;
				case TextEditCommand.Diff_Select_LeftTab: Command_Diff_Select_LeftRightBothTabs(true); break;
				case TextEditCommand.Diff_Select_RightTab: Command_Diff_Select_LeftRightBothTabs(false); break;
				case TextEditCommand.Diff_Select_BothTabs: Command_Diff_Select_LeftRightBothTabs(null); break;
				case TextEditCommand.View_Full: Command_View_Type(TabsLayout.Full, null); break;
				case TextEditCommand.View_Grid: Command_View_Type(TabsLayout.Grid, null); break;
				case TextEditCommand.View_CustomGrid: Command_View_Type(TabsLayout.Grid, dialogResult as CustomGridDialog.Result); break;
				case TextEditCommand.View_ActiveTabs: Command_View_ActiveTabs(); break;
				case TextEditCommand.View_FontSize: Command_View_FontSize(); break;
				case TextEditCommand.View_Select_TabsWithSelections: Command_View_SelectTabsWithSelections(true); break;
				case TextEditCommand.View_Select_TabsWithoutSelections: Command_View_SelectTabsWithSelections(false); break;
				case TextEditCommand.View_Select_TabsWithSelectionsToTop: Command_View_Select_TabsWithSelectionsToTop(); break;
				case TextEditCommand.View_Close_TabsWithSelections: Command_View_CloseTabsWithSelections(true); break;
				case TextEditCommand.View_Close_TabsWithoutSelections: Command_View_CloseTabsWithSelections(false); break;
				case TextEditCommand.View_Close_ActiveTabs: Command_View_Close_ActiveTabs(true); break;
				case TextEditCommand.View_Close_InactiveTabs: Command_View_Close_ActiveTabs(false); break;
				case TextEditCommand.View_NewWindow: Command_View_NewWindow(); break;
				case TextEditCommand.View_WordList: Command_View_WordList(); break;
				case TextEditCommand.Macro_Open_Quick_1: Macro_Open_Quick(1); return true;
				case TextEditCommand.Macro_Open_Quick_2: Macro_Open_Quick(2); return true;
				case TextEditCommand.Macro_Open_Quick_3: Macro_Open_Quick(3); return true;
				case TextEditCommand.Macro_Open_Quick_4: Macro_Open_Quick(4); return true;
				case TextEditCommand.Macro_Open_Quick_5: Macro_Open_Quick(5); return true;
				case TextEditCommand.Macro_Open_Quick_6: Macro_Open_Quick(6); return true;
				case TextEditCommand.Macro_Open_Quick_7: Macro_Open_Quick(7); return true;
				case TextEditCommand.Macro_Open_Quick_8: Macro_Open_Quick(8); return true;
				case TextEditCommand.Macro_Open_Quick_9: Macro_Open_Quick(9); return true;
				case TextEditCommand.Macro_Open_Quick_10: Macro_Open_Quick(10); return true;
				case TextEditCommand.Macro_Open_Quick_11: Macro_Open_Quick(11); return true;
				case TextEditCommand.Macro_Open_Quick_12: Macro_Open_Quick(12); return true;
				case TextEditCommand.Macro_Open_Open: Command_File_Open_Open(dialogResult as OpenFileDialogResult); return true;
			}

			var answer = new AnswerResult();
			newClipboard = null;
			foreach (var textEditorItem in ItemTabs.Items.Where(item => item.Active).ToList())
			{
				textEditorItem.HandleCommand(command, shiftDown, dialogResult, multiStatus, answer);
				if (answer.Answer == Message.OptionsEnum.Cancel)
					break;
			}
			if (newClipboard != null)
				NEClipboard.Current = newClipboard;

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

			var key = e.Key;
			if (key == Key.System)
				key = e.SystemKey;
			e.Handled = HandleKey(key, shiftDown, controlDown, altDown);

			if ((recordingMacro != null) && (e.Handled))
				recordingMacro.AddKey(key, shiftDown, controlDown, altDown);
		}

		public override bool HandleKey(Key key, bool shiftDown, bool controlDown, bool altDown)
		{
			var result = false;
			var activeTabs = ItemTabs.Items.Where(item => item.Active).ToList();
			var previousData = default(object);
			foreach (var textEditorItems in activeTabs)
				textEditorItems.PreHandleKey(key, shiftDown, controlDown, altDown, ref previousData);
			foreach (var textEditorItems in activeTabs)
				result = textEditorItems.HandleKey(key, shiftDown, controlDown, altDown, previousData) || result;
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

		public void QueueDoActivated() => doActivatedTimer.Start();

		public void QueueUpdateCounts() => countsTimer.Start();

		void OnActivated(object sender, EventArgs e) => QueueDoActivated();

		void DoActivated()
		{
			if (!IsActive)
				return;

			Activated -= OnActivated;
			try
			{
				var answer = new AnswerResult();
				foreach (var item in ItemTabs.Items)
				{
					item.Activated(answer);
					if (answer.Answer == Message.OptionsEnum.Cancel)
						break;
				}
			}
			finally { Activated += OnActivated; }
		}
	}
}
