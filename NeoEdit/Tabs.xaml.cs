using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using NeoEdit;
using NeoEdit.Content;
using NeoEdit.Controls;
using NeoEdit.Converters;
using NeoEdit.Dialogs;
using NeoEdit.Expressions;
using NeoEdit.Misc;
using NeoEdit.NEClipboards;
using NeoEdit.Transform;

namespace NeoEdit
{
	partial class Tabs
	{
		[DepProp]
		public ObservableCollection<TextEditor> Items { get { return UIHelper<Tabs>.GetPropValue<ObservableCollection<TextEditor>>(this); } private set { UIHelper<Tabs>.SetPropValue(this, value); } }
		[DepProp]
		public TextEditor TopMost { get { return UIHelper<Tabs>.GetPropValue<TextEditor>(this); } set { UIHelper<Tabs>.SetPropValue(this, value); } }
		[DepProp]
		public TabsLayout Layout { get { return UIHelper<Tabs>.GetPropValue<TabsLayout>(this); } set { UIHelper<Tabs>.SetPropValue(this, value); } }
		[DepProp]
		public int? Columns { get { return UIHelper<Tabs>.GetPropValue<int?>(this); } set { UIHelper<Tabs>.SetPropValue(this, value); } }
		[DepProp]
		public int? Rows { get { return UIHelper<Tabs>.GetPropValue<int?>(this); } set { UIHelper<Tabs>.SetPropValue(this, value); } }
		[DepProp]
		public Tabs WindowParent { get { return UIHelper<Tabs>.GetPropValue<Tabs>(this); } set { UIHelper<Tabs>.SetPropValue(this, value); } }
		[DepProp]
		public string ActiveCountText { get { return UIHelper<Tabs>.GetPropValue<string>(this); } private set { UIHelper<Tabs>.SetPropValue(this, value); } }
		[DepProp]
		public string InactiveCountText { get { return UIHelper<Tabs>.GetPropValue<string>(this); } private set { UIHelper<Tabs>.SetPropValue(this, value); } }
		[DepProp]
		public string TotalCountText { get { return UIHelper<Tabs>.GetPropValue<string>(this); } private set { UIHelper<Tabs>.SetPropValue(this, value); } }
		[DepProp]
		public string ClipboardCountText { get { return UIHelper<Tabs>.GetPropValue<string>(this); } private set { UIHelper<Tabs>.SetPropValue(this, value); } }

		public delegate void TabsChangedDelegate();
		public event TabsChangedDelegate TabsChanged;

		readonly RunOnceTimer layoutTimer, topMostTimer;

		readonly Canvas canvas;
		readonly ScrollBar scrollBar;
		Action<TextEditor> ShowItem;
		int itemOrder = 0;

		static Tabs()
		{
			UIHelper<Tabs>.Register();
			UIHelper<Tabs>.AddObservableCallback(a => a.Items, (obj, s, e) => obj.ItemsChanged());
			UIHelper<Tabs>.AddCallback(a => a.TopMost, (obj, o, n) => obj.TopMostChanged());
			UIHelper<Tabs>.AddCoerce(a => a.TopMost, (obj, value) => (value != null) && (obj.Items?.Contains(value) == true) ? value : null);
			UIHelper<Tabs>.AddCallback(a => a.Layout, (obj, o, n) => obj.layoutTimer.Start());
			UIHelper<Tabs>.AddCallback(a => a.Rows, (obj, o, n) => obj.layoutTimer.Start());
			UIHelper<Tabs>.AddCallback(a => a.Columns, (obj, o, n) => obj.layoutTimer.Start());
		}

		bool shiftDown => Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
		bool controlDown => Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
		bool altDown => Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);

		readonly RunOnceTimer doActivatedTimer, countsTimer;
		public Tabs(bool addEmpty = false)
		{
			layoutTimer = new RunOnceTimer(DoLayout);
			topMostTimer = new RunOnceTimer(ShowTopMost);
			topMostTimer.AddDependency(layoutTimer);

			Items = new ObservableCollection<TextEditor>();
			Layout = TabsLayout.Full;
			Focusable = true;
			FocusVisualStyle = null;
			AllowDrop = true;
			VerticalAlignment = VerticalAlignment.Stretch;
			Drop += (s, e) => OnDrop(e, null);

			NEMenuItem.RegisterCommands(this, (command, multiStatus) => RunCommand(command, multiStatus));
			InitializeComponent();
			UIHelper.AuditMenu(menu);

			AllowDrop = true;
			Drop += OnDrop;
			doActivatedTimer = new RunOnceTimer(() => DoActivated());
			countsTimer = new RunOnceTimer(() => UpdateStatusBarText());
			TabsChanged += ItemTabs_TabsChanged;
			NEClipboard.ClipboardChanged += () => UpdateStatusBarText();
			Activated += OnActivated;

			SetupLayout(out canvas, out scrollBar);
			SizeChanged += (s, e) => layoutTimer.Start();
			scrollBar.ValueChanged += (s, e) => layoutTimer.Start();
			scrollBar.MouseWheel += (s, e) => scrollBar.Value -= e.Delta * scrollBar.ViewportSize / 1200;

			UpdateStatusBarText();

			if (addEmpty)
				Add(new TextEditor());
		}

		void ItemTabs_TabsChanged()
		{
			Items.ForEach(item => item.InvalidateCanvas());
			UpdateStatusBarText();
		}

