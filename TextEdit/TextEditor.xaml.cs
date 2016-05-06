using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using NeoEdit.Common;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.NEClipboards;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.GUI;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Converters;
using NeoEdit.GUI.Dialogs;
using NeoEdit.GUI.Misc;
using NeoEdit.TextEdit.Content;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	public class TabsControl : TabsControl<TextEditor, TextEditCommand> { }

	partial class TextEditor
	{
		TextData _data = new TextData();
		TextData Data
		{
			get { return _data; }
			set
			{
				_data = value;
				Selections.Clear();
				Regions.Clear();
				Searches.Clear();
				Bookmarks.Clear();
				undoRedo.Clear();
				CalculateDiff();
				CalculateBoundaries();
			}
		}
		readonly UndoRedo undoRedo;

		[DepProp]
		public string DisplayName { get { return UIHelper<TextEditor>.GetPropValue<string>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public string FileName { get { return UIHelper<TextEditor>.GetPropValue<string>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsModified { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public Highlighting.HighlightingType HighlightType { get { return UIHelper<TextEditor>.GetPropValue<Highlighting.HighlightingType>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public Parser.ParserType ContentType { get { return UIHelper<TextEditor>.GetPropValue<Parser.ParserType>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public Coder.CodePage CodePage { get { return UIHelper<TextEditor>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public string AESKey { get { return UIHelper<TextEditor>.GetPropValue<string>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? LineMin { get { return UIHelper<TextEditor>.GetPropValue<int?>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? LineMax { get { return UIHelper<TextEditor>.GetPropValue<int?>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? ColumnMin { get { return UIHelper<TextEditor>.GetPropValue<int?>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? ColumnMax { get { return UIHelper<TextEditor>.GetPropValue<int?>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? IndexMin { get { return UIHelper<TextEditor>.GetPropValue<int?>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? IndexMax { get { return UIHelper<TextEditor>.GetPropValue<int?>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? PositionMin { get { return UIHelper<TextEditor>.GetPropValue<int?>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? PositionMax { get { return UIHelper<TextEditor>.GetPropValue<int?>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int NumSelections { get { return UIHelper<TextEditor>.GetPropValue<int>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int NumRegions { get { return UIHelper<TextEditor>.GetPropValue<int>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int xScrollValue { get { return UIHelper<TextEditor>.GetPropValue<int>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int yScrollValue { get { return UIHelper<TextEditor>.GetPropValue<int>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public string LineEnding { get { return UIHelper<TextEditor>.GetPropValue<string>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int ClipboardCount { get { return UIHelper<TextEditor>.GetPropValue<int>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool DiffIgnoreWhitespace { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool DiffIgnoreCase { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool DiffIgnoreNumbers { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool DiffIgnoreLineEndings { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsDiff { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool UseCurrentWindow { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }

		TextEditor diffTarget;
		public TextEditor DiffTarget
		{
			get { return diffTarget; }
			set
			{
				if (value == this)
					value = null;

				IsDiff = false;

				if (diffTarget != null)
				{
					diffTarget.IsDiff = false;
					BindingOperations.ClearBinding(this, UIHelper<TextEditor>.GetProperty(a => a.xScrollValue));
					BindingOperations.ClearBinding(this, UIHelper<TextEditor>.GetProperty(a => a.yScrollValue));
					BindingOperations.ClearBinding(diffTarget, UIHelper<TextEditor>.GetProperty(a => a.xScrollValue));
					BindingOperations.ClearBinding(diffTarget, UIHelper<TextEditor>.GetProperty(a => a.yScrollValue));
					Data.ClearDiff();
					diffTarget.Data.ClearDiff();
					CalculateBoundaries();
					diffTarget.CalculateBoundaries();
					diffTarget.diffTarget = null;
					diffTarget = null;
				}

				if (value != null)
				{
					value.DiffTarget = null;
					diffTarget = value;
					value.diffTarget = this;
					SetBinding(UIHelper<TextEditor>.GetProperty(a => a.xScrollValue), new Binding(UIHelper<TextEditor>.GetProperty(a => a.xScrollValue).Name) { Source = value, Mode = BindingMode.TwoWay });
					SetBinding(UIHelper<TextEditor>.GetProperty(a => a.yScrollValue), new Binding(UIHelper<TextEditor>.GetProperty(a => a.yScrollValue).Name) { Source = value, Mode = BindingMode.TwoWay });
					IsDiff = diffTarget.IsDiff = true;
				}

				CalculateDiff();
			}
		}

		int xScrollViewportFloor => (int)Math.Floor(xScroll.ViewportSize);
		int xScrollViewportCeiling => (int)Math.Ceiling(xScroll.ViewportSize);
		int yScrollViewportFloor => (int)Math.Floor(yScroll.ViewportSize);
		int yScrollViewportCeiling => (int)Math.Ceiling(yScroll.ViewportSize);

		readonly RangeList Selections, Searches, Regions, Bookmarks;

		static ThreadSafeRandom random = new ThreadSafeRandom();

		readonly NELocalClipboard clipboard = new NELocalClipboard();

		static Dictionary<string, List<string>> variables { get; } = new Dictionary<string, List<string>>();

		static TextEditor()
		{
			UIHelper<TextEditor>.Register();
			UIHelper<TextEditor>.AddCallback(a => a.xScrollValue, (obj, o, n) => obj.canvasRenderTimer.Start());
			UIHelper<TextEditor>.AddCallback(a => a.yScrollValue, (obj, o, n) => { obj.canvasRenderTimer.Start(); obj.bookmarkRenderTimer.Start(); });
			UIHelper<TextEditor>.AddCallback(a => a.HighlightType, (obj, o, n) => obj.canvasRenderTimer.Start());
			UIHelper<TextEditor>.AddCallback(a => a.canvas, Canvas.ActualWidthProperty, obj => obj.CalculateBoundaries());
			UIHelper<TextEditor>.AddCallback(a => a.canvas, Canvas.ActualHeightProperty, obj => obj.CalculateBoundaries());
			UIHelper<TextEditor>.AddCoerce(a => a.xScrollValue, (obj, value) => (int)Math.Max(obj.xScroll.Minimum, Math.Min(obj.xScroll.Maximum, value)));
			UIHelper<TextEditor>.AddCoerce(a => a.yScrollValue, (obj, value) => (int)Math.Max(obj.yScroll.Minimum, Math.Min(obj.yScroll.Maximum, value)));
			SetupStaticKeys();
		}

		RunOnceTimer canvasRenderTimer, bookmarkRenderTimer;
		List<PropertyChangeNotifier> localCallbacks;

		public TextEditor(string fileName = null, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, bool? modified = null, int? line = null, int? column = null)
		{
			SetupLocalKeys();

			InitializeComponent();
			bookmarks.Width = Font.lineHeight;

			SetupTabLabel();

			clipboard.ClipboardChanged += SetClipboardCount;
			SetupDropAccept();

			undoRedo = new UndoRedo();
			Selections = new RangeList(SelectionsInvalidated);
			Searches = new RangeList(SearchesInvalidated);
			Regions = new RangeList(RegionsInvalidated);
			Bookmarks = new RangeList(BookmarksInvalidated);

			canvasRenderTimer = new RunOnceTimer(() => canvas.InvalidateVisual());
			canvasRenderTimer.AddDependency(Selections.Timer, Searches.Timer, Regions.Timer);
			bookmarkRenderTimer = new RunOnceTimer(() => bookmarks.InvalidateVisual());

			OpenFile(fileName, displayName, bytes, codePage, modified);
			Goto(line, column);

			localCallbacks = UIHelper<TextEditor>.GetLocalCallbacks(this);

			canvas.MouseLeftButtonDown += OnCanvasMouseLeftButtonDown;
			canvas.MouseLeftButtonUp += OnCanvasMouseLeftButtonUp;
			canvas.MouseMove += OnCanvasMouseMove;
			canvas.Render += OnCanvasRender;

			bookmarks.Render += OnBookmarksRender;

			MouseWheel += (s, e) => yScrollValue -= e.Delta / 40;

			Loaded += (s, e) =>
			{
				EnsureVisible();
				canvasRenderTimer.Start();
			};
		}

		void SetupTabLabel()
		{
			var multiBinding = new MultiBinding { Converter = new NEExpressionConverter(), ConverterParameter = @"([0] ?? FileName([1]) ?? ""[Untitled]"")t+([2]?""*"":"""")t+([3]?"" (Diff)"":"""")" };
			multiBinding.Bindings.Add(new Binding(UIHelper<TextEditor>.GetProperty(a => a.DisplayName).Name) { Source = this });
			multiBinding.Bindings.Add(new Binding(UIHelper<TextEditor>.GetProperty(a => a.FileName).Name) { Source = this });
			multiBinding.Bindings.Add(new Binding(UIHelper<TextEditor>.GetProperty(a => a.IsModified).Name) { Source = this });
			multiBinding.Bindings.Add(new Binding(UIHelper<TextEditor>.GetProperty(a => a.IsDiff).Name) { Source = this });
			SetBinding(UIHelper<TabsControl<TextEditor, TextEditCommand>>.GetProperty(a => a.TabLabel), multiBinding);
		}

		void SetClipboardCount() => ClipboardCount = clipboard.Strings.Count;

		void SetClipboardText(string text) => clipboard.SetText(text, TabsParent.ActiveCount == 1);
		void SetClipboardStrings(IEnumerable<string> strs) => clipboard.SetStrings(strs, TabsParent.ActiveCount == 1);
		void SetClipboardFile(string fileName, bool isCut = false) => clipboard.SetFile(fileName, isCut, TabsParent.ActiveCount == 1);
		void SetClipboardFiles(IEnumerable<string> fileNames, bool isCut = false) => clipboard.SetFiles(fileNames, isCut, TabsParent.ActiveCount == 1);

		void SetupDropAccept()
		{
			AllowDrop = true;
			DragEnter += (s, e) => e.Effects = DragDropEffects.Link;
			Drop += (s, e) =>
			{
				var fileList = e.Data.GetData("FileDrop") as string[];
				if (fileList != null)
				{
					if (Selections.Count != 1)
						throw new Exception("Must have one selection.");

					var files = fileList.Select(file => file + Data.DefaultEnding).ToList();
					var offset = Selections.Single().Start;
					ReplaceSelections(string.Join("", files));
					Selections.Clear();
					foreach (var str in files)
					{
						Selections.Add(Range.FromIndex(offset, str.Length - Data.DefaultEnding.Length));
						offset += str.Length;
					}

					e.Handled = true;
				}
			};
		}

		CacheValue modifiedChecksum = new CacheValue();
		void SetModifiedFlag(bool? newValue = null)
		{
			if (newValue.HasValue)
			{
				if (newValue == false)
					modifiedChecksum.SetValue(Data.Data);
				else
					modifiedChecksum.Invalidate(); // Nothing will match, file will be perpetually modified
			}
			IsModified = !modifiedChecksum.Match(Data.Data);
		}

		internal void Goto(int? line, int? column)
		{
			var useLine = Math.Max(0, Math.Min(line ?? 1, Data.NumLines) - 1);
			var index = Data.GetIndexFromColumn(useLine, Math.Max(0, (column ?? 1) - 1), true);
			Selections.Add(new Range(Data.GetOffset(useLine, index)));

		}

		DateTime fileLastWrite;
		internal void OpenFile(string fileName, string displayName = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, bool? modified = null, bool keepUndo = false)
		{
			FileName = fileName;
			DisplayName = displayName;
			var isModified = modified ?? bytes != null;
			if (bytes == null)
			{
				if (FileName == null)
					bytes = new byte[0];
				else
					bytes = File.ReadAllBytes(FileName);
			}

			string aesKey;
			FileEncryptor.HandleDecrypt(ref bytes, out aesKey);
			AESKey = aesKey;

			if (codePage == Coder.CodePage.AutoByBOM)
				codePage = Coder.CodePageFromBOM(bytes);
			var data = Coder.BytesToString(bytes, codePage, true);
			Replace(new List<Range> { FullRange }, new List<string> { data });
			CodePage = codePage;
			HighlightType = Highlighting.Get(FileName);
			Command_Content_Type_SetFromExtension();
			if (File.Exists(FileName))
				fileLastWrite = new FileInfo(FileName).LastWriteTime;

			// If encoding can't exactly express bytes mark as modified (only for < 50 MB)
			if ((!isModified) && ((bytes.Length >> 20) < 50))
				isModified = !Coder.CanFullyEncode(bytes, CodePage);

			if (!keepUndo)
				undoRedo.Clear();
			SetModifiedFlag(isModified);
		}

		int BeginOffset => Data.GetOffset(0, 0);
		int EndOffset => Data.GetOffset(Data.NumLines - 1, Data.GetLineLength(Data.NumLines - 1));
		Range BeginRange => new Range(BeginOffset);
		Range EndRange => new Range(EndOffset);
		Range FullRange => new Range(EndOffset, BeginOffset);
		string AllText => GetString(FullRange);

		public override bool CanClose(ref Message.OptionsEnum answer)
		{
			if (!IsModified)
				return true;

			if ((answer != Message.OptionsEnum.YesToAll) && (answer != Message.OptionsEnum.NoToAll))
				answer = new Message
				{
					Title = "Confirm",
					Text = "Do you want to save changes?",
					Options = Message.OptionsEnum.YesNoYesAllNoAllCancel,
					DefaultCancel = Message.OptionsEnum.Cancel,
				}.Show();

			switch (answer)
			{
				case Message.OptionsEnum.Cancel:
					return false;
				case Message.OptionsEnum.No:
				case Message.OptionsEnum.NoToAll:
					return true;
				case Message.OptionsEnum.Yes:
				case Message.OptionsEnum.YesToAll:
					Command_File_Save_Save();
					return !IsModified;
			}
			return false;
		}

		public override void Closed()
		{
			DiffTarget = null;
			globalKeysChanged -= SetupLocalOrGlobalKeys;
			base.Closed();
		}

		internal enum GetPathType
		{
			FileName,
			FileNameWoExtension,
			Directory,
			Extension,
		}

		Range GetPathRange(GetPathType type, Range range)
		{
			var path = GetString(range);
			var dirLength = Math.Max(0, path.LastIndexOf('\\'));
			if ((path.StartsWith(@"\\")) && (dirLength == 1))
				dirLength = 0;
			var dirTotal = dirLength == 0 ? 0 : dirLength + 1;
			var extLen = Path.GetExtension(path).Length;

			switch (type)
			{
				case GetPathType.FileName: return new Range(range.End, range.Start + dirTotal);
				case GetPathType.FileNameWoExtension: return new Range(range.End - extLen, range.Start + dirTotal);
				case GetPathType.Directory: return new Range(range.Start + dirLength, range.Start);
				case GetPathType.Extension: return new Range(range.End, range.End - extLen);
				default: throw new ArgumentException();
			}
		}

		bool ConfirmVerifyCanFullyEncode()
		{
			switch (new Message
			{
				Title = "Confirm",
				Text = "The current encoding cannot fully represent this data.  Switch to UTF-8?",
				Options = Message.OptionsEnum.YesNoCancel,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.Cancel,
			}.Show())
			{
				case Message.OptionsEnum.Yes: CodePage = Coder.CodePage.UTF8; return true;
				case Message.OptionsEnum.No: return true;
				case Message.OptionsEnum.Cancel: return false;
				default: throw new Exception("Invalid response");
			}
		}

		bool VerifyCanFullyEncode() => (Data.CanFullyEncode(CodePage)) || (ConfirmVerifyCanFullyEncode());
		bool VerifyCanFullyEncode(List<string> strs, Coder.CodePage codePage) => (strs.AsParallel().All(str => Coder.CanFullyEncode(str, codePage))) || (ConfirmVerifyCanFullyEncode());

		internal NEVariables GetVariables()
		{
			// Can't access DependencyProperties/clipboard from other threads; grab a copy:
			var fileName = FileName;
			var clipboard = this.clipboard.Strings;
			var KeysAndValues = this.KeysAndValues;

			var results = new NEVariables();

			var strs = default(List<string>);
			var initializeStrs = new NEVariableInitializer(() => strs = Selections.Select(range => GetString(range)).ToList());

			results.Add(NEVariable.Constant("f", "Filename", () => fileName));
			results.Add(NEVariable.Enumerable("x", "Selected text", () => strs, initializeStrs));
			results.Add(NEVariable.Enumerable("xl", "Selection length", () => strs.Select(str => str.Length), initializeStrs));
			results.Add(NEVariable.Constant("xn", "Selections count", () => Selections.Count));
			results.Add(NEVariable.Enumerable("y", "One-based index", () => Enumerable.Range(1, int.MaxValue), infinite: true));
			results.Add(NEVariable.Enumerable("z", "Zero-based index", () => Enumerable.Range(0, int.MaxValue), infinite: true));
			if (clipboard.Count == 1)
			{
				results.Add(NEVariable.Constant("c", "Clipboard string", () => clipboard[0]));
				results.Add(NEVariable.Constant("cl", "Clipboard string length", () => clipboard[0].Length));
			}
			else
			{
				results.Add(NEVariable.Enumerable("c", "Clipboard string", () => clipboard));
				results.Add(NEVariable.Enumerable("cl", "Clipboard string length", () => clipboard.Select(str => str.Length)));
			}
			results.Add(NEVariable.Constant("cn", "Clipboard count", () => clipboard.Count));

			var lines = default(List<int>);
			var initializeLines = new NEVariableInitializer(() => lines = Selections.AsParallel().AsOrdered().Select(range => Data.GetOffsetLine(range.Start)).ToList());
			results.Add(NEVariable.Enumerable("line", "Selection line", () => lines.Select(line => line + 1), initializeLines));

			var cols = default(List<int>);
			var initializeCols = new NEVariableInitializer(() => cols = Selections.AsParallel().AsOrdered().Select((range, index) => Data.GetOffsetIndex(range.Start, lines[index]) + 1).ToList(), initializeLines);
			results.Add(NEVariable.Enumerable("col", "Selection column", () => cols, initializeCols));

			results.Add(NEVariable.Enumerable("pos", "Selection position", () => Selections.Select(range => range.Start)));

			var keyOrdering = default(List<int?>);
			var initializeKeyOrdering = new NEVariableInitializer(() => keyOrdering = strs.Select(str => keysHash.ContainsKey(str) ? (int?)keysHash[str] : null).ToList(), initializeStrs);
			for (var ctr = 0; ctr <= 9; ++ctr)
			{
				var num = ctr; // If we don't copy this the threads get the wrong value
				var prefix = ctr == 0 ? "k" : $"v{ctr}";
				var kvName = prefix;
				var kvlName = $"{prefix}l";
				var rkvName = $"r{prefix}";
				var rkvlName = $"r{prefix}l";
				var rkvnName = $"r{prefix}n";
				results.Add(NEVariable.Enumerable(rkvName, "Raw keys/values", () => KeysAndValues[num], initializeKeyOrdering));
				results.Add(NEVariable.Enumerable(rkvlName, "Raw keys/values length", () => KeysAndValues[num].Select(str => str.Length), initializeKeyOrdering));
				results.Add(NEVariable.Constant(rkvnName, "Raw keys/values count", () => KeysAndValues[num].Count, initializeKeyOrdering));

				var values = default(List<string>);
				var kvInitialize = new NEVariableInitializer(() =>
				{
					if (KeysAndValues[0].Count == KeysAndValues[num].Count)
						values = keyOrdering.Select(order => order.HasValue ? KeysAndValues[num][order.Value] : "").ToList();
					else
						values = new List<string>();
				}, initializeKeyOrdering);
				results.Add(NEVariable.Enumerable(kvName, "Keys/values", () => values, kvInitialize));
				results.Add(NEVariable.Enumerable(kvlName, "Keys/values length", () => values.Select(str => str.Length), kvInitialize));
			}

			// Add variables that aren't already set
			results.AddRange(variables.Where(pair => !results.Contains(pair.Key)).ForEach(pair => NEVariable.Enumerable(pair.Key, "User-defined", () => pair.Value)));

			return results;
		}

		List<T> GetFixedExpressionResults<T>(string expression) => new NEExpression(expression).EvaluateRows<T>(GetVariables(), Selections.Count());

		List<T> GetVariableExpressionResults<T>(string expression) => new NEExpression(expression).EvaluateRows<T>(GetVariables());

		void CopyDirectory(string src, string dest)
		{
			var srcDirs = new List<string> { src };
			for (var ctr = 0; ctr < srcDirs.Count; ++ctr)
				srcDirs.AddRange(Directory.GetDirectories(srcDirs[ctr]));

			var srcFiles = new List<string>();
			foreach (var dir in srcDirs)
				srcFiles.AddRange(Directory.GetFiles(dir));

			var destDirs = srcDirs.Select(dir => dest + dir.Substring(src.Length)).ToList();
			var destFiles = srcFiles.Select(file => dest + file.Substring(src.Length)).ToList();
			destDirs.ForEach(dir => Directory.CreateDirectory(dir));
			for (var ctr = 0; ctr < srcFiles.Count; ++ctr)
				File.Copy(srcFiles[ctr], destFiles[ctr]);
		}

		void Save(string fileName)
		{
			if (((Data.NumChars >> 20) < 50) && (!VerifyCanFullyEncode()))
				return;

			var triedReadOnly = false;
			while (true)
			{
				try
				{
					File.WriteAllBytes(fileName, FileEncryptor.Encrypt(Data.GetBytes(CodePage), AESKey));
					break;
				}
				catch (UnauthorizedAccessException)
				{
					if ((triedReadOnly) || (!new FileInfo(fileName).IsReadOnly))
						throw;

					if (new Message
					{
						Title = "Confirm",
						Text = "Save failed.  Remove read-only flag?",
						Options = Message.OptionsEnum.YesNo,
						DefaultAccept = Message.OptionsEnum.Yes,
						DefaultCancel = Message.OptionsEnum.No,
					}.Show() != Message.OptionsEnum.Yes)
						throw;
					new FileInfo(fileName).IsReadOnly = false;
					triedReadOnly = true;
				}
			}
			fileLastWrite = new FileInfo(fileName).LastWriteTime;
			SetModifiedFlag(false);
			if (FileName != fileName)
			{
				FileName = fileName;
				DisplayName = null;
			}
		}

		internal List<string> GetSelectionStrings() => Selections.AsParallel().AsOrdered().Select(range => GetString(range)).ToList();

		internal List<string> RelativeSelectedFiles()
		{
			var fileName = FileName;
			return Selections.AsParallel().AsOrdered().Select(range => fileName.RelativeChild(GetString(range))).ToList();
		}

		internal bool GetDialogResult(TextEditCommand command, out object dialogResult)
		{
			dialogResult = null;

			switch (command)
			{
				case TextEditCommand.File_Encoding_Encoding: dialogResult = Command_File_Encoding_Encoding_Dialog(); break;
				case TextEditCommand.File_Encoding_ReopenWithEncoding: dialogResult = Command_File_Encoding_ReopenWithEncoding_Dialog(); break;
				case TextEditCommand.File_Encryption: dialogResult = Command_File_Encryption_Dialog(); break;
				case TextEditCommand.Edit_Find_Find: dialogResult = Command_Edit_Find_FindReplace_Dialog(false); break;
				case TextEditCommand.Edit_Find_Replace: dialogResult = Command_Edit_Find_FindReplace_Dialog(true); break;
				case TextEditCommand.Edit_Repeat: dialogResult = Command_Edit_Repeat_Dialog(); break;
				case TextEditCommand.Edit_URL_Absolute: dialogResult = Command_Edit_URL_Absolute_Dialog(); break;
				case TextEditCommand.Edit_Color: dialogResult = Command_Edit_Color_Dialog(); break;
				case TextEditCommand.Edit_Hash: dialogResult = Command_Edit_Hash_Dialog(); break;
				case TextEditCommand.Edit_Sort: dialogResult = Command_Edit_Sort_Dialog(); break;
				case TextEditCommand.Edit_Convert: dialogResult = Command_Edit_Convert_Dialog(); break;
				case TextEditCommand.Files_Names_MakeAbsolute: dialogResult = Command_Files_Names_MakeAbsolute_Dialog(); break;
				case TextEditCommand.Files_Names_GetUnique: dialogResult = Command_Files_Names_GetUnique_Dialog(); break;
				case TextEditCommand.Files_Set_Size: dialogResult = Command_Files_Set_Size_Dialog(); break;
				case TextEditCommand.Files_Set_WriteTime: dialogResult = Command_Files_Set_Time_Dialog(); break;
				case TextEditCommand.Files_Set_AccessTime: dialogResult = Command_Files_Set_Time_Dialog(); break;
				case TextEditCommand.Files_Set_CreateTime: dialogResult = Command_Files_Set_Time_Dialog(); break;
				case TextEditCommand.Files_Set_AllTimes: dialogResult = Command_Files_Set_Time_Dialog(); break;
				case TextEditCommand.Files_Set_Attributes: dialogResult = Command_Files_Set_Attributes_Dialog(); break;
				case TextEditCommand.Files_Hash: dialogResult = Command_Files_Hash_Dialog(); break;
				case TextEditCommand.Files_Operations_Create_FromExpressions: dialogResult = Command_Files_Operations_Create_FromExpressions_Dialog(); break;
				case TextEditCommand.Expression_Expression: dialogResult = Command_Expression_Expression_Dialog(); break;
				case TextEditCommand.Expression_Copy: dialogResult = Command_Expression_Expression_Dialog(); break;
				case TextEditCommand.Expression_SelectByExpression: dialogResult = Command_Expression_SelectByExpression_Dialog(); break;
				case TextEditCommand.Expression_SetVariables: dialogResult = Command_Expression_SetVariables_Dialog(); break;
				case TextEditCommand.Text_Select_ByWidth: dialogResult = Command_Text_Select_ByWidth_Dialog(); break;
				case TextEditCommand.Text_Width: dialogResult = Command_Text_Width_Dialog(); break;
				case TextEditCommand.Text_Trim: dialogResult = Command_Text_Trim_Dialog(); break;
				case TextEditCommand.Text_RandomText: dialogResult = Command_Text_RandomText_Dialog(); break;
				case TextEditCommand.Text_ReverseRegEx: dialogResult = Command_Text_ReverseRegEx_Dialog(); break;
				case TextEditCommand.Numeric_ConvertBase: dialogResult = Command_Numeric_ConvertBase_Dialog(); break;
				case TextEditCommand.Numeric_Series_Linear: dialogResult = Command_Numeric_Series_LinearGeometric_Dialog(true); break;
				case TextEditCommand.Numeric_Series_Geometric: dialogResult = Command_Numeric_Series_LinearGeometric_Dialog(false); break;
				case TextEditCommand.Numeric_Scale: dialogResult = Command_Numeric_Scale_Dialog(); break;
				case TextEditCommand.Numeric_Floor: dialogResult = Command_Numeric_Floor_Dialog(); break;
				case TextEditCommand.Numeric_Round: dialogResult = Command_Numeric_Round_Dialog(); break;
				case TextEditCommand.Numeric_Ceiling: dialogResult = Command_Numeric_Ceiling_Dialog(); break;
				case TextEditCommand.Numeric_RandomNumber: dialogResult = Command_Numeric_RandomNumber_Dialog(); break;
				case TextEditCommand.Numeric_CombinationsPermutations: dialogResult = Command_Numeric_CombinationsPermutations_Dialog(); break;
				case TextEditCommand.Numeric_MinMaxValues: dialogResult = Command_Numeric_MinMaxValues_Dialog(); break;
				case TextEditCommand.DateTime_Convert: dialogResult = Command_DateTime_Convert_Dialog(); break;
				case TextEditCommand.Table_Convert: dialogResult = Command_Table_Convert_Dialog(); break;
				case TextEditCommand.Table_EditTable: dialogResult = Command_Table_EditTable_Dialog(); break;
				case TextEditCommand.Table_AddColumn: dialogResult = Command_Table_AddColumn_Dialog(); break;
				case TextEditCommand.Table_Select_RowsByExpression: dialogResult = Command_Table_Select_RowsByExpression_Dialog(); break;
				case TextEditCommand.Table_Join: dialogResult = Command_Table_Join_Dialog(); break;
				case TextEditCommand.Table_Database_GenerateInserts: dialogResult = Command_Table_Database_GenerateInserts_Dialog(); break;
				case TextEditCommand.Table_Database_GenerateUpdates: dialogResult = Command_Table_Database_GenerateUpdates_Dialog(); break;
				case TextEditCommand.Table_Database_GenerateDeletes: dialogResult = Command_Table_Database_GenerateDeletes_Dialog(); break;
				case TextEditCommand.Position_Goto_Lines: dialogResult = Command_Position_Goto_Dialog(GotoType.Line); break;
				case TextEditCommand.Position_Goto_Columns: dialogResult = Command_Position_Goto_Dialog(GotoType.Column); break;
				case TextEditCommand.Position_Goto_Positions: dialogResult = Command_Position_Goto_Dialog(GotoType.Position); break;
				case TextEditCommand.Content_Ancestor: dialogResult = Command_Content_FindByAttribute_Dialog(ParserNode.ParserNodeListType.Parents); break;
				case TextEditCommand.Content_Attributes_ByAttribute: dialogResult = Command_Content_Attributes_ByAttribute_Dialog(); break;
				case TextEditCommand.Content_Children_ByAttribute: dialogResult = Command_Content_FindByAttribute_Dialog(ParserNode.ParserNodeListType.Children); break;
				case TextEditCommand.Content_Descendants_ByAttribute: dialogResult = Command_Content_FindByAttribute_Dialog(ParserNode.ParserNodeListType.Descendants); break;
				case TextEditCommand.Content_Select_ByAttribute: dialogResult = Command_Content_FindByAttribute_Dialog(ParserNode.ParserNodeListType.Self); break;
				case TextEditCommand.Network_Ping: dialogResult = Command_Network_Ping_Dialog(); break;
				case TextEditCommand.Network_ScanPorts: dialogResult = Command_Network_ScanPorts_Dialog(); break;
				case TextEditCommand.Database_Connect: dialogResult = Command_Database_Connect_Dialog(); break;
				case TextEditCommand.Database_QueryTable: dialogResult = Command_Database_QueryTable_Dialog(); break;
				case TextEditCommand.Database_Examine: Command_Database_Examine_Dialog(); break;
				case TextEditCommand.Select_Limit: dialogResult = Command_Select_Limit_Dialog(); break;
				case TextEditCommand.Select_Rotate: dialogResult = Command_Select_Rotate_Dialog(); break;
				case TextEditCommand.Select_ByCount: dialogResult = Command_Select_ByCount_Dialog(); break;
				case TextEditCommand.Select_Split: dialogResult = Command_Select_Split_Dialog(); break;
				default: return true;
			}

			return dialogResult != null;
		}

		static HashSet<string> drives = new HashSet<string>(DriveInfo.GetDrives().Select(drive => drive.Name));
		bool StringsAreFiles(List<string> strs)
		{
			if (!strs.Any())
				return false;
			if (strs.Any(str => str.IndexOfAny(Path.GetInvalidPathChars()) != -1))
				return false;
			if (strs.Any(str => !drives.Any(drive => str.StartsWith(drive))))
				return false;
			if (strs.Any(str => !FileOrDirectoryExists(str)))
				return false;
			return true;
		}

		class PreviousStruct
		{
			public TextEditCommand Command { get; set; }
			public bool ShiftDown { get; set; }
			public object DialogResult { get; set; }
			public bool? MultiStatus { get; set; }
		}
		PreviousStruct previous = null;

		bool timeNext = false;
		internal void HandleCommand(TextEditCommand command, bool shiftDown, object dialogResult, bool? multiStatus)
		{
			doDrag = DragType.None;

			var start = DateTime.UtcNow;
			if (command != TextEditCommand.Macro_RepeatLastAction)
			{
				previous = new PreviousStruct
				{
					Command = command,
					ShiftDown = shiftDown,
					DialogResult = dialogResult,
					MultiStatus = multiStatus,
				};
			}

			switch (command)
			{
				case TextEditCommand.File_Open_Selected: Command_File_Open_Selected(); break;
				case TextEditCommand.File_OpenWith_Disk: Command_File_OpenWith_Disk(); break;
				case TextEditCommand.File_OpenWith_HexEditor: Command_File_OpenWith_HexEditor(); break;
				case TextEditCommand.File_Save_Save: Command_File_Save_Save(); break;
				case TextEditCommand.File_Save_SaveAs: Command_File_Save_SaveAs(); break;
				case TextEditCommand.File_Operations_Rename: Command_File_Operations_Rename(); break;
				case TextEditCommand.File_Operations_Delete: Command_File_Operations_Delete(); break;
				case TextEditCommand.File_Operations_Explore: Command_File_Operations_Explore(); break;
				case TextEditCommand.File_Operations_CommandPrompt: Command_File_Operations_CommandPrompt(); break;
				case TextEditCommand.File_Operations_DragDrop: Command_File_Operations_DragDrop(); break;
				case TextEditCommand.File_Close: if (CanClose()) { TabsParent.Remove(this); } break;
				case TextEditCommand.File_Refresh: Command_File_Refresh(); break;
				case TextEditCommand.File_Revert: Command_File_Revert(); break;
				case TextEditCommand.File_Insert_Files: Command_File_Insert_Files(); break;
				case TextEditCommand.File_Insert_CopiedCut: Command_File_Insert_CopiedCut(); break;
				case TextEditCommand.File_Insert_Selected: Command_File_Insert_Selected(); break;
				case TextEditCommand.File_Copy_Path: Command_File_Copy_Path(); break;
				case TextEditCommand.File_Copy_Name: Command_File_Copy_Name(); break;
				case TextEditCommand.File_Copy_Count: Command_File_Copy_Count(); break;
				case TextEditCommand.File_Encoding_Encoding: Command_File_Encoding_Encoding(dialogResult as EncodingDialog.Result); break;
				case TextEditCommand.File_Encoding_ReopenWithEncoding: Command_File_Encoding_ReopenWithEncoding(dialogResult as EncodingDialog.Result); break;
				case TextEditCommand.File_Encryption: Command_File_Encryption(dialogResult as string); break;
				case TextEditCommand.Edit_Undo: Command_Edit_Undo(); break;
				case TextEditCommand.Edit_Redo: Command_Edit_Redo(); break;
				case TextEditCommand.Edit_Copy_Copy: Command_Edit_Copy_CutCopy(false); break;
				case TextEditCommand.Edit_Copy_Cut: Command_Edit_Copy_CutCopy(true); break;
				case TextEditCommand.Edit_Paste_Paste: Command_Edit_Paste_Paste(shiftDown); break;
				case TextEditCommand.Edit_Find_Find: Command_Edit_Find_FindReplace(false, shiftDown, dialogResult as FindTextDialog.Result); break;
				case TextEditCommand.Edit_Find_Next: Command_Edit_Find_NextPrevious(true, shiftDown); break;
				case TextEditCommand.Edit_Find_Previous: Command_Edit_Find_NextPrevious(false, shiftDown); break;
				case TextEditCommand.Edit_Find_Replace: Command_Edit_Find_FindReplace(true, shiftDown, dialogResult as FindTextDialog.Result); break;
				case TextEditCommand.Edit_CopyDown: Command_Edit_CopyDown(); break;
				case TextEditCommand.Edit_Repeat: Command_Edit_Repeat(dialogResult as RepeatDialog.Result); break;
				case TextEditCommand.Edit_Markup_Escape: Command_Edit_Markup_Escape(); break;
				case TextEditCommand.Edit_Markup_Unescape: Command_Edit_Markup_Unescape(); break;
				case TextEditCommand.Edit_RegEx_Escape: Command_Edit_RegEx_Escape(); break;
				case TextEditCommand.Edit_RegEx_Unescape: Command_Edit_RegEx_Unescape(); break;
				case TextEditCommand.Edit_URL_Escape: Command_Edit_URL_Escape(); break;
				case TextEditCommand.Edit_URL_Unescape: Command_Edit_URL_Unescape(); break;
				case TextEditCommand.Edit_URL_Absolute: Command_Edit_URL_Absolute(dialogResult as MakeAbsoluteDialog.Result); break;
				case TextEditCommand.Edit_Color: Command_Edit_Color(dialogResult as ChooseColorDialog.Result); break;
				case TextEditCommand.Edit_Hash: Command_Edit_Hash(dialogResult as HashTextDialog.Result); break;
				case TextEditCommand.Edit_Sort: Command_Edit_Sort(dialogResult as SortDialog.Result); break;
				case TextEditCommand.Edit_Convert: Command_Edit_Convert(dialogResult as ConvertDialog.Result); break;
				case TextEditCommand.Edit_Bookmarks_Toggle: Command_Edit_Bookmarks_Toggle(); break;
				case TextEditCommand.Edit_Bookmarks_Next: Command_Edit_Bookmarks_NextPreviousBookmark(true, shiftDown); break;
				case TextEditCommand.Edit_Bookmarks_Previous: Command_Edit_Bookmarks_NextPreviousBookmark(false, shiftDown); break;
				case TextEditCommand.Edit_Bookmarks_Clear: Command_Edit_Bookmarks_Clear(); break;
				case TextEditCommand.Diff_Selections: Command_Diff_Selections(); break;
				case TextEditCommand.Diff_SelectedFiles: Command_Diff_SelectedFiles(); break;
				case TextEditCommand.Diff_Break: Command_Diff_Break(); break;
				case TextEditCommand.Diff_IgnoreWhitespace: Command_Diff_IgnoreWhitespace(multiStatus); break;
				case TextEditCommand.Diff_IgnoreCase: Command_Diff_IgnoreCase(multiStatus); break;
				case TextEditCommand.Diff_IgnoreNumbers: Command_Diff_IgnoreNumbers(multiStatus); break;
				case TextEditCommand.Diff_IgnoreLineEndings: Command_Diff_IgnoreLineEndings(multiStatus); break;
				case TextEditCommand.Diff_Next: Command_Diff_NextPrevious(true); break;
				case TextEditCommand.Diff_Previous: Command_Diff_NextPrevious(false); break;
				case TextEditCommand.Diff_CopyLeft: Command_Diff_CopyLeftRight(true); break;
				case TextEditCommand.Diff_CopyRight: Command_Diff_CopyLeftRight(false); break;
				case TextEditCommand.Diff_SelectMatch: Command_Diff_SelectMatch(true); break;
				case TextEditCommand.Diff_SelectNonMatch: Command_Diff_SelectMatch(false); break;
				case TextEditCommand.Files_Names_Simplify: Command_Files_Names_Simplify(); break;
				case TextEditCommand.Files_Names_MakeAbsolute: Command_Files_Names_MakeAbsolute(dialogResult as MakeAbsoluteDialog.Result); break;
				case TextEditCommand.Files_Names_GetUnique: Command_Files_Names_GetUnique(dialogResult as GetUniqueNamesDialog.Result); break;
				case TextEditCommand.Files_Names_Sanitize: Command_Files_Names_Sanitize(); break;
				case TextEditCommand.Files_Get_Size: Command_Files_Get_Size(); break;
				case TextEditCommand.Files_Get_WriteTime: Command_Files_Get_WriteTime(); break;
				case TextEditCommand.Files_Get_AccessTime: Command_Files_Get_AccessTime(); break;
				case TextEditCommand.Files_Get_CreateTime: Command_Files_Get_CreateTime(); break;
				case TextEditCommand.Files_Get_Attributes: Command_Files_Get_Attributes(); break;
				case TextEditCommand.Files_Set_Size: Command_Files_Set_Size(dialogResult as SetSizeDialog.Result); break;
				case TextEditCommand.Files_Set_WriteTime: Command_Files_Set_Time(TextEditor.TimestampType.Write, dialogResult as ChooseDateTimeDialog.Result); break;
				case TextEditCommand.Files_Set_AccessTime: Command_Files_Set_Time(TextEditor.TimestampType.Access, dialogResult as ChooseDateTimeDialog.Result); break;
				case TextEditCommand.Files_Set_CreateTime: Command_Files_Set_Time(TextEditor.TimestampType.Create, dialogResult as ChooseDateTimeDialog.Result); break;
				case TextEditCommand.Files_Set_AllTimes: Command_Files_Set_Time(TextEditor.TimestampType.All, dialogResult as ChooseDateTimeDialog.Result); break;
				case TextEditCommand.Files_Set_Attributes: Command_Files_Set_Attributes(dialogResult as SetAttributesDialog.Result); break;
				case TextEditCommand.Files_Select_Name_Directory: Command_Files_Select_Name(TextEditor.GetPathType.Directory); break;
				case TextEditCommand.Files_Select_Name_Name: Command_Files_Select_Name(TextEditor.GetPathType.FileName); break;
				case TextEditCommand.Files_Select_Name_FileNamewoExtension: Command_Files_Select_Name(TextEditor.GetPathType.FileNameWoExtension); break;
				case TextEditCommand.Files_Select_Name_Extension: Command_Files_Select_Name(TextEditor.GetPathType.Extension); break;
				case TextEditCommand.Files_Select_Files: Command_Files_Select_Files(); break;
				case TextEditCommand.Files_Select_Directories: Command_Files_Select_Directories(); break;
				case TextEditCommand.Files_Select_Existing: Command_Files_Select_Existing(true); break;
				case TextEditCommand.Files_Select_NonExisting: Command_Files_Select_Existing(false); break;
				case TextEditCommand.Files_Select_Roots: Command_Files_Select_Roots(true); break;
				case TextEditCommand.Files_Select_NonRoots: Command_Files_Select_Roots(false); break;
				case TextEditCommand.Files_Hash: Command_Files_Hash(dialogResult as HashDialog.Result); break;
				case TextEditCommand.Files_Operations_Copy: Command_Files_Operations_CopyMove(false); break;
				case TextEditCommand.Files_Operations_Move: Command_Files_Operations_CopyMove(true); break;
				case TextEditCommand.Files_Operations_Delete: Command_Files_Operations_Delete(); break;
				case TextEditCommand.Files_Operations_SaveClipboards: Command_Files_Operations_SaveClipboards(); break;
				case TextEditCommand.Files_Operations_DragDrop: Command_Files_Operations_DragDrop(); break;
				case TextEditCommand.Files_Operations_OpenDisk: Command_Files_Operations_OpenDisk(); break;
				case TextEditCommand.Files_Operations_Explore: Command_Files_Operations_Explore(); break;
				case TextEditCommand.Files_Operations_CommandPrompt: Command_Files_Operations_CommandPrompt(); break;
				case TextEditCommand.Files_Operations_Create_Files: Command_Files_Operations_Create_Files(); break;
				case TextEditCommand.Files_Operations_Create_Directories: Command_Files_Operations_Create_Directories(); break;
				case TextEditCommand.Files_Operations_Create_FromExpressions: Command_Files_Operations_Create_FromExpressions(dialogResult as CreateFilesDialog.Result); break;
				case TextEditCommand.Files_Operations_RunCommand_Parallel: Command_Files_Operations_RunCommand_Parallel(); break;
				case TextEditCommand.Files_Operations_RunCommand_Sequential: Command_Files_Operations_RunCommand_Sequential(); break;
				case TextEditCommand.Expression_Expression: Command_Expression_Expression(dialogResult as GetExpressionDialog.Result); break;
				case TextEditCommand.Expression_Copy: Command_Expression_Copy(dialogResult as GetExpressionDialog.Result); break;
				case TextEditCommand.Expression_EvaluateSelected: Command_Expression_EvaluateSelected(); break;
				case TextEditCommand.Expression_SelectByExpression: Command_Expression_SelectByExpression(dialogResult as GetExpressionDialog.Result); break;
				case TextEditCommand.Expression_ClearVariables: Command_Expression_ClearVariables(); break;
				case TextEditCommand.Expression_SetVariables: Command_Expression_SetVariables(dialogResult as SetVariablesDialog.Result); break;
				case TextEditCommand.Text_Copy_Length: Command_Text_Copy_Length(); break;
				case TextEditCommand.Text_Copy_Min_Text: Command_Type_Copy_MinMax(true, TextEditor.Command_MinMax_Type.String); break;
				case TextEditCommand.Text_Copy_Min_Length: Command_Type_Copy_MinMax(true, TextEditor.Command_MinMax_Type.Length); break;
				case TextEditCommand.Text_Copy_Max_Text: Command_Type_Copy_MinMax(false, TextEditor.Command_MinMax_Type.String); break;
				case TextEditCommand.Text_Copy_Max_Length: Command_Type_Copy_MinMax(false, TextEditor.Command_MinMax_Type.Length); break;
				case TextEditCommand.Text_Select_Trim: Command_Text_Select_Trim(); break;
				case TextEditCommand.Text_Select_ByWidth: Command_Text_Select_ByWidth(dialogResult as WidthDialog.Result); break;
				case TextEditCommand.Text_Select_Min_Text: Command_Type_Select_MinMax(true, TextEditor.Command_MinMax_Type.String); break;
				case TextEditCommand.Text_Select_Min_Length: Command_Type_Select_MinMax(true, TextEditor.Command_MinMax_Type.Length); break;
				case TextEditCommand.Text_Select_Max_Text: Command_Type_Select_MinMax(false, TextEditor.Command_MinMax_Type.String); break;
				case TextEditCommand.Text_Select_Max_Length: Command_Type_Select_MinMax(false, TextEditor.Command_MinMax_Type.Length); break;
				case TextEditCommand.Text_Case_Upper: Command_Text_Case_Upper(); break;
				case TextEditCommand.Text_Case_Lower: Command_Text_Case_Lower(); break;
				case TextEditCommand.Text_Case_Proper: Command_Text_Case_Proper(); break;
				case TextEditCommand.Text_Case_Toggle: Command_Text_Case_Toggle(); break;
				case TextEditCommand.Text_Length: Command_Text_Length(); break;
				case TextEditCommand.Text_Width: Command_Text_Width(dialogResult as WidthDialog.Result); break;
				case TextEditCommand.Text_Trim: Command_Text_Trim(dialogResult as TrimDialog.Result); break;
				case TextEditCommand.Text_SingleLine: Command_Text_SingleLine(); break;
				case TextEditCommand.Text_GUID: Command_Text_GUID(); break;
				case TextEditCommand.Text_RandomText: Command_Text_RandomText(dialogResult as RandomDataDialog.Result); break;
				case TextEditCommand.Text_LoremIpsum: Command_Text_LoremIpsum(); break;
				case TextEditCommand.Text_ReverseRegEx: Command_Text_ReverseRegEx(dialogResult as RevRegExDialog.Result); break;
				case TextEditCommand.Numeric_Copy_Min: Command_Type_Copy_MinMax(true, TextEditor.Command_MinMax_Type.Numeric); break;
				case TextEditCommand.Numeric_Copy_Max: Command_Type_Copy_MinMax(false, TextEditor.Command_MinMax_Type.Numeric); break;
				case TextEditCommand.Numeric_Copy_Sum: Command_Numeric_Copy_Sum(); break;
				case TextEditCommand.Numeric_Copy_GCF: Command_Numeric_Copy_GCF(); break;
				case TextEditCommand.Numeric_Copy_LCM: Command_Numeric_Copy_LCM(); break;
				case TextEditCommand.Numeric_Select_Min: Command_Type_Select_MinMax(true, TextEditor.Command_MinMax_Type.Numeric); break;
				case TextEditCommand.Numeric_Select_Max: Command_Type_Select_MinMax(false, TextEditor.Command_MinMax_Type.Numeric); break;
				case TextEditCommand.Numeric_Select_Whole: Command_Numeric_Select_Whole(); break;
				case TextEditCommand.Numeric_Select_Fraction: Command_Numeric_Select_Fraction(); break;
				case TextEditCommand.Numeric_Hex_ToHex: Command_Numeric_Hex_ToHex(); break;
				case TextEditCommand.Numeric_Hex_FromHex: Command_Numeric_Hex_FromHex(); break;
				case TextEditCommand.Numeric_ConvertBase: Command_Numeric_ConvertBase(dialogResult as ConvertBaseDialog.Result); break;
				case TextEditCommand.Numeric_Series_ZeroBased: Command_Numeric_Series_ZeroBased(); break;
				case TextEditCommand.Numeric_Series_OneBased: Command_Numeric_Series_OneBased(); break;
				case TextEditCommand.Numeric_Series_Linear: Command_Numeric_Series_Linear(dialogResult as NumericSeriesDialog.Result); break;
				case TextEditCommand.Numeric_Series_Geometric: Command_Numeric_Series_Geometric(dialogResult as NumericSeriesDialog.Result); break;
				case TextEditCommand.Numeric_Scale: Command_Numeric_Scale(dialogResult as ScaleDialog.Result); break;
				case TextEditCommand.Numeric_ForwardSum: Command_Numeric_ForwardReverseSum(true); break;
				case TextEditCommand.Numeric_ReverseSum: Command_Numeric_ForwardReverseSum(false); break;
				case TextEditCommand.Numeric_Whole: Command_Numeric_Whole(); break;
				case TextEditCommand.Numeric_Fraction: Command_Numeric_Fraction(); break;
				case TextEditCommand.Numeric_Floor: Command_Numeric_Floor(dialogResult as FloorRoundCeilingDialog.Result); break;
				case TextEditCommand.Numeric_Round: Command_Numeric_Round(dialogResult as FloorRoundCeilingDialog.Result); break;
				case TextEditCommand.Numeric_Ceiling: Command_Numeric_Ceiling(dialogResult as FloorRoundCeilingDialog.Result); break;
				case TextEditCommand.Numeric_Factor: Command_Numeric_Factor(); break;
				case TextEditCommand.Numeric_RandomNumber: Command_Numeric_RandomNumber(dialogResult as RandomNumberDialog.Result); break;
				case TextEditCommand.Numeric_CombinationsPermutations: Command_Numeric_CombinationsPermutations(dialogResult as CombinationsPermutationsDialog.Result); break;
				case TextEditCommand.Numeric_MinMaxValues: Command_Numeric_MinMaxValues(dialogResult as MinMaxValuesDialog.Result); break;
				case TextEditCommand.DateTime_Now: Command_DateTime_Now(); break;
				case TextEditCommand.DateTime_Convert: Command_DateTime_Convert(dialogResult as ConvertDateTimeDialog.Result); break;
				case TextEditCommand.Table_DetectType: Command_Table_Type_Detect(); break;
				case TextEditCommand.Table_Convert: Command_Table_Convert(dialogResult as ChooseTableTypeDialog.Result); break;
				case TextEditCommand.Table_AddHeaders: Command_Table_AddHeaders(); break;
				case TextEditCommand.Table_LineSelectionsToTable: Command_Table_LineSelectionsToTable(); break;
				case TextEditCommand.Table_RegionSelectionsToTable: Command_Table_RegionSelectionsToTable(); break;
				case TextEditCommand.Table_EditTable: Command_Table_EditTable(dialogResult as EditTableDialog.Result); break;
				case TextEditCommand.Table_AddColumn: Command_Table_AddColumn(dialogResult as AddColumnDialog.Result); break;
				case TextEditCommand.Table_Select_RowsByExpression: Command_Table_Select_RowsByExpression(dialogResult as GetExpressionDialog.Result); break;
				case TextEditCommand.Table_SetJoinSource: Command_Table_SetJoinSource(); break;
				case TextEditCommand.Table_Join: Command_Table_Join(dialogResult as JoinDialog.Result); break;
				case TextEditCommand.Table_Transpose: Command_Table_Transpose(); break;
				case TextEditCommand.Table_SetVariables: Command_Table_SetVariables(); break;
				case TextEditCommand.Table_Database_GenerateInserts: Command_Table_Database_GenerateInserts(dialogResult as GenerateInsertsDialog.Result); break;
				case TextEditCommand.Table_Database_GenerateUpdates: Command_Table_Database_GenerateUpdates(dialogResult as GenerateUpdatesDialog.Result); break;
				case TextEditCommand.Table_Database_GenerateDeletes: Command_Table_Database_GenerateDeletes(dialogResult as GenerateDeletesDialog.Result); break;
				case TextEditCommand.Position_Goto_Lines: Command_Position_Goto(GotoType.Line, shiftDown, dialogResult as GotoDialog.Result); break;
				case TextEditCommand.Position_Goto_Columns: Command_Position_Goto(GotoType.Column, shiftDown, dialogResult as GotoDialog.Result); break;
				case TextEditCommand.Position_Goto_Positions: Command_Position_Goto(GotoType.Position, shiftDown, dialogResult as GotoDialog.Result); break;
				case TextEditCommand.Position_Goto_FilesLines: Command_Position_Goto_FilesLines(); break;
				case TextEditCommand.Position_Copy_Lines: Command_Position_Copy(GotoType.Line); break;
				case TextEditCommand.Position_Copy_Columns: Command_Position_Copy(GotoType.Column); break;
				case TextEditCommand.Position_Copy_Positions: Command_Position_Copy(GotoType.Position); break;
				case TextEditCommand.Content_Type_SetFromExtension: Command_Content_Type_SetFromExtension(); break;
				case TextEditCommand.Content_Type_None: Command_Content_Type(Parser.ParserType.None); break;
				case TextEditCommand.Content_Type_Balanced: Command_Content_Type(Parser.ParserType.Balanced); break;
				case TextEditCommand.Content_Type_Columns: Command_Content_Type(Parser.ParserType.Columns); break;
				case TextEditCommand.Content_Type_CSharp: Command_Content_Type(Parser.ParserType.CSharp); break;
				case TextEditCommand.Content_Type_CSV: Command_Content_Type(Parser.ParserType.CSV); break;
				case TextEditCommand.Content_Type_HTML: Command_Content_Type(Parser.ParserType.HTML); break;
				case TextEditCommand.Content_Type_JSON: Command_Content_Type(Parser.ParserType.JSON); break;
				case TextEditCommand.Content_Type_TSV: Command_Content_Type(Parser.ParserType.TSV); break;
				case TextEditCommand.Content_Type_XML: Command_Content_Type(Parser.ParserType.XML); break;
				case TextEditCommand.Content_Reformat: Command_Content_Reformat(); break;
				case TextEditCommand.Content_Comment: Command_Content_Comment(); break;
				case TextEditCommand.Content_Uncomment: Command_Content_Uncomment(); break;
				case TextEditCommand.Content_TogglePosition: Command_Content_TogglePosition(shiftDown); break;
				case TextEditCommand.Content_Current: Command_Content_Current(); break;
				case TextEditCommand.Content_Parent: Command_Content_Parent(); break;
				case TextEditCommand.Content_Ancestor: Command_Content_List(ParserNode.ParserNodeListType.Parents, findAttr: dialogResult as FindContentAttributeDialog.Result); break;
				case TextEditCommand.Content_Attributes_Attributes: Command_Content_List(ParserNode.ParserNodeListType.Attributes); break;
				case TextEditCommand.Content_Attributes_First: Command_Content_List(ParserNode.ParserNodeListType.Attributes, true); break;
				case TextEditCommand.Content_Attributes_ByAttribute: Command_Content_Attributes_ByAttribute(dialogResult as SelectContentAttributeDialog.Result); break;
				case TextEditCommand.Content_Children_Children: Command_Content_List(ParserNode.ParserNodeListType.Children); break;
				case TextEditCommand.Content_Children_SelfAndChildren: Command_Content_List(ParserNode.ParserNodeListType.SelfAndChildren); break;
				case TextEditCommand.Content_Children_First: Command_Content_List(ParserNode.ParserNodeListType.Children, first: true); break;
				case TextEditCommand.Content_Children_ByAttribute: Command_Content_List(ParserNode.ParserNodeListType.Children, findAttr: dialogResult as FindContentAttributeDialog.Result); break;
				case TextEditCommand.Content_Descendants_Descendants: Command_Content_List(ParserNode.ParserNodeListType.Descendants); break;
				case TextEditCommand.Content_Descendants_SelfAndDescendants: Command_Content_List(ParserNode.ParserNodeListType.SelfAndDescendants); break;
				case TextEditCommand.Content_Descendants_First: Command_Content_List(ParserNode.ParserNodeListType.Children, first: true); break;
				case TextEditCommand.Content_Descendants_ByAttribute: Command_Content_List(ParserNode.ParserNodeListType.Descendants, findAttr: dialogResult as FindContentAttributeDialog.Result); break;
				case TextEditCommand.Content_Next: Command_Content_NextPrevious(true); break;
				case TextEditCommand.Content_Previous: Command_Content_NextPrevious(false); break;
				case TextEditCommand.Content_Select_ByAttribute: Command_Content_Select_ByAttribute(dialogResult as FindContentAttributeDialog.Result); break;
				case TextEditCommand.Content_Select_Topmost: Command_Content_Select_Topmost(); break;
				case TextEditCommand.Content_Select_Deepest: Command_Content_Select_Deepest(); break;
				case TextEditCommand.Content_Select_MaxTopmost: Command_Content_Select_MaxTopmost(); break;
				case TextEditCommand.Content_Select_MaxDeepest: Command_Content_Select_MaxDeepest(); break;
				case TextEditCommand.Network_Fetch: Command_Network_Fetch(); break;
				case TextEditCommand.Network_Fetch_Hex: Command_Network_Fetch(Coder.CodePage.Hex); break;
				case TextEditCommand.Network_Lookup_IP: Command_Network_Lookup_IP(); break;
				case TextEditCommand.Network_Lookup_HostName: Command_Network_Lookup_HostName(); break;
				case TextEditCommand.Network_AdaptersInfo: Command_Network_AdaptersInfo(); break;
				case TextEditCommand.Network_Ping: Command_Network_Ping(dialogResult as PingDialog.Result); break;
				case TextEditCommand.Network_ScanPorts: Command_Network_ScanPorts(dialogResult as ScanPortsDialog.Result); break;
				case TextEditCommand.Database_Connect: Command_Database_Connect(dialogResult as DatabaseConnectDialog.Result); break;
				case TextEditCommand.Database_ExecuteQuery: Command_Database_ExecuteQuery(); break;
				case TextEditCommand.Database_UseCurrentWindow: Command_Database_UseCurrentWindow(multiStatus); break;
				case TextEditCommand.Database_QueryTable: Command_Database_QueryTable(dialogResult as string); break;
				case TextEditCommand.Keys_Set_Keys: Command_Keys_Set(0); break;
				case TextEditCommand.Keys_Set_Values1: Command_Keys_Set(1); break;
				case TextEditCommand.Keys_Set_Values2: Command_Keys_Set(2); break;
				case TextEditCommand.Keys_Set_Values3: Command_Keys_Set(3); break;
				case TextEditCommand.Keys_Set_Values4: Command_Keys_Set(4); break;
				case TextEditCommand.Keys_Set_Values5: Command_Keys_Set(5); break;
				case TextEditCommand.Keys_Set_Values6: Command_Keys_Set(6); break;
				case TextEditCommand.Keys_Set_Values7: Command_Keys_Set(7); break;
				case TextEditCommand.Keys_Set_Values8: Command_Keys_Set(8); break;
				case TextEditCommand.Keys_Set_Values9: Command_Keys_Set(9); break;
				case TextEditCommand.Keys_Add_Keys: Command_Keys_Add(0); break;
				case TextEditCommand.Keys_Add_Values1: Command_Keys_Add(1); break;
				case TextEditCommand.Keys_Add_Values2: Command_Keys_Add(2); break;
				case TextEditCommand.Keys_Add_Values3: Command_Keys_Add(3); break;
				case TextEditCommand.Keys_Add_Values4: Command_Keys_Add(4); break;
				case TextEditCommand.Keys_Add_Values5: Command_Keys_Add(5); break;
				case TextEditCommand.Keys_Add_Values6: Command_Keys_Add(6); break;
				case TextEditCommand.Keys_Add_Values7: Command_Keys_Add(7); break;
				case TextEditCommand.Keys_Add_Values8: Command_Keys_Add(8); break;
				case TextEditCommand.Keys_Add_Values9: Command_Keys_Add(9); break;
				case TextEditCommand.Keys_Replace_Values1: Command_Keys_Replace(1); break;
				case TextEditCommand.Keys_Replace_Values2: Command_Keys_Replace(2); break;
				case TextEditCommand.Keys_Replace_Values3: Command_Keys_Replace(3); break;
				case TextEditCommand.Keys_Replace_Values4: Command_Keys_Replace(4); break;
				case TextEditCommand.Keys_Replace_Values5: Command_Keys_Replace(5); break;
				case TextEditCommand.Keys_Replace_Values6: Command_Keys_Replace(6); break;
				case TextEditCommand.Keys_Replace_Values7: Command_Keys_Replace(7); break;
				case TextEditCommand.Keys_Replace_Values8: Command_Keys_Replace(8); break;
				case TextEditCommand.Keys_Replace_Values9: Command_Keys_Replace(9); break;
				case TextEditCommand.Keys_Find_Keys: Command_Keys_Find(0); break;
				case TextEditCommand.Keys_Find_Values1: Command_Keys_Find(1); break;
				case TextEditCommand.Keys_Find_Values2: Command_Keys_Find(2); break;
				case TextEditCommand.Keys_Find_Values3: Command_Keys_Find(3); break;
				case TextEditCommand.Keys_Find_Values4: Command_Keys_Find(4); break;
				case TextEditCommand.Keys_Find_Values5: Command_Keys_Find(5); break;
				case TextEditCommand.Keys_Find_Values6: Command_Keys_Find(6); break;
				case TextEditCommand.Keys_Find_Values7: Command_Keys_Find(7); break;
				case TextEditCommand.Keys_Find_Values8: Command_Keys_Find(8); break;
				case TextEditCommand.Keys_Find_Values9: Command_Keys_Find(9); break;
				case TextEditCommand.Keys_Copy_Keys: Command_Keys_Copy(0); break;
				case TextEditCommand.Keys_Copy_Values1: Command_Keys_Copy(1); break;
				case TextEditCommand.Keys_Copy_Values2: Command_Keys_Copy(2); break;
				case TextEditCommand.Keys_Copy_Values3: Command_Keys_Copy(3); break;
				case TextEditCommand.Keys_Copy_Values4: Command_Keys_Copy(4); break;
				case TextEditCommand.Keys_Copy_Values5: Command_Keys_Copy(5); break;
				case TextEditCommand.Keys_Copy_Values6: Command_Keys_Copy(6); break;
				case TextEditCommand.Keys_Copy_Values7: Command_Keys_Copy(7); break;
				case TextEditCommand.Keys_Copy_Values8: Command_Keys_Copy(8); break;
				case TextEditCommand.Keys_Copy_Values9: Command_Keys_Copy(9); break;
				case TextEditCommand.Keys_Hits_Keys: Command_Keys_HitsMisses(0, true); break;
				case TextEditCommand.Keys_Hits_Values1: Command_Keys_HitsMisses(1, true); break;
				case TextEditCommand.Keys_Hits_Values2: Command_Keys_HitsMisses(2, true); break;
				case TextEditCommand.Keys_Hits_Values3: Command_Keys_HitsMisses(3, true); break;
				case TextEditCommand.Keys_Hits_Values4: Command_Keys_HitsMisses(4, true); break;
				case TextEditCommand.Keys_Hits_Values5: Command_Keys_HitsMisses(5, true); break;
				case TextEditCommand.Keys_Hits_Values6: Command_Keys_HitsMisses(6, true); break;
				case TextEditCommand.Keys_Hits_Values7: Command_Keys_HitsMisses(7, true); break;
				case TextEditCommand.Keys_Hits_Values8: Command_Keys_HitsMisses(8, true); break;
				case TextEditCommand.Keys_Hits_Values9: Command_Keys_HitsMisses(9, true); break;
				case TextEditCommand.Keys_Misses_Keys: Command_Keys_HitsMisses(0, false); break;
				case TextEditCommand.Keys_Misses_Values1: Command_Keys_HitsMisses(1, false); break;
				case TextEditCommand.Keys_Misses_Values2: Command_Keys_HitsMisses(2, false); break;
				case TextEditCommand.Keys_Misses_Values3: Command_Keys_HitsMisses(3, false); break;
				case TextEditCommand.Keys_Misses_Values4: Command_Keys_HitsMisses(4, false); break;
				case TextEditCommand.Keys_Misses_Values5: Command_Keys_HitsMisses(5, false); break;
				case TextEditCommand.Keys_Misses_Values6: Command_Keys_HitsMisses(6, false); break;
				case TextEditCommand.Keys_Misses_Values7: Command_Keys_HitsMisses(7, false); break;
				case TextEditCommand.Keys_Misses_Values8: Command_Keys_HitsMisses(8, false); break;
				case TextEditCommand.Keys_Misses_Values9: Command_Keys_HitsMisses(9, false); break;
				case TextEditCommand.Select_All: Command_Select_All(); break;
				case TextEditCommand.Select_Nothing: Command_Select_Nothing(); break;
				case TextEditCommand.Select_Limit: Command_Select_Limit(dialogResult as LimitDialog.Result); break;
				case TextEditCommand.Select_Lines: Command_Select_Lines(); break;
				case TextEditCommand.Select_Rectangle: Command_Select_Rectangle(); break;
				case TextEditCommand.Select_Rotate: Command_Select_Rotate(dialogResult as SelectRotateDialog.Result); break;
				case TextEditCommand.Select_Invert: Command_Select_Invert(); break;
				case TextEditCommand.Select_Join: Command_Select_Join(); break;
				case TextEditCommand.Select_Empty: Command_Select_Empty(true); break;
				case TextEditCommand.Select_NonEmpty: Command_Select_Empty(false); break;
				case TextEditCommand.Select_Unique: Command_Select_Unique(); break;
				case TextEditCommand.Select_Duplicates: Command_Select_Duplicates(); break;
				case TextEditCommand.Select_RepeatedLines: Command_Select_RepeatedLines(); break;
				case TextEditCommand.Select_ByCount: Command_Select_ByCount(dialogResult as CountDialog.Result); break;
				case TextEditCommand.Select_Split: Command_Select_Split(dialogResult as SelectSplitDialog.Result); break;
				case TextEditCommand.Select_Regions: Command_Select_Regions(); break;
				case TextEditCommand.Select_FindResults: Command_Select_FindResults(); break;
				case TextEditCommand.Select_Selection_First: Command_Select_Selection_First(); break;
				case TextEditCommand.Select_Selection_CenterVertically: Command_Select_Selection_CenterVertically(); break;
				case TextEditCommand.Select_Selection_Center: Command_Select_Selection_Center(); break;
				case TextEditCommand.Select_Selection_Next: Command_Select_Selection_Next(); break;
				case TextEditCommand.Select_Selection_Previous: Command_Select_Selection_Previous(); break;
				case TextEditCommand.Select_Selection_Single: Command_Select_Selection_Single(); break;
				case TextEditCommand.Select_Selection_Remove: Command_Select_Selection_Remove(); break;
				case TextEditCommand.Region_ToggleRegionsSelections: Command_Region_ToggleRegionsSelections(); break;
				case TextEditCommand.Region_SetSelection: Command_Region_SetSelection(); break;
				case TextEditCommand.Region_SetFindResults: Command_Region_SetFindResults(); break;
				case TextEditCommand.Region_ClearRegions: Command_Region_ClearRegions(); break;
				case TextEditCommand.Region_LimitToSelection: Command_Region_LimitToSelection(); break;
				case TextEditCommand.Region_WithEnclosingRegion: Command_Region_WithEnclosingRegion(); break;
				case TextEditCommand.Region_WithoutEnclosingRegion: Command_Region_WithoutEnclosingRegion(); break;
				case TextEditCommand.Region_SelectEnclosingRegion: Command_Region_SelectEnclosingRegion(); break;
				case TextEditCommand.Region_CopyEnclosingRegion: Command_Region_CopyEnclosingRegion(); break;
				case TextEditCommand.View_Highlighting_None: Command_View_Highlighting(Highlighting.HighlightingType.None); break;
				case TextEditCommand.View_Highlighting_CSharp: Command_View_Highlighting(Highlighting.HighlightingType.CSharp); break;
				case TextEditCommand.View_Highlighting_CPlusPlus: Command_View_Highlighting(Highlighting.HighlightingType.CPlusPlus); break;
				case TextEditCommand.Macro_RepeatLastAction: if (previous != null) HandleCommand(previous.Command, previous.ShiftDown, previous.DialogResult, previous.MultiStatus); break;
				case TextEditCommand.Macro_TimeNextAction: timeNext = !timeNext; break;
			}

			var end = DateTime.UtcNow;
			var elapsed = (end - start).TotalMilliseconds;

			if ((command != TextEditCommand.Macro_TimeNextAction) && (timeNext))
			{
				timeNext = false;
				new Message
				{
					Title = "Timer",
					Text = $"Elapsed time: {elapsed:n} ms",
					Options = Message.OptionsEnum.Ok,
				}.Show();
			}
		}

		internal void Command_File_Save_Save()
		{
			if (FileName == null)
				Command_File_Save_SaveAs();
			else
				Save(FileName);
		}

		internal void Command_File_OpenWith_Disk() => Launcher.Static.LaunchDisk(FileName);

		internal void Command_File_OpenWith_HexEditor()
		{
			if (!VerifyCanFullyEncode())
				return;
			Launcher.Static.LaunchHexEditor(FileName, Data.GetBytes(CodePage), CodePage, IsModified);
			WindowParent.Remove(this, true);
		}

		string GetSaveFileName()
		{
			var dialog = new SaveFileDialog
			{
				Filter = "All files|*.*",
				FileName = Path.GetFileName(FileName) ?? DisplayName,
				InitialDirectory = Path.GetDirectoryName(FileName),
				DefaultExt = "txt",
			};
			if (dialog.ShowDialog() != true)
				return null;

			if (Directory.Exists(dialog.FileName))
				throw new Exception("A directory by that name already exists");
			if (!Directory.Exists(Path.GetDirectoryName(dialog.FileName)))
				throw new Exception("Directory doesn't exist");
			return dialog.FileName;
		}

		internal void Command_File_Save_SaveAs()
		{
			var fileName = GetSaveFileName();
			if (fileName != null)
				Save(fileName);
		}

		internal void Command_File_Operations_Rename()
		{
			if (string.IsNullOrEmpty(FileName))
			{
				Command_File_Save_SaveAs();
				return;
			}

			var fileName = GetSaveFileName();
			if (fileName == null)
				return;

			File.Delete(fileName);
			File.Move(FileName, fileName);
			FileName = fileName;
			DisplayName = null;
		}

		internal void Command_File_Refresh()
		{
			if (string.IsNullOrEmpty(FileName))
				return;

			if (!File.Exists(FileName))
				throw new Exception("This file has been deleted.");

			if (fileLastWrite != new FileInfo(FileName).LastWriteTime)
			{
				if (new Message
				{
					Title = "Confirm",
					Text = "This file has been updated on disk.  Reload?",
					Options = Message.OptionsEnum.YesNo,
					DefaultAccept = Message.OptionsEnum.Yes,
					DefaultCancel = Message.OptionsEnum.No,
				}.Show() == Message.OptionsEnum.Yes)
					Command_File_Revert();
			}
		}

		internal void Command_File_Revert()
		{
			if (IsModified)
			{
				if (new Message
				{
					Title = "Confirm",
					Text = "You have unsaved changes.  Are you sure you want to reload?",
					Options = Message.OptionsEnum.YesNo,
					DefaultAccept = Message.OptionsEnum.Yes,
					DefaultCancel = Message.OptionsEnum.No,
				}.Show() != Message.OptionsEnum.Yes)
					return;
			}

			OpenFile(FileName, keepUndo: true);
		}

		internal void Command_File_Operations_Delete()
		{
			if (FileName == null)
				throw new Exception("No filename.");

			if (new Message
			{
				Title = "Confirm",
				Text = "Are you sure you want to delete this file?",
				Options = Message.OptionsEnum.YesNo,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.No,
			}.Show() != Message.OptionsEnum.Yes)
				return;

			File.Delete(FileName);
		}

		void InsertFiles(IEnumerable<string> fileNames)
		{
			if ((Selections.Count != 1) && (Selections.Count != fileNames.Count()))
				throw new Exception("Must have either one or equal number of selections.");

			var strs = new List<string>();
			foreach (var fileName in fileNames)
			{
				var bytes = File.ReadAllBytes(fileName);
				strs.Add(Coder.BytesToString(bytes, Coder.CodePage.AutoByBOM, true));
			}

			if (Selections.Count == 1)
				ReplaceOneWithMany(strs);
			if (Selections.Count == fileNames.Count())
				ReplaceSelections(strs);
		}

		internal void Command_File_Insert_Files()
		{
			if (Selections.Count != 1)
			{
				new Message
				{
					Title = "Error",
					Text = "Must have one selection.",
					Options = Message.OptionsEnum.Ok,
				}.Show();
				return;
			}

			var dialog = new OpenFileDialog { DefaultExt = "txt", Filter = "Text files|*.txt|All files|*.*", FilterIndex = 2, Multiselect = true };
			if (dialog.ShowDialog() == true)
				InsertFiles(dialog.FileNames);
		}

		internal void Command_File_Insert_CopiedCut()
		{
			if (Selections.Count != 1)
			{
				new Message
				{
					Title = "Error",
					Text = "Must have one selection.",
					Options = Message.OptionsEnum.Ok,
				}.Show();
				return;
			}

			var files = clipboard.Strings;
			if (files.Count == 0)
				return;

			if ((files.Count > 5) && (new Message
			{
				Title = "Confirm",
				Text = $"Are you sure you want to insert these {files.Count} files?",
				Options = Message.OptionsEnum.YesNoCancel,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.Cancel,
			}.Show() != Message.OptionsEnum.Yes))
				return;

			InsertFiles(files);
		}

		internal void Command_File_Copy_Path() => SetClipboardFile(FileName);

		internal void Command_File_Copy_Name() => SetClipboardText(Path.GetFileName(FileName));

		internal void Command_File_Operations_Explore() => Process.Start("explorer.exe", $"/select,\"{FileName}\"");

		internal void Command_File_Operations_CommandPrompt() => Process.Start(new ProcessStartInfo("cmd.exe") { WorkingDirectory = Path.GetDirectoryName(FileName) });

		internal void Command_File_Operations_DragDrop()
		{
			if (string.IsNullOrWhiteSpace(FileName))
				throw new Exception("No current file.");
			if (!File.Exists(FileName))
				throw new Exception("Current file does not exist.");
			doDrag = DragType.CurrentFile;
		}

		internal EncodingDialog.Result Command_File_Encoding_Encoding_Dialog() => EncodingDialog.Run(WindowParent, CodePage, lineEndings: LineEnding ?? "");

		internal void Command_File_Encoding_Encoding(EncodingDialog.Result result)
		{
			CodePage = result.CodePage;

			if (result.LineEndings != "")
			{
				var lines = Data.NumLines;
				var sel = new List<Range>();
				for (var line = 0; line < lines; ++line)
				{
					var current = Data.GetEnding(line);
					if ((current.Length == 0) || (current == result.LineEndings))
						continue;
					var start = Data.GetOffset(line, Data.GetLineLength(line));
					sel.Add(Range.FromIndex(start, current.Length));
				}
				Replace(sel, sel.Select(str => result.LineEndings).ToList());
			}
		}

		internal EncodingDialog.Result Command_File_Encoding_ReopenWithEncoding_Dialog() => EncodingDialog.Run(WindowParent, CodePage);

		internal void Command_File_Encoding_ReopenWithEncoding(EncodingDialog.Result result)
		{
			if (IsModified)
			{
				if (new Message
				{
					Title = "Confirm",
					Text = "You have unsaved changes.  Are you sure you want to reload?",
					Options = Message.OptionsEnum.YesNo,
					DefaultAccept = Message.OptionsEnum.Yes,
					DefaultCancel = Message.OptionsEnum.No,
				}.Show() != Message.OptionsEnum.Yes)
					return;
			}

			OpenFile(FileName, codePage: result.CodePage);
		}

		internal string Command_File_Encryption_Dialog() => FileEncryptor.GetKey(WindowParent);

		internal void Command_File_Encryption(string result)
		{
			if (result == null)
				return;
			AESKey = result == "" ? null : result;
		}

		internal void Command_Edit_Undo()
		{
			var undo = undoRedo.GetUndo();
			if (undo == null)
				return;
			Selections.Replace(undo.ranges);
			ReplaceSelections(undo.text, replaceType: ReplaceType.Undo);
		}

		internal void Command_Edit_Redo()
		{
			var redo = undoRedo.GetRedo();
			if (redo == null)
				return;
			Selections.Replace(redo.ranges);
			ReplaceSelections(redo.text, replaceType: ReplaceType.Redo);
		}

		internal void Command_Edit_Copy_CutCopy(bool isCut)
		{
			var strs = GetSelectionStrings();

			if (!StringsAreFiles(strs))
				SetClipboardStrings(strs);
			else
				SetClipboardFiles(strs, isCut);
			if (isCut)
				ReplaceSelections("");
		}

		void ReplaceOneWithMany(List<string> strs)
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			var text = new List<string> { string.Join("", strs) };

			var offset = Selections.Single().Start;
			ReplaceSelections(text);
			Selections.Clear();
			foreach (var str in strs)
			{
				Selections.Add(Range.FromIndex(offset, str.Length - Data.DefaultEnding.Length));
				offset += str.Length;
			}
		}

		internal void Command_Edit_Paste_Paste(bool highlight)
		{
			var clipboardStrings = clipboard.Strings;
			if (clipboardStrings.Count == 0)
				return;

			if (clipboardStrings.Count == 1)
				clipboardStrings = Selections.Select(str => clipboardStrings[0]).ToList();

			if ((Selections.Count != 1) && (Selections.Count != clipboardStrings.Count()))
				throw new Exception("Must have either one or equal number of selections.");

			if (Selections.Count == clipboardStrings.Count)
			{
				ReplaceSelections(clipboardStrings, highlight);
				return;
			}

			clipboardStrings = clipboardStrings.Select(str => str.TrimEnd('\r', '\n') + Data.DefaultEnding).ToList();
			ReplaceOneWithMany(clipboardStrings);
		}

		internal void Command_Edit_Paste_AllFiles(string str, bool highlight) => ReplaceSelections(Selections.Select(value => str).ToList(), highlight);

		internal FindTextDialog.Result Command_Edit_Find_FindReplace_Dialog(bool isReplace)
		{
			string text = null;
			var selectionOnly = Selections.AsParallel().AsOrdered().Any(range => range.HasSelection);

			if (Selections.Count == 1)
			{
				var sel = Selections.Single();
				if ((selectionOnly) && (Data.GetOffsetLine(sel.Cursor) == Data.GetOffsetLine(sel.Highlight)) && (sel.Length < 1000))
				{
					selectionOnly = false;
					text = GetString(sel);
				}
			}

			return FindTextDialog.Run(WindowParent, isReplace ? FindTextDialog.FindTextType.Replace : FindTextDialog.FindTextType.Selections, text, selectionOnly);
		}

		internal void Command_Edit_Find_FindReplace(bool replace, bool selecting, FindTextDialog.Result result)
		{
			if ((result.KeepMatching) || (result.RemoveMatching))
			{
				var keep = result.KeepMatching;
				Selections.Replace(Selections.AsParallel().AsOrdered().Where(range => result.Regex.IsMatch(GetString(range)) == keep).ToList());
				return;
			}

			RunSearch(result);

			if ((replace) || (result.ResultType == FindTextDialog.GetRegExResultType.All))
			{
				Selections.Replace(Searches);
				Searches.Clear();

				if (replace)
					ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => result.Regex.Replace(GetString(range), result.Replace)).ToList());

				return;
			}

			FindNext(true, selecting);
		}

		internal void Command_Edit_Find_NextPrevious(bool next, bool selecting) => FindNext(next, selecting);

		internal GotoDialog.Result Command_Position_Goto_Dialog(GotoType gotoType)
		{
			int line = 1, index = 1, position = 0;
			var range = Selections.FirstOrDefault();
			if (range != null)
			{
				line = Data.GetOffsetLine(range.Start) + 1;
				index = Data.GetOffsetIndex(range.Start, line - 1) + 1;
				position = range.Start;
			}
			int startValue;
			switch (gotoType)
			{
				case GotoType.Line: startValue = Data.GetDiffLine(line - 1) + 1; break;
				case GotoType.Column: startValue = index; break;
				case GotoType.Position: startValue = position; break;
				default: throw new ArgumentException("GotoType invalid");
			}
			return GotoDialog.Run(WindowParent, gotoType, startValue, GetVariables());
		}

		internal void Command_Position_Goto(GotoType gotoType, bool selecting, GotoDialog.Result result)
		{
			var offsets = GetVariableExpressionResults<int>(result.Expression);
			if (!offsets.Any())
				return;

			var sels = Selections.ToList();

			if ((sels.Count == 0) && (gotoType == GotoType.Line))
				sels.Add(BeginRange);
			if (sels.Count == 1)
				sels = sels.Resize(offsets.Count, sels[0]).ToList();
			if (offsets.Count == 1)
				offsets = offsets.Expand(sels.Count, offsets[0]).ToList();
			if (offsets.Count != sels.Count)
				throw new Exception("Expression count doesn't match selection count");

			if (gotoType != GotoType.Position)
				offsets = offsets.Select(ofs => ofs - 1).ToList();

			switch (gotoType)
			{
				case GotoType.Line:
					Selections.Replace(sels.AsParallel().AsOrdered().Select((range, ctr) => MoveCursor(range, Data.GetNonDiffLine(offsets[ctr]), 0, selecting, false, false)).ToList());
					break;
				case GotoType.Column:
					Selections.Replace(sels.AsParallel().AsOrdered().Select((range, ctr) => MoveCursor(range, 0, offsets[ctr], selecting, true, false)).ToList());
					break;
				case GotoType.Position:
					Selections.Replace(sels.AsParallel().AsOrdered().Select((range, ctr) => MoveCursor(range, offsets[ctr], selecting)).ToList());
					break;
			}
		}

		internal void Command_Position_Goto_FilesLines()
		{
			var strs = GetSelectionStrings();
			var startPos = strs.Select(str => str.LastIndexOf("(")).ToList();
			if ((strs.Any(str => string.IsNullOrWhiteSpace(str))) || (startPos.Any(val => val == -1)) || (strs.Any(str => str[str.Length - 1] != ')')))
				throw new Exception("Format: FileName(Line)");
			var files = strs.Select((str, index) => str.Substring(0, startPos[index]).Trim()).ToList();
			var lines = strs.Select((str, index) => int.Parse(str.Substring(startPos[index] + 1, str.Length - startPos[index] - 2))).ToList();
			var data = files.Zip(lines, (file, line) => new { file, line }).GroupBy(obj => obj.file).ToDictionary(group => group.Key, group => group.Select(obj => obj.line).ToList());
			foreach (var pair in data)
			{
				var textEditor = new TextEditor(pair.Key);
				textEditor.Selections.Replace(pair.Value.Select(line => new Range(textEditor.Data.GetOffset(line - 1, 0))));
				TabsParent.CreateTab(textEditor);
			}
		}

		internal void Command_Edit_Bookmarks_Toggle()
		{
			var linePairs = Selections.AsParallel().AsOrdered().Select(range => new { start = Data.GetOffsetLine(range.Start), end = Data.GetOffsetLine(range.End) }).ToList();
			if (linePairs.Any(pair => pair.start != pair.end))
				throw new Exception("Selections must be on a single line.");

			var lineRanges = linePairs.AsParallel().AsOrdered().Select(pair => new Range(Data.GetOffset(pair.start, 0))).ToList();
			var comparer = Comparer<Range>.Create((r1, r2) => r1.Start.CompareTo(r2.Start));
			var indexes = lineRanges.AsParallel().Select(range => new { range = range, index = Bookmarks.BinarySearch(range, comparer) }).Reverse().ToList();

			if (indexes.Any(index => index.index < 0))
			{
				foreach (var pair in indexes)
					if (pair.index < 0)
						Bookmarks.Insert(~pair.index, pair.range);
			}
			else
			{
				foreach (var pair in indexes)
					Bookmarks.RemoveAt(pair.index);
			}
		}

		Range GetNextPrevBookmark(Range range, bool next, bool selecting)
		{
			int index;
			if (next)
			{
				index = Bookmarks.BinaryFindFirst(r => r.Start > range.Cursor);
				if (index == -1)
					index = 0;
			}
			else
			{
				index = Bookmarks.BinaryFindLast(r => r.Start < range.Cursor);
				if (index == -1)
					index = Bookmarks.Count - 1;
			}
			return MoveCursor(range, Bookmarks[index].Start, selecting);
		}

		internal void Command_Edit_Bookmarks_NextPreviousBookmark(bool next, bool selecting)
		{
			if (!Bookmarks.Any())
				return;
			Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => GetNextPrevBookmark(range, next, selecting)).ToList());
		}

		internal void Command_Edit_Bookmarks_Clear() => Bookmarks.Clear();

		internal void Command_File_Open_Selected()
		{
			var files = RelativeSelectedFiles();
			foreach (var file in files)
				TextEditTabs.Create(file);
		}

		internal void Command_File_Insert_Selected() => InsertFiles(RelativeSelectedFiles());

		internal void Command_Files_Operations_SaveClipboards()
		{
			var clipboardStrings = clipboard.Strings;
			if (clipboardStrings.Count != Selections.Count)
				throw new Exception("Clipboard count must match selection count.");

			for (var ctr = 0; ctr < clipboardStrings.Count; ++ctr)
			{
				var fileName = GetString(Selections[ctr]);
				var data = clipboardStrings[ctr];
				File.WriteAllText(fileName, data, Coder.GetEncoding(CodePage));
			}
		}

		internal void Command_Files_Operations_Create_Files()
		{
			var files = RelativeSelectedFiles();
			if (files.Any(file => Directory.Exists(file)))
				throw new Exception("Directory already exists");
			files = files.Where(file => !File.Exists(file)).ToList();
			var data = new byte[0];
			foreach (var file in files)
				File.WriteAllBytes(file, data);
		}

		[Flags]
		internal enum TimestampType
		{
			Write = 1,
			Access = 2,
			Create = 4,
			All = Write | Access | Create,
		}

		internal void Command_Files_Operations_Create_Directories()
		{
			var files = RelativeSelectedFiles();
			foreach (var file in files)
				Directory.CreateDirectory(file);
		}

		internal CreateFilesDialog.Result Command_Files_Operations_Create_FromExpressions_Dialog() => CreateFilesDialog.Run(WindowParent, GetVariables(), CodePage);

		internal void Command_Files_Operations_Create_FromExpressions(CreateFilesDialog.Result result)
		{
			var variables = GetVariables();

			var datas = new NEExpression(result.Data).EvaluateRows<string>(variables);
			if (!datas.Any())
				throw new Exception("No data");

			var fileNames = new NEExpression(result.FileName).EvaluateRows<string>(variables, datas.Count);
			if (!fileNames.Any())
				throw new Exception("No filenames");

			if (fileNames.Count != datas.Count)
				throw new Exception("File name and data count must match.");

			var outputs = fileNames.Zip(datas, (filename, data) => new { filename, data }).ToList();
			foreach (var output in outputs)
			{
				var bytes = Coder.StringToBytes(output.data, result.CodePage, true);
				File.WriteAllBytes(output.filename, bytes);
			}
		}

		string RunCommand(string arguments)
		{
			var output = new StringBuilder();
			output.AppendLine($"Command: {arguments}");

			var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "cmd.exe",
					Arguments = $"/c \"{arguments}\"",
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
				},
			};
			process.OutputDataReceived += (s, e) => { if (e.Data != null) output.AppendLine(e.Data); };
			process.ErrorDataReceived += (s, e) => { if (e.Data != null) output.AppendLine($"Error: {e.Data}"); };
			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			process.WaitForExit();
			return output.ToString();
		}

		internal void Command_Files_Operations_RunCommand_Parallel() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => RunCommand(GetString(range))).ToList());

		internal void Command_Files_Operations_RunCommand_Sequential() => ReplaceSelections(GetSelectionStrings().Select(str => RunCommand(str)).ToList());

		internal void Command_Files_Operations_Delete()
		{
			if (new Message
			{
				Title = "Confirm",
				Text = "Are you sure you want to delete these files/directories?",
				Options = Message.OptionsEnum.YesNo,
				DefaultCancel = Message.OptionsEnum.No,
			}.Show() != Message.OptionsEnum.Yes)
				return;

			var files = RelativeSelectedFiles();
			var answer = Message.OptionsEnum.None;
			foreach (var file in files)
			{
				try
				{
					if (File.Exists(file))
						File.Delete(file);
					if (Directory.Exists(file))
						Directory.Delete(file, true);
				}
				catch (Exception ex)
				{
					if (answer != Message.OptionsEnum.YesToAll)
						answer = new Message
						{
							Title = "Confirm",
							Text = $"An error occurred:\n\n{ex.Message}\n\nContinue?",
							Options = Message.OptionsEnum.YesNoYesAll,
							DefaultAccept = Message.OptionsEnum.Yes,
							DefaultCancel = Message.OptionsEnum.No,
						}.Show();

					if (answer == Message.OptionsEnum.No)
						break;
				}
			}
		}

		internal void Command_Files_Operations_DragDrop()
		{
			var strs = RelativeSelectedFiles();
			if (!StringsAreFiles(strs))
				throw new Exception("Selections must be files.");
			doDrag = DragType.Selections;
		}

		internal void Command_Files_Names_Simplify() => ReplaceSelections(Selections.Select(range => Path.GetFullPath(GetString(range))).ToList());

		internal MakeAbsoluteDialog.Result Command_Files_Names_MakeAbsolute_Dialog() => MakeAbsoluteDialog.Run(WindowParent, GetVariables(), true);

		internal void Command_Files_Names_MakeAbsolute(MakeAbsoluteDialog.Result result)
		{
			var results = GetFixedExpressionResults<string>(result.Expression);
			ReplaceSelections(GetSelectionStrings().Select((str, index) => new Uri(new Uri(results[index] + (result.Type == MakeAbsoluteDialog.ResultType.Directory ? "\\" : "")), str).LocalPath).ToList());
		}

		internal GetUniqueNamesDialog.Result Command_Files_Names_GetUnique_Dialog() => GetUniqueNamesDialog.Run(WindowParent);

		internal void Command_Files_Names_GetUnique(GetUniqueNamesDialog.Result result)
		{
			var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (!result.Format.Contains("{Unique}"))
				throw new Exception("Format must contain \"{Unique}\" tag");
			var newNames = new List<string>();
			var format = result.Format.Replace("{Path}", "{0}").Replace("{Name}", "{1}").Replace("{Unique}", "{2}").Replace("{Ext}", "{3}");
			foreach (var fileName in GetSelectionStrings())
			{
				var path = Path.GetDirectoryName(fileName);
				if (!string.IsNullOrEmpty(path))
					path += @"\";
				var name = Path.GetFileNameWithoutExtension(fileName);
				var ext = Path.GetExtension(fileName);
				var newFileName = fileName;
				for (var num = result.RenameAll ? 1 : 2; ; ++num)
				{
					if ((result.CheckExisting) && (FileOrDirectoryExists(newFileName)))
						used.Add(newFileName);
					if (((num != 1) || (!result.RenameAll)) && (!used.Contains(newFileName)))
						break;
					var unique = result.UseGUIDs ? Guid.NewGuid().ToString() : num.ToString();

					newFileName = string.Format(format, path, name, unique, ext);
				}
				newNames.Add(newFileName);
				used.Add(newFileName);
			}

			ReplaceSelections(newNames);
		}

		string SanitizeFileName(string fileName)
		{
			fileName = fileName.Trim();
			var start = "";
			if ((fileName.Length >= 2) && (fileName[1] == ':'))
				start = fileName.Substring(0, 2);
			fileName = fileName.Replace("/", @"\");
			fileName = Regex.Replace(fileName, "[<>:\"|?*\u0000-\u001f]", "_");
			fileName = start + fileName.Substring(start.Length);
			return fileName;
		}

		internal void Command_Files_Names_Sanitize() => ReplaceSelections(Selections.Select(range => SanitizeFileName(GetString(range))).ToList());

		string GetSize(string path)
		{
			if (File.Exists(path))
			{
				var fileinfo = new FileInfo(path);
				return fileinfo.Length.ToString();
			}

			if (Directory.Exists(path))
			{
				var dirs = new List<string> { path };
				for (var ctr = 0; ctr < dirs.Count; ++ctr)
					dirs.AddRange(Directory.EnumerateDirectories(dirs[ctr]));
				int files = 0;
				long totalSize = 0;
				foreach (var dir in dirs)
				{
					foreach (var file in Directory.EnumerateFiles(dir))
					{
						++files;
						var fileinfo = new FileInfo(file);
						totalSize += fileinfo.Length;
					}
				}

				return $"{dirs.Count} directories, {files} files, {totalSize} bytes";
			}

			return "INVALID";
		}

		internal void Command_Files_Get_Size() => ReplaceSelections(RelativeSelectedFiles().Select(file => GetSize(file)).ToList());

		internal void Command_Files_Get_WriteTime()
		{
			var files = RelativeSelectedFiles();
			var strs = new List<string>();
			foreach (var file in files)
			{
				if (File.Exists(file))
				{
					var fileinfo = new FileInfo(file);
					strs.Add(fileinfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
				}
				else if (Directory.Exists(file))
				{
					var dirinfo = new DirectoryInfo(file);
					strs.Add(dirinfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
				}
				else
					strs.Add("INVALID");
			}
			ReplaceSelections(strs);
		}

		internal void Command_Files_Get_AccessTime()
		{
			var files = RelativeSelectedFiles();
			var strs = new List<string>();
			foreach (var file in files)
			{
				if (File.Exists(file))
				{
					var fileinfo = new FileInfo(file);
					strs.Add(fileinfo.LastAccessTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
				}
				else if (Directory.Exists(file))
				{
					var dirinfo = new DirectoryInfo(file);
					strs.Add(dirinfo.LastAccessTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
				}
				else
					strs.Add("INVALID");
			}
			ReplaceSelections(strs);
		}

		internal void Command_Files_Get_CreateTime()
		{
			var files = RelativeSelectedFiles();
			var strs = new List<string>();
			foreach (var file in files)
			{
				if (File.Exists(file))
				{
					var fileinfo = new FileInfo(file);
					strs.Add(fileinfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
				}
				else if (Directory.Exists(file))
				{
					var dirinfo = new DirectoryInfo(file);
					strs.Add(dirinfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
				}
				else
					strs.Add("INVALID");
			}
			ReplaceSelections(strs);
		}

		internal void Command_Files_Get_Attributes()
		{
			var files = RelativeSelectedFiles();
			var strs = new List<string>();
			foreach (var file in files)
			{
				if (File.Exists(file))
				{
					var fileinfo = new FileInfo(file);
					strs.Add(fileinfo.Attributes.ToString());
				}
				else if (Directory.Exists(file))
				{
					var dirinfo = new DirectoryInfo(file);
					strs.Add(dirinfo.Attributes.ToString());
				}
				else
					strs.Add("INVALID");
			}
			ReplaceSelections(strs);
		}

		internal SetSizeDialog.Result Command_Files_Set_Size_Dialog() => SetSizeDialog.Run(WindowParent, GetVariables());

		void SetFileSize(string fileName, SetSizeDialog.SizeType type, long value)
		{
			var fileInfo = new FileInfo(fileName);
			if (!fileInfo.Exists)
				throw new Exception($"File doesn't exist: {fileName}");

			long length;
			switch (type)
			{
				case SetSizeDialog.SizeType.Absolute: length = value; break;
				case SetSizeDialog.SizeType.Relative: length = fileInfo.Length + value; break;
				case SetSizeDialog.SizeType.Minimum: length = Math.Max(fileInfo.Length, value); break;
				case SetSizeDialog.SizeType.Maximum: length = Math.Min(fileInfo.Length, value); break;
				case SetSizeDialog.SizeType.Multiple: length = fileInfo.Length + value - 1 - (fileInfo.Length + value - 1) % value; break;
				default: throw new ArgumentException("Invalid width type");
			}

			length = Math.Max(0, length);

			if (fileInfo.Length == length)
				return;

			using (var file = File.Open(fileName, FileMode.Open))
				file.SetLength(length);
		}

		internal void Command_Files_Set_Size(SetSizeDialog.Result result)
		{
			var results = GetFixedExpressionResults<long>(result.Expression).Select(size => size * result.Factor).ToList();
			for (var ctr = 0; ctr < Selections.Count; ++ctr)
				SetFileSize(GetString(Selections[ctr]), result.Type, results[ctr]);
		}

		internal ChooseDateTimeDialog.Result Command_Files_Set_Time_Dialog() => ChooseDateTimeDialog.Run(WindowParent, DateTime.Now);

		internal void Command_Files_Set_Time(TimestampType type, ChooseDateTimeDialog.Result result)
		{
			var files = RelativeSelectedFiles();
			foreach (var file in files)
			{
				if (!FileOrDirectoryExists(file))
					File.WriteAllBytes(file, new byte[0]);

				if (File.Exists(file))
				{
					var info = new FileInfo(file);
					if (type.HasFlag(TimestampType.Write))
						info.LastWriteTime = result.Value;
					if (type.HasFlag(TimestampType.Access))
						info.LastAccessTime = result.Value;
					if (type.HasFlag(TimestampType.Create))
						info.CreationTime = result.Value;
				}
				else if (Directory.Exists(file))
				{
					var info = new DirectoryInfo(file);
					if (type.HasFlag(TimestampType.Write))
						info.LastWriteTime = result.Value;
					if (type.HasFlag(TimestampType.Access))
						info.LastAccessTime = result.Value;
					if (type.HasFlag(TimestampType.Create))
						info.CreationTime = result.Value;
				}
			}
		}

		internal SetAttributesDialog.Result Command_Files_Set_Attributes_Dialog()
		{
			var filesAttrs = Selections.Select(range => GetString(range)).Select(file => new DirectoryInfo(file).Attributes).ToList();
			var availAttrs = Helpers.GetValues<FileAttributes>();
			var current = new Dictionary<FileAttributes, bool?>();
			foreach (var fileAttrs in filesAttrs)
				foreach (var availAttr in availAttrs)
				{
					var fileHasAttr = fileAttrs.HasFlag(availAttr);
					if (!current.ContainsKey(availAttr))
						current[availAttr] = fileHasAttr;
					if (current[availAttr] != fileHasAttr)
						current[availAttr] = null;
				}

			return SetAttributesDialog.Run(WindowParent, current);
		}

		internal void Command_Files_Set_Attributes(SetAttributesDialog.Result result)
		{
			FileAttributes andMask = 0, orMask = 0;
			foreach (var pair in result.Attributes)
			{
				andMask |= pair.Key;
				if ((pair.Value.HasValue) && (pair.Value.Value))
					orMask |= pair.Key;
			}
			foreach (var file in Selections.Select(range => GetString(range)))
				new FileInfo(file).Attributes = new FileInfo(file).Attributes & ~andMask | orMask;
		}

		internal void Command_Files_Select_Name(GetPathType type) => Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => GetPathRange(type, range)).ToList());

		static bool FileOrDirectoryExists(string name) => (Directory.Exists(name)) || (File.Exists(name));

		internal void Command_Files_Select_Existing(bool existing) => Selections.Replace(Selections.Where(range => FileOrDirectoryExists(FileName.RelativeChild(GetString(range))) == existing).ToList());

		internal void Command_Files_Select_Files() => Selections.Replace(Selections.Where(range => File.Exists(FileName.RelativeChild(GetString(range)))).ToList());

		internal void Command_Files_Select_Directories() => Selections.Replace(Selections.Where(range => Directory.Exists(FileName.RelativeChild(GetString(range)))).ToList());

		internal void Command_Files_Select_Roots(bool include)
		{
			var sels = Selections.Select(range => new { range = range, str = GetString(range).ToLower().Replace(@"\\", @"\").TrimEnd('\\') }).ToList();
			var files = sels.Select(obj => obj.str).Distinct().OrderBy().ToList();
			var roots = new HashSet<string>();
			string root = null;
			foreach (var file in files)
			{
				if ((root != null) && (file.StartsWith(root)))
					continue;

				roots.Add(file);
				root = file + @"\";
			}

			Selections.Replace(sels.AsParallel().AsOrdered().Where(sel => roots.Contains(sel.str) == include).Select(sel => sel.range).ToList());
		}

		internal HashDialog.Result Command_Files_Hash_Dialog() => HashDialog.Run(WindowParent);

		internal void Command_Files_Hash(HashDialog.Result result) => ReplaceSelections(RelativeSelectedFiles().Select(file => Hasher.Get(file, result.HashType)).ToList());

		internal void Command_Files_Operations_CopyMove(bool move)
		{
			var strs = Selections.Select(range => GetString(range).Split(new string[] { "=>" }, StringSplitOptions.None).Select(str => FileName.RelativeChild(str.Trim())).ToList()).ToList();
			if (strs.Any(pair => (pair.Count != 2) || (pair.Any(item => string.IsNullOrEmpty(item)))))
				throw new Exception("Format: Source => Destination");

			var files = strs.Select(pair => new { source = pair[0], dest = pair[1] }).Where(pair => pair.source != pair.dest).ToList();
			if (files.Count == 0)
				throw new Exception("Nothing to do!");

			const int InvalidCount = 10;
			var invalid = files.Select(pair => pair.source).Distinct().Where(file => !FileOrDirectoryExists(file)).Take(InvalidCount).ToList();
			if (invalid.Any())
				throw new Exception($"Sources don't exist:\n{string.Join("\n", invalid)}");

			invalid = files.Select(pair => Path.GetDirectoryName(pair.dest)).Distinct().Where(dir => !Directory.Exists(dir)).Take(InvalidCount).ToList();
			if (invalid.Any())
				throw new Exception($"Directories don't exist:\n{string.Join("\n", invalid)}");

			// If user specified a file and a directory, assume they want the file (with the same name) in that directory
			files = files.Select(pair => new { source = pair.source, dest = (File.Exists(pair.source)) && (Directory.Exists(pair.dest)) ? Path.Combine(pair.dest, Path.GetFileName(pair.source)) : pair.dest }).ToList();

			invalid = files.Where(pair => (Directory.Exists(pair.dest)) || ((Directory.Exists(pair.source)) && (File.Exists(pair.dest)))).Select(pair => pair.dest).Distinct().Take(InvalidCount).ToList();
			if (invalid.Any())
				throw new Exception($"Destinations already exist:\n{string.Join("\n", invalid)}");

			if (new Message
			{
				Title = "Confirm",
				Text = $"Are you sure you want to {(move ? "move" : "copy")} these {files.Count} files/directories?",
				Options = Message.OptionsEnum.YesNo,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.No,
			}.Show() != Message.OptionsEnum.Yes)
				return;

			invalid = files.Select(pair => pair.dest).Where(pair => File.Exists(pair)).Distinct().Take(InvalidCount).ToList();
			if (invalid.Any())
			{
				if (new Message
				{
					Title = "Confirm",
					Text = $"Are you sure you want to overwrite these files:\n{string.Join("\n", invalid)}",
					Options = Message.OptionsEnum.YesNo,
					DefaultAccept = Message.OptionsEnum.Yes,
					DefaultCancel = Message.OptionsEnum.No,
				}.Show() != Message.OptionsEnum.Yes)
					return;
			}

			foreach (var pair in files)
				if (Directory.Exists(pair.source))
				{
					if (move)
						Directory.Move(pair.source, pair.dest);
					else
						CopyDirectory(pair.source, pair.dest);
				}
				else
				{
					if (File.Exists(pair.dest))
						File.Delete(pair.dest);

					if (move)
						File.Move(pair.source, pair.dest);
					else
						File.Copy(pair.source, pair.dest);
				}
		}

		internal void Command_Files_Operations_OpenDisk() => Launcher.Static.LaunchDisk(files: RelativeSelectedFiles());

		internal void Command_Files_Operations_Explore()
		{
			if (Selections.Count != 1)
				throw new Exception("Can only explore one file.");
			Process.Start("explorer.exe", $"/select,\"{RelativeSelectedFiles()[0]}\"");
		}

		internal void Command_Files_Operations_CommandPrompt()
		{
			var dirs = RelativeSelectedFiles().Select(path => File.Exists(path) ? Path.GetDirectoryName(path) : path).Distinct().ToList();
			if (dirs.Count != 1)
				throw new Exception("Too many file locations.");
			Process.Start(new ProcessStartInfo("cmd.exe") { WorkingDirectory = dirs[0] });
		}

		internal void Command_Text_Case_Upper() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => GetString(range).ToUpperInvariant()).ToList());

		internal void Command_Text_Case_Lower() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => GetString(range).ToLowerInvariant()).ToList());

		internal void Command_Text_Case_Proper() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => GetString(range).ToProper()).ToList());

		internal void Command_Text_Case_Toggle() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => GetString(range).ToToggled()).ToList());

		internal void Command_Numeric_Select_Whole()
		{
			Selections.Replace(Selections.AsParallel().AsOrdered().Select(range =>
			{
				var str = GetString(range);
				var idx = str.IndexOf('.');
				if (idx == -1)
					return range;
				return Range.FromIndex(range.Start, idx);
			}).ToList());
		}

		internal void Command_Numeric_Select_Fraction()
		{
			Selections.Replace(Selections.AsParallel().AsOrdered().Select(range =>
			{
				var str = GetString(range);
				var idx = str.IndexOf('.');
				if (idx == -1)
					return Range.FromIndex(range.End, 0);
				return new Range(range.Start + idx, range.End);
			}).ToList());
		}

		internal void Command_Numeric_Hex_ToHex() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => BigInteger.Parse(GetString(range)).ToString("x")).ToList());

		internal void Command_Numeric_Hex_FromHex() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => BigInteger.Parse(GetString(range), NumberStyles.HexNumber).ToString()).ToList());

		private string ConvertBase(string str, Dictionary<char, int> inputSet, Dictionary<int, char> outputSet)
		{
			BigInteger value = 0;
			for (var ctr = 0; ctr < str.Length; ++ctr)
				value = value * inputSet.Count + inputSet[str[ctr]];
			var output = new LinkedList<char>();
			while (value != 0)
			{
				output.AddFirst(outputSet[(int)(value % outputSet.Count)]);
				value /= outputSet.Count;
			}
			return new string(output.ToArray());
		}

		internal ConvertBaseDialog.Result Command_Numeric_ConvertBase_Dialog() => ConvertBaseDialog.Run(WindowParent);

		internal void Command_Numeric_ConvertBase(ConvertBaseDialog.Result result) => ReplaceSelections(GetSelectionStrings().Select(str => ConvertBase(str, result.InputSet, result.OutputSet)).ToList());

		internal void Command_DateTime_Now() => ReplaceSelections(DateTime.Now.ToString("O"));

		internal ConvertDateTimeDialog.Result Command_DateTime_Convert_Dialog()
		{
			if (Selections.Count < 1)
				return null;

			return ConvertDateTimeDialog.Run(WindowParent, GetString(Selections.First()));
		}

		internal void Command_DateTime_Convert(ConvertDateTimeDialog.Result result) => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => ConvertDateTimeDialog.ConvertFormat(GetString(range), result.InputFormat, result.InputUTC, result.OutputFormat, result.OutputUTC)).ToList());

		internal ConvertDialog.Result Command_Edit_Convert_Dialog() => ConvertDialog.Run(WindowParent);

		internal void Command_Edit_Convert(ConvertDialog.Result result) => ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, num) => Coder.BytesToString(Coder.StringToBytes(GetString(range), result.InputType, result.InputBOM), result.OutputType, result.OutputBOM)).ToList());

		internal void Command_Text_Length() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => range.Length.ToString()).ToList());

		internal WidthDialog.Result Command_Text_Width_Dialog()
		{
			var minLength = Selections.Any() ? Selections.AsParallel().Min(range => range.Length) : 0;
			var maxLength = Selections.Any() ? Selections.AsParallel().Max(range => range.Length) : 0;
			var numeric = Selections.Any() ? Selections.AsParallel().All(range => GetString(range).IsNumeric()) : false;
			return WidthDialog.Run(WindowParent, minLength, maxLength, numeric, false, GetVariables());
		}

		internal void Command_Text_Width(WidthDialog.Result result)
		{
			var results = GetFixedExpressionResults<int>(result.Expression);
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, index) => SetWidth(GetString(range), result, results[index])).ToList());
		}

		internal TrimDialog.Result Command_Text_Trim_Dialog()
		{
			var numeric = Selections.AsParallel().All(range => GetString(range).IsNumeric());
			return TrimDialog.Run(WindowParent, numeric);
		}

		string TrimString(string str, TrimDialog.Result result)
		{
			switch (result.Location)
			{
				case TrimDialog.TrimLocation.Start: return str.TrimStart(result.TrimChars);
				case TrimDialog.TrimLocation.Both: return str.Trim(result.TrimChars);
				case TrimDialog.TrimLocation.End: return str.TrimEnd(result.TrimChars);
				default: throw new Exception("Invalid location");
			}
		}

		internal void Command_Text_Trim(TrimDialog.Result result) => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(str => TrimString(GetString(str), result)).ToList());

		internal void Command_Text_SingleLine() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => GetString(range).Replace("\r", "").Replace("\n", "")).ToList());

		internal GetExpressionDialog.Result Command_Expression_Expression_Dialog() => GetExpressionDialog.Run(WindowParent, GetVariables(), Selections.Count);

		internal void Command_Expression_Expression(GetExpressionDialog.Result result) => ReplaceSelections(GetFixedExpressionResults<string>(result.Expression));

		internal void Command_Expression_Copy(GetExpressionDialog.Result result) => SetClipboardStrings(GetVariableExpressionResults<string>(result.Expression));

		internal void Command_Expression_EvaluateSelected() => ReplaceSelections(GetFixedExpressionResults<string>("Eval(x)"));

		internal void Command_Numeric_Series_ZeroBased() => ReplaceSelections(Selections.Select((range, index) => index.ToString()).ToList());

		internal void Command_Numeric_Series_OneBased() => ReplaceSelections(Selections.Select((range, index) => (index + 1).ToString()).ToList());

		internal NumericSeriesDialog.Result Command_Numeric_Series_LinearGeometric_Dialog(bool linear)
		{
			var nonNulls = Selections.AsParallel().AsOrdered().Select((range, index) => new { str = GetString(range), index = index }).Where(obj => !string.IsNullOrWhiteSpace(obj.str)).Select(obj => Tuple.Create(double.Parse(obj.str), obj.index)).ToList();
			if (nonNulls.Count == 0)
				return NumericSeriesDialog.Run(WindowParent, 1, 1);

			if (nonNulls.Count == 1)
				return NumericSeriesDialog.Run(WindowParent, 1, (nonNulls[0].Item1 - 1) / nonNulls[0].Item2);

			var first = nonNulls.First();
			var last = nonNulls.Last();

			var multiplier = linear ? (last.Item1 - first.Item1) / (last.Item2 - first.Item2) : Math.Pow(last.Item1 / first.Item1, 1.0 / (last.Item2 - first.Item2));
			var start = linear ? first.Item1 - multiplier * first.Item2 : first.Item1 / Math.Pow(multiplier, first.Item2);

			return NumericSeriesDialog.Run(WindowParent, start, multiplier);
		}

		internal void Command_Numeric_Series_Linear(NumericSeriesDialog.Result result) => ReplaceSelections(Selections.Select((range, index) => (result.Multiplier * index + result.Start).ToString()).ToList());

		internal void Command_Numeric_Series_Geometric(NumericSeriesDialog.Result result) => ReplaceSelections(Selections.Select((range, index) => (Math.Pow(result.Multiplier, index) * result.Start).ToString()).ToList());

		decimal Floor(decimal number, decimal interval) => Math.Truncate(number / interval) * interval;

		decimal Ceiling(decimal number, decimal interval)
		{
			var val = number / interval;
			var intPart = Math.Truncate(val);
			return (intPart + (val - intPart != 0m ? 1 : 0)) * interval;
		}

		internal ScaleDialog.Result Command_Numeric_Scale_Dialog() => ScaleDialog.Run(WindowParent);

		internal void Command_Numeric_Scale(ScaleDialog.Result result)
		{
			var ratio = (result.NewMax - result.NewMin) / (result.PrevMax - result.PrevMin);
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => ((double.Parse(GetString(range)) - result.PrevMin) * ratio + result.NewMin).ToString()).ToList());
		}

		internal void Command_Numeric_ForwardReverseSum(bool forward)
		{
			var numbers = Selections.AsParallel().AsOrdered().Select(range => double.Parse(GetString(range))).ToList();
			double total = 0;
			var start = forward ? 0 : numbers.Count - 1;
			var end = forward ? numbers.Count : -1;
			var step = forward ? 1 : -1;
			for (var ctr = start; ctr != end; ctr += step)
			{
				total += numbers[ctr];
				numbers[ctr] = total;
			}
			ReplaceSelections(numbers.Select(num => num.ToString()).ToList());
		}

		internal void Command_Numeric_Whole()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => GetString(range)).Select(str =>
			{
				var idx = str.IndexOf('.');
				if (idx == -1)
					return str;
				return str.Substring(0, idx);
			}).ToList());
		}

		internal void Command_Numeric_Fraction()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => GetString(range)).Select(str =>
			{
				var idx = str.IndexOf('.');
				if (idx == -1)
					return "0";
				return str.Substring(idx);
			}).ToList());
		}

		internal FloorRoundCeilingDialog.Result Command_Numeric_Floor_Dialog() => FloorRoundCeilingDialog.Run(WindowParent);

		internal void Command_Numeric_Floor(FloorRoundCeilingDialog.Result result) => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Floor(decimal.Parse(GetString(range)), result.Interval).ToString()).ToList());

		internal FloorRoundCeilingDialog.Result Command_Numeric_Round_Dialog() => FloorRoundCeilingDialog.Run(WindowParent);

		internal void Command_Numeric_Round(FloorRoundCeilingDialog.Result result) => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => (Math.Round(decimal.Parse(GetString(range)) / result.Interval, MidpointRounding.AwayFromZero) * result.Interval).ToString()).ToList());

		internal FloorRoundCeilingDialog.Result Command_Numeric_Ceiling_Dialog() => FloorRoundCeilingDialog.Run(WindowParent);

		internal void Command_Numeric_Ceiling(FloorRoundCeilingDialog.Result result) => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Ceiling(decimal.Parse(GetString(range)), result.Interval).ToString()).ToList());

		string Factor(BigInteger value)
		{
			var factors = new List<BigInteger>();
			if (value < 0)
			{
				factors.Add(-1);
				value = -value;
			}

			BigInteger factor = 2;
			while (value > 1)
			{
				if (value % factor == 0)
				{
					factors.Add(factor);
					value /= factor;
					continue;
				}

				++factor;
			}

			if (!factors.Any())
				factors.Add(value);

			factors.Reverse();

			return string.Join("*", factors);
		}

		internal void Command_Numeric_Factor() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Factor(BigInteger.Parse(GetString(range)))).ToList());

		internal void Command_Edit_CopyDown()
		{
			var strs = GetSelectionStrings();
			var index = 0;
			for (var ctr = 0; ctr < strs.Count; ++ctr)
				if (string.IsNullOrWhiteSpace(strs[ctr]))
					strs[ctr] = strs[index];
				else
					index = ctr;
			ReplaceSelections(strs);
		}

		internal void Command_File_Copy_Count() => SetClipboardText(Selections.Count.ToString());

		internal void Command_Text_Copy_Length() => SetClipboardStrings(Selections.Select(range => range.Length.ToString()));

		internal enum Command_MinMax_Type { String, Numeric, Length }
		internal Tuple<string, List<Range>> DoCommand_Type_MinMax<Input>(bool min, Func<Range, Input> select, Comparer<Input> sort)
		{
			if (!Selections.Any())
				throw new Exception("No selections");
			var selections = Selections.AsParallel().GroupBy(range => select(range)).OrderBy(group => group.Key, sort);
			var result = min ? selections.First() : selections.Last();
			return Tuple.Create(result.Key.ToString(), result.ToList());
		}

		internal Tuple<string, List<Range>> Command_Type_Copy_MinMax2(bool min, Command_MinMax_Type type)
		{
			switch (type)
			{
				case Command_MinMax_Type.String: return DoCommand_Type_MinMax(min, range => GetString(range), Comparer<string>.Default);
				case Command_MinMax_Type.Numeric: return DoCommand_Type_MinMax(min, range => GetString(range), Comparer<string>.Create((x, y) => x.CompareWithNumeric(y)));
				case Command_MinMax_Type.Length: return DoCommand_Type_MinMax(min, range => range.Length, Comparer<int>.Default);
				default: throw new Exception("Invalid type");
			}
		}

		internal void Command_Type_Copy_MinMax(bool min, Command_MinMax_Type type) => SetClipboardText(Command_Type_Copy_MinMax2(min, type).Item1);

		internal void Command_Type_Select_MinMax(bool min, Command_MinMax_Type type) => Selections.Replace(Command_Type_Copy_MinMax2(min, type).Item2);

		internal void Command_Numeric_Copy_Sum() => SetClipboardText(Selections.AsParallel().Select(range => double.Parse(GetString(range))).Sum().ToString());

		static BigInteger GCF(BigInteger value1, BigInteger value2)
		{
			while (value2 != 0)
			{
				var newValue = value1 % value2;
				value1 = value2;
				value2 = newValue;
			}

			return value1;
		}

		internal void Command_Numeric_Copy_GCF()
		{
			if (Selections.Count <= 0)
				return;

			var nums = Selections.AsParallel().Select(range => BigInteger.Abs(BigInteger.Parse(GetString(range)))).ToList();
			if (nums.Any(factor => factor == 0))
				throw new Exception("Factors cannot be 0");

			var gcf = nums[0];
			for (var ctr = 1; ctr < nums.Count; ++ctr)
				gcf = GCF(gcf, nums[ctr]);

			SetClipboardText(gcf.ToString());
		}

		internal void Command_Numeric_Copy_LCM()
		{
			if (Selections.Count <= 0)
				return;

			var nums = Selections.AsParallel().Select(range => BigInteger.Abs(BigInteger.Parse(GetString(range)))).ToList();
			if (nums.Any(factor => factor == 0))
				throw new Exception("Factors cannot be 0");

			var lcm = nums[0];
			for (var ctr = 1; ctr < nums.Count; ++ctr)
				lcm *= nums[ctr] / GCF(lcm, nums[ctr]);

			SetClipboardText(lcm.ToString());
		}

		internal void Command_Position_Copy(GotoType gotoType)
		{
			if (gotoType == GotoType.Position)
			{
				SetClipboardStrings(Selections.Select(range => $"{range.Start}{(range.HasSelection ? $"-{range.End}" : "")}"));
				return;
			}

			var starts = Selections.Select(range => range.Start).ToList();
			var lines = starts.Select(pos => Data.GetOffsetLine(pos)).ToList();
			if (gotoType == GotoType.Line)
			{
				SetClipboardStrings(lines.Select(pos => (Data.GetDiffLine(pos) + 1).ToString()));
				return;
			}

			var indexes = starts.Select((pos, line) => Data.GetOffsetIndex(pos, lines[line])).ToList();
			SetClipboardStrings(indexes.Select(pos => (pos + 1).ToString()));
		}

		internal RepeatDialog.Result Command_Edit_Repeat_Dialog() => RepeatDialog.Run(WindowParent, Selections.Count == 1, GetVariables());

		internal void Command_Edit_Repeat(RepeatDialog.Result result)
		{
			var results = GetFixedExpressionResults<int>(result.Expression);
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, index) => RepeatString(GetString(range), results[index])).ToList());
			if (result.SelectRepetitions)
			{
				var sels = new List<Range>();
				for (var ctr = 0; ctr < Selections.Count; ++ctr)
				{
					var selection = Selections[ctr];
					var repeatCount = results[ctr];
					var len = selection.Length / repeatCount;
					for (var index = selection.Start; index < selection.End; index += len)
						sels.Add(new Range(index + len, index));
				}
				Selections.Replace(sels);
			}
		}

		internal void Command_Edit_Markup_Escape() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => HttpUtility.HtmlEncode(GetString(range))).ToList());

		internal void Command_Edit_RegEx_Escape() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Regex.Escape(GetString(range))).ToList());

		internal void Command_Edit_URL_Escape() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => HttpUtility.UrlEncode(GetString(range))).ToList());

		internal void Command_Edit_Markup_Unescape() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => HttpUtility.HtmlDecode(GetString(range))).ToList());

		internal void Command_Edit_RegEx_Unescape() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Regex.Unescape(GetString(range))).ToList());

		internal void Command_Edit_URL_Unescape() => ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => HttpUtility.UrlDecode(GetString(range))).ToList());

		internal HashTextDialog.Result Command_Edit_Hash_Dialog() => HashTextDialog.Run(WindowParent, CodePage);

		internal void Command_Edit_Hash(HashTextDialog.Result result)
		{
			var strs = GetSelectionStrings();
			if (!VerifyCanFullyEncode(strs, result.CodePage))
				return;

			ReplaceSelections(strs.AsParallel().AsOrdered().Select(str => Hasher.Get(Coder.StringToBytes(str, result.CodePage), result.HashType, result.HMACKey)).ToList());
		}

		async Task<string> GetURL(string url, Coder.CodePage codePage = Coder.CodePage.None)
		{
			using (var client = new WebClient())
			{
				client.Encoding = Encoding.UTF8;
				var uri = new Uri(url);
				if (codePage == Coder.CodePage.None)
					return await client.DownloadStringTaskAsync(uri);

				var data = await client.DownloadDataTaskAsync(uri);
				return Coder.BytesToString(data, codePage);
			}
		}

		async Task<List<Tuple<string, string, bool>>> GetURLs(List<string> urls, Coder.CodePage codePage = Coder.CodePage.None)
		{
			var tasks = urls.Select(url => GetURL(url, codePage)).ToList();
			var results = new List<Tuple<string, string, bool>>();
			for (var ctr = 0; ctr < tasks.Count; ++ctr)
			{
				string data;
				bool error = false;
				try { data = await tasks[ctr]; }
				catch (Exception ex)
				{
					error = true;
					data = $"<error>{Data.DefaultEnding}";
					data += $"\t<url>{urls[ctr]}</url>{Data.DefaultEnding}";
					data += $"\t<data>{Data.DefaultEnding}";
					for (; ex != null; ex = ex.InnerException)
						data += $"\t\t{ex.Message}{Data.DefaultEnding}";
					data += $"\t</data>{Data.DefaultEnding}";
					data += $"</error>{Data.DefaultEnding}";
				}
				results.Add(Tuple.Create(urls[ctr], data, error));
			}
			return results;
		}

		internal MakeAbsoluteDialog.Result Command_Edit_URL_Absolute_Dialog() => MakeAbsoluteDialog.Run(WindowParent, GetVariables(), false);

		internal void Command_Edit_URL_Absolute(MakeAbsoluteDialog.Result result)
		{
			var results = GetFixedExpressionResults<string>(result.Expression);
			ReplaceSelections(GetSelectionStrings().Select((str, index) =>
			{
				var uri = new Uri(new Uri(results[index]), str);
				return uri.AbsoluteUri;
			}).ToList());
		}

		internal ChooseColorDialog.Result Command_Edit_Color_Dialog() => ChooseColorDialog.Run(WindowParent, Selections.Select(range => GetString(range)).FirstOrDefault());

		internal void Command_Edit_Color(ChooseColorDialog.Result result) => ReplaceSelections(result.Color);

		internal void Command_Diff_Selections()
		{
			if (!Selections.Any())
				return;

			if (Selections.Count % 2 != 0)
				throw new Exception("Must have even number of selections.");

			var codePage = CodePage; // Must save as other threads can't access DependencyProperties
			var tabs = TextEditTabs.CreateDiff();
			var batches = Selections.AsParallel().AsOrdered().Select(range => GetString(range)).Select(str => Coder.StringToBytes(str, codePage)).Batch(2).Select(batch => batch.ToList()).ToList();
			foreach (var batch in batches)
				tabs.AddDiff(bytes1: batch[0], bytes2: batch[1], codePage1: codePage, codePage2: codePage, modified1: false, modified2: false);
		}

		internal void Command_Diff_SelectedFiles()
		{
			if (!Selections.Any())
				return;

			if (Selections.Count % 2 != 0)
				throw new Exception("Must have even number of selections.");

			var files = GetSelectionStrings();
			if (files.Any(file => !File.Exists(file)))
				throw new Exception("Selections must be files.");

			var tabs = TextEditTabs.CreateDiff();
			var batches = files.Batch(2).Select(batch => batch.ToList()).ToList();
			foreach (var batch in batches)
				tabs.AddDiff(fileName1: batch[0], fileName2: batch[1]);
		}

		internal void Command_Diff_Break() => DiffTarget = null;

		internal void Command_Diff_IgnoreWhitespace(bool? multiStatus)
		{
			DiffIgnoreWhitespace = multiStatus != true;
			CalculateDiff();
		}

		internal void Command_Diff_IgnoreCase(bool? multiStatus)
		{
			DiffIgnoreCase = multiStatus != true;
			CalculateDiff();
		}

		internal void Command_Diff_IgnoreNumbers(bool? multiStatus)
		{
			DiffIgnoreNumbers = multiStatus != true;
			CalculateDiff();
		}

		internal void Command_Diff_IgnoreLineEndings(bool? multiStatus)
		{
			DiffIgnoreLineEndings = multiStatus != true;
			CalculateDiff();
		}

		Tuple<int, int> GetDiffNextPrevious(Range range, bool next)
		{
			var offset = next ? range.End : Math.Max(0, range.Start - 1);
			var line = Data.GetOffsetLine(offset);
			var delta = next ? 1 : -1;
			int? start = null;
			while (true)
			{
				line += delta;
				if ((line < 0) || (line >= Data.NumLines) || ((start.HasValue) && (Data.GetLineDiffStatus(line) == LCS.MatchType.Match)))
				{
					line = Math.Max(-1, Math.Min(line, Data.NumLines - 1));
					if (!start.HasValue)
						start = line;
					if (next)
						return Tuple.Create(start.Value, line);
					return Tuple.Create(line + 1, start.Value + 1);
				}
				if ((!start.HasValue) && (Data.GetLineDiffStatus(line) != LCS.MatchType.Match))
					start = line;
			}
		}

		internal void Command_Diff_NextPrevious(bool next)
		{
			if (DiffTarget == null)
				return;

			if ((TabsParent.GetIndex(this) < DiffTarget.TabsParent.GetIndex(DiffTarget)) && (DiffTarget.Active))
				return;

			var lines = Selections.AsParallel().AsOrdered().Select(range => GetDiffNextPrevious(range, next)).ToList();
			for (var pass = 0; pass < 2; ++pass)
			{
				var target = pass == 0 ? this : DiffTarget;
				target.Selections.Replace(lines.Select(tuple => new Range(target.Data.GetOffset(tuple.Item1, 0), target.Data.GetOffset(tuple.Item2, 0))).ToList());
			}
		}

		internal void Command_Diff_CopyLeftRight(bool moveLeft)
		{
			if (DiffTarget == null)
				return;

			TextEditor left, right;
			if (TabsParent.GetIndex(this) < DiffTarget.TabsParent.GetIndex(DiffTarget))
			{
				left = this;
				right = DiffTarget;
			}
			else
			{
				left = DiffTarget;
				right = this;
			}

			if (moveLeft)
				left.ReplaceSelections(right.GetSelectionStrings());
			else
				right.ReplaceSelections(left.GetSelectionStrings());
		}

		internal void Command_Diff_SelectMatch(bool matching)
		{
			if (DiffTarget == null)
				return;

			Selections.Replace(Data.GetDiffMatches(matching).Select(tuple => new Range(tuple.Item2, tuple.Item1)));
		}

		internal void Command_Text_GUID() => ReplaceSelections(Selections.AsParallel().Select(range => Guid.NewGuid().ToString()).ToList());

		internal RandomNumberDialog.Result Command_Numeric_RandomNumber_Dialog() => RandomNumberDialog.Run(WindowParent);

		internal void Command_Numeric_RandomNumber(RandomNumberDialog.Result result) => ReplaceSelections(Selections.AsParallel().Select(range => random.Next(result.MinValue, result.MaxValue + 1).ToString()).ToList());

		internal RandomDataDialog.Result Command_Text_RandomText_Dialog() => RandomDataDialog.Run(GetVariables(), WindowParent);

		string GetRandomData(string chars, int length) => new string(Enumerable.Range(0, length).Select(num => chars[random.Next(chars.Length)]).ToArray());

		internal void Command_Text_RandomText(RandomDataDialog.Result result)
		{
			var results = GetFixedExpressionResults<int>(result.Expression);
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, index) => GetRandomData(result.Chars, results[index])).ToList());
		}

		internal void Command_Text_LoremIpsum() => ReplaceSelections(new LoremGenerator().GetSentences().Take(Selections.Count).ToList());

		internal MinMaxValuesDialog.Result Command_Numeric_MinMaxValues_Dialog() => MinMaxValuesDialog.Run(WindowParent);

		internal void Command_Numeric_MinMaxValues(MinMaxValuesDialog.Result result) => ReplaceSelections(string.Join(" ", new List<string> { result.Min ? result.CodePage.MinValue() : null, result.Max ? result.CodePage.MaxValue() : null }.Where(str => !string.IsNullOrEmpty(str))));

		internal CombinationsPermutationsDialog.Result Command_Numeric_CombinationsPermutations_Dialog()
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			return CombinationsPermutationsDialog.Run(WindowParent);
		}

		internal void Command_Numeric_CombinationsPermutations(CombinationsPermutationsDialog.Result result)
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			var output = new List<List<string>>();
			var nums = new int[result.UseCount];
			var used = new bool[result.ItemCount];
			nums[0] = -1;
			var onNum = 0;
			while (true)
			{
				++nums[onNum];
				if (nums[onNum] >= result.ItemCount)
				{
					--onNum;
					if (onNum < 0)
						break;
					used[nums[onNum]] = false;
					continue;
				}
				if ((!result.Repeat) && (used[nums[onNum]]))
					continue;

				used[nums[onNum]] = true;
				++onNum;
				if (onNum < result.UseCount)
				{
					if (result.Type == CombinationsPermutationsDialog.CombinationsPermutationsType.Combinations)
						nums[onNum] = nums[onNum - 1] - 1;
					else
						nums[onNum] = -1;
				}
				else
				{
					output.Add(nums.Select(num => (num + 1).ToString()).ToList());
					--onNum;
					used[nums[onNum]] = false;
				}
			}

			ReplaceSelections(string.Join("", output.Select(row => string.Join(" ", row) + Data.DefaultEnding)));

			var start = Selections.Single().Start;
			var sels = new List<Range>();
			foreach (var row in output)
			{
				foreach (var str in row)
				{
					sels.Add(Range.FromIndex(start, str.Length));
					start += str.Length + 1; // +1 is for space
				}
				start += Data.DefaultEnding.Length - 1; // -1 is for space added before
			}
			Selections.Replace(sels);
		}

		internal RevRegExDialog.Result Command_Text_ReverseRegEx_Dialog()
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			return RevRegExDialog.Run(WindowParent);
		}

		internal void Command_Text_ReverseRegEx(RevRegExDialog.Result result)
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			var data = RevRegEx.RevRegExVisitor.Parse(result.RegEx);
			var output = data.GetPossibilities().Select(str => str + Data.DefaultEnding).ToList();
			ReplaceSelections(string.Join("", output));

			var start = Selections.Single().Start;
			var sels = new List<Range>();
			foreach (var str in output)
			{
				sels.Add(Range.FromIndex(start, str.Length - Data.DefaultEnding.Length));
				start += str.Length;
			}
			Selections.Replace(sels);
		}

		internal void Command_Network_Fetch(Coder.CodePage codePage = Coder.CodePage.None)
		{
			var urls = GetSelectionStrings();
			var results = Task.Run(() => GetURLs(urls, codePage).Result).Result;
			if (results.Any(result => result.Item3))
				new Message
				{
					Title = "Error",
					Text = $"Failed to fetch the URLs:\n{string.Join("\n", results.Where(result => result.Item3).Select(result => result.Item1))}",
					Options = Message.OptionsEnum.Ok,
				}.Show();
			ReplaceSelections(results.Select(result => result.Item2).ToList());
		}

		internal void Command_Network_Lookup_IP() { ReplaceSelections(Task.Run(async () => await Task.WhenAll(GetSelectionStrings().Select(async name => { try { return string.Join(" / ", (await Dns.GetHostEntryAsync(name)).AddressList.Select(address => address.ToString()).Distinct()); } catch { return "<ERROR>"; } }).ToList())).Result.ToList()); }

		internal void Command_Network_Lookup_HostName() { ReplaceSelections(Task.Run(async () => await Task.WhenAll(GetSelectionStrings().Select(async name => { try { return (await Dns.GetHostEntryAsync(name)).HostName; } catch { return "<ERROR>"; } }).ToList())).Result.ToList()); }

		internal void Command_Network_AdaptersInfo()
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			var data = new List<List<string>>();
			data.Add(new List<string>
			{
				"Name",
				"Desc",
				"Status",
				"Type",
				"IPs"
			});
			foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces().OrderBy(inter => inter.OperationalStatus).ThenBy(inter => inter.Name))
			{
				var props = networkInterface.GetIPProperties();
				data.Add(new List<string>
				{
					networkInterface.Name,
					networkInterface.Description,
					networkInterface.OperationalStatus.ToString(),
					networkInterface.NetworkInterfaceType.ToString(),
					string.Join(" / ", props.UnicastAddresses.Select(info=>info.Address)),
				});
			}
			var columnLens = data[0].Select((item, column) => data.Max(row => row[column].Length)).ToList();
			ReplaceOneWithMany(data.Select(row => string.Join("│", row.Select((item, column) => item + new string(' ', columnLens[column] - item.Length))) + Data.DefaultEnding).ToList());
		}

		internal PingDialog.Result Command_Network_Ping_Dialog() => PingDialog.Run(WindowParent);

		internal void Command_Network_Ping(PingDialog.Result result)
		{
			var replies = Task.Run(async () =>
			{
				var strs = GetSelectionStrings().Select(async str =>
				{
					try
					{
						using (var ping = new Ping())
						{
							var reply = await ping.SendPingAsync(IPAddress.Parse(str), result.Timeout);
							return $"{str}: {reply.Status}{(reply.Status == IPStatus.Success ? $": {reply.RoundtripTime} ms" : "")}";
						}
					}
					catch (Exception ex)
					{
						return $"{str}: {ex.Message}";
					}
				}).ToList();
				return await Task.WhenAll(strs);
			}).Result.ToList();
			ReplaceSelections(replies);
		}

		internal ScanPortsDialog.Result Command_Network_ScanPorts_Dialog() => ScanPortsDialog.Run(WindowParent);

		internal void Command_Network_ScanPorts(ScanPortsDialog.Result result)
		{
			var strs = GetSelectionStrings();
			var results = PortScanner.ScanPorts(strs.Select(str => IPAddress.Parse(str)).ToList(), result.Ports, result.Attempts, TimeSpan.FromMilliseconds(result.Timeout), result.Concurrency);
			ReplaceSelections(strs.Zip(results, (str, strResult) => $"{str}: {string.Join(", ", strResult)}").ToList());
		}

		internal DatabaseConnectDialog.Result Command_Database_Connect_Dialog() => DatabaseConnectDialog.Run(WindowParent);

		DbConnection dbConnection;
		internal void Command_Database_Connect(DatabaseConnectDialog.Result result)
		{
			if (dbConnection != null)
			{
				dbConnection.Dispose();
				dbConnection = null;
			}
			dbConnection = result.DBConnectInfo.GetConnection();
		}

		Tuple<string, Table> RunDBSelect(string commandText)
		{
			var tableName = Regex.Match(commandText, @"\bFROM\b.*?([\[\]a-z\.]+)", RegexOptions.IgnoreCase).Groups[1].Value.Replace("[", "").Replace("]", "").CoalesceNullOrEmpty();
			using (var command = dbConnection.CreateCommand())
			{
				command.CommandText = commandText;
				using (var reader = command.ExecuteReader())
				{
					if (reader.FieldCount == 0)
						return null;
					return Tuple.Create(tableName, new Table(reader));
				}
			}
		}

		void ValidateConnection()
		{
			if (dbConnection == null)
				throw new Exception("No connection.");
		}

		internal void Command_Database_ExecuteQuery()
		{
			ValidateConnection();
			var selections = Selections.ToList();
			if ((Selections.Count == 1) && (!Selections[0].HasSelection))
				selections = new List<Range> { FullRange };
			var tables = selections.Select(range => RunDBSelect(GetString(range))).ToList();
			if (!tables.NonNull().Any())
			{
				Message.Show($"Quer{(selections.Count == 1 ? "y" : "ies")} run successfully.");
				return;
			}

			if (!UseCurrentWindow)
			{
				foreach (var table in tables)
					OpenTable(table.Item2, table.Item1);
				return;
			}

			ReplaceSelections(tables.Select(table => table == null ? "Success" : GetTableText(table.Item2)).ToList());
		}

		internal void Command_Database_UseCurrentWindow(bool? multiStatus) => UseCurrentWindow = multiStatus != true;

		string DBSanitize(string name) => (!string.IsNullOrEmpty(name)) && (!char.IsLetter(name[0])) ? $"[{name}]" : name;

		internal string Command_Database_QueryTable_Dialog()
		{
			ValidateConnection();
			var tableSchema = dbConnection.GetSchema("Tables");
			var tableCatalogColumn = tableSchema.Columns["table_catalog"];
			var tableSchemaColumn = tableSchema.Columns["table_schema"];
			var tableNameColumn = tableSchema.Columns["table_name"];
			List<string> tables;
			if (dbConnection is MySql.Data.MySqlClient.MySqlConnection)
				tables = tableSchema.Rows.Cast<DataRow>().Select(row => $"{DBSanitize(row[tableSchemaColumn]?.ToString())}.{DBSanitize(row[tableNameColumn]?.ToString())}").ToList();
			else
				tables = tableSchema.Rows.Cast<DataRow>().Select(row => $"{DBSanitize(row[tableCatalogColumn]?.ToString())}.{DBSanitize(row[tableSchemaColumn]?.ToString())}.{DBSanitize(row[tableNameColumn]?.ToString())}").ToList();

			return QueryTableDialog.Run(WindowParent, tables);
		}

		internal void Command_Database_QueryTable(string result) => ReplaceSelections(result);

		internal void Command_Database_Examine_Dialog()
		{
			ValidateConnection();
			ExamineDatabaseDialog.Run(WindowParent, dbConnection);
		}

		internal void Command_Region_ToggleRegionsSelections()
		{
			if (Selections.Count > 1)
			{
				Regions.AddRange(Selections);
				Selections.Replace(Selections[visibleIndex]);
			}
			else if (Regions.Count != 0)
			{
				Selections.Replace(Regions);
				Regions.Clear();
			}
		}

		internal void Command_Select_All() => Selections.Replace(FullRange);

		internal void Command_Select_Nothing() => Selections.Clear();

		internal LimitDialog.Result Command_Select_Limit_Dialog() => LimitDialog.Run(WindowParent, Selections.Count, GetVariables());

		internal void Command_Select_Limit(LimitDialog.Result result)
		{
			var variables = GetVariables();
			var firstSel = new NEExpression(result.FirstSel).EvaluateRow<int>(variables);
			var selMult = new NEExpression(result.SelMult).EvaluateRow<int>(variables);
			var numSels = new NEExpression(result.NumSels).EvaluateRow<int>(variables);

			IEnumerable<Range> retval = Selections;

			if (result.IgnoreBlank)
				retval = retval.Where(sel => sel.HasSelection);

			retval = retval.Skip(firstSel - 1);
			retval = retval.EveryNth(selMult);
			retval = retval.Take(numSels);
			Selections.Replace(retval.ToList());
		}

		internal void Command_Select_Lines()
		{
			var lineSets = Selections.AsParallel().Select(range => new { start = Data.GetOffsetLine(range.Start), end = Data.GetOffsetLine(Math.Max(range.Start, range.End - 1)) }).ToList();

			var hasLine = new bool[Data.NumLines];
			foreach (var set in lineSets)
				for (var ctr = set.start; ctr <= set.end; ++ctr)
					hasLine[ctr] = true;

			var lines = new List<int>();
			for (var line = 0; line < hasLine.Length; ++line)
				if (hasLine[line])
					lines.Add(line);

			Selections.Replace(lines.AsParallel().AsOrdered().Select(line => new Range(Data.GetOffset(line, Data.GetLineLength(line)), Data.GetOffset(line, 0))).ToList());
		}

		IEnumerable<Range> SelectRectangle(Range range)
		{
			var startLine = Data.GetOffsetLine(range.Start);
			var endLine = Data.GetOffsetLine(range.End);
			if (startLine == endLine)
			{
				yield return range;
				yield break;
			}
			var startIndex = Data.GetOffsetIndex(range.Start, startLine);
			var endIndex = Data.GetOffsetIndex(range.End, endLine);
			for (var line = startLine; line <= endLine; ++line)
			{
				var length = Data.GetLineLength(line);
				var lineStartOffset = Data.GetOffset(line, Math.Min(length, startIndex));
				var lineEndOffset = Data.GetOffset(line, Math.Min(length, endIndex));
				yield return new Range(lineEndOffset, lineStartOffset);
			}
		}

		internal void Command_Select_Rectangle() => Selections.Replace(Selections.AsParallel().AsOrdered().SelectMany(range => SelectRectangle(range)).ToList());

		internal SelectRotateDialog.Result Command_Select_Rotate_Dialog() => SelectRotateDialog.Run(WindowParent, GetVariables());

		internal void Command_Select_Rotate(SelectRotateDialog.Result result)
		{
			var count = new NEExpression(result.Count).EvaluateRow<int>(GetVariables());
			if (count == 0)
				return;

			var strs = GetSelectionStrings();
			if (count < 0)
				count = -count;
			else
				count = strs.Count - count;
			strs.AddRange(strs.Take(count).ToList());
			strs.RemoveRange(0, count);
			ReplaceSelections(strs);
		}

		internal void Command_Select_Invert()
		{
			var start = new[] { 0 }.Concat(Selections.Select(sel => sel.End));
			var end = Selections.Select(sel => sel.Start).Concat(new[] { Data.NumChars });
			Selections.Replace(Enumerable.Zip(start, end, (startPos, endPos) => new Range(endPos, startPos)).Where(range => (range.HasSelection) || ((range.Start != 0) && (range.Start != Data.NumChars))).ToList());
		}

		internal void Command_Select_Join()
		{
			var sels = Selections.ToList();
			var ctr = 0;
			while (ctr < sels.Count - 1)
			{
				if (sels[ctr].End == sels[ctr + 1].Start)
				{
					sels[ctr] = new Range(sels[ctr + 1].End, sels[ctr].Start);
					sels.RemoveAt(ctr + 1);
				}
				else
					++ctr;
			}
			Selections.Replace(sels);
		}

		internal void Command_Select_Empty(bool include) => Selections.Replace(Selections.Where(range => range.HasSelection != include).ToList());

		Range TrimRange(Range range)
		{
			var index = range.Start;
			var length = range.Length;
			Data.Trim(ref index, ref length);
			if ((index == range.Start) && (length == range.Length))
				return range;
			return Range.FromIndex(index, length);
		}

		internal void Command_Text_Select_Trim() => Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => TrimRange(range)).ToList());

		internal WidthDialog.Result Command_Text_Select_ByWidth_Dialog()
		{
			var minLength = Selections.Any() ? Selections.AsParallel().Min(range => range.Length) : 0;
			var maxLength = Selections.Any() ? Selections.AsParallel().Max(range => range.Length) : 0;
			return WidthDialog.Run(WindowParent, minLength, maxLength, false, true, GetVariables());
		}

		bool WidthMatch(string str, WidthDialog.Result result, int value)
		{
			switch (result.Type)
			{
				case WidthDialog.WidthType.Absolute: return str.Length == value;
				case WidthDialog.WidthType.Minimum: return str.Length >= value;
				case WidthDialog.WidthType.Maximum: return str.Length <= value;
				case WidthDialog.WidthType.Multiple: return str.Length % value == 0;
				default: throw new ArgumentException("Invalid width type");
			}
		}

		internal void Command_Text_Select_ByWidth(WidthDialog.Result result)
		{
			var results = GetFixedExpressionResults<int>(result.Expression);
			Selections.Replace(Selections.AsParallel().AsOrdered().Where((range, index) => WidthMatch(GetString(range), result, results[index])).ToList());
		}

		internal void Command_Select_Unique() => Selections.Replace(Selections.AsParallel().AsOrdered().Distinct(range => GetString(range)).ToList());

		internal void Command_Select_Duplicates() => Selections.Replace(Selections.AsParallel().AsOrdered().GroupBy(range => GetString(range)).SelectMany(list => list.Skip(1)).ToList());

		int GetRepetitionScore(List<string> data, int lines)
		{
			var count = data.Count / lines;
			if (count * lines != data.Count)
				return 0;

			var score = 0;
			for (var repetition = 1; repetition < count; ++repetition)
			{
				var repetitionIndex = repetition * lines;
				for (var index = 0; index < lines; ++index)
				{
					List<LCS.MatchType> output1, output2;
					LCS.GetLCS(data[index], data[repetitionIndex + index], out output1, out output2);
					score += output1.Count(match => match == LCS.MatchType.Match);
				}
			}
			return score;
		}

		IEnumerable<Range> FindRepetitions(Range inputRange)
		{
			var startLine = Data.GetOffsetLine(inputRange.Start);
			var endLine = Data.GetOffsetLine(inputRange.End);
			var lineRanges = Enumerable.Range(startLine, endLine - startLine + 1).Select(line => new Range(Math.Max(inputRange.Start, Data.GetOffset(line, 0)), Math.Min(inputRange.End, Data.GetOffset(line, Data.GetLineLength(line))))).ToList();
			if ((lineRanges.Count >= 2) && (!lineRanges[lineRanges.Count - 1].HasSelection))
				lineRanges.RemoveAt(lineRanges.Count - 1);
			var lineStrs = lineRanges.Select(range => GetString(range)).ToList();
			var lines = Enumerable.Range(1, lineStrs.Count).MaxBy(x => GetRepetitionScore(lineStrs, x));
			for (var ctr = 0; ctr < lineRanges.Count; ctr += lines)
				yield return new Range(lineRanges[ctr + lines - 1].End, lineRanges[ctr].Start);
		}

		internal void Command_Select_RepeatedLines() => Selections.Replace(Selections.AsParallel().AsOrdered().SelectMany(range => FindRepetitions(range)).ToList());

		internal CountDialog.Result Command_Select_ByCount_Dialog() => CountDialog.Run(WindowParent);

		internal void Command_Select_ByCount(CountDialog.Result result)
		{
			var strs = Selections.Select((range, index) => Tuple.Create(GetString(range), index)).ToList();
			var counts = new Dictionary<string, int>(result.CaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
			foreach (var tuple in strs)
			{
				if (!counts.ContainsKey(tuple.Item1))
					counts[tuple.Item1] = 0;
				++counts[tuple.Item1];
			}
			strs = strs.Where(tuple => (counts[tuple.Item1] >= result.MinCount) && (counts[tuple.Item1] <= result.MaxCount)).ToList();
			Selections.Replace(strs.Select(tuple => Selections[tuple.Item2]).ToList());
		}

		IEnumerable<Range> SelectSplit(Range range, SelectSplitDialog.Result result)
		{
			var str = GetString(range);
			var start = 0;
			foreach (Match match in result.Regex.Matches(str))
			{
				if (match.Index != start)
					yield return Range.FromIndex(range.Start + start, match.Index - start);
				if (result.IncludeResults)
					yield return Range.FromIndex(range.Start + match.Index, match.Length);
				start = match.Index + match.Length;
			}
			if (str.Length != start)
				yield return Range.FromIndex(range.Start + start, str.Length - start);
		}

		internal SelectSplitDialog.Result Command_Select_Split_Dialog() => SelectSplitDialog.Run(WindowParent);

		internal void Command_Select_Split(SelectSplitDialog.Result result) => Selections.Replace(Selections.AsParallel().AsOrdered().SelectMany(range => SelectSplit(range, result)).ToList());

		internal void Command_Select_Regions() => Selections.Replace(Regions);

		internal void Command_Select_FindResults()
		{
			Selections.Replace(Searches);
			Searches.Clear();
		}

		internal GetExpressionDialog.Result Command_Expression_SelectByExpression_Dialog() => GetExpressionDialog.Run(WindowParent, GetVariables(), Selections.Count);

		internal void Command_Expression_SelectByExpression(GetExpressionDialog.Result result)
		{
			var results = GetFixedExpressionResults<bool>(result.Expression);
			Selections.Replace(Selections.Where((str, num) => results[num]).ToList());
		}

		internal void Command_Expression_ClearVariables() => variables.Clear();

		internal SetVariablesDialog.Result Command_Expression_SetVariables_Dialog() => SetVariablesDialog.Run(WindowParent, Selections.Select(range => GetString(range)).FirstOrDefault() ?? "");

		internal void Command_Expression_SetVariables(SetVariablesDialog.Result result) => variables[result.VarName] = GetSelectionStrings();

		internal void Command_Select_Selection_First()
		{
			visibleIndex = 0;
			EnsureVisible(true);
			canvasRenderTimer.Start();
		}

		internal void Command_Select_Selection_CenterVertically() => EnsureVisible(true);

		internal void Command_Select_Selection_Center() => EnsureVisible(true, true);

		internal void Command_Select_Selection_Next()
		{
			++visibleIndex;
			if (visibleIndex >= Selections.Count)
				visibleIndex = 0;
			EnsureVisible(true);
			canvasRenderTimer.Start();
		}

		internal void Command_Select_Selection_Previous()
		{
			--visibleIndex;
			if (visibleIndex < 0)
				visibleIndex = Selections.Count - 1;
			EnsureVisible(true);
			canvasRenderTimer.Start();
		}

		internal void Command_Select_Selection_Single()
		{
			if (!Selections.Any())
				return;
			visibleIndex = Math.Max(0, Math.Min(visibleIndex, Selections.Count - 1));
			Selections.Replace(Selections[visibleIndex]);
			visibleIndex = 0;
		}

		internal void Command_Select_Selection_Remove()
		{
			if (!Selections.Any())
				return;
			Selections.RemoveAt(visibleIndex);
		}

		internal void Command_Region_SetSelection() => Regions.AddRange(Selections);

		internal void Command_Region_SetFindResults()
		{
			Regions.AddRange(Searches);
			Searches.Clear();
		}

		internal void Command_Region_ClearRegions()
		{
			if (!Selections.Any(range => range.HasSelection))
				Regions.Clear();
			else
			{
				foreach (var selection in Selections)
				{
					var toRemove = Regions.Where(region => (region.Start >= selection.Start) && (region.End <= selection.End)).ToList();
					toRemove.ForEach(region => Regions.Remove(region));
				}
			}
		}

		internal void Command_Region_LimitToSelection() => Regions.Replace(Regions.Where(region => Selections.Any(selection => (region.Start >= selection.Start) && (region.End <= selection.End))).ToList());

		internal void Command_Region_WithEnclosingRegion() => Selections.Replace(Selections.Zip(GetEnclosingRegions(mustBeInRegion: false), (selection, region) => region == null ? null : selection).Where(selection => selection != null).ToList());

		internal void Command_Region_WithoutEnclosingRegion() => Selections.Replace(Selections.Zip(GetEnclosingRegions(mustBeInRegion: false), (selection, region) => region == null ? selection : null).Where(selection => selection != null).ToList());

		internal void Command_Region_SelectEnclosingRegion() => Selections.Replace(GetEnclosingRegions());

		internal void Command_Region_CopyEnclosingRegion() => SetClipboardStrings(GetEnclosingRegions().Select(range => GetString(range)).ToList());

		internal void Command_View_Highlighting(Highlighting.HighlightingType highlightType) => HighlightType = highlightType;

		int visibleIndex = 0;
		internal void EnsureVisible(bool centerVertically = false, bool centerHorizontally = false)
		{
			visibleIndex = Math.Max(0, Math.Min(visibleIndex, Selections.Count - 1));
			if (!Selections.Any())
			{
				LineMin = LineMax = IndexMin = IndexMax = PositionMin = PositionMax = ColumnMin = ColumnMax = null;
				return;
			}

			var range = Selections[visibleIndex];
			var lineMin = Data.GetOffsetLine(range.Start);
			var lineMax = Data.GetOffsetLine(range.End);
			var indexMin = Data.GetOffsetIndex(range.Start, lineMin);
			var indexMax = Data.GetOffsetIndex(range.End, lineMax);
			LineMin = Data.GetDiffLine(lineMin) + 1;
			LineMax = Data.GetDiffLine(lineMax) + 1;
			IndexMin = indexMin + 1;
			IndexMax = indexMax + 1;
			PositionMin = range.Start;
			PositionMax = range.End;
			ColumnMin = Data.GetColumnFromIndex(lineMin, indexMin) + 1;
			ColumnMax = Data.GetColumnFromIndex(lineMax, indexMax) + 1;

			if (centerVertically)
			{
				yScrollValue = (lineMin + lineMax - yScrollViewportFloor) / 2;
				if (centerHorizontally)
					xScrollValue = (Data.GetColumnFromIndex(lineMin, indexMin) + Data.GetColumnFromIndex(lineMax, indexMax) - xScrollViewportFloor) / 2;
				else
					xScrollValue = 0;
			}

			var line = Data.GetOffsetLine(range.Cursor);
			var index = Data.GetOffsetIndex(range.Cursor, line);
			var x = Data.GetColumnFromIndex(line, index);
			yScrollValue = Math.Min(line, Math.Max(line - yScrollViewportFloor + 1, yScrollValue));
			xScrollValue = Math.Min(x, Math.Max(x - xScrollViewportFloor + 1, xScrollValue));
		}

		void SelectionsInvalidated()
		{
			var visible = (visibleIndex >= 0) && (visibleIndex < Selections.Count) ? Selections[visibleIndex] : null;
			Selections.DeOverlap();
			if (visible != null)
			{
				visibleIndex = Selections.FindIndex(range => (range.Start == visible.Start) && (range.End == visible.End));
				if (visibleIndex < 0)
					visibleIndex = 0;
			}

			EnsureVisible();
			canvasRenderTimer.Start();
		}

		void SearchesInvalidated()
		{
			Searches.Replace(Searches.Where(range => range.HasSelection).ToList());
			Searches.DeOverlap();
			canvasRenderTimer.Start();
		}

		void RegionsInvalidated()
		{
			Regions.DeOverlap();
			canvasRenderTimer.Start();
		}

		void BookmarksInvalidated()
		{
			Bookmarks.Replace(Bookmarks.Select(range => MoveCursor(range, 0, 0, false, lineRel: true, indexRel: false)).ToList());
			Bookmarks.DeOverlap();
			bookmarkRenderTimer.Start();
		}

		void OnCanvasRender(object sender, DrawingContext dc)
		{
			if ((Data == null) || (yScrollViewportCeiling == 0) || (xScrollViewportCeiling == 0))
				return;

			var brushes = new List<Tuple<RangeList, Brush>>
			{
				Tuple.Create(Selections, Misc.selectionBrush),
				Tuple.Create(Searches, Misc.searchBrush),
				Tuple.Create(Regions, Misc.regionBrush),
			};

			NumSelections = Selections.Count;
			NumRegions = Regions.Count;

			var startLine = yScrollValue;
			var endLine = Math.Min(Data.NumLines, startLine + yScrollViewportCeiling);
			var startColumn = xScrollValue;
			var endColumn = Math.Min(Data.MaxColumn + 1, startColumn + xScrollViewportCeiling);

			var lines = Enumerable.Range(startLine, endLine - startLine).ToList();
			var lineRanges = lines.ToDictionary(line => line, line => new Range(Data.GetOffset(line, 0), Data.GetOffset(line, Data.GetLineLength(line) + 1)));
			var screenStart = lineRanges.First().Value.Start;
			var screenEnd = lineRanges.Last().Value.End + 1;
			var startIndexes = lines.ToDictionary(line => line, line => Data.GetIndexFromColumn(line, startColumn, true));
			var endIndexes = lines.ToDictionary(line => line, line => Data.GetIndexFromColumn(line, endColumn, true));
			var y = lines.ToDictionary(line => line, line => (line - startLine) * Font.lineHeight);
			var cursorLineDone = new HashSet<int>();
			var visibleCursor = (visibleIndex >= 0) && (visibleIndex < Selections.Count) ? Selections[visibleIndex] : null;

			foreach (var entry in brushes)
			{
				var hasSelection = entry.Item1.Any(range => range.HasSelection);

				foreach (var range in entry.Item1)
				{
					if ((range.End < screenStart) || (range.Start > screenEnd))
						continue;

					var entryStartLine = Data.GetOffsetLine(range.Start);
					var entryEndLine = Data.GetOffsetLine(range.End);
					var cursorLine = range.Cursor == range.Start ? entryStartLine : entryEndLine;
					entryStartLine = Math.Max(startLine, entryStartLine);
					entryEndLine = Math.Min(endLine, entryEndLine + 1);

					if ((entry.Item1 == Selections) && (!hasSelection) && (cursorLine >= entryStartLine) && (cursorLine < entryEndLine))
					{
						if (range == visibleCursor)
							dc.DrawRectangle(Misc.visibleCursorBrush, null, new Rect(0, y[cursorLine], canvas.ActualWidth, Font.lineHeight));

						if (!cursorLineDone.Contains(cursorLine))
						{
							dc.DrawRectangle(Misc.cursorBrush, Misc.cursorPen, new Rect(0, y[cursorLine], canvas.ActualWidth, Font.lineHeight));
							cursorLineDone.Add(cursorLine);
						}

						var cursor = Data.GetOffsetIndex(range.Cursor, cursorLine);
						if ((cursor >= startIndexes[cursorLine]) && (cursor <= endIndexes[cursorLine]))
						{
							cursor = Data.GetColumnFromIndex(cursorLine, cursor);
							dc.DrawRectangle(Brushes.Black, null, new Rect((cursor - startColumn) * Font.charWidth - 1, y[cursorLine], 2, Font.lineHeight));
						}
					}

					for (var line = entryStartLine; line < entryEndLine; ++line)
					{
						var start = Math.Max(lineRanges[line].Start, range.Start);
						var end = Math.Min(lineRanges[line].End, range.End);
						start = Data.GetOffsetIndex(start, line);
						end = Data.GetOffsetIndex(end, line);

						if ((start > endIndexes[line]) || (end < startIndexes[line]))
							continue;

						start = Data.GetColumnFromIndex(line, start);
						end = Data.GetColumnFromIndex(line, end);

						start = Math.Max(0, start - startColumn);
						end = Math.Max(0, Math.Min(endColumn, end) - startColumn);
						var width = Math.Max(0, end - start);

						var steps = range == visibleCursor ? 2 : 1;
						for (var ctr = 0; ctr < steps; ++ctr)
							dc.DrawRectangle(entry.Item2, null, new Rect(start * Font.charWidth, y[line], width * Font.charWidth + 1, Font.lineHeight));
					}
				}
			}

			var highlightDictionary = Highlighting.Get(HighlightType).GetDictionary();

			for (var line = startLine; line < endLine; ++line)
			{
				if (Data.GetLineDiffStatus(line) != LCS.MatchType.Match)
					dc.DrawRectangle(Misc.diffMinorBrush, null, new Rect(0, y[line], canvas.ActualWidth, Font.lineHeight));

				foreach (var tuple in Data.GetLineColumnDiffs(line))
				{
					var start = Math.Max(0, tuple.Item1 - startColumn);
					var len = tuple.Item2 + Math.Min(0, tuple.Item1 - startColumn);
					if (len > 0)
						dc.DrawRectangle(Misc.diffMajorBrush, null, new Rect(Font.charWidth * start, y[line], len * Font.charWidth, Font.lineHeight));
				}

				var str = Data.GetLineColumns(line);
				if (str.Length <= startColumn)
					continue;

				var highlight = new List<Tuple<Brush, int, int>>();
				foreach (var entry in highlightDictionary)
				{
					var matches = entry.Key.Matches(str);
					foreach (Match match in matches)
						highlight.Add(new Tuple<Brush, int, int>(entry.Value, match.Index, match.Length));
				}

				str = str.Substring(startColumn, Math.Min(endColumn, str.Length) - startColumn);
				var text = Font.GetText(str);
				foreach (var entry in highlight)
				{
					var start = entry.Item2 - startColumn;
					var count = entry.Item3;
					if (start < 0)
					{
						count += start;
						start = 0;
					}
					count = Math.Min(count, str.Length - start);
					if (count <= 0)
						continue;
					text.SetForegroundBrush(entry.Item1, start, count);
				}
				dc.DrawText(text, new Point(0, y[line]));
			}
		}

		void OnBookmarksRender(object sender, DrawingContext dc)
		{
			if ((Data == null) || (yScrollViewportCeiling == 0) || (!Bookmarks.Any()))
				return;
			var startLine = yScrollValue;
			var endLine = Math.Min(Data.NumLines, startLine + yScrollViewportCeiling);
			for (var line = startLine; line < endLine; ++line)
			{
				var offset = Data.GetOffset(line, 0);
				if (!Bookmarks.Any(range => range.Start == offset))
					continue;

				var y = (line - startLine) * Font.lineHeight;

				dc.DrawRoundedRectangle(Brushes.CadetBlue, new Pen(Brushes.Black, 1), new Rect(1, y + 1, Font.lineHeight - 2, Font.lineHeight - 2), 2, 2);
			}
		}

		void BlockSelDown()
		{
			foreach (var range in Selections.ToList())
			{
				var cursorLine = Data.GetOffsetLine(range.Cursor);
				var highlightLine = Data.GetOffsetLine(range.Highlight);
				var cursorIndex = Data.GetOffsetIndex(range.Cursor, cursorLine);
				var highlightIndex = Data.GetOffsetIndex(range.Highlight, highlightLine);

				cursorLine = Math.Max(0, Math.Min(cursorLine + 1, Data.NumLines - 1));
				highlightLine = Math.Max(0, Math.Min(highlightLine + 1, Data.NumLines - 1));
				cursorIndex = Math.Max(0, Math.Min(cursorIndex, Data.GetLineLength(cursorLine)));
				highlightIndex = Math.Max(0, Math.Min(highlightIndex, Data.GetLineLength(highlightLine)));

				Selections.Add(new Range(Data.GetOffset(cursorLine, cursorIndex), Data.GetOffset(highlightLine, highlightIndex)));
			}
		}

		void BlockSelUp()
		{
			var found = new HashSet<string>();
			foreach (var range in Selections)
				found.Add(range.ToString());

			var sels = new List<Range>();
			foreach (var range in Selections.ToList())
			{
				var startLine = Data.GetOffsetLine(range.Start);
				var endLine = Data.GetOffsetLine(range.End);
				var startIndex = Data.GetOffsetIndex(range.Start, startLine);
				var endIndex = Data.GetOffsetIndex(range.End, endLine);

				startLine = Math.Max(0, Math.Min(startLine - 1, Data.NumLines - 1));
				endLine = Math.Max(0, Math.Min(endLine - 1, Data.NumLines - 1));
				startIndex = Math.Max(0, Math.Min(startIndex, Data.GetLineLength(startLine)));
				endIndex = Math.Max(0, Math.Min(endIndex, Data.GetLineLength(endLine)));

				var prevLineRange = new Range(Data.GetOffset(startLine, startIndex), Data.GetOffset(endLine, endIndex));
				if (found.Contains(prevLineRange.ToString()))
					sels.Add(prevLineRange);
				else
					sels.Add(range);
			}

			Selections.Replace(sels);
		}

		internal bool HandleKey(Key key, bool shiftDown, bool controlDown, bool altDown)
		{
			var ret = true;
			switch (key)
			{
				case Key.Back:
				case Key.Delete:
					{
						if (Selections.Any(range => range.HasSelection))
						{
							ReplaceSelections("");
							break;
						}

						Replace(Selections.AsParallel().AsOrdered().Select(range =>
						{
							var offset = range.Start;

							if (controlDown)
							{
								if (key == Key.Back)
									offset = GetPrevWord(offset);
								else
									offset = GetNextWord(offset);
							}
							else
							{
								var line = Data.GetOffsetLine(offset);
								var index = Data.GetOffsetIndex(offset, line);

								if (key == Key.Back)
									--index;
								else
									++index;

								if (index < 0)
								{
									--line;
									if (line < 0)
										return null;
									index = Data.GetLineLength(line);
								}
								if (index > Data.GetLineLength(line))
								{
									++line;
									if (line >= Data.NumLines)
										return null;
									index = 0;
								}

								offset = Data.GetOffset(line, index);
							}

							return new Range(offset, range.Highlight);
						}).Where(range => range != null).ToList(), null);
					}
					break;
				case Key.Escape:
					Searches.Clear();
					doDrag = DragType.None;
					break;
				case Key.Left:
					{
						var hasSelection = Selections.Any(range => range.HasSelection);
						Selections.Replace(Selections.AsParallel().AsOrdered().Select(range =>
						{
							var line = Data.GetOffsetLine(range.Cursor);
							var index = Data.GetOffsetIndex(range.Cursor, line);
							if (controlDown)
								return MoveCursor(range, GetPrevWord(range.Cursor), shiftDown);
							else if ((!shiftDown) && (hasSelection))
								return new Range(range.Start);
							else if ((index == 0) && (line != 0))
								return MoveCursor(range, -1, int.MaxValue, shiftDown, indexRel: false);
							else
								return MoveCursor(range, 0, -1, shiftDown);
						}).ToList());
					}
					break;
				case Key.Right:
					{
						var hasSelection = Selections.Any(range => range.HasSelection);
						Selections.Replace(Selections.AsParallel().AsOrdered().Select(range =>
						{
							var line = Data.GetOffsetLine(range.Cursor);
							var index = Data.GetOffsetIndex(range.Cursor, line);
							if (controlDown)
								return MoveCursor(range, GetNextWord(range.Cursor), shiftDown);
							else if ((!shiftDown) && (hasSelection))
								return new Range(range.End);
							else if ((index == Data.GetLineLength(line)) && (line != Data.NumLines - 1))
								return MoveCursor(range, 1, 0, shiftDown, indexRel: false);
							else
								return MoveCursor(range, 0, 1, shiftDown);
						}).ToList());
					}
					break;
				case Key.Up:
				case Key.Down:
					{
						var mult = key == Key.Up ? -1 : 1;
						if (!controlDown)
							Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, mult, 0, shiftDown)).ToList());
						else if (!shiftDown)
							yScrollValue += mult;
						else if (key == Key.Down)
							BlockSelDown();
						else
							BlockSelUp();
					}
					break;
				case Key.Home:
					if (controlDown)
					{
						var sels = Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, BeginOffset, shiftDown)).ToList(); // Have to use MoveCursor for selection
						if ((!sels.Any()) && (!shiftDown))
							sels.Add(BeginRange);
						Selections.Replace(sels);
					}
					else
					{
						bool changed = false;
						for (var ctr = 0; ctr < Selections.Count; ++ctr)
						{
							var line = Data.GetOffsetLine(Selections[ctr].Cursor);
							var index = Data.GetOffsetIndex(Selections[ctr].Cursor, line);

							int first;
							var end = Data.GetLineLength(line);
							for (first = 0; first < end; ++first)
							{
								if (!char.IsWhiteSpace(Data[line, first]))
									break;
							}
							if (first == end)
								first = 0;

							if (first != index)
								changed = true;
							Selections[ctr] = MoveCursor(Selections[ctr], 0, first, shiftDown, indexRel: false);
						}
						if (!changed)
						{
							Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, 0, 0, shiftDown, indexRel: false)).ToList());
							xScrollValue = 0;
						}
					}
					break;
				case Key.End:
					if (controlDown)
					{
						var sels = Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, EndOffset, shiftDown)).ToList(); // Have to use MoveCursor for selection
						if ((!sels.Any()) && (!shiftDown))
							sels.Add(EndRange);
						Selections.Replace(sels);
					}
					else
						Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, 0, int.MaxValue, shiftDown, indexRel: false)).ToList());
					break;
				case Key.PageUp:
					if (controlDown)
						yScrollValue -= yScrollViewportFloor / 2;
					else
					{
						var savedYScrollViewportFloor = yScrollViewportFloor;
						Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, 1 - savedYScrollViewportFloor, 0, shiftDown)).ToList());
					}
					break;
				case Key.PageDown:
					if (controlDown)
						yScrollValue += yScrollViewportFloor / 2;
					else
					{
						var savedYScrollViewportFloor = yScrollViewportFloor;
						Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, savedYScrollViewportFloor - 1, 0, shiftDown)).ToList());
					}
					break;
				case Key.Tab:
					{
						if (Selections.AsParallel().All(range => (!range.HasSelection) || (Data.GetOffsetLine(range.Start) == Data.GetOffsetLine(range.End))))
						{
							if (!shiftDown)
								HandleText("\t");
							else
							{
								var tabs = Selections.AsParallel().AsOrdered().Where(range => (range.Start != 0) && (Data.GetString(range.Start - 1, 1) == "\t")).Select(range => Range.FromIndex(range.Start - 1, 1)).ToList();
								Replace(tabs, null);
							}
							break;
						}

						var selLines = Selections.AsParallel().AsOrdered().Where(a => a.HasSelection).Select(range => new { start = Data.GetOffsetLine(range.Start), end = Data.GetOffsetLine(range.End - 1) }).ToList();
						var lines = selLines.SelectMany(entry => Enumerable.Range(entry.start, entry.end - entry.start + 1)).Distinct().OrderBy().ToDictionary(line => line, line => Data.GetOffset(line, 0));
						int offset;
						string replace;
						if (shiftDown)
						{
							offset = 1;
							replace = "";
							lines = lines.Where(entry => (Data.GetLineLength(entry.Key) != 0) && (Data[entry.Key, 0] == '\t')).ToDictionary(entry => entry.Key, entry => entry.Value);
						}
						else
						{
							offset = 0;
							replace = "\t";
							lines = lines.Where(entry => Data.GetLineLength(entry.Key) != 0).ToDictionary(entry => entry.Key, entry => entry.Value);
						}

						var sels = lines.Select(line => Range.FromIndex(line.Value, offset)).ToList();
						var insert = sels.Select(range => replace).ToList();
						Replace(sels, insert);
					}
					break;
				case Key.Enter:
					HandleText(Data.DefaultEnding);
					break;
				case Key.OemCloseBrackets:
					if (controlDown)
					{
						Selections.Replace(Selections.AsParallel().AsOrdered().Select(range =>
						{
							var newPos = Data.GetOppositeBracket(range.Cursor);
							if (newPos == -1)
								return range;

							return MoveCursor(range, newPos, shiftDown);
						}).ToList());
					}
					else
						ret = false;
					break;
				case Key.Space:
					if (controlDown)
					{
						if (Selections.Any())
							Selections.Replace(Selections.Select(range => new Range(range.Highlight, range.Cursor)).ToList());
						else
						{
							var line = Math.Min(yScrollValue, Data.NumLines - 1);
							Selections.Replace(new Range(Data.GetOffset(line, 0)));
						}
					}
					else
						ret = false;
					break;
				default: ret = false; break;
			}

			if (Selections.Changed)
				EnsureVisible();

			return ret;
		}

		enum WordSkipType
		{
			None,
			Char,
			Symbol,
			Space,
		}

		int GetNextWord(int offset)
		{
			WordSkipType moveType = WordSkipType.None;

			var line = Data.GetOffsetLine(offset);
			var index = Math.Min(Data.GetLineLength(line), Data.GetOffsetIndex(offset, line) - 1);
			while (true)
			{
				if (index >= Data.GetLineLength(line))
				{
					++line;
					if (line >= Data.NumLines)
						return EndOffset;
					index = -1;
				}

				++index;
				WordSkipType current;
				if (index >= Data.GetLineLength(line))
					current = WordSkipType.Space;
				else
				{
					var c = Data[line, index];
					if (char.IsWhiteSpace(c))
						current = WordSkipType.Space;
					else if ((char.IsLetterOrDigit(c)) || (c == '_'))
						current = WordSkipType.Char;
					else
						current = WordSkipType.Symbol;
				}

				if (moveType == WordSkipType.None)
					moveType = current;

				if (current != moveType)
					return Data.GetOffset(line, index);
			}
		}

		int GetPrevWord(int offset)
		{
			WordSkipType moveType = WordSkipType.None;

			var line = Data.GetOffsetLine(offset);
			var index = Math.Min(Data.GetLineLength(line), Data.GetOffsetIndex(offset, line));
			int lastLine = -1, lastIndex = -1;
			while (true)
			{
				if (index < 0)
				{
					--line;
					if (line < 0)
						return BeginOffset;
					index = Data.GetLineLength(line);
					continue;
				}

				lastLine = line;
				lastIndex = index;

				--index;
				WordSkipType current;
				if (index < 0)
					current = WordSkipType.Space;
				else
				{
					var c = Data[line, index];
					if (char.IsWhiteSpace(c))
						current = WordSkipType.Space;
					else if ((char.IsLetterOrDigit(c)) || (Data[line, index] == '_'))
						current = WordSkipType.Char;
					else
						current = WordSkipType.Symbol;
				}

				if (moveType == WordSkipType.None)
					moveType = current;

				if (current != moveType)
					return Data.GetOffset(lastLine, lastIndex);
			}
		}

		Range MoveCursor(Range range, int cursor, bool selecting)
		{
			cursor = Math.Max(BeginOffset, Math.Min(cursor, EndOffset));
			if (selecting)
				if (range.Cursor == cursor)
					return range;
				else
					return new Range(cursor, range.Highlight);

			if ((range.Cursor == cursor) && (range.Highlight == cursor))
				return range;
			return new Range(cursor);
		}

		Range MoveCursor(Range range, int line, int index, bool selecting, bool lineRel = true, bool indexRel = true)
		{
			if ((lineRel) || (indexRel))
			{
				var startLine = Data.GetOffsetLine(range.Cursor);
				var startIndex = Data.GetOffsetIndex(range.Cursor, startLine);

				if (lineRel)
					line = Data.SkipDiffGaps(line + startLine, line > 0 ? 1 : -1);
				if (indexRel)
					index += startIndex;
			}

			line = Math.Max(0, Math.Min(line, Data.NumLines - 1));
			index = Math.Max(0, Math.Min(index, Data.GetLineLength(line)));
			return MoveCursor(range, Data.GetOffset(line, index), selecting);
		}

		void MouseHandler(Point mousePos, int clickCount, bool selecting)
		{
			var line = Math.Min(Data.NumLines - 1, (int)(mousePos.Y / Font.lineHeight) + yScrollValue);
			var index = Math.Min(Data.GetLineLength(line), Data.GetIndexFromColumn(line, (int)(mousePos.X / Font.charWidth) + xScrollValue, true));
			var offset = Data.GetOffset(line, index);
			var mouseRange = visibleIndex < Selections.Count ? Selections[visibleIndex] : null;

			if (selecting)
			{
				if (mouseRange != null)
				{
					Selections.Remove(mouseRange);
					Selections.Add(MoveCursor(mouseRange, offset, true));
					visibleIndex = Selections.Count - 1;
				}
				return;
			}

			if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.None)
				Selections.Clear();

			if (clickCount == 1)
				Selections.Add(new Range(offset));
			else
			{
				if (mouseRange != null)
					Selections.Remove(mouseRange);
				Selections.Add(new Range(GetNextWord(offset), GetPrevWord(offset + 1)));
			}
			visibleIndex = Selections.Count - 1;
		}

		enum DragType
		{
			None,
			CurrentFile,
			Selections,
		}

		DragType doDrag = DragType.None;
		void OnCanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (doDrag != DragType.None)
			{
				List<string> strs;
				switch (doDrag)
				{
					case DragType.CurrentFile: strs = new List<string> { FileName }; break;
					case DragType.Selections: strs = RelativeSelectedFiles(); break;
					default: throw new Exception("Invalid drag type");
				}
				if (!StringsAreFiles(strs))
					throw new Exception("Selections must be files.");

				DragDrop.DoDragDrop(this, new DataObject(DataFormats.FileDrop, strs.ToArray()), DragDropEffects.Copy);
				doDrag = DragType.None;
				return;
			}

			MouseHandler(e.GetPosition(canvas), e.ClickCount, (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None);
			canvas.CaptureMouse();
			e.Handled = true;
		}

		void OnCanvasMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			canvas.ReleaseMouseCapture();
			e.Handled = true;
		}

		void OnCanvasMouseMove(object sender, MouseEventArgs e)
		{
			if (!canvas.IsMouseCaptured)
				return;

			MouseHandler(e.GetPosition(canvas), 0, true);
			e.Handled = true;
		}

		void RunSearch(FindTextDialog.Result result)
		{
			if ((result == null) || (result.Regex == null))
				return;

			Searches.Clear();

			var regions = result.SelectionOnly ? Selections.ToList() : new List<Range> { FullRange };
			foreach (var region in regions)
				Searches.AddRange(Data.RegexMatches(result.Regex, region.Start, region.Length, result.IncludeEndings, result.RegexGroups, false).Select(tuple => Range.FromIndex(tuple.Item1, tuple.Item2)));
		}

		string GetString(Range range) => Data.GetString(range.Start, range.Length);

		enum ReplaceType
		{
			Normal,
			Undo,
			Redo,
		}

		void ReplaceSelections(string str, bool highlight = true, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false) => ReplaceSelections(Selections.Select(range => str).ToList(), highlight, replaceType, tryJoinUndo);

		void ReplaceSelections(List<string> strs, bool highlight = true, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false)
		{
			Replace(Selections, strs, replaceType, tryJoinUndo);

			if (highlight)
				Selections.Replace(Selections.AsParallel().AsOrdered().Select((range, index) => new Range(range.End, range.End - (strs == null ? 0 : strs[index].Length))).ToList());
			else
				Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => new Range(range.End)).ToList());
		}

		void CalculateDiff()
		{
			if (diffTarget == null)
				return;

			diffTarget.DiffIgnoreWhitespace = DiffIgnoreWhitespace;
			diffTarget.DiffIgnoreCase = DiffIgnoreCase;
			diffTarget.DiffIgnoreNumbers = DiffIgnoreNumbers;
			diffTarget.DiffIgnoreLineEndings = DiffIgnoreLineEndings;
			TextData.CalculateDiff(Data, diffTarget.Data, DiffIgnoreWhitespace, DiffIgnoreCase, DiffIgnoreNumbers, DiffIgnoreLineEndings);

			CalculateBoundaries();
			diffTarget.CalculateBoundaries();
		}

		void Replace(List<Range> ranges, List<string> strs, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false)
		{
			if (strs == null)
				strs = Enumerable.Repeat("", ranges.Count).ToList();

			if (ranges.Count != strs.Count)
				throw new Exception("Invalid string count");

			var undoRanges = new List<Range>();
			var undoText = new List<string>();

			var change = 0;
			for (var ctr = 0; ctr < ranges.Count; ++ctr)
			{
				var undoRange = new Range(ranges[ctr].Start + change, ranges[ctr].Start + strs[ctr].Length + change);
				undoRanges.Add(undoRange);
				undoText.Add(GetString(ranges[ctr]));
				change = undoRange.Highlight - ranges[ctr].End;
			}

			var textCanvasUndoRedo = new UndoRedo.UndoRedoStep(undoRanges, undoText, tryJoinUndo);
			switch (replaceType)
			{
				case ReplaceType.Undo: undoRedo.AddUndone(textCanvasUndoRedo); break;
				case ReplaceType.Redo: undoRedo.AddRedone(textCanvasUndoRedo); break;
				case ReplaceType.Normal: undoRedo.AddUndo(textCanvasUndoRedo, IsModified); break;
			}

			Data.Replace(ranges.Select(range => range.Start).ToList(), ranges.Select(range => range.Length).ToList(), strs);
			SetModifiedFlag();
			CalculateDiff();

			var translateMap = RangeList.GetTranslateMap(ranges, strs, Selections, Regions, Searches, Bookmarks);
			Selections.Translate(translateMap);
			Regions.Translate(translateMap);
			var searchLens = Searches.Select(range => range.Length).ToList();
			Searches.Translate(translateMap);
			Searches.Replace(Searches.Where((range, index) => searchLens[index] == range.Length).ToList());
			Bookmarks.Translate(translateMap);

			CalculateBoundaries();
		}

		void FindNext(bool forward, bool selecting)
		{
			if (Searches.Count == 0)
			{
				Selections.Clear();
				return;
			}

			for (var ctr = 0; ctr < Selections.Count; ++ctr)
			{
				int index;
				if (forward)
				{
					index = Searches.BinaryFindFirst(range => range.Start >= Selections[ctr].End);
					if (index == -1)
						index = 0;
				}
				else
				{
					index = Searches.BinaryFindLast(range => range.Start < Selections[ctr].Start);
					if (index == -1)
						index = Searches.Count - 1;
				}

				if (!selecting)
					Selections[ctr] = new Range(Searches[index].End, Searches[index].Start);
				else if (forward)
					Selections[ctr] = new Range(Searches[index].End, Selections[ctr].Start);
				else
					Selections[ctr] = new Range(Searches[index].Start, Selections[ctr].End);
			}
		}

		string SetWidth(string str, WidthDialog.Result result, int value)
		{
			int length;
			switch (result.Type)
			{
				case WidthDialog.WidthType.Absolute: length = value; break;
				case WidthDialog.WidthType.Relative: length = Math.Max(0, str.Length + value); break;
				case WidthDialog.WidthType.Minimum: length = Math.Max(str.Length, value); break;
				case WidthDialog.WidthType.Maximum: length = Math.Min(str.Length, value); break;
				case WidthDialog.WidthType.Multiple: length = str.Length + value - 1 - (str.Length + value - 1) % value; break;
				default: throw new ArgumentException("Invalid width type");
			}

			if (str.Length == length)
				return str;

			if (str.Length > length)
			{
				switch (result.Location)
				{
					case WidthDialog.TextLocation.Start: return str.Substring(0, length);
					case WidthDialog.TextLocation.Middle: return str.Substring((str.Length - length + 1) / 2, length);
					case WidthDialog.TextLocation.End: return str.Substring(str.Length - length);
					default: throw new ArgumentException("Invalid");
				}
			}
			else
			{
				var len = length - str.Length;
				switch (result.Location)
				{
					case WidthDialog.TextLocation.Start: return str + new string(result.PadChar, len);
					case WidthDialog.TextLocation.Middle: return new string(result.PadChar, (len + 1) / 2) + str + new string(result.PadChar, len / 2);
					case WidthDialog.TextLocation.End: return new string(result.PadChar, len) + str;
					default: throw new ArgumentException("Invalid");
				}
			}
		}

		public string RepeatString(string input, int count)
		{
			var builder = new StringBuilder(input.Length * count);
			for (int ctr = 0; ctr < count; ++ctr)
				builder.Append(input);
			return builder.ToString();
		}

		public override bool Empty() => (FileName == null) && (!IsModified) && (BeginOffset == EndOffset);

		internal bool HandleText(string text)
		{
			if (text.Length == 0)
				return true;

			ReplaceSelections(text, false, tryJoinUndo: true);
			if (Selections.Changed)
				EnsureVisible();
			return true;
		}

		void CalculateBoundaries()
		{
			if ((canvas.ActualWidth <= 0) || (canvas.ActualHeight <= 0))
				return;

			xScroll.ViewportSize = canvas.ActualWidth / Font.charWidth;
			xScroll.Minimum = 0;
			xScroll.Maximum = Data.MaxColumn - xScrollViewportFloor;
			xScroll.SmallChange = 1;
			xScroll.LargeChange = Math.Max(0, xScroll.ViewportSize - 1);
			xScrollValue = xScrollValue;

			yScroll.ViewportSize = canvas.ActualHeight / Font.lineHeight;
			yScroll.Minimum = 0;
			yScroll.Maximum = Data.NumLines - yScrollViewportFloor;
			yScroll.SmallChange = 1;
			yScroll.LargeChange = Math.Max(0, yScroll.ViewportSize - 1);
			yScrollValue = yScrollValue;

			LineEnding = Data.OnlyEnding;

			canvasRenderTimer.Start();
		}

		public override string ToString() => FileName;
	}
}
