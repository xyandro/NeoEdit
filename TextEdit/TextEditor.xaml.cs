using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
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
using NeoEdit.Common.Transform;
using NeoEdit.GUI;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	public partial class TextEditor
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
				CalculateBoundaries();
			}
		}
		readonly UndoRedo undoRedo;

		[DepProp]
		public string FileName { get { return UIHelper<TextEditor>.GetPropValue<string>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsModified { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public Highlighting.HighlightingType HighlightType { get { return UIHelper<TextEditor>.GetPropValue<Highlighting.HighlightingType>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public Coder.CodePage CodePage { get { return UIHelper<TextEditor>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int Line { get { return UIHelper<TextEditor>.GetPropValue<int>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int Column { get { return UIHelper<TextEditor>.GetPropValue<int>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int Index { get { return UIHelper<TextEditor>.GetPropValue<int>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int PositionMin { get { return UIHelper<TextEditor>.GetPropValue<int>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int PositionMax { get { return UIHelper<TextEditor>.GetPropValue<int>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int NumSelections { get { return UIHelper<TextEditor>.GetPropValue<int>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int xScrollValue { get { return UIHelper<TextEditor>.GetPropValue<int>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int yScrollValue { get { return UIHelper<TextEditor>.GetPropValue<int>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public string LineEnding { get { return UIHelper<TextEditor>.GetPropValue<string>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }

		TextEditTabs TabsParent { get { return UIHelper.FindParent<TextEditTabs>(this); } }
		Window WindowParent { get { return UIHelper.FindParent<Window>(this); } }

		int xScrollViewportFloor { get { return (int)Math.Floor(xScroll.ViewportSize); } }
		int xScrollViewportCeiling { get { return (int)Math.Ceiling(xScroll.ViewportSize); } }
		int yScrollViewportFloor { get { return (int)Math.Floor(yScroll.ViewportSize); } }
		int yScrollViewportCeiling { get { return (int)Math.Ceiling(yScroll.ViewportSize); } }

		readonly RangeList Selections, Searches, Regions, Bookmarks;

		static ThreadSafeRandom random = new ThreadSafeRandom();

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
		}

		RunOnceTimer canvasRenderTimer, bookmarkRenderTimer;
		List<PropertyChangeNotifier> localCallbacks;

		public TextEditor(string filename = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, int line = -1, int column = -1)
		{
			InitializeComponent();
			bookmarks.Width = Font.lineHeight;

			SetupDragDrop();

			undoRedo = new UndoRedo(b => IsModified = b);
			Selections = new RangeList(SelectionsInvalidated);
			Searches = new RangeList(SearchesInvalidated);
			Regions = new RangeList(RegionsInvalidated);
			Bookmarks = new RangeList(BookmarksInvalidated);

			canvasRenderTimer = new RunOnceTimer(() => canvas.InvalidateVisual());
			canvasRenderTimer.AddDependency(Selections.Timer, Searches.Timer, Regions.Timer);
			bookmarkRenderTimer = new RunOnceTimer(() => bookmarks.InvalidateVisual());

			OpenFile(filename, bytes, codePage);
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

		void SetupDragDrop()
		{
			AllowDrop = true;
			DragEnter += (s, e) => e.Effects = DragDropEffects.Link;
			Drop += (s, e) =>
			{
				var fileList = e.Data.GetData("FileDrop") as string[];
				if (fileList != null)
				{
					if (Selections.Count != 1)
						throw new Exception("Must have only 1 selection.");

					if (fileList.Length == 1)
					{
						ReplaceSelections(fileList[0]);
						return;
					}

					var files = fileList.Select(file => file + Data.DefaultEnding).ToList();
					var offset = Selections.First().Start;
					ReplaceSelections(String.Join("", files));
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

		internal void Goto(int line, int column)
		{
			line = Math.Max(0, Math.Min(line, Data.NumLines) - 1);
			var index = Data.GetIndexFromColumn(line, Math.Max(0, column - 1), true);
			Selections.Add(new Range(Data.GetOffset(line, index)));

		}

		internal Label GetLabel()
		{
			var label = new Label { Padding = new Thickness(10, 2, 10, 2) };
			var multiBinding = new MultiBinding { Converter = new NeoEdit.GUI.Common.ExpressionConverter(), ConverterParameter = @"([0] t== ''?'[Untitled]':FileName([0]))t+([1]?'*':'')" };
			multiBinding.Bindings.Add(new Binding("FileName") { Source = this });
			multiBinding.Bindings.Add(new Binding("IsModified") { Source = this });
			label.SetBinding(Label.ContentProperty, multiBinding);
			return label;
		}

		DateTime fileLastWrite;
		internal void OpenFile(string filename, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM)
		{
			FileName = filename;
			var modified = bytes != null;
			if (bytes == null)
			{
				if (FileName == null)
					bytes = new byte[0];
				else
					bytes = File.ReadAllBytes(FileName);
			}
			if (codePage == Coder.CodePage.AutoByBOM)
				codePage = Coder.CodePageFromBOM(bytes);
			Data = new TextData(bytes, codePage);
			CodePage = codePage;
			HighlightType = Highlighting.Get(FileName);
			if (File.Exists(FileName))
				fileLastWrite = new FileInfo(FileName).LastWriteTime;

			// If encoding can't exactly express bytes mark as modified (only for < 50 MB)
			if ((!modified) && ((bytes.Length >> 20) < 50))
				modified = !Coder.CanFullyEncode(bytes, CodePage);

			undoRedo.SetModified(modified);
		}

		int BeginOffset()
		{
			return Data.GetOffset(0, 0);
		}

		int EndOffset()
		{
			return Data.GetOffset(Data.NumLines - 1, Data.GetLineLength(Data.NumLines - 1));
		}

		internal bool CanClose()
		{
			var answer = Message.OptionsEnum.None;
			return CanClose(ref answer);
		}

		internal bool CanClose(ref Message.OptionsEnum answer)
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
					Command_File_Save();
					return !IsModified;
			}
			return false;
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

		static List<string>[] keysAndValues = new List<string>[10] { new List<string>(), new List<string>(), new List<string>(), new List<string>(), new List<string>(), new List<string>(), new List<string>(), new List<string>(), new List<string>(), new List<string>() };
		static Dictionary<string, int> keysHash = new Dictionary<string, int>();

		bool ConfirmVerifyCanFullyEncode()
		{
			return new Message
			{
				Title = "Confirm",
				Text = String.Format("The current encoding cannot fully represent this data.  Continue?"),
				Options = Message.OptionsEnum.YesNo,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.No,
			}.Show() == Message.OptionsEnum.Yes;
		}

		bool VerifyCanFullyEncode()
		{
			return (Data.CanFullyEncode(CodePage)) || (ConfirmVerifyCanFullyEncode());
		}

		bool VerifyCanFullyEncode(List<string> strs, Coder.CodePage codePage)
		{
			return (strs.AsParallel().All(str => Coder.CanFullyEncode(str, codePage))) || (ConfirmVerifyCanFullyEncode());
		}

		bool VerifyCanFullyEncode(List<byte[]> data, Coder.CodePage codePage)
		{
			return (data.AsParallel().All(str => Coder.CanFullyEncode(str, codePage))) || (ConfirmVerifyCanFullyEncode());
		}

		Coder.CodePage DetectUnicode(List<byte[]> data)
		{
			if (data.Count == 0)
				return Coder.CodePage.Default;

			return data.Select(a => Coder.GuessUnicodeEncoding(a)).GroupBy(a => a).OrderByDescending(a => a.Count()).First().Key;
		}

		Dictionary<string, List<string>> GetExpressionData(int count = -1, NeoEdit.Common.Expression expression = null)
		{
			var sels = count == -1 ? Selections.ToList() : Selections.Take(Math.Min(count, Selections.Count)).ToList();
			var strs = sels.Select(range => GetString(range)).ToList();
			var c = NEClipboard.GetStrings();
			var keyOrdering = strs.Select(str => keysHash.ContainsKey(str) ? keysHash[str] : -1);

			var parallelDataActions = new Dictionary<string[], Action<Action<string, List<string>>>>
			{
				{ new string[] { "xl" }, addData => addData("xl", strs.Select(str => str.Length.ToString()).ToList()) },
				{ new string[] { "n" }, addData => addData("n", Enumerable.Repeat(Selections.Count.ToString(), sels.Count).ToList()) },
				{ new string[] { "y" }, addData => addData("y", strs.Select((str, order) => (order + 1).ToString()).ToList()) },
				{ new string[] { "z" }, addData => addData("z", strs.Select((str, order) => order.ToString()).ToList()) },
				{ new string[] { "c" }, addData => addData("c", c) },
				{ new string[] { "cl" }, addData => addData("cl", c.Select(str => str.Length.ToString()).ToList()) },
				{ new string[] { "rk" }, addData => addData("rk", keysAndValues[0]) },
				{ new string[] { "rkl" }, addData => addData("rkl", keysAndValues[0].Select(str => str.Length.ToString()).ToList()) },
			};

			for (var ctr = 1; ctr <= 9; ++ctr)
			{
				var num = ctr; // If we don't copy this the threads get the wrong value
				parallelDataActions.Add(new string[] { String.Format("rv{0}", num) }, addData => addData(String.Format("rv{0}", num), keysAndValues[num]));
				parallelDataActions.Add(new string[] { String.Format("rv{0}l", num) }, addData => addData(String.Format("rv{0}l", num), keysAndValues[num].Select(str => str.Length.ToString()).ToList()));
				parallelDataActions.Add(new string[] { String.Format("v{0}", num), String.Format("v{0}l", num) }, addData =>
				{
					List<string> values;
					if (keysAndValues[0].Count == keysAndValues[num].Count)
						values = keyOrdering.Select(order => order == -1 ? "" : keysAndValues[num][order]).ToList();
					else
						values = new List<string>();

					addData(String.Format("v{0}", num), values);
					addData(String.Format("v{0}l", num), values.Select(str => str.Length.ToString()).ToList());
				});
			}

			var used = expression != null ? expression.DictValues() : null;
			var data = new Dictionary<string, List<string>>();
			data["x"] = strs;
			Parallel.ForEach(parallelDataActions, pair =>
			{
				if (pair.Key.Any(key => (used == null) || (used.Contains(key))))
					pair.Value((key, value) =>
					{
						lock (data)
							data[key] = value;
					});
			});

			return data;
		}

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

			File.WriteAllBytes(fileName, Data.GetBytes(CodePage));
			fileLastWrite = new FileInfo(fileName).LastWriteTime;
			undoRedo.SetModified(false);
			FileName = fileName;
		}

		List<string> GetSelectionStrings()
		{
			return Selections.AsParallel().AsOrdered().Select(range => GetString(range)).ToList();
		}

		internal bool GetDialogResult(TextEditCommand command, out object dialogResult)
		{
			dialogResult = null;

			switch (command)
			{
				case TextEditCommand.File_Encoding: dialogResult = Command_File_Encoding_Dialog(); break;
				case TextEditCommand.File_ReopenWithEncoding: dialogResult = Command_File_ReopenWithEncoding_Dialog(); break;
				case TextEditCommand.Edit_Find: dialogResult = Command_Edit_FindReplace_Dialog(false); break;
				case TextEditCommand.Edit_Replace: dialogResult = Command_Edit_FindReplace_Dialog(true); break;
				case TextEditCommand.Edit_GotoLine: dialogResult = Command_Edit_Goto_Dialog(GotoDialog.GotoType.Line); break;
				case TextEditCommand.Edit_GotoColumn: dialogResult = Command_Edit_Goto_Dialog(GotoDialog.GotoType.Column); break;
				case TextEditCommand.Edit_GotoPosition: dialogResult = Command_Edit_Goto_Dialog(GotoDialog.GotoType.Position); break;
				case TextEditCommand.Files_GetUniqueNames: dialogResult = Command_Files_GetUniqueNames_Dialog(); break;
				case TextEditCommand.Files_Set_Size: dialogResult = Command_Files_Set_Size_Dialog(); break;
				case TextEditCommand.Files_Set_WriteTime: dialogResult = Command_Files_Set_Time_Dialog(); break;
				case TextEditCommand.Files_Set_AccessTime: dialogResult = Command_Files_Set_Time_Dialog(); break;
				case TextEditCommand.Files_Set_CreateTime: dialogResult = Command_Files_Set_Time_Dialog(); break;
				case TextEditCommand.Files_Set_AllTimes: dialogResult = Command_Files_Set_Time_Dialog(); break;
				case TextEditCommand.Files_Set_Attributes: dialogResult = Command_Files_Set_Attributes_Dialog(); break;
				case TextEditCommand.Data_DateTime_Convert: dialogResult = Command_Data_DateTime_Convert_Dialog(); break;
				case TextEditCommand.Data_Convert: dialogResult = Command_Data_Convert_Dialog(); break;
				case TextEditCommand.Data_Width: dialogResult = Command_Data_Width_Dialog(); break;
				case TextEditCommand.Data_Trim: dialogResult = Command_Data_Trim_Dialog(); break;
				case TextEditCommand.Data_EvaluateExpression: dialogResult = Command_Data_EvaluateExpression_Dialog(); break;
				case TextEditCommand.Data_Repeat: dialogResult = Command_Data_Repeat_Dialog(); break;
				case TextEditCommand.Data_Hash_MD5: dialogResult = Command_Data_Hash_Dialog(); break;
				case TextEditCommand.Data_Hash_SHA1: dialogResult = Command_Data_Hash_Dialog(); break;
				case TextEditCommand.Data_Hash_SHA256: dialogResult = Command_Data_Hash_Dialog(); break;
				case TextEditCommand.Data_Sort: dialogResult = Command_Data_Sort_Dialog(); break;
				case TextEditCommand.Insert_RandomNumber: dialogResult = Command_Insert_RandomNumber_Dialog(); break;
				case TextEditCommand.Insert_RandomData: dialogResult = Command_Insert_RandomData_Dialog(); break;
				case TextEditCommand.Insert_MinMaxValues: dialogResult = Command_Insert_MinMaxValues_Dialog(); break;
				case TextEditCommand.Select_Limit: dialogResult = Command_Select_Limit_Dialog(); break;
				case TextEditCommand.Select_Width: dialogResult = Command_Select_Width_Dialog(); break;
				case TextEditCommand.Select_Count: dialogResult = Command_Select_Count_Dialog(); break;
				case TextEditCommand.Select_ExpressionMatches: dialogResult = Command_Select_ExpressionMatches_Dialog(); break;
				default: return true;
			}

			return dialogResult != null;
		}

		class PreviousStruct
		{
			public TextEditCommand Command { get; set; }
			public bool ShiftDown { get; set; }
			public object DialogResult { get; set; }
		}
		PreviousStruct previous = null;

		internal void HandleCommand(TextEditCommand command, bool shiftDown, object dialogResult)
		{
			if (command != TextEditCommand.Edit_RepeatLastAction)
			{
				previous = new PreviousStruct
				{
					Command = command,
					ShiftDown = shiftDown,
					DialogResult = dialogResult,
				};
			}

			switch (command)
			{
				case TextEditCommand.File_Save: Command_File_Save(); break;
				case TextEditCommand.File_SaveAs: Command_File_SaveAs(); break;
				case TextEditCommand.File_Close: if (CanClose()) { TabsParent.Remove(this); } break;
				case TextEditCommand.File_Refresh: Command_File_Refresh(); break;
				case TextEditCommand.File_Revert: Command_File_Revert(); break;
				case TextEditCommand.File_InsertFiles: Command_File_InsertFiles(); break;
				case TextEditCommand.File_InsertCopiedCutFiles: Command_File_InsertCopiedCutFiles(); break;
				case TextEditCommand.File_CopyPath: Command_File_CopyPath(); break;
				case TextEditCommand.File_CopyName: Command_File_CopyName(); break;
				case TextEditCommand.File_Encoding: Command_File_Encoding(dialogResult as EncodingDialog.Result); break;
				case TextEditCommand.File_ReopenWithEncoding: Command_File_ReopenWithEncoding(dialogResult as EncodingDialog.Result); break;
				case TextEditCommand.File_HexEditor: if (Command_File_HexEditor()) { TabsParent.Remove(this, true); } break;
				case TextEditCommand.Edit_Undo: Command_Edit_Undo(); break;
				case TextEditCommand.Edit_Redo: Command_Edit_Redo(); break;
				case TextEditCommand.Edit_RepeatLastAction: if (previous != null) HandleCommand(previous.Command, previous.ShiftDown, previous.DialogResult); break;
				case TextEditCommand.Edit_Cut: Command_Edit_CutCopy(true); break;
				case TextEditCommand.Edit_Copy: Command_Edit_CutCopy(false); break;
				case TextEditCommand.Edit_Paste: Command_Edit_Paste(shiftDown); break;
				case TextEditCommand.Edit_Find: Command_Edit_FindReplace(false, shiftDown, dialogResult as GetRegExDialog.Result); break;
				case TextEditCommand.Edit_FindNext: Command_Edit_FindNextPrev(true, shiftDown); break;
				case TextEditCommand.Edit_FindPrev: Command_Edit_FindNextPrev(false, shiftDown); break;
				case TextEditCommand.Edit_Replace: Command_Edit_FindReplace(true, shiftDown, dialogResult as GetRegExDialog.Result); break;
				case TextEditCommand.Edit_GotoLine: Command_Edit_Goto(shiftDown, dialogResult as GotoDialog.Result); break;
				case TextEditCommand.Edit_GotoColumn: Command_Edit_Goto(shiftDown, dialogResult as GotoDialog.Result); break;
				case TextEditCommand.Edit_GotoPosition: Command_Edit_Goto(shiftDown, dialogResult as GotoDialog.Result); break;
				case TextEditCommand.Edit_ToggleBookmark: Command_Edit_ToggleBookmark(); break;
				case TextEditCommand.Edit_NextBookmark: Command_Edit_NextPreviousBookmark(true, shiftDown); break;
				case TextEditCommand.Edit_PreviousBookmark: Command_Edit_NextPreviousBookmark(false, shiftDown); break;
				case TextEditCommand.Edit_ClearBookmarks: Command_Edit_ClearBookmarks(); break;
				case TextEditCommand.Files_Open: Command_Files_Open(); break;
				case TextEditCommand.Files_Insert: Command_Files_Insert(); break;
				case TextEditCommand.Files_SaveClipboards: Command_Files_SaveClipboards(); break;
				case TextEditCommand.Files_CreateFiles: Command_Files_CreateFiles(); break;
				case TextEditCommand.Files_CreateDirectories: Command_Files_CreateDirectories(); break;
				case TextEditCommand.Files_Delete: Command_Files_Delete(); break;
				case TextEditCommand.Files_Simplify: Command_Files_Simplify(); break;
				case TextEditCommand.Files_GetUniqueNames: Command_Files_GetUniqueNames(dialogResult as GetUniqueNamesDialog.Result); break;
				case TextEditCommand.Files_SanitizeNames: Command_Files_SanitizeNames(); break;
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
				case TextEditCommand.Files_Select_DirectoryName: Command_Files_Select_GetFilePath(TextEditor.GetPathType.Directory); break;
				case TextEditCommand.Files_Select_FileName: Command_Files_Select_GetFilePath(TextEditor.GetPathType.FileName); break;
				case TextEditCommand.Files_Select_FileNamewoExtension: Command_Files_Select_GetFilePath(TextEditor.GetPathType.FileNameWoExtension); break;
				case TextEditCommand.Files_Select_Extension: Command_Files_Select_GetFilePath(TextEditor.GetPathType.Extension); break;
				case TextEditCommand.Files_Select_Existing: Command_Files_Select_Existing(true); break;
				case TextEditCommand.Files_Select_NonExisting: Command_Files_Select_Existing(false); break;
				case TextEditCommand.Files_Select_Files: Command_Files_Select_Files(); break;
				case TextEditCommand.Files_Select_Directories: Command_Files_Select_Directories(); break;
				case TextEditCommand.Files_Select_Roots: Command_Files_Select_Roots(true); break;
				case TextEditCommand.Files_Select_NonRoots: Command_Files_Select_Roots(false); break;
				case TextEditCommand.Files_Hash_MD5: Command_Files_Hash(Hash.Type.MD5); break;
				case TextEditCommand.Files_Hash_SHA1: Command_Files_Hash(Hash.Type.SHA1); break;
				case TextEditCommand.Files_Hash_SHA256: Command_Files_Hash(Hash.Type.SHA256); break;
				case TextEditCommand.Files_Operations_Copy: Command_Files_Operations_CopyMove(false); break;
				case TextEditCommand.Files_Operations_Move: Command_Files_Operations_CopyMove(true); break;
				case TextEditCommand.Data_Case_Upper: Command_Data_Case_Upper(); break;
				case TextEditCommand.Data_Case_Lower: Command_Data_Case_Lower(); break;
				case TextEditCommand.Data_Case_Proper: Command_Data_Case_Proper(); break;
				case TextEditCommand.Data_Case_Toggle: Command_Data_Case_Toggle(); break;
				case TextEditCommand.Data_Hex_ToHex: Command_Data_Hex_ToHex(); break;
				case TextEditCommand.Data_Hex_FromHex: Command_Data_Hex_FromHex(); break;
				case TextEditCommand.Data_DateTime_Insert: Command_Data_DateTime_Insert(); break;
				case TextEditCommand.Data_DateTime_Convert: Command_Data_DateTime_Convert(dialogResult as ConvertDateTimeDialog.Result); break;
				case TextEditCommand.Data_Convert: Command_Data_Convert(dialogResult as ConvertDialog.Result); break;
				case TextEditCommand.Data_Length: Command_Data_Length(); break;
				case TextEditCommand.Data_Width: Command_Data_Width(dialogResult as WidthDialog.Result); break;
				case TextEditCommand.Data_Trim: Command_Data_Trim(dialogResult as TrimDialog.Result); break;
				case TextEditCommand.Data_SingleLine: Command_Data_SingleLine(); break;
				case TextEditCommand.Data_Table_ToTable: Command_Data_Table_ToTable(); break;
				case TextEditCommand.Data_Table_FromTable: Command_Data_Table_FromTable(); break;
				case TextEditCommand.Data_EvaluateExpression: Command_Data_EvaluateExpression(dialogResult as GetExpressionDialog.Result); break;
				case TextEditCommand.Data_EvaluateSelectedExpression: Command_Data_EvaluateSelectedExpression(); break;
				case TextEditCommand.Data_Series: Command_Data_Series(); break;
				case TextEditCommand.Data_CopyDown: Command_Data_CopyDown(); break;
				case TextEditCommand.Data_Copy_Count: Command_Data_Copy_Count(); break;
				case TextEditCommand.Data_Copy_Length: Command_Data_Copy_Length(); break;
				case TextEditCommand.Data_Copy_Min_String: Command_Data_Copy_MinMax(true, TextEditor.Command_MinMax_Type.String); break;
				case TextEditCommand.Data_Copy_Min_Numeric: Command_Data_Copy_MinMax(true, TextEditor.Command_MinMax_Type.Numeric); break;
				case TextEditCommand.Data_Copy_Min_Length: Command_Data_Copy_MinMax(true, TextEditor.Command_MinMax_Type.Length); break;
				case TextEditCommand.Data_Copy_Max_String: Command_Data_Copy_MinMax(false, TextEditor.Command_MinMax_Type.String); break;
				case TextEditCommand.Data_Copy_Max_Numeric: Command_Data_Copy_MinMax(false, TextEditor.Command_MinMax_Type.Numeric); break;
				case TextEditCommand.Data_Copy_Max_Length: Command_Data_Copy_MinMax(false, TextEditor.Command_MinMax_Type.Length); break;
				case TextEditCommand.Data_Copy_Sum: Command_Data_Copy_Sum(); break;
				case TextEditCommand.Data_Copy_Lines: Command_Data_Copy_LinesColumnsPositions(GotoDialog.GotoType.Line); break;
				case TextEditCommand.Data_Copy_Columns: Command_Data_Copy_LinesColumnsPositions(GotoDialog.GotoType.Column); break;
				case TextEditCommand.Data_Copy_Positions: Command_Data_Copy_LinesColumnsPositions(GotoDialog.GotoType.Position); break;
				case TextEditCommand.Data_Repeat: Command_Data_Repeat(dialogResult as RepeatDialog.Result); break;
				case TextEditCommand.Data_Escape_XML: Command_Data_Escape_XML(); break;
				case TextEditCommand.Data_Escape_Regex: Command_Data_Escape_Regex(); break;
				case TextEditCommand.Data_Escape_URL: Command_Data_Escape_URL(); break;
				case TextEditCommand.Data_Unescape_XML: Command_Data_Unescape_XML(); break;
				case TextEditCommand.Data_Unescape_Regex: Command_Data_Unescape_Regex(); break;
				case TextEditCommand.Data_Unescape_URL: Command_Data_Unescape_URL(); break;
				case TextEditCommand.Data_Hash_MD5: Command_Data_Hash(Hash.Type.MD5, dialogResult as EncodingDialog.Result); break;
				case TextEditCommand.Data_Hash_SHA1: Command_Data_Hash(Hash.Type.SHA1, dialogResult as EncodingDialog.Result); break;
				case TextEditCommand.Data_Hash_SHA256: Command_Data_Hash(Hash.Type.SHA256, dialogResult as EncodingDialog.Result); break;
				case TextEditCommand.Data_Sort: Command_Data_Sort(dialogResult as SortDialog.Result); break;
				case TextEditCommand.Markup_FetchURL: Command_Markup_FetchURL(); break;
				case TextEditCommand.Markup_Tidy: Command_Markup_Tidy(); break;
				case TextEditCommand.Markup_Validate: Command_Markup_Validate(); break;
				case TextEditCommand.Markup_Parent: Command_Markup_Parent(); break;
				case TextEditCommand.Markup_Children: Command_Markup_Children(MarkupNode.MarkupNodeType.All); break;
				case TextEditCommand.Markup_Children_All: Command_Markup_Children(MarkupNode.MarkupNodeType.All, false); break;
				case TextEditCommand.Markup_Children_Elements: Command_Markup_Children(MarkupNode.MarkupNodeType.Element); break;
				case TextEditCommand.Markup_Children_Text: Command_Markup_Children(MarkupNode.MarkupNodeType.Text); break;
				case TextEditCommand.Markup_Children_AllText: Command_Markup_Children(MarkupNode.MarkupNodeType.Text, false); break;
				case TextEditCommand.Markup_Children_Comments: Command_Markup_Children(MarkupNode.MarkupNodeType.Comment); break;
				case TextEditCommand.Markup_Outer: Command_Markup_OuterTag(); break;
				case TextEditCommand.Markup_Inner: Command_Markup_InnerTag(); break;
				case TextEditCommand.Insert_GUID: Command_Insert_GUID(); break;
				case TextEditCommand.Insert_RandomNumber: Command_Insert_RandomNumber(dialogResult as RandomNumberDialog.Result); break;
				case TextEditCommand.Insert_RandomData: Command_Insert_RandomData(dialogResult as RandomDataDialog.Result); break;
				case TextEditCommand.Insert_MinMaxValues: Command_Insert_MinMaxValues(dialogResult as MinMaxValuesDialog.Result); break;
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
				case TextEditCommand.Keys_SelectionReplace_Values1: Command_Keys_SelectionReplace(1); break;
				case TextEditCommand.Keys_SelectionReplace_Values2: Command_Keys_SelectionReplace(2); break;
				case TextEditCommand.Keys_SelectionReplace_Values3: Command_Keys_SelectionReplace(3); break;
				case TextEditCommand.Keys_SelectionReplace_Values4: Command_Keys_SelectionReplace(4); break;
				case TextEditCommand.Keys_SelectionReplace_Values5: Command_Keys_SelectionReplace(5); break;
				case TextEditCommand.Keys_SelectionReplace_Values6: Command_Keys_SelectionReplace(6); break;
				case TextEditCommand.Keys_SelectionReplace_Values7: Command_Keys_SelectionReplace(7); break;
				case TextEditCommand.Keys_SelectionReplace_Values8: Command_Keys_SelectionReplace(8); break;
				case TextEditCommand.Keys_SelectionReplace_Values9: Command_Keys_SelectionReplace(9); break;
				case TextEditCommand.Keys_GlobalFind_Keys: Command_Keys_GlobalFind(0); break;
				case TextEditCommand.Keys_GlobalFind_Values1: Command_Keys_GlobalFind(1); break;
				case TextEditCommand.Keys_GlobalFind_Values2: Command_Keys_GlobalFind(2); break;
				case TextEditCommand.Keys_GlobalFind_Values3: Command_Keys_GlobalFind(3); break;
				case TextEditCommand.Keys_GlobalFind_Values4: Command_Keys_GlobalFind(4); break;
				case TextEditCommand.Keys_GlobalFind_Values5: Command_Keys_GlobalFind(5); break;
				case TextEditCommand.Keys_GlobalFind_Values6: Command_Keys_GlobalFind(6); break;
				case TextEditCommand.Keys_GlobalFind_Values7: Command_Keys_GlobalFind(7); break;
				case TextEditCommand.Keys_GlobalFind_Values8: Command_Keys_GlobalFind(8); break;
				case TextEditCommand.Keys_GlobalFind_Values9: Command_Keys_GlobalFind(9); break;
				case TextEditCommand.Keys_GlobalReplace_Values1: Command_Keys_GlobalReplace(1); break;
				case TextEditCommand.Keys_GlobalReplace_Values2: Command_Keys_GlobalReplace(2); break;
				case TextEditCommand.Keys_GlobalReplace_Values3: Command_Keys_GlobalReplace(3); break;
				case TextEditCommand.Keys_GlobalReplace_Values4: Command_Keys_GlobalReplace(4); break;
				case TextEditCommand.Keys_GlobalReplace_Values5: Command_Keys_GlobalReplace(5); break;
				case TextEditCommand.Keys_GlobalReplace_Values6: Command_Keys_GlobalReplace(6); break;
				case TextEditCommand.Keys_GlobalReplace_Values7: Command_Keys_GlobalReplace(7); break;
				case TextEditCommand.Keys_GlobalReplace_Values8: Command_Keys_GlobalReplace(8); break;
				case TextEditCommand.Keys_GlobalReplace_Values9: Command_Keys_GlobalReplace(9); break;
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
				case TextEditCommand.Keys_CountstoKeysValues1: Command_Keys_CountstoKeysValues1(); break;
				case TextEditCommand.SelectRegion_Toggle: Command_SelectRegion_Toggle(); break;
				case TextEditCommand.Select_All: Command_Select_All(); break;
				case TextEditCommand.Select_Limit: Command_Select_Limit(dialogResult as LimitDialog.Result); break;
				case TextEditCommand.Select_Lines: Command_Select_Lines(); break;
				case TextEditCommand.Select_Empty: Command_Select_Empty(true); break;
				case TextEditCommand.Select_NonEmpty: Command_Select_Empty(false); break;
				case TextEditCommand.Select_Trim: Command_Select_Trim(); break;
				case TextEditCommand.Select_Width: Command_Select_Width(dialogResult as WidthDialog.Result); break;
				case TextEditCommand.Select_Unique: Command_Select_Unique(); break;
				case TextEditCommand.Select_Duplicates: Command_Select_Duplicates(); break;
				case TextEditCommand.Select_Count: Command_Select_Count(dialogResult as CountDialog.Result); break;
				case TextEditCommand.Select_Regions: Command_Select_Regions(); break;
				case TextEditCommand.Select_FindResults: Command_Select_FindResults(); break;
				case TextEditCommand.Select_Min_String: Command_Select_MinMax(true, TextEditor.Command_MinMax_Type.String); break;
				case TextEditCommand.Select_Min_Numeric: Command_Select_MinMax(true, TextEditor.Command_MinMax_Type.Numeric); break;
				case TextEditCommand.Select_Min_Length: Command_Select_MinMax(true, TextEditor.Command_MinMax_Type.Length); break;
				case TextEditCommand.Select_Max_String: Command_Select_MinMax(false, TextEditor.Command_MinMax_Type.String); break;
				case TextEditCommand.Select_Max_Numeric: Command_Select_MinMax(false, TextEditor.Command_MinMax_Type.Numeric); break;
				case TextEditCommand.Select_Max_Length: Command_Select_MinMax(false, TextEditor.Command_MinMax_Type.Length); break;
				case TextEditCommand.Select_ExpressionMatches: Command_Select_ExpressionMatches(dialogResult as GetExpressionDialog.Result); break;
				case TextEditCommand.Select_FirstSelection: Command_Select_FirstSelection(); break;
				case TextEditCommand.Select_ShowCurrent: Command_Select_ShowCurrent(); break;
				case TextEditCommand.Select_NextSelection: Command_Select_NextSelection(); break;
				case TextEditCommand.Select_PrevSelection: Command_Select_PrevSelection(); break;
				case TextEditCommand.Select_Single: Command_Select_Single(); break;
				case TextEditCommand.Select_Remove: Command_Select_Remove(); break;
				case TextEditCommand.Region_SetSelection: Command_Region_SetSelection(); break;
				case TextEditCommand.Region_SetFindResults: Command_Region_SetFindResults(); break;
				case TextEditCommand.Region_ClearRegions: Command_Region_ClearRegions(); break;
				case TextEditCommand.Region_LimitToSelection: Command_Region_LimitToSelection(); break;
				case TextEditCommand.View_Highlighting_None: HighlightType = Highlighting.HighlightingType.None; break;
				case TextEditCommand.View_Highlighting_CSharp: HighlightType = Highlighting.HighlightingType.CSharp; break;
				case TextEditCommand.View_Highlighting_CPlusPlus: HighlightType = Highlighting.HighlightingType.CPlusPlus; break;
			}
		}

		internal void Command_File_Save()
		{
			if (FileName == null)
				Command_File_SaveAs();
			else
				Save(FileName);
		}

		internal void Command_File_SaveAs()
		{
			var dialog = new SaveFileDialog
			{
				Filter = "Text files|*.txt|All files|*.*",
				FileName = Path.GetFileName(FileName),
				InitialDirectory = Path.GetDirectoryName(FileName),
			};
			if (dialog.ShowDialog() == true)
			{
				if (Directory.Exists(dialog.FileName))
					throw new Exception("A directory by that name already exists");
				if (!Directory.Exists(Path.GetDirectoryName(dialog.FileName)))
					throw new Exception("Directory doesn't exist");
				Save(dialog.FileName);
			}
		}

		internal void Command_File_Refresh()
		{
			if (String.IsNullOrEmpty(FileName))
				return;
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

			OpenFile(FileName);
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

		internal void Command_File_InsertFiles()
		{
			if (Selections.Count != 1)
			{
				new Message
				{
					Title = "Error",
					Text = "You have more than one selection.",
					Options = Message.OptionsEnum.Ok,
				}.Show();
				return;
			}

			var dialog = new OpenFileDialog { DefaultExt = "txt", Filter = "Text files|*.txt|All files|*.*", FilterIndex = 2, Multiselect = true };
			if (dialog.ShowDialog() == true)
				InsertFiles(dialog.FileNames);
		}

		internal void Command_File_InsertCopiedCutFiles()
		{
			if (Selections.Count != 1)
			{
				new Message
				{
					Title = "Error",
					Text = "You have more than one selection.",
					Options = Message.OptionsEnum.Ok,
				}.Show();
				return;
			}

			var files = NEClipboard.GetStrings();
			if ((files == null) || (files.Count < 0))
				return;

			if ((files.Count > 5) && (new Message
			{
				Title = "Confirm",
				Text = String.Format("Are you sure you want to insert these {0} files?", files.Count),
				Options = Message.OptionsEnum.YesNoCancel,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.Cancel,
			}.Show() != Message.OptionsEnum.Yes))
				return;

			InsertFiles(files);
		}

		internal void Command_File_CopyPath()
		{
			NEClipboard.SetFiles(new List<string> { FileName }, false);
		}

		internal void Command_File_CopyName()
		{
			Clipboard.SetText(Path.GetFileName(FileName));
		}

		internal EncodingDialog.Result Command_File_Encoding_Dialog()
		{
			return EncodingDialog.Run(WindowParent, CodePage, lineEndings: LineEnding ?? "");
		}

		internal void Command_File_Encoding(EncodingDialog.Result result)
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

		internal EncodingDialog.Result Command_File_ReopenWithEncoding_Dialog()
		{
			return EncodingDialog.Run(WindowParent, CodePage);
		}

		internal void Command_File_ReopenWithEncoding(EncodingDialog.Result result)
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

		internal bool Command_File_HexEditor()
		{
			if (!VerifyCanFullyEncode())
				return false;
			Launcher.Static.LaunchHexEditor(FileName, Data.GetBytes(CodePage), CodePage, true);
			return true;
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

		internal void Command_Edit_CutCopy(bool isCut)
		{
			var strs = GetSelectionStrings();

			if (strs.All(str => str.IndexOfAny(Path.GetInvalidPathChars()) == -1) && strs.All(str => FileOrDirectoryExists(str)))
				NEClipboard.SetFiles(strs, isCut);
			else
			{
				NEClipboard.SetStrings(strs);
				if (isCut)
					ReplaceSelections("");
			}
		}

		void ReplaceOneWithMany(List<string> strs)
		{
			if (Selections.Count != 1)
				throw new Exception(String.Format("You must have either 1 or the number copied selections ({0}).", strs.Count));

			var text = new List<string> { String.Join("", strs) };

			var offset = Selections.First().Start;
			ReplaceSelections(text);
			Selections.Clear();
			foreach (var str in strs)
			{
				Selections.Add(Range.FromIndex(offset, str.Length - Data.DefaultEnding.Length));
				offset += str.Length;
			}
		}

		internal void Command_Edit_Paste(bool highlight)
		{
			var clipboardStrings = NEClipboard.GetStrings();
			if ((clipboardStrings == null) || (clipboardStrings.Count == 0))
				return;

			if (clipboardStrings.Count == 1)
				clipboardStrings = Selections.Select(str => clipboardStrings[0]).ToList();

			if (Selections.Count == clipboardStrings.Count)
			{
				ReplaceSelections(clipboardStrings, highlight);
				return;
			}

			clipboardStrings = clipboardStrings.Select(str => str.TrimEnd('\r', '\n') + Data.DefaultEnding).ToList();
			ReplaceOneWithMany(clipboardStrings);
		}

		internal GetRegExDialog.Result Command_Edit_FindReplace_Dialog(bool isReplace)
		{
			string text = null;
			var selectionOnly = Selections.AsParallel().AsOrdered().Any(range => range.HasSelection);

			if (Selections.Count == 1)
			{
				var sel = Selections.First();
				if ((selectionOnly) && (Data.GetOffsetLine(sel.Cursor) == Data.GetOffsetLine(sel.Highlight)) && (sel.Length < 1000))
				{
					selectionOnly = false;
					text = GetString(sel);
				}
			}

			return GetRegExDialog.Run(WindowParent, isReplace, text, selectionOnly);
		}

		internal void Command_Edit_FindReplace(bool replace, bool selecting, GetRegExDialog.Result result)
		{
			if ((result.KeepMatching) || (result.RemoveMatching))
			{
				var keep = result.KeepMatching;
				Selections.Replace(Selections.AsParallel().AsOrdered().Where(range => result.Regex.IsMatch(GetString(range)) == keep).ToList());
				return;
			}

			RunSearch(result);

			if ((replace) || (result.ResultType == GetRegExDialog.GetRegExResultType.All))
			{
				if (Searches.Count != 0)
					Selections.Replace(Searches);
				Searches.Clear();

				if (replace)
					ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => result.Regex.Replace(GetString(range), result.Replace)).ToList());

				return;
			}

			FindNext(true, selecting);
		}

		internal void Command_Edit_FindNextPrev(bool next, bool selecting)
		{
			FindNext(next, selecting);
		}

		internal GotoDialog.Result Command_Edit_Goto_Dialog(GotoDialog.GotoType gotoType)
		{
			int line = 1, index = 1, position = 0;
			var range = Selections.FirstOrDefault();
			if (range != null)
			{
				line = Data.GetOffsetLine(range.Start) + 1;
				index = Data.GetOffsetIndex(range.Start, line - 1) + 1;
				position = range.Start;
			}
			return GotoDialog.Run(WindowParent, gotoType, gotoType == GotoDialog.GotoType.Line ? line : gotoType == GotoDialog.GotoType.Column ? index : position);
		}

		internal void Command_Edit_Goto(bool selecting, GotoDialog.Result result)
		{
			List<int> offsets;
			if (result.ClipboardValue)
			{
				var clipboardStrings = NEClipboard.GetStrings();
				if (clipboardStrings.Count != Selections.Count)
					throw new Exception("Number of items on clipboard doesn't match number of selections.");
				offsets = new List<int>(clipboardStrings.Select(str => Int32.Parse(str)));
			}
			else
				offsets = Selections.Select(range => result.Value).ToList();

			if (result.Relative)
			{
				var lines = Selections.AsParallel().AsOrdered().Select(range => Data.GetOffsetLine(range.Start)).ToList();
				var indexes = Selections.AsParallel().AsOrdered().Select((range, ctr) => Data.GetOffsetIndex(range.Start, lines[ctr])).ToList();
				var positions = Selections.Select(range => range.Start).ToList();

				var list = result.GotoType == GotoDialog.GotoType.Line ? lines : result.GotoType == GotoDialog.GotoType.Column ? indexes : positions;
				offsets = offsets.Select((ofs, ctr) => ofs + list[ctr]).ToList();
			}
			else if (result.GotoType != GotoDialog.GotoType.Position)
				offsets = offsets.Select(ofs => ofs - 1).ToList();

			if (result.GotoType == GotoDialog.GotoType.Position)
				Selections.Replace(Selections.AsParallel().AsOrdered().Select((range, ctr) => MoveCursor(range, offsets[ctr], selecting)).ToList());
			else
			{
				var isLine = result.GotoType == GotoDialog.GotoType.Line;
				Selections.Replace(Selections.AsParallel().AsOrdered().Select((range, ctr) => MoveCursor(range, isLine ? offsets[ctr] : 0, isLine ? 0 : offsets[ctr], selecting, !isLine, isLine)).ToList());
			}
		}

		internal void Command_Edit_ToggleBookmark()
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

		internal void Command_Edit_NextPreviousBookmark(bool next, bool selecting)
		{
			if (!Bookmarks.Any())
				return;
			Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => GetNextPrevBookmark(range, next, selecting)).ToList());
		}

		internal void Command_Edit_ClearBookmarks()
		{
			Bookmarks.Clear();
		}

		internal void Command_Files_Open()
		{
			var files = GetSelectionStrings();
			foreach (var file in files)
				TextEditTabs.Create(file);
		}

		internal void Command_Files_Insert()
		{
			InsertFiles(GetSelectionStrings());
		}

		internal void Command_Files_SaveClipboards()
		{
			var clipboardStrings = NEClipboard.GetStrings();
			if (clipboardStrings.Count != Selections.Count)
				throw new Exception("Clipboard count must match selection count.");

			for (var ctr = 0; ctr < clipboardStrings.Count; ++ctr)
			{
				var fileName = GetString(Selections[ctr]);
				var data = clipboardStrings[ctr];
				File.WriteAllText(fileName, data, Coder.GetEncoding(CodePage));
			}
		}

		internal void Command_Files_CreateFiles()
		{
			var files = GetSelectionStrings();
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

		internal void Command_Files_CreateDirectories()
		{
			var files = GetSelectionStrings();
			foreach (var file in files)
				Directory.CreateDirectory(file);
		}

		internal void Command_Files_Delete()
		{
			if (new Message
			{
				Title = "Confirm",
				Text = "Are you sure you want to delete these files/directories?",
				Options = Message.OptionsEnum.YesNo,
				DefaultCancel = Message.OptionsEnum.No,
			}.Show() == Message.OptionsEnum.Yes)
			{
				var files = GetSelectionStrings();
				foreach (var file in files)
				{
					if (File.Exists(file))
						File.Delete(file);
					if (Directory.Exists(file))
						Directory.Delete(file, true);
				}
			}
		}

		internal void Command_Files_Simplify()
		{
			ReplaceSelections(Selections.Select(range => Path.GetFullPath(GetString(range))).ToList());
		}

		internal GetUniqueNamesDialog.Result Command_Files_GetUniqueNames_Dialog()
		{
			return GetUniqueNamesDialog.Run(WindowParent);
		}

		internal void Command_Files_GetUniqueNames(GetUniqueNamesDialog.Result result)
		{
			var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (!result.Format.Contains("{Unique}"))
				throw new Exception("Format must contain \"{Unique}\" tag");
			var newNames = new List<string>();
			var format = result.Format.Replace("{Path}", "{0}").Replace("{Name}", "{1}").Replace("{Unique}", "{2}").Replace("{Ext}", "{3}");
			foreach (var fileName in GetSelectionStrings())
			{
				var path = Path.GetDirectoryName(fileName);
				if (!String.IsNullOrEmpty(path))
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

					newFileName = String.Format(format, path, name, unique, ext);
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
			//fileName = fileName.Replace("<", "_").Replace(">", "_").Replace(":", "_").Replace("\"", "_").Replace("|", "_").Replace("?", "_").Replace("*", "_").Replace("\u0000", "_");
			fileName = start + fileName.Substring(start.Length);
			return fileName;
		}

		internal void Command_Files_SanitizeNames()
		{
			ReplaceSelections(Selections.Select(range => SanitizeFileName(GetString(range))).ToList());
		}

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

				return String.Format("{0} directories, {1} files, {2} bytes", dirs.Count, files, totalSize);
			}

			return "INVALID";
		}

		internal void Command_Files_Get_Size()
		{
			ReplaceSelections(Selections.Select(range => GetSize(GetString(range))).ToList());
		}

		internal void Command_Files_Get_WriteTime()
		{
			var files = GetSelectionStrings();
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
			var files = GetSelectionStrings();
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
			var files = GetSelectionStrings();
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
			var files = GetSelectionStrings();
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

		internal SetSizeDialog.Result Command_Files_Set_Size_Dialog()
		{
			return SetSizeDialog.Run(WindowParent);
		}

		void SetFileSize(string fileName, SetSizeDialog.Result result, long clipboardLen)
		{
			var fileInfo = new FileInfo(fileName);
			if (!fileInfo.Exists)
				throw new Exception(String.Format("File doesn't exist: {0}", fileName));

			long length;
			switch (result.Type)
			{
				case SetSizeDialog.SizeType.Absolute: length = result.Value; break;
				case SetSizeDialog.SizeType.Relative: length = fileInfo.Length + result.Value; break;
				case SetSizeDialog.SizeType.Minimum: length = Math.Max(fileInfo.Length, result.Value); break;
				case SetSizeDialog.SizeType.Maximum: length = Math.Min(fileInfo.Length, result.Value); break;
				case SetSizeDialog.SizeType.Multiple: length = fileInfo.Length + result.Value - 1 - (fileInfo.Length + result.Value - 1) % result.Value; break;
				case SetSizeDialog.SizeType.Clipboard: length = clipboardLen; break;
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
			List<long> clipboardLens = null;
			if (result.Type == SetSizeDialog.SizeType.Clipboard)
			{
				var clipboardStrings = NEClipboard.GetStrings();
				if (clipboardStrings.Count != Selections.Count)
					throw new Exception("Number of items on clipboard doesn't match number of selections.");
				clipboardLens = clipboardStrings.AsParallel().AsOrdered().Select(str => long.Parse(str)).ToList();
			}

			for (var ctr = 0; ctr < Selections.Count; ++ctr)
				SetFileSize(GetString(Selections[ctr]), result, clipboardLens == null ? 0 : clipboardLens[ctr]);
		}

		internal ChooseDateTimeDialog.Result Command_Files_Set_Time_Dialog()
		{
			return ChooseDateTimeDialog.Run(WindowParent, DateTime.Now);
		}

		internal void Command_Files_Set_Time(TimestampType type, ChooseDateTimeDialog.Result result)
		{
			var files = GetSelectionStrings();
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

		internal void Command_Files_Select_GetFilePath(GetPathType type)
		{
			Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => GetPathRange(type, range)).ToList());
		}

		static bool FileOrDirectoryExists(string name)
		{
			return (Directory.Exists(name)) || (File.Exists(name));
		}

		internal void Command_Files_Select_Existing(bool existing)
		{
			Selections.Replace(Selections.AsParallel().AsOrdered().Where(range => FileOrDirectoryExists(GetString(range)) == existing).ToList());
		}

		internal void Command_Files_Select_Files()
		{
			Selections.Replace(Selections.AsParallel().AsOrdered().Where(range => File.Exists(GetString(range))).ToList());
		}

		internal void Command_Files_Select_Directories()
		{
			Selections.Replace(Selections.AsParallel().AsOrdered().Where(range => Directory.Exists(GetString(range))).ToList());
		}

		internal void Command_Files_Select_Roots(bool include)
		{
			var sels = Selections.Select(range => new { range = range, str = GetString(range).ToLower().Replace(@"\\", @"\").TrimEnd('\\') }).ToList();
			var files = sels.Select(obj => obj.str).Distinct().OrderBy(str => str).ToList();
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

		internal void Command_Files_Hash(Hash.Type type)
		{
			ReplaceSelections(Selections.Select(range => Hash.Get(type, GetString(range))).ToList());
		}

		internal void Command_Files_Operations_CopyMove(bool move)
		{
			var strs = Selections.Select(range => GetString(range).Split(new string[] { "=>" }, StringSplitOptions.None).Select(str => str.Trim()).ToList()).ToList();
			if (strs.Any(pair => pair.Count != 2))
				throw new Exception("Format: Source => Destination");

			var sels = strs.Select(pair => new { source = pair[0], dest = pair[1] }).Where(pair => pair.source != pair.dest).ToList();
			if (sels.Count == 0)
				throw new Exception("Nothing to do!");

			if (sels.Any(pair => (String.IsNullOrEmpty(pair.source)) || (String.IsNullOrEmpty(pair.dest))))
				throw new Exception("Can't have empty items in list");

			const int invalidCount = 10;
			var invalid = sels.Select(pair => pair.source).Concat(sels.Select(pair => pair.dest)).GroupBy(str => str).Where(group => group.Count() > 1).Select(group => group.Key).Distinct().Take(invalidCount);
			if (invalid.Any())
				throw new Exception(String.Format("Some items are listed more than once:\n{0}", String.Join("\n", invalid)));

			invalid = sels.Select(pair => pair.source).Where(file => !FileOrDirectoryExists(file)).Take(invalidCount);
			if (invalid.Any())
				throw new Exception(String.Format("Source file/directory doesn't exist:\n{0}", String.Join("\n", invalid)));

			invalid = sels.Select(pair => pair.dest).Where(pair => FileOrDirectoryExists(pair)).Take(invalidCount);
			if (invalid.Any())
				throw new Exception(String.Format("Destination file/directory already exist:\n{0}", String.Join("\n", invalid)));

			invalid = sels.Select(pair => Path.GetDirectoryName(pair.dest)).Distinct().Where(dir => !Directory.Exists(dir)).Take(invalidCount);
			if (invalid.Any())
				throw new Exception(String.Format("Directory doesn't exist:\n{0}", String.Join("\n", invalid)));

			if (new Message
			{
				Title = "Confirm",
				Text = String.Format("Are you sure you want to " + (move ? "move" : "copy") + " these {0} file(s)?", sels.Count),
				Options = Message.OptionsEnum.YesNo,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.No,
			}.Show() != Message.OptionsEnum.Yes)
				return;

			foreach (var pair in sels)
				if (Directory.Exists(pair.source))
				{
					if (move)
						Directory.Move(pair.source, pair.dest);
					else
						CopyDirectory(pair.source, pair.dest);
				}
				else
				{
					if (move)
						File.Move(pair.source, pair.dest);
					else
						File.Copy(pair.source, pair.dest);
				}
		}

		internal void Command_Data_Case_Upper()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => GetString(range).ToUpperInvariant()).ToList());
		}

		internal void Command_Data_Case_Lower()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => GetString(range).ToLowerInvariant()).ToList());
		}

		internal void Command_Data_Case_Proper()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => GetString(range).ToProper()).ToList());
		}

		internal void Command_Data_Case_Toggle()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => GetString(range).ToToggled()).ToList());
		}

		internal void Command_Data_Hex_ToHex()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Int64.Parse(GetString(range)).ToString("x")).ToList());
		}

		internal void Command_Data_Hex_FromHex()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Int64.Parse(GetString(range), NumberStyles.HexNumber).ToString()).ToList());
		}

		internal void Command_Data_DateTime_Insert()
		{
			ReplaceSelections(DateTime.Now.ToString("O"));
		}

		internal ConvertDateTimeDialog.Result Command_Data_DateTime_Convert_Dialog()
		{
			if (Selections.Count < 1)
				return null;

			return ConvertDateTimeDialog.Run(WindowParent, GetString(Selections.First()));
		}

		internal void Command_Data_DateTime_Convert(ConvertDateTimeDialog.Result result)
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => ConvertDateTimeDialog.ConvertFormat(GetString(range), result.InputFormat, result.InputUTC, result.OutputFormat, result.OutputUTC)).ToList());
		}

		internal ConvertDialog.Result Command_Data_Convert_Dialog()
		{
			return ConvertDialog.Run(WindowParent);
		}

		internal void Command_Data_Convert(ConvertDialog.Result result)
		{
			List<Coder.CodePage> clipboardCodePages = null;
			if ((result.InputType == Coder.CodePage.Clipboard) || (result.OutputType == Coder.CodePage.Clipboard))
			{
				var clipboardStrings = NEClipboard.GetStrings();
				if (clipboardStrings.Count != Selections.Count)
					throw new Exception("Number of items on clipboard must match number of selections");
				clipboardCodePages = clipboardStrings.Select(codePageStr => Helpers.ParseEnum<Coder.CodePage>(codePageStr)).ToList();
			}

			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, num) => Coder.BytesToString(Coder.StringToBytes(GetString(range), result.InputType == Coder.CodePage.Clipboard ? clipboardCodePages[num] : result.InputType, result.InputBOM), result.OutputType == Coder.CodePage.Clipboard ? clipboardCodePages[num] : result.OutputType, result.OutputBOM)).ToList());
		}

		internal void Command_Data_Length()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => range.Length.ToString()).ToList());
		}

		internal WidthDialog.Result Command_Data_Width_Dialog()
		{
			var minLength = Selections.AsParallel().AsOrdered().Min(range => range.Length);
			var maxLength = Selections.AsParallel().AsOrdered().Max(range => range.Length);
			var numeric = Selections.AsParallel().AsOrdered().All(range => GetString(range).IsNumeric());
			return WidthDialog.Run(WindowParent, minLength, maxLength, numeric, false);
		}

		internal void Command_Data_Width(WidthDialog.Result result)
		{
			List<int> clipboardLens = null;
			if (result.Type == WidthDialog.WidthType.Clipboard)
			{
				var clipboardStrings = NEClipboard.GetStrings();
				if (clipboardStrings.Count != Selections.Count)
					throw new Exception("Number of items on clipboard doesn't match number of selections.");
				clipboardLens = clipboardStrings.AsParallel().AsOrdered().Select(str => Int32.Parse(str)).ToList();
			}

			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, index) => SetWidth(GetString(range), result, clipboardLens == null ? 0 : clipboardLens[index])).ToList());
		}

		internal TrimDialog.Result Command_Data_Trim_Dialog()
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

		internal void Command_Data_Trim(TrimDialog.Result result)
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(str => TrimString(GetString(str), result)).ToList());
		}

		internal void Command_Data_SingleLine()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => GetString(range).Replace("\r", "").Replace("\n", "")).ToList());
		}

		internal void Command_Data_Table_ToTable()
		{
			var lines = Selections.AsParallel().AsOrdered().Select(range => GetString(range).Split('\t', '|', ',').Select(str => str.Trim()).ToList()).ToList();
			var numColumns = lines.Max(line => line.Count);
			foreach (var line in lines)
				line.AddRange(Enumerable.Range(0, numColumns - line.Count).Select(a => ""));
			var columnWidths = Enumerable.Range(0, numColumns).Select(column => lines.Max(line => line[column].Length)).ToList();
			var columns = Enumerable.Range(0, numColumns).Where(column => columnWidths[column] != 0).ToList();
			var strs = lines.AsParallel().AsOrdered().Select(line => "|" + String.Join("|", columns.Select(column => line[column] + new string(' ', columnWidths[column] - line[column].Length))) + "|").ToList();
			ReplaceSelections(strs);
		}

		internal void Command_Data_Table_FromTable()
		{
			var lines = Selections.AsParallel().AsOrdered().Select(range => GetString(range).Split('|').Select(str => str.Trim()).ToList()).ToList();

			// Strip leading tabs if all lines have them
			while (lines.All(line => (line.Count != 0) && (line[0].Length == 0)))
				lines.ForEach(line => line.RemoveAt(0));

			// Strip trailing tabs from each line
			foreach (var line in lines)
				while ((line.Count != 0) && (String.IsNullOrEmpty(line[line.Count - 1])))
					line.RemoveAt(line.Count - 1);

			var strs = lines.AsParallel().AsOrdered().Select(line => String.Join("\t", line)).ToList();
			ReplaceSelections(strs);
		}

		internal GetExpressionDialog.Result Command_Data_EvaluateExpression_Dialog()
		{
			return GetExpressionDialog.Run(WindowParent, GetExpressionData(10));
		}

		internal void Command_Data_EvaluateExpression(GetExpressionDialog.Result result)
		{
			var expressionData = GetExpressionData(expression: result.Expression);
			ReplaceSelections(ParallelEnumerable.Range(0, expressionData["x"].Count).AsOrdered().Select(index => result.Expression.EvaluateDict(expressionData, index).ToString()).ToList());
		}

		internal void Command_Data_EvaluateSelectedExpression()
		{
			var expression = new NeoEdit.Common.Expression("Eval([0])");
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => expression.Evaluate(GetString(range)).ToString()).ToList());
		}

		internal void Command_Data_Series()
		{
			ReplaceSelections(Selections.Select((range, index) => (index + 1).ToString()).ToList());
		}

		internal void Command_Data_CopyDown()
		{
			var strs = GetSelectionStrings();
			var index = 0;
			for (var ctr = 0; ctr < strs.Count; ++ctr)
				if (String.IsNullOrWhiteSpace(strs[ctr]))
					strs[ctr] = strs[index];
				else
					index = ctr;
			ReplaceSelections(strs);
		}

		internal void Command_Data_Copy_Count()
		{
			Clipboard.SetText(Selections.Count.ToString());
		}

		internal void Command_Data_Copy_Length()
		{
			NEClipboard.SetStrings(Selections.Select(range => range.Length.ToString()).ToList());
		}

		internal enum Command_MinMax_Type { String, Numeric, Length }
		internal void DoCommand_Data_Copy_MinMax<T>(bool min, Func<Range, T> sortBy, Func<Range, string> value)
		{
			var strs = Selections.AsParallel().AsOrdered().Select(range => new { range = range, sort = sortBy(range) }).OrderBy(obj => obj.sort).ToList();
			var found = min ? strs.First().range : strs.Last().range;
			Clipboard.SetText(value(found));
		}

		internal void Command_Data_Copy_MinMax(bool min, Command_MinMax_Type type)
		{
			switch (type)
			{
				case Command_MinMax_Type.String: DoCommand_Data_Copy_MinMax(min, range => GetString(range), range => GetString(range)); break;
				case Command_MinMax_Type.Numeric: DoCommand_Data_Copy_MinMax(min, range => NumericSort(GetString(range)), range => GetString(range)); break;
				case Command_MinMax_Type.Length: DoCommand_Data_Copy_MinMax(min, range => range.Length, range => range.Length.ToString()); break;
			}
		}

		internal void Command_Data_Copy_Sum()
		{
			Clipboard.SetText(Selections.AsParallel().Select(range => Double.Parse(GetString(range))).Sum().ToString());
		}

		internal void Command_Data_Copy_LinesColumnsPositions(GotoDialog.GotoType gotoType)
		{
			var positions = Selections.Select(range => range.Start).ToList();
			if (gotoType == GotoDialog.GotoType.Position)
			{
				NEClipboard.SetStrings(positions.Select(pos => pos.ToString()).ToList());
				return;
			}

			var lines = positions.Select(pos => Data.GetOffsetLine(pos)).ToList();
			if (gotoType == GotoDialog.GotoType.Line)
			{
				NEClipboard.SetStrings(lines.Select(pos => (pos + 1).ToString()).ToList());
				return;
			}

			var indexes = positions.Select((pos, line) => Data.GetOffsetIndex(pos, lines[line])).ToList();
			NEClipboard.SetStrings(indexes.Select(pos => (pos + 1).ToString()).ToList());
		}

		internal RepeatDialog.Result Command_Data_Repeat_Dialog()
		{
			return RepeatDialog.Run(WindowParent, Selections.Count == 1);
		}

		internal void Command_Data_Repeat(RepeatDialog.Result result)
		{
			List<int> repeatCounts;
			if (result.ClipboardValue)
			{
				var clipboardStrings = NEClipboard.GetStrings();
				if (clipboardStrings.Count != Selections.Count)
					throw new Exception("Number of items on clipboard doesn't match number of selections.");
				repeatCounts = new List<int>(clipboardStrings.Select(str => Int32.Parse(str)));
			}
			else
				repeatCounts = Enumerable.Range(0, Selections.Count).Select(range => result.RepeatCount).ToList();
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, index) => RepeatString(GetString(range), repeatCounts[index])).ToList());
			if (result.SelectRepetitions)
			{
				var sels = new List<Range>();
				for (var ctr = 0; ctr < Selections.Count; ++ctr)
				{
					var selection = Selections[ctr];
					var repeatCount = repeatCounts[ctr];
					var len = selection.Length / repeatCount;
					for (var index = selection.Start; index < selection.End; index += len)
						sels.Add(new Range(index + len, index));
				}
				Selections.Replace(sels);
			}
		}

		internal void Command_Data_Escape_XML()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => GetString(range).Replace("&", "&amp;").Replace("'", "&apos;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;")).ToList());
		}

		internal void Command_Data_Escape_Regex()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Regex.Escape(GetString(range))).ToList());
		}

		internal void Command_Data_Escape_URL()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => HttpUtility.UrlEncode(GetString(range))).ToList());
		}

		internal void Command_Data_Unescape_XML()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => GetString(range).Replace("&apos;", "'").Replace("&quot;", "\"").Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">")).ToList());
		}

		internal void Command_Data_Unescape_Regex()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Regex.Unescape(GetString(range))).ToList());
		}

		internal void Command_Data_Unescape_URL()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => HttpUtility.UrlDecode(GetString(range))).ToList());
		}

		internal EncodingDialog.Result Command_Data_Hash_Dialog()
		{
			return EncodingDialog.Run(WindowParent, CodePage);
		}

		internal void Command_Data_Hash(Hash.Type type, EncodingDialog.Result result)
		{
			var strs = GetSelectionStrings();
			if (!VerifyCanFullyEncode(strs, result.CodePage))
				return;

			ReplaceSelections(strs.AsParallel().AsOrdered().Select(str => Hash.Get(type, Coder.StringToBytes(str, result.CodePage))).ToList());
		}

		async Task<string> GetURL(string url)
		{
			using (var client = new WebClient())
				return await client.DownloadStringTaskAsync(new Uri(url));
		}

		internal async void Command_Markup_FetchURL()
		{
			var urls = GetSelectionStrings();
			var tasks = urls.Select(url => GetURL(url)).ToArray();
			await Task.WhenAll(tasks);
			ReplaceSelections(tasks.Select(task => task.Result).ToList());
		}

		internal void Command_Insert_GUID()
		{
			ReplaceSelections(Selections.AsParallel().Select(range => Guid.NewGuid().ToString()).ToList());
		}

		internal RandomNumberDialog.Result Command_Insert_RandomNumber_Dialog()
		{
			return RandomNumberDialog.Run(WindowParent);
		}

		internal void Command_Insert_RandomNumber(RandomNumberDialog.Result result)
		{
			ReplaceSelections(Selections.AsParallel().Select(range => random.Next(result.MinValue, result.MaxValue + 1).ToString()).ToList());
		}

		internal RandomDataDialog.Result Command_Insert_RandomData_Dialog()
		{
			var hasSelections = Selections.AsParallel().Any(range => range.HasSelection);
			return RandomDataDialog.Run(WindowParent, hasSelections);
		}

		string GetRandomData(RandomDataDialog.Result result, int selectionLength, int clipboardLength)
		{
			int length;
			switch (result.Type)
			{
				case RandomDataDialog.RandomDataType.Absolute: length = result.Value; break;
				case RandomDataDialog.RandomDataType.SelectionLength: length = selectionLength; break;
				case RandomDataDialog.RandomDataType.Clipboard: length = clipboardLength; break;
				default: throw new ArgumentException("Invalid type");
			}
			return new string(Enumerable.Range(0, length).Select(num => result.Chars[random.Next(result.Chars.Length)]).ToArray());
		}

		internal void Command_Insert_RandomData(RandomDataDialog.Result result)
		{
			List<int> clipboardLens = null;
			if (result.Type == RandomDataDialog.RandomDataType.Clipboard)
			{
				var clipboardStrings = NEClipboard.GetStrings();
				if (clipboardStrings.Count != Selections.Count)
					throw new Exception("Number of items on clipboard doesn't match number of selections.");
				clipboardLens = clipboardStrings.AsParallel().AsOrdered().Select(str => Int32.Parse(str)).ToList();
			}
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, num) => GetRandomData(result, range.Length, clipboardLens == null ? 0 : clipboardLens[num])).ToList());
		}

		internal MinMaxValuesDialog.Result Command_Insert_MinMaxValues_Dialog()
		{
			return MinMaxValuesDialog.Run(WindowParent);
		}

		internal void Command_Insert_MinMaxValues(MinMaxValuesDialog.Result result)
		{
			List<Coder.CodePage> clipboardCodePages = null;
			var codePageMinMaxValues = new Dictionary<Coder.CodePage, string> { { result.CodePage, "" } };
			if (result.CodePage == Coder.CodePage.Clipboard)
			{
				var clipboardStrings = NEClipboard.GetStrings();
				if (clipboardStrings.Count != Selections.Count)
					throw new Exception("Number of items on clipboard must match number of selections");
				clipboardCodePages = clipboardStrings.Select(codePageStr => Helpers.ParseEnum<Coder.CodePage>(codePageStr)).ToList();
				codePageMinMaxValues = clipboardCodePages.Distinct().ToDictionary(codePage => codePage, codePage => "");
			}

			foreach (var codePage in codePageMinMaxValues.Keys.ToList())
			{
				if (result.Min)
				{
					codePageMinMaxValues[codePage] += codePage.MinValue();
					if (result.Max)
						codePageMinMaxValues[codePage] += " ";
				}
				if (result.Max)
					codePageMinMaxValues[codePage] += codePage.MaxValue();
			}

			ReplaceSelections(Selections.AsParallel().Select((range, num) => codePageMinMaxValues[result.CodePage == Coder.CodePage.Clipboard ? clipboardCodePages[num] : result.CodePage]).ToList());
		}

		internal void Command_Keys_Set(int index)
		{
			// Handles keys as well as values
			var values = GetSelectionStrings();
			if ((index == 0) && (values.Distinct().Count() != values.Count))
				throw new ArgumentException("Cannot have duplicate keys");
			keysAndValues[index] = values;
			if (index == 0)
				keysHash = values.Select((key, pos) => new { key = key, pos = pos }).ToDictionary(entry => entry.key, entry => entry.pos);
		}

		internal void Command_Keys_SelectionReplace(int index)
		{
			if (keysAndValues[0].Count != keysAndValues[index].Count)
				throw new Exception("Keys and values count must match");

			var strs = new List<string>();
			foreach (var range in Selections)
			{
				var str = GetString(range);
				if (!keysHash.ContainsKey(str))
					strs.Add(str);
				else
					strs.Add(keysAndValues[index][keysHash[str]]);
			}
			ReplaceSelections(strs);
		}

		internal void Command_Keys_GlobalFind(int index)
		{
			if (keysAndValues[0].Count != keysAndValues[index].Count)
				throw new Exception("Keys and values count must match");

			var searcher = new Searcher(keysAndValues[0], true);
			var ranges = new List<Range>();
			var selections = Selections.ToList();
			if ((Selections.Count == 1) && (!Selections[0].HasSelection))
				selections = new List<Range> { new Range(BeginOffset(), EndOffset()) };
			foreach (var selection in selections)
				ranges.AddRange(Data.StringMatches(searcher, selection.Start, selection.Length).Select(tuple => Range.FromIndex(tuple.Item1, tuple.Item2)));

			ranges = ranges.OrderBy(range => range.Start).ToList();
			Selections.Replace(ranges);
		}

		internal void Command_Keys_GlobalReplace(int index)
		{
			if (keysAndValues[0].Count != keysAndValues[index].Count)
				throw new Exception("Keys and values count must match");

			var searcher = new Searcher(keysAndValues[0], true);
			var ranges = new List<Range>();
			var selections = Selections.ToList();
			if ((Selections.Count == 1) && (!Selections[0].HasSelection))
				selections = new List<Range> { new Range(BeginOffset(), EndOffset()) };
			foreach (var selection in selections)
				ranges.AddRange(Data.StringMatches(searcher, selection.Start, selection.Length).Select(tuple => Range.FromIndex(tuple.Item1, tuple.Item2)));

			ranges = ranges.OrderBy(range => range.Start).ToList();

			var strs = new List<string>();
			foreach (var range in ranges)
			{
				var str = GetString(range);
				if (!keysHash.ContainsKey(str))
					strs.Add(str);
				else
					strs.Add(keysAndValues[index][keysHash[str]]);
			}
			Replace(ranges, strs);
		}

		internal void Command_Keys_Copy(int index)
		{
			NEClipboard.SetStrings(keysAndValues[index]);
		}

		internal void Command_Keys_HitsMisses(int index, bool hits)
		{
			var set = new HashSet<string>(keysAndValues[index]);
			Selections.Replace(Selections.AsParallel().AsOrdered().Where(range => set.Contains(GetString(range)) == hits).ToList());
		}

		internal void Command_Keys_CountstoKeysValues1()
		{
			var strs = GetSelectionStrings();
			var group = strs.GroupBy(a => a).Select(a => new { key = a.Key, count = a.Count() }).OrderBy(a => a.count).ToList();
			keysAndValues[0] = group.Select(a => a.key).ToList();
			keysHash = keysAndValues[0].Select((key, pos) => new { key = key, pos = pos }).ToDictionary(entry => entry.key, entry => entry.pos);
			keysAndValues[1] = group.Select(a => a.count.ToString()).ToList();
		}

		internal void Command_SelectRegion_Toggle()
		{
			if (Selections.Count > 1)
			{
				Regions.AddRange(Selections);
				Selections.Replace(Selections.First());
			}
			else if (Regions.Count != 0)
			{
				Selections.Replace(Regions);
				Regions.Clear();
			}
		}

		internal void Command_Select_All()
		{
			Selections.Replace(new Range(EndOffset(), BeginOffset()));
		}

		internal LimitDialog.Result Command_Select_Limit_Dialog()
		{
			return LimitDialog.Run(WindowParent, Selections.Count);
		}

		internal void Command_Select_Limit(LimitDialog.Result result)
		{
			if (result.IgnoreBlank)
				Selections.Replace(Selections.Where(sel => sel.HasSelection).ToList());
			if (result.SelMult > 1)
				Selections.Replace(Selections.AsParallel().AsOrdered().Where((sel, index) => index % result.SelMult == 0).ToList());
			var sels = Math.Min(Selections.Count, result.NumSels);
			Selections.RemoveRange(sels, Selections.Count - sels);
		}

		internal void Command_Select_Lines()
		{
			var lineSets = Selections.AsParallel().Select(range => new { start = Data.GetOffsetLine(range.Start), end = Data.GetOffsetLine(Math.Max(BeginOffset(), range.End - 1)) }).ToList();

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

		internal void Command_Select_Empty(bool include)
		{
			Selections.Replace(Selections.Where(range => range.HasSelection != include).ToList());
		}

		Range TrimRange(Range range)
		{
			var index = range.Start;
			var length = range.Length;
			Data.Trim(ref index, ref length);
			if ((index == range.Start) && (length == range.Length))
				return range;
			return Range.FromIndex(index, length);
		}

		internal void Command_Select_Trim()
		{
			Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => TrimRange(range)).ToList());
		}

		internal WidthDialog.Result Command_Select_Width_Dialog()
		{
			var minLength = Selections.AsParallel().AsOrdered().Min(range => range.Length);
			var maxLength = Selections.AsParallel().AsOrdered().Max(range => range.Length);
			return WidthDialog.Run(WindowParent, minLength, maxLength, false, true);
		}

		bool WidthMatch(string str, WidthDialog.Result result, int clipboardLen)
		{
			switch (result.Type)
			{
				case WidthDialog.WidthType.Absolute: return str.Length == result.Value;
				case WidthDialog.WidthType.Minimum: return str.Length >= result.Value;
				case WidthDialog.WidthType.Maximum: return str.Length <= result.Value;
				case WidthDialog.WidthType.Multiple: return str.Length % result.Value == 0;
				case WidthDialog.WidthType.Clipboard: return str.Length == clipboardLen;
				default: throw new ArgumentException("Invalid width type");
			}
		}

		internal void Command_Select_Width(WidthDialog.Result result)
		{
			List<int> clipboardLens = null;
			if (result.Type == WidthDialog.WidthType.Clipboard)
			{
				var clipboardStrings = NEClipboard.GetStrings();
				if (clipboardStrings.Count != Selections.Count)
					throw new Exception("Number of items on clipboard doesn't match number of selections.");
				clipboardLens = clipboardStrings.AsParallel().AsOrdered().Select(str => Int32.Parse(str)).ToList();
			}

			Selections.Replace(Selections.AsParallel().AsOrdered().Where((range, index) => WidthMatch(GetString(range), result, clipboardLens == null ? 0 : clipboardLens[index])).ToList());
		}

		internal void Command_Select_Unique()
		{
			Selections.Replace(Selections.AsParallel().AsOrdered().GroupBy(range => GetString(range)).Select(list => list.First()).ToList());
		}

		internal void Command_Select_Duplicates()
		{
			Selections.Replace(Selections.AsParallel().AsOrdered().GroupBy(range => GetString(range)).SelectMany(list => list.Skip(1)).ToList());
		}

		internal CountDialog.Result Command_Select_Count_Dialog()
		{
			return CountDialog.Run(WindowParent);
		}

		internal void Command_Select_Count(CountDialog.Result result)
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

		internal void Command_Select_Regions()
		{
			Selections.Replace(Regions);
		}

		internal void Command_Select_FindResults()
		{
			Selections.Replace(Searches);
			Searches.Clear();
		}

		void DoCommand_Select_MinMax<T>(bool min, Func<Range, T> sortBy)
		{
			var selections = Selections.AsParallel().AsOrdered().Select(range => new { range = range, sort = sortBy(range) }).OrderBy(obj => obj.sort).ToList();
			var found = min ? selections.First().sort : selections.Last().sort;
			Selections.Replace(selections.AsParallel().AsOrdered().Where(obj => obj.sort.Equals(found)).Select(obj => obj.range).ToList());
		}

		internal void Command_Select_MinMax(bool min, Command_MinMax_Type type)
		{
			switch (type)
			{
				case Command_MinMax_Type.String: DoCommand_Select_MinMax(min, range => GetString(range)); break;
				case Command_MinMax_Type.Numeric: DoCommand_Select_MinMax(min, range => NumericSort(GetString(range))); break;
				case Command_MinMax_Type.Length: DoCommand_Select_MinMax(min, range => range.Length); break;
			}
		}

		internal GetExpressionDialog.Result Command_Select_ExpressionMatches_Dialog()
		{
			return GetExpressionDialog.Run(WindowParent, GetExpressionData(10));
		}

		internal void Command_Select_ExpressionMatches(GetExpressionDialog.Result result)
		{
			var expressionData = GetExpressionData(expression: result.Expression);
			Selections.Replace(ParallelEnumerable.Range(0, expressionData["x"].Count).AsOrdered().Where(num => (bool)result.Expression.EvaluateDict(expressionData, num)).Select(num => Selections[num]).ToList());
		}

		internal void Command_Select_FirstSelection()
		{
			visibleIndex = 0;
			EnsureVisible(true);
			canvasRenderTimer.Start();
		}

		internal void Command_Select_ShowCurrent()
		{
			EnsureVisible(true);
		}

		internal void Command_Select_NextSelection()
		{
			++visibleIndex;
			if (visibleIndex >= Selections.Count)
				visibleIndex = 0;
			EnsureVisible(true);
			canvasRenderTimer.Start();
		}

		internal void Command_Select_PrevSelection()
		{
			--visibleIndex;
			if (visibleIndex < 0)
				visibleIndex = Selections.Count - 1;
			EnsureVisible(true);
			canvasRenderTimer.Start();
		}

		internal void Command_Select_Single()
		{
			visibleIndex = Math.Max(0, Math.Min(visibleIndex, Selections.Count - 1));
			Selections.Replace(Selections[visibleIndex]);
			visibleIndex = 0;
		}

		internal void Command_Select_Remove()
		{
			Selections.RemoveAt(visibleIndex);
		}

		internal void Command_Region_SetSelection()
		{
			Regions.AddRange(Selections);
		}

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

		internal void Command_Region_LimitToSelection()
		{
			Regions.Replace(Regions.Where(region => Selections.Any(selection => (region.Start >= selection.Start) && (region.End <= selection.End))).ToList());
		}

		int visibleIndex = 0;
		internal void EnsureVisible(bool highlight = false)
		{
			if (Selections.Count == 0)
				return;

			visibleIndex = Math.Max(0, Math.Min(visibleIndex, Selections.Count - 1));
			var range = Selections[visibleIndex];
			var line = Data.GetOffsetLine(range.Cursor);
			var index = Data.GetOffsetIndex(range.Cursor, line);
			Line = line + 1;
			Index = index + 1;
			PositionMin = range.Start;
			PositionMax = range.End;
			Column = Data.GetColumnFromIndex(line, index) + 1;
			if (highlight)
				yScrollValue = line - yScrollViewportFloor / 2;
			yScrollValue = Math.Min(line, Math.Max(line - yScrollViewportFloor + 1, yScrollValue));
			var x = Data.GetColumnFromIndex(line, index);
			xScrollValue = Math.Min(x, Math.Max(x - xScrollViewportFloor + 1, xScrollValue));
		}

		void SelectionsInvalidated()
		{
			if (Selections.Count == 0)
				Selections.Add(new Range(BeginOffset()));
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

		internal bool HandleKey(Key key, bool shiftDown, bool controlDown)
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
								return MoveCursor(range, -1, Int32.MaxValue, shiftDown, indexRel: false);
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
						Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, BeginOffset(), shiftDown)).ToList()); // Have to use MoveCursor for selection
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
								if (!Char.IsWhiteSpace(Data[line, first]))
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
						Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, EndOffset(), shiftDown)).ToList()); // Have to use MoveCursor for selection
					else
						Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, 0, Int32.MaxValue, shiftDown, indexRel: false)).ToList());
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
						if (!Selections.Any(range => range.HasSelection))
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
						var lines = selLines.SelectMany(entry => Enumerable.Range(entry.start, entry.end - entry.start + 1)).Distinct().OrderBy(line => line).ToDictionary(line => line, line => Data.GetOffset(line, 0));
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
						return EndOffset();
					index = -1;
				}

				++index;
				WordSkipType current;
				if (index >= Data.GetLineLength(line))
					current = WordSkipType.Space;
				else
				{
					var c = Data[line, index];
					if (Char.IsWhiteSpace(c))
						current = WordSkipType.Space;
					else if ((Char.IsLetterOrDigit(c)) || (c == '_'))
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
						return BeginOffset();
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
					if (Char.IsWhiteSpace(c))
						current = WordSkipType.Space;
					else if ((Char.IsLetterOrDigit(c)) || (Data[line, index] == '_'))
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
			cursor = Math.Max(BeginOffset(), Math.Min(cursor, EndOffset()));
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
					line += startLine;
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
			var mouseRange = Selections[visibleIndex];

			if (selecting)
			{
				Selections.Remove(mouseRange);
				Selections.Add(MoveCursor(mouseRange, offset, true));
				visibleIndex = Selections.Count - 1;
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

		void OnCanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
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

		void RunSearch(GetRegExDialog.Result result)
		{
			if ((result == null) || (result.Regex == null))
				return;

			Searches.Clear();

			var regions = result.SelectionOnly ? Selections.ToList() : new List<Range> { new Range(EndOffset(), BeginOffset()) };
			foreach (var region in regions)
				Searches.AddRange(Data.RegexMatches(result.Regex, region.Start, region.Length, result.IncludeEndings, result.RegexGroups).Select(tuple => Range.FromIndex(tuple.Item1, tuple.Item2)));
		}

		string GetString(Range range)
		{
			return Data.GetString(range.Start, range.Length);
		}

		enum ReplaceType
		{
			Normal,
			Undo,
			Redo,
		}

		void ReplaceSelections(string str, bool highlight = true, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false)
		{
			ReplaceSelections(Selections.Select(range => str).ToList(), highlight, replaceType, tryJoinUndo);
		}

		void ReplaceSelections(List<string> strs, bool highlight = true, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false)
		{
			Replace(Selections, strs, replaceType, tryJoinUndo);

			if (highlight)
				Selections.Replace(Selections.AsParallel().AsOrdered().Select((range, index) => new Range(range.End, range.End - (strs == null ? 0 : strs[index].Length))).ToList());
			else
				Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => new Range(range.End)).ToList());
		}

		void Replace(List<Range> ranges, List<string> strs, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false)
		{
			if (!ranges.Any())
				return;

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

			// Abort if no changes
			if (!Enumerable.Range(0, ranges.Count).Any(ctr => undoText[ctr] != strs[ctr]))
				return;

			var textCanvasUndoRedo = new UndoRedo.UndoRedoStep(undoRanges, undoText, tryJoinUndo);
			switch (replaceType)
			{
				case ReplaceType.Undo: undoRedo.AddUndone(textCanvasUndoRedo); break;
				case ReplaceType.Redo: undoRedo.AddRedone(textCanvasUndoRedo); break;
				case ReplaceType.Normal: undoRedo.AddUndo(textCanvasUndoRedo); break;
			}

			Data.Replace(ranges.Select(range => range.Start).ToList(), ranges.Select(range => range.Length).ToList(), strs);

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
				return;

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

		string SetWidth(string str, WidthDialog.Result result, int clipboardLen)
		{
			int length;
			switch (result.Type)
			{
				case WidthDialog.WidthType.Absolute: length = result.Value; break;
				case WidthDialog.WidthType.Relative: length = Math.Max(0, str.Length + result.Value); break;
				case WidthDialog.WidthType.Minimum: length = Math.Max(str.Length, result.Value); break;
				case WidthDialog.WidthType.Maximum: length = Math.Min(str.Length, result.Value); break;
				case WidthDialog.WidthType.Multiple: length = str.Length + result.Value - 1 - (str.Length + result.Value - 1) % result.Value; break;
				case WidthDialog.WidthType.Clipboard: length = clipboardLen; break;
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

		protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			base.OnPreviewMouseLeftButtonDown(e);
			Focus();
		}

		internal bool Empty()
		{
			return (FileName == null) && (!IsModified) && (BeginOffset() == EndOffset());
		}

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

		public override string ToString()
		{
			return FileName;
		}
	}
}