		void UpdateStatusBarText()
		{
			Func<int, string, string> plural = (count, item) => $"{count:n0} {item}{(count == 1 ? "" : "s")}";

			ActiveCountText = $"{plural(Items.Where(item => item.Active).Count(), "file")}, {plural(Items.Where(item => item.Active).Sum(item => item.NumSelections), "selection")}";
			InactiveCountText = $"{plural(Items.Where(item => !item.Active).Count(), "file")}, {plural(Items.Where(item => !item.Active).Sum(item => item.NumSelections), "selection")}";
			TotalCountText = $"{plural(Items.Count, "file")}, {plural(Items.Sum(item => item.NumSelections), "selection")}";
			ClipboardCountText = $"{plural(NEClipboard.Current.Count, "file")}, {plural(NEClipboard.Current.ChildCount, "selection")}";
		}

		Dictionary<TextEditor, List<string>> clipboard;
		public List<string> GetClipboard(TextEditor textEditor)
		{
			if (clipboard == null)
			{
				var empty = new List<string>();
				clipboard = Items.ToDictionary(x => x, x => empty);

				var activeTabs = Items.Where(item => item.Active).ToList();

				if (NEClipboard.Current.Count == activeTabs.Count)
					NEClipboard.Current.ForEach((cb, index) => clipboard[activeTabs[index]] = cb.Strings);
				else if (NEClipboard.Current.ChildCount == activeTabs.Count)
					NEClipboard.Current.Strings.ForEach((str, index) => clipboard[activeTabs[index]] = new List<string> { str });
				else if (((NEClipboard.Current.Count == 1) || (NEClipboard.Current.Count == NEClipboard.Current.ChildCount)) && (NEClipboard.Current.ChildCount == activeTabs.Sum(tab => tab.NumSelections)))
					NEClipboard.Current.Strings.Take(activeTabs.Select(tab => tab.NumSelections)).ForEach((obj, index) => clipboard[activeTabs[index]] = obj.ToList());
				else
				{
					var strs = NEClipboard.Current.Strings;
					activeTabs.ForEach(tab => clipboard[tab] = strs);
				}
			}

			return clipboard[textEditor];
		}

		void OnDrop(object sender, DragEventArgs e)
		{
			var fileList = e.Data.GetData("FileDrop") as string[];
			if (fileList == null)
				return;
			fileList.ForEach(file => Add(new TextEditor(file)));
			e.Handled = true;
		}

		class OpenFileDialogResult
		{
			public List<string> files { get; set; }
		}

		OpenFileDialogResult Command_File_Open_Open_Dialog(string initialDirectory = null)
		{
			if ((initialDirectory == null) && (TopMost != null))
				initialDirectory = Path.GetDirectoryName(TopMost.FileName);
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

		void Command_File_New_New(bool createTabs) => (createTabs ? new Tabs() : this).Add(new TextEditor());

		void Command_File_New_FromClipboards() => NEClipboard.Current.Strings.ForEach((str, index) => Add(new TextEditor(displayName: $"Clipboard {index + 1}", bytes: Coder.StringToBytes(str, Coder.CodePage.UTF8), codePage: Coder.CodePage.UTF8, modified: false)));

		void Command_File_Open_Open(OpenFileDialogResult result)
		{
			foreach (var filename in result.files)
				Add(new TextEditor(filename));
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

			foreach (var item in Items)
				item.Active = false;
			foreach (var file in files)
				Add(new TextEditor(file));
		}

		void Command_Diff_Diff()
		{
			var diffTargets = Items.Count == 2 ? Items.ToList() : Items.Where(data => data.Active).ToList();
			if (diffTargets.Any(item => item.DiffTarget != null))
			{
				diffTargets.ForEach(item => item.DiffTarget = null);
				return;
			}

			if ((diffTargets.Count == 0) || (diffTargets.Count % 2 != 0))
				throw new Exception("Must have even number of files active for diff.");

			if (shiftDown)
			{
				if (!Items.Except(diffTargets).Any())
					Layout = TabsLayout.Grid;
				else
				{
					diffTargets.ForEach(diffTarget => Items.Remove(diffTarget));

					var textEditTabs = new Tabs();
					textEditTabs.Layout = TabsLayout.Grid;
					diffTargets.ForEach(diffTarget => textEditTabs.Add(diffTarget));
					textEditTabs.TopMost = diffTargets[0];
				}
			}

			diffTargets.Batch(2).ForEach(batch => batch[0].DiffTarget = batch[1]);
		}

		void Command_Diff_Select_LeftRightBothTabs(bool? left)
		{
			var topMost = TopMost;
			var active = Items.Where(item => (item.Active) && (item.DiffTarget != null)).SelectMany(item => new List<TextEditor> { item, item.DiffTarget }).Distinct().Where(item => (!left.HasValue) || ((GetIndex(item) < GetIndex(item.DiffTarget)) == left)).ToList();
			Items.ForEach(item => item.Active = false);

			if (!active.Any())
				return;

			if (!active.Contains(topMost))
				topMost = active.First();
			TopMost = topMost;
			active.ForEach(item => item.Active = true);
		}

		CustomGridDialog.Result Command_View_Type_Dialog() => CustomGridDialog.Run(this, Columns, Rows);

		void Command_View_Type(TabsLayout layout, CustomGridDialog.Result result) => SetLayout(layout, result?.Columns, result?.Rows);

		void Command_View_ActiveTabs() => ShowActiveTabsDialog();

		void Command_View_FontSize() => FontSizeDialog.Run(this);

		void Command_View_SelectTabsWithSelections(bool hasSelections)
		{
			var topMost = TopMost;
			var active = Items.Where(tab => (tab.Active) && (tab.HasSelections == hasSelections)).ToList();
			Items.ToList().ForEach(tab => tab.Active = false);

			if (!active.Any())
				return;

			if (!active.Contains(topMost))
				topMost = active.First();
			TopMost = topMost;
			active.ForEach(tab => tab.Active = true);
		}

		void Command_View_Select_TabsWithSelectionsToTop()
		{
			var topMost = TopMost;
			var active = Items.Where(tab => tab.Active).ToList();
			var hasSelections = active.Where(tab => tab.HasSelections).ToList();
			if ((!active.Any()) || (!hasSelections.Any()))
				return;

			MoveToTop(hasSelections);
			if (!active.Contains(topMost))
				topMost = active.First();
			TopMost = topMost;
			active.ForEach(tab => tab.Active = true);
		}

		void Command_View_CloseTabsWithSelections(bool hasSelections)
		{
			var topMost = TopMost;
			var active = Items.Where(tab => (tab.Active) && (tab.HasSelections != hasSelections)).ToList();

			var answer = new AnswerResult();
			var closeTabs = Items.Where(tab => (tab.Active) && (tab.HasSelections == hasSelections)).ToList();
			if (!closeTabs.All(tab => tab.CanClose(answer)))
				return;
			closeTabs.ForEach(tab => Remove(tab));

			if (!active.Any())
				return;

			if (!active.Contains(topMost))
				topMost = active.First();
			TopMost = topMost;
			active.ForEach(tab => tab.Active = true);
		}

		void Command_View_Close_ActiveTabs(bool active)
		{
			var answer = new AnswerResult();
			var closeTabs = Items.Where(tab => tab.Active == active).ToList();
			if (!closeTabs.All(tab => tab.CanClose(answer)))
				return;
			closeTabs.ForEach(tab => Remove(tab));
		}

		void Command_View_NewWindow()
		{
			var active = Items.Where(tab => tab.Active).ToList();
			active.ForEach(tab => Items.Remove(tab));

			var newWindow = new Tabs();
			newWindow.Layout = Layout;
			newWindow.Columns = Columns;
			newWindow.Rows = Rows;
			active.ForEach(tab => newWindow.Add(tab));
		}

		void Command_View_WordList()
		{
			var type = GetType();
			byte[] data;
			using (var stream = type.Assembly.GetManifestResourceStream($"{type.Namespace}.Resources.Words.txt.gz"))
			using (var ms = new MemoryStream())
			{
				stream.CopyTo(ms);
				data = ms.ToArray();
			}

			data = Compressor.Decompress(data, Compressor.Type.GZip);
			data = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(data).Replace("\n", "\r\n"));
			Add(new TextEditor(bytes: data, modified: false));
		}

		void Command_Window_NewWindow() => new Tabs(true);

		void Command_Help_About() => HelpAboutDialog.Run();

		void Command_Help_Update()
		{
			const string url = "https://github.com/xyandro/NeoEdit/releases/latest";
			const string check = "https://github.com/xyandro/NeoEdit/releases/tag/";
			const string exe = "https://github.com/xyandro/NeoEdit/releases/download/{0}/NeoEdit.exe";

			var oldVersion = ((AssemblyFileVersionAttribute)typeof(App).Assembly.GetCustomAttribute(typeof(AssemblyFileVersionAttribute))).Version;
			string newVersion;

			var request = WebRequest.Create(url) as HttpWebRequest;
			request.AllowAutoRedirect = false;
			using (var response = request.GetResponse() as HttpWebResponse)
			{
				var redirUrl = response.Headers["Location"];
				if (!redirUrl.StartsWith(check))
					throw new Exception("Version check failed to find latest version");

				newVersion = redirUrl.Substring(check.Length);
			}

			var oldNums = oldVersion.Split('.').Select(str => int.Parse(str)).ToList();
			var newNums = newVersion.Split('.').Select(str => int.Parse(str)).ToList();
			if (oldNums.Count != newNums.Count)
				throw new Exception("Version length mismatch");

			var newer = oldNums.Zip(newNums, (oldNum, newNum) => newNum.IsGreater(oldNum)).NonNull().FirstOrDefault();
			if (new Message
			{
				Title = "Download new version?",
				Text = newer ? $"A newer version ({newVersion}) is available.  Download it?" : $"Already up to date ({newVersion}).  Update anyway?",
				Options = Message.OptionsEnum.YesNo,
				DefaultAccept = newer ? Message.OptionsEnum.Yes : Message.OptionsEnum.No,
				DefaultCancel = Message.OptionsEnum.No,
			}.Show() != Message.OptionsEnum.Yes)
				return;

			var oldLocation = Assembly.GetEntryAssembly().Location;
			var newLocation = Path.Combine(Path.GetDirectoryName(oldLocation), $"{Path.GetFileNameWithoutExtension(oldLocation)}-Update{Path.GetExtension(oldLocation)}");

			byte[] result = null;
			ProgressDialog.Run(null, "Downloading new version...", (cancelled, progress) =>
			{
				var finished = new ManualResetEvent(false);
				using (var client = new WebClient())
				{
					client.DownloadProgressChanged += (s, e) => progress(e.ProgressPercentage);
					client.DownloadDataCompleted += (s, e) =>
					{
						if (!e.Cancelled)
							result = e.Result;
						finished.Set();
					};
					client.DownloadDataAsync(new Uri(string.Format(exe, newVersion)));
					while (!finished.WaitOne(500))
						if (cancelled())
							client.CancelAsync();
				}
			});

			if (result == null)
				return;

			File.WriteAllBytes(newLocation, result);

			Message.Show("The program will be updated after exiting.");
			Process.Start(newLocation, $@"-update ""{oldLocation}"" {Process.GetCurrentProcess().Id}");
		}

		void Command_Help_RunGC() => GC.Collect();

		string QuickMacro(int num) => $"QuickText{num}.xml";
		void Macro_Open_Quick(int quickNum) => Add(new TextEditor(Path.Combine(Macro.MacroDirectory, QuickMacro(quickNum))));

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
				recordingMacro = Macro.Load(QuickMacro(quickNum), true);
			else
				Command_Macro_Record_StopRecording(QuickMacro(quickNum));
		}

		void Command_Macro_Append_Append()
		{
			ValidateNoCurrentMacro();
			recordingMacro = Macro.Load();
		}

		void Command_Macro_Play_Quick(int quickNum) => Macro.Load(QuickMacro(quickNum), true).Play(this, playing => macroPlaying = playing);

		void ValidateNoCurrentMacro()
		{
			if (recordingMacro == null)
				return;

			throw new Exception("Cannot start recording; recording is already in progess.");
		}

		void Command_Macro_Record_Record()
		{
			ValidateNoCurrentMacro();
			recordingMacro = new Macro();
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

		void Command_Macro_Play_Play() => Macro.Load().Play(this, playing => macroPlaying = playing);

		void Command_Macro_Play_PlayOnCopiedFiles()
		{
			var files = new Queue<string>(NEClipboard.Current.Strings);
			var macro = Macro.Load();
			Action startNext = null;
			startNext = () =>
			{
				if (!files.Any())
					return;
				Add(new TextEditor(files.Dequeue()));
				macro.Play(this, playing => macroPlaying = playing, startNext);
			};
			startNext();
		}

		void Command_Macro_Play_Repeat()
		{
			var result = MacroPlayRepeatDialog.Run(this, Macro.ChooseMacro);
			if (result == null)
				return;

			var macro = Macro.Load(result.Macro);
			var expression = new NEExpression(result.Expression);
			var count = int.MaxValue;
			if (result.RepeatType == MacroPlayRepeatDialog.RepeatTypeEnum.Number)
				count = expression.Evaluate<int>();

			Action startNext = null;
			startNext = () =>
			{
				if ((TopMost == null) || (--count < 0))
					return;

				if (result.RepeatType == MacroPlayRepeatDialog.RepeatTypeEnum.Condition)
					if (!expression.Evaluate<bool>(TopMost.GetVariables()))
						return;

				macro.Play(this, playing => macroPlaying = playing, startNext);
			};
			startNext();
		}

		Macro recordingMacro;
		internal Macro macroPlaying = null;

		internal void RunCommand(NECommand command, bool? multiStatus)
		{
			if (macroPlaying != null)
				return;

			switch (command)
			{
				case NECommand.Macro_Record_Quick_1: Command_Macro_Record_Quick(1); return;
				case NECommand.Macro_Record_Quick_2: Command_Macro_Record_Quick(2); return;
				case NECommand.Macro_Record_Quick_3: Command_Macro_Record_Quick(3); return;
				case NECommand.Macro_Record_Quick_4: Command_Macro_Record_Quick(4); return;
				case NECommand.Macro_Record_Quick_5: Command_Macro_Record_Quick(5); return;
				case NECommand.Macro_Record_Quick_6: Command_Macro_Record_Quick(6); return;
				case NECommand.Macro_Record_Quick_7: Command_Macro_Record_Quick(7); return;
				case NECommand.Macro_Record_Quick_8: Command_Macro_Record_Quick(8); return;
				case NECommand.Macro_Record_Quick_9: Command_Macro_Record_Quick(9); return;
				case NECommand.Macro_Record_Quick_10: Command_Macro_Record_Quick(10); return;
				case NECommand.Macro_Record_Quick_11: Command_Macro_Record_Quick(11); return;
				case NECommand.Macro_Record_Quick_12: Command_Macro_Record_Quick(12); return;
				case NECommand.Macro_Record_Record: Command_Macro_Record_Record(); return;
				case NECommand.Macro_Record_StopRecording: Command_Macro_Record_StopRecording(); return;
				case NECommand.Macro_Append_Quick_1: Command_Macro_Append_Quick(1); return;
				case NECommand.Macro_Append_Quick_2: Command_Macro_Append_Quick(2); return;
				case NECommand.Macro_Append_Quick_3: Command_Macro_Append_Quick(3); return;
				case NECommand.Macro_Append_Quick_4: Command_Macro_Append_Quick(4); return;
				case NECommand.Macro_Append_Quick_5: Command_Macro_Append_Quick(5); return;
				case NECommand.Macro_Append_Quick_6: Command_Macro_Append_Quick(6); return;
				case NECommand.Macro_Append_Quick_7: Command_Macro_Append_Quick(7); return;
				case NECommand.Macro_Append_Quick_8: Command_Macro_Append_Quick(8); return;
				case NECommand.Macro_Append_Quick_9: Command_Macro_Append_Quick(9); return;
				case NECommand.Macro_Append_Quick_10: Command_Macro_Append_Quick(10); return;
				case NECommand.Macro_Append_Quick_11: Command_Macro_Append_Quick(11); return;
				case NECommand.Macro_Append_Quick_12: Command_Macro_Append_Quick(12); return;
				case NECommand.Macro_Append_Append: Command_Macro_Append_Append(); return;
				case NECommand.Macro_Play_Quick_1: Command_Macro_Play_Quick(1); return;
				case NECommand.Macro_Play_Quick_2: Command_Macro_Play_Quick(2); return;
				case NECommand.Macro_Play_Quick_3: Command_Macro_Play_Quick(3); return;
				case NECommand.Macro_Play_Quick_4: Command_Macro_Play_Quick(4); return;
				case NECommand.Macro_Play_Quick_5: Command_Macro_Play_Quick(5); return;
				case NECommand.Macro_Play_Quick_6: Command_Macro_Play_Quick(6); return;
				case NECommand.Macro_Play_Quick_7: Command_Macro_Play_Quick(7); return;
				case NECommand.Macro_Play_Quick_8: Command_Macro_Play_Quick(8); return;
				case NECommand.Macro_Play_Quick_9: Command_Macro_Play_Quick(9); return;
				case NECommand.Macro_Play_Quick_10: Command_Macro_Play_Quick(10); return;
				case NECommand.Macro_Play_Quick_11: Command_Macro_Play_Quick(11); return;
				case NECommand.Macro_Play_Quick_12: Command_Macro_Play_Quick(12); return;
				case NECommand.Macro_Play_Play: Command_Macro_Play_Play(); return;
				case NECommand.Macro_Play_Repeat: Command_Macro_Play_Repeat(); return;
				case NECommand.Macro_Play_PlayOnCopiedFiles: Command_Macro_Play_PlayOnCopiedFiles(); return;
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

		internal bool GetDialogResult(NECommand command, out object dialogResult, bool? multiStatus)
		{
			dialogResult = null;

			switch (command)
			{
				case NECommand.File_Open_Open: dialogResult = Command_File_Open_Open_Dialog(); break;
				case NECommand.View_CustomGrid: dialogResult = Command_View_Type_Dialog(); break;
				case NECommand.Macro_Open_Open: dialogResult = Command_File_Open_Open_Dialog(Macro.MacroDirectory); break;
				default: return TopMost == null ? true : TopMost.GetDialogResult(command, out dialogResult, multiStatus);
			}

			return dialogResult != null;
		}

		public bool HandleCommand(NECommand command, bool shiftDown, object dialogResult, bool? multiStatus)
		{
			switch (command)
			{
				case NECommand.File_New_New: Command_File_New_New(shiftDown); break;
				case NECommand.File_New_FromClipboards: Command_File_New_FromClipboards(); break;
				case NECommand.File_Open_Open: Command_File_Open_Open(dialogResult as OpenFileDialogResult); break;
				case NECommand.File_Open_CopiedCut: Command_File_Open_CopiedCut(); break;
				case NECommand.File_Shell_Integrate: Command_File_Shell_Integrate(); break;
				case NECommand.File_Shell_Unintegrate: Command_File_Shell_Unintegrate(); break;
				case NECommand.File_Exit: Close(); break;
				case NECommand.Diff_Diff: Command_Diff_Diff(); break;
				case NECommand.Diff_Select_LeftTab: Command_Diff_Select_LeftRightBothTabs(true); break;
				case NECommand.Diff_Select_RightTab: Command_Diff_Select_LeftRightBothTabs(false); break;
				case NECommand.Diff_Select_BothTabs: Command_Diff_Select_LeftRightBothTabs(null); break;
				case NECommand.View_Full: Command_View_Type(TabsLayout.Full, null); break;
				case NECommand.View_Grid: Command_View_Type(TabsLayout.Grid, null); break;
				case NECommand.View_CustomGrid: Command_View_Type(TabsLayout.Custom, dialogResult as CustomGridDialog.Result); break;
				case NECommand.View_ActiveTabs: Command_View_ActiveTabs(); break;
				case NECommand.View_FontSize: Command_View_FontSize(); break;
				case NECommand.View_Select_TabsWithSelections: Command_View_SelectTabsWithSelections(true); break;
				case NECommand.View_Select_TabsWithoutSelections: Command_View_SelectTabsWithSelections(false); break;
				case NECommand.View_Select_TabsWithSelectionsToTop: Command_View_Select_TabsWithSelectionsToTop(); break;
				case NECommand.View_Close_TabsWithSelections: Command_View_CloseTabsWithSelections(true); break;
				case NECommand.View_Close_TabsWithoutSelections: Command_View_CloseTabsWithSelections(false); break;
				case NECommand.View_Close_ActiveTabs: Command_View_Close_ActiveTabs(true); break;
				case NECommand.View_Close_InactiveTabs: Command_View_Close_ActiveTabs(false); break;
				case NECommand.View_NewWindow: Command_View_NewWindow(); break;
				case NECommand.View_WordList: Command_View_WordList(); break;
				case NECommand.Macro_Open_Quick_1: Macro_Open_Quick(1); return true;
				case NECommand.Macro_Open_Quick_2: Macro_Open_Quick(2); return true;
				case NECommand.Macro_Open_Quick_3: Macro_Open_Quick(3); return true;
				case NECommand.Macro_Open_Quick_4: Macro_Open_Quick(4); return true;
				case NECommand.Macro_Open_Quick_5: Macro_Open_Quick(5); return true;
				case NECommand.Macro_Open_Quick_6: Macro_Open_Quick(6); return true;
				case NECommand.Macro_Open_Quick_7: Macro_Open_Quick(7); return true;
				case NECommand.Macro_Open_Quick_8: Macro_Open_Quick(8); return true;
				case NECommand.Macro_Open_Quick_9: Macro_Open_Quick(9); return true;
				case NECommand.Macro_Open_Quick_10: Macro_Open_Quick(10); return true;
				case NECommand.Macro_Open_Quick_11: Macro_Open_Quick(11); return true;
				case NECommand.Macro_Open_Quick_12: Macro_Open_Quick(12); return true;
				case NECommand.Macro_Open_Open: Command_File_Open_Open(dialogResult as OpenFileDialogResult); return true;
				case NECommand.Window_NewWindow: Command_Window_NewWindow(); break;
				case NECommand.Help_About: Command_Help_About(); break;
				case NECommand.Help_Update: Command_Help_Update(); break;
				case NECommand.Help_RunGC: Command_Help_RunGC(); break;
			}

			try
			{
				var answer = new AnswerResult();
				foreach (var textEditorItem in Items.Where(item => item.Active).ToList())
				{
					textEditorItem.HandleCommand(command, shiftDown, dialogResult, multiStatus, answer);
					if (answer.Answer == Message.OptionsEnum.Cancel)
						break;
				}
				if (newClipboard != null)
					NEClipboard.Current = newClipboard;
			}
			finally
			{
				clipboard = null;
				newClipboard = null;
			}

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

		public bool HandleKey(Key key, bool shiftDown, bool controlDown, bool altDown)
		{
			var result = false;
			var activeTabs = Items.Where(item => item.Active).ToList();
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

		public bool HandleText(string text)
		{
			var result = false;
			foreach (var textEditorItems in Items.Where(item => item.Active).ToList())
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
				foreach (var item in Items)
				{
					item.Activated(answer);
					if (answer.Answer == Message.OptionsEnum.Cancel)
						break;
				}
			}
			finally { Activated += OnActivated; }
		}

		void ShowTopMost()
		{
			if (TopMost == null)
				return;
			ShowItem?.Invoke(TopMost);
			TopMost.Focus();
		}

		public void SetLayout(TabsLayout layout, int? columns = null, int? rows = null)
		{
			Layout = layout;
			Columns = columns;
			Rows = rows;
			topMostTimer.Start();
		}

		public TextEditor Add(TextEditor item, int? index = null)
		{
			var replace = (!index.HasValue) && (!item.Empty()) && (TopMost != null) && (TopMost.Empty()) ? TopMost : default(TextEditor);
			if (replace != null)
			{
				replace.Closed();
				Items[Items.IndexOf(replace)] = item;
			}
			else
				Items.Insert(index ?? Items.Count, item);
			TopMost = item;
			return replace;
		}

		public Window AddDiff(TextEditor textEdit1, TextEditor textEdit2)
		{
			if (textEdit1.ContentType == Parser.ParserType.None)
				textEdit1.ContentType = textEdit2.ContentType;
			if (textEdit2.ContentType == Parser.ParserType.None)
				textEdit2.ContentType = textEdit1.ContentType;
			Add(textEdit1);
			Add(textEdit2);
			textEdit1.DiffTarget = textEdit2;
			Layout = TabsLayout.Custom;
			Columns = 2;
			return this;
		}

		public void ShowActiveTabsDialog()
		{
			ActiveTabsDialog.Run(this);
			UpdateTopMost();
		}

		void ItemsChanged()
		{
			TabsChanged?.Invoke();

			if (Items == null)
				return;

			foreach (var item in Items)
			{
				EnhancedFocusManager.SetIsEnhancedFocusScope(item, true);
				item.TabsParent = this;
			}

			UpdateTopMost();
			layoutTimer.Start();
		}

		void TopMostChanged()
		{
			if (TopMost == null)
			{
				UpdateTopMost();
				return;
			}

			if (!shiftDown)
				foreach (var item in Items)
					item.Active = false;
			TopMost.Active = true;

			if (!controlDown)
				TopMost.ItemOrder = ++itemOrder;

			Dispatcher.BeginInvoke((Action)(() =>
			{
				UpdateLayout();
				if (TopMost != null)
					TopMost.Focus();
			}));

			topMostTimer.Start();
		}

		void UpdateTopMost()
		{
			var topMost = TopMost;
			if ((topMost == null) || (!topMost.Active))
				topMost = null;
			if (topMost == null)
				topMost = Items.Where(item => item.Active).OrderByDescending(item => item.ItemOrder).FirstOrDefault();
			if (topMost == null)
				topMost = Items.OrderByDescending(item => item.ItemOrder).FirstOrDefault();
			TopMost = topMost;
		}

		public bool TabIsActive(TextEditor item) => Items.Where(x => x == item).Select(x => x.Active).DefaultIfEmpty(false).First();

		public int GetIndex(TextEditor item, bool activeOnly = false)
		{
			var index = Items.Where(x => (!activeOnly) || (x.Active)).Indexes(x => x == item).DefaultIfEmpty(-1).First();
			if (index == -1)
				throw new ArgumentException("Not found");
			return index;
		}

		public void Remove(TextEditor item)
		{
			Items.Remove(item);
			item.Closed();
		}

		public void RemoveAll()
		{
			var items = Items.ToList();
			Items.Clear();
			foreach (var item in items)
				item.Closed();
		}

		public int ActiveCount => Items.Count(item => item.Active);

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnPreviewKeyDown(e);

			if ((controlDown) && (!altDown))
			{
				e.Handled = true;
				switch (e.Key)
				{
					case Key.PageUp: MovePrev(); break;
					case Key.PageDown: MoveNext(); break;
					case Key.Tab: MoveTabOrder(); break;
					default: e.Handled = false; break;
				}
			}
		}

		protected override void OnPreviewKeyUp(KeyEventArgs e)
		{
			base.OnPreviewKeyUp(e);
			if ((e.Key == Key.LeftCtrl) || (e.Key == Key.RightCtrl))
				if (TopMost != null)
					TopMost.ItemOrder = ++itemOrder;
		}

		void MovePrev()
		{
			var index = Items.IndexOf(TopMost) - 1;
			if (index < 0)
				index = Items.Count - 1;
			if (index >= 0)
				TopMost = Items[index];
		}

		void MoveNext()
		{
			var index = Items.IndexOf(TopMost) + 1;
			if (index >= Items.Count)
				index = 0;
			if (index < Items.Count)
				TopMost = Items[index];
		}

		void MoveTabOrder()
		{
			var ordering = Items.OrderBy(item => item.ItemOrder).ToList();
			var current = ordering.IndexOf(TopMost) - 1;
			if (current == -2) // Not found
				return;
			if (current == -1)
				current = ordering.Count - 1;
			TopMost = ordering[current];
		}

		protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			base.OnMouseLeftButtonDown(e);
			var source = e.OriginalSource as DependencyObject;
			foreach (var item in Items)
				if (item.IsAncestorOf(source))
					TopMost = item;
		}

		void OnDrop(DragEventArgs e, TextEditor toItem)
		{
			var fromItems = e.Data.GetData(typeof(List<TextEditor>)) as List<TextEditor>;
			if (fromItems == null)
				return;

			var toIndex = Items.IndexOf(toItem);
			fromItems.ForEach(fromItem => fromItem.TabsParent.Items.Remove(fromItem));

			if (toIndex == -1)
				toIndex = Items.Count;
			else
				toIndex = Math.Min(toIndex, Items.Count);

			foreach (var fromItem in fromItems)
			{
				Items.Insert(toIndex, fromItem);
				++toIndex;
				TopMost = fromItem;
				e.Handled = true;
			}
		}

		public void MoveToTop(IEnumerable<TextEditor> tabs)
		{
			var found = new HashSet<TextEditor>(tabs);
			var indexes = Items.Indexes(item => found.Contains(item)).ToList();
			for (var ctr = 0; ctr < indexes.Count; ++ctr)
				Items.Move(indexes[ctr], ctr);
		}

		DockPanel GetTabLabel(Tabs tabs, bool tiles, TextEditor item)
		{
			var dockPanel = new DockPanel { Margin = new Thickness(0, 0, tiles ? 0 : 2, 1), Tag = item };

			var multiBinding = new MultiBinding { Converter = new NEExpressionConverter(), ConverterParameter = "p0 o== p2 ? \"CadetBlue\" : (p1 ? \"LightBlue\" : \"LightGray\")" };
			multiBinding.Bindings.Add(new Binding { Source = item });
			multiBinding.Bindings.Add(new Binding(nameof(TextEditor.Active)) { Source = item });
			multiBinding.Bindings.Add(new Binding(nameof(TopMost)) { Source = tabs });
			dockPanel.SetBinding(DockPanel.BackgroundProperty, multiBinding);

			dockPanel.MouseLeftButtonDown += (s, e) => tabs.TopMost = item;
			dockPanel.MouseMove += (s, e) =>
			{
				if (e.LeftButton == MouseButtonState.Pressed)
				{
					var active = item.TabsParent.Items.Where(tab => tab.Active).ToList();
					DragDrop.DoDragDrop(s as DockPanel, new DataObject(typeof(List<TextEditor>), active), DragDropEffects.Move);
				}
			};

			var text = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 2, 0) };
			text.SetBinding(TextBlock.TextProperty, new Binding(nameof(TextEditor.TabLabel)) { Source = item });
			dockPanel.Children.Add(text);

			var closeButton = new Button
			{
				Content = "x",
				BorderThickness = new Thickness(0),
				Style = FindResource(ToolBar.ButtonStyleKey) as Style,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(2, 0, 5, 0),
				Foreground = new SolidColorBrush(Color.FromRgb(128, 32, 32)),
				Focusable = false,
				HorizontalAlignment = HorizontalAlignment.Right,
			};
			closeButton.Click += (s, e) =>
			{
				if (item.CanClose())
					tabs.Remove(item);
			};
			dockPanel.Children.Add(closeButton);
			return dockPanel;
		}

		void SetupLayout(out Canvas canvas, out ScrollBar scrollBar)
		{
			var grid = new Grid();
			grid.RowDefinitions.Add(new RowDefinition());
			grid.ColumnDefinitions.Add(new ColumnDefinition());
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			tabs.Content = grid;

			canvas = new Canvas { Background = Brushes.Gray, ClipToBounds = true };
			Grid.SetRow(canvas, 0);
			Grid.SetColumn(canvas, 0);
			grid.Children.Add(canvas);

			scrollBar = new ScrollBar();
			Grid.SetRow(scrollBar, 0);
			Grid.SetColumn(scrollBar, 1);
			grid.Children.Add(scrollBar);
		}

		void ClearLayout()
		{
			canvas.Children.Clear();
			foreach (var item in Items)
			{
				var parent = item.Parent;
				if (parent is Panel p)
					p.Children.Clear();
				else if (parent is ContentControl cc)
					cc.Content = null;
				else if (parent != null)
					throw new Exception("Don't know how to disconnect item");
			}
		}

		void DoLayout()
		{
			ClearLayout();
			if (Layout == TabsLayout.Full)
				DoFullLayout();
			else
				DoGridLayout();
			TopMost?.Focus();
		}

		void DoFullLayout()
		{
			if (scrollBar.Visibility != Visibility.Collapsed)
			{
				scrollBar.Visibility = Visibility.Collapsed;
				UpdateLayout();
			}

			var grid = new Grid { Width = canvas.ActualWidth, Height = canvas.ActualHeight, AllowDrop = true };
			grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			grid.RowDefinitions.Add(new RowDefinition());
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			grid.ColumnDefinitions.Add(new ColumnDefinition());
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			var tabLabels = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden };

			var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
			foreach (var item in Items)
			{
				var tabLabel = GetTabLabel(this, false, item);
				tabLabel.Drop += (s, e) => OnDrop(e, (s as FrameworkElement).Tag as TextEditor);
				stackPanel.Children.Add(tabLabel);
			}

			ShowItem = item =>
			{
				var show = stackPanel.Children.OfType<FrameworkElement>().Where(x => x.Tag == item).FirstOrDefault();
				if (show == null)
					return;
				tabLabels.UpdateLayout();
				var left = show.TranslatePoint(new Point(0, 0), tabLabels).X + tabLabels.HorizontalOffset;
				tabLabels.ScrollToHorizontalOffset(Math.Min(left, Math.Max(tabLabels.HorizontalOffset, left + show.ActualWidth - tabLabels.ViewportWidth)));
			};

			tabLabels.Content = stackPanel;
			Grid.SetRow(tabLabels, 0);
			Grid.SetColumn(tabLabels, 1);
			grid.Children.Add(tabLabels);

			var moveLeft = new RepeatButton { Content = "<", Margin = new Thickness(0, 0, 4, 0), Padding = new Thickness(5, 0, 5, 0) };
			moveLeft.Click += (s, e) => tabLabels.ScrollToHorizontalOffset(Math.Max(0, Math.Min(tabLabels.HorizontalOffset - 50, tabLabels.ScrollableWidth)));
			Grid.SetRow(moveLeft, 0);
			Grid.SetColumn(moveLeft, 0);
			grid.Children.Add(moveLeft);

			var moveRight = new RepeatButton { Content = ">", Margin = new Thickness(2, 0, 0, 0), Padding = new Thickness(5, 0, 5, 0) };
			moveRight.Click += (s, e) => tabLabels.ScrollToHorizontalOffset(Math.Max(0, Math.Min(tabLabels.HorizontalOffset + 50, tabLabels.ScrollableWidth)));
			Grid.SetRow(moveRight, 0);
			Grid.SetColumn(moveRight, 2);
			grid.Children.Add(moveRight);

			var contentControl = new ContentControl { FocusVisualStyle = null };
			contentControl.SetBinding(ContentControl.ContentProperty, new Binding(nameof(TopMost)) { Source = this });
			Grid.SetRow(contentControl, 1);
			Grid.SetColumn(contentControl, 0);
			Grid.SetColumnSpan(contentControl, 3);
			grid.Children.Add(contentControl);

			canvas.Children.Add(grid);
		}

		void DoGridLayout()
		{
			int columns, rows;
			if (Layout == TabsLayout.Grid)
			{
				columns = Math.Max(1, Math.Min((int)Math.Ceiling(Math.Sqrt(Items.Count)), 5));
				rows = Math.Max(1, Math.Min((Items.Count + columns - 1) / columns, 5));
			}
			else if (!Rows.HasValue)
			{
				columns = Math.Max(1, Columns ?? (int)Math.Ceiling(Math.Sqrt(Items.Count)));
				rows = Math.Max(1, (Items.Count + columns - 1) / columns);
			}
			else
			{
				rows = Math.Max(1, Rows.Value);
				columns = Math.Max(1, Columns ?? (Items.Count + rows - 1) / rows);
			}

			var totalRows = (Items.Count + columns - 1) / columns;

			var scrollBarVisibility = totalRows > rows ? Visibility.Visible : Visibility.Collapsed;
			if (scrollBar.Visibility != scrollBarVisibility)
			{
				scrollBar.Visibility = scrollBarVisibility;
				UpdateLayout();
			}

			var width = canvas.ActualWidth / columns;
			var height = canvas.ActualHeight / rows;

			scrollBar.ViewportSize = scrollBar.LargeChange = canvas.ActualHeight;
			scrollBar.Maximum = height * totalRows - scrollBar.ViewportSize;

			for (var ctr = 0; ctr < Items.Count; ++ctr)
			{
				var item = Items[ctr];
				var top = ctr / columns * height - scrollBar.Value;
				if ((top + height < 0) || (top > canvas.ActualHeight))
					continue;

				var dockPanel = new DockPanel { AllowDrop = true, Margin = new Thickness(0, 0, 2, 2) };
				dockPanel.Drop += (s, e) => OnDrop(e, item);
				var tabLabel = GetTabLabel(this, true, item);
				DockPanel.SetDock(tabLabel, Dock.Top);
				dockPanel.Children.Add(tabLabel);
				{
					item.SetValue(DockPanel.DockProperty, Dock.Bottom);
					item.FocusVisualStyle = null;
					dockPanel.Children.Add(item);
				}

				Canvas.SetLeft(dockPanel, ctr % columns * width + 1);
				Canvas.SetTop(dockPanel, top + 1);
				dockPanel.Width = width - 2;
				dockPanel.Height = height - 2;
				canvas.Children.Add(dockPanel);
			}

			ShowItem = item =>
			{
				var index = Items.IndexOf(item);
				if (index == -1)
					return;
				var top = index / columns * height;
				scrollBar.Value = Math.Min(top, Math.Max(scrollBar.Value, top + height - scrollBar.ViewportSize));
			};
		}

		internal void NotifyActiveChanged() => TabsChanged?.Invoke();

		protected override void OnClosing(CancelEventArgs e)
		{
			var answer = new AnswerResult();
			var topMost = TopMost;
			foreach (var item in Items)
			{
				TopMost = item;
				if (!item.CanClose(answer))
				{
					e.Cancel = true;
					return;
				}
			}
			TopMost = topMost;
			Items.ToList().ForEach(item => item.Closed());
			base.OnClosing(e);
		}
	}
}
