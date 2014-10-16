using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using NeoEdit.TextEditor.Dialogs;

namespace NeoEdit.TextEditor
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
				Marks.Clear();
				Searches.Clear();
				undoRedo.Clear();
				CalculateBoundaries();
			}
		}
		readonly UndoRedo undoRedo;

		[DepProp]
		public string FileName { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool IsModified { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public Highlighting.HighlightingType HighlightType { get { return uiHelper.GetPropValue<Highlighting.HighlightingType>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public Coder.Type CoderUsed { get { return uiHelper.GetPropValue<Coder.Type>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool HasBOM { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int Line { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int Column { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int Index { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int NumSelections { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int xScrollValue { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int yScrollValue { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public string LineEnding { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }

		int xScrollViewportFloor { get { return (int)Math.Floor(xScroll.ViewportSize); } }
		int xScrollViewportCeiling { get { return (int)Math.Ceiling(xScroll.ViewportSize); } }
		int yScrollViewportFloor { get { return (int)Math.Floor(yScroll.ViewportSize); } }
		int yScrollViewportCeiling { get { return (int)Math.Ceiling(yScroll.ViewportSize); } }

		readonly ObservableCollection<Range> Selections = new ObservableCollection<Range>();
		readonly ObservableCollection<Range> Searches = new ObservableCollection<Range>();
		readonly ObservableCollection<Range> Marks = new ObservableCollection<Range>();

		Random random = new Random();

		static TextEditor()
		{
			UIHelper<TextEditor>.Register();
			UIHelper<TextEditor>.AddCallback(a => a.xScrollValue, (obj, o, n) => obj.renderTimer.Start());
			UIHelper<TextEditor>.AddCallback(a => a.yScrollValue, (obj, o, n) => obj.renderTimer.Start());
			UIHelper<TextEditor>.AddCallback(a => a.HighlightType, (obj, o, n) => obj.renderTimer.Start());
			UIHelper<TextEditor>.AddCoerce(a => a.xScrollValue, (obj, value) => (int)Math.Max(obj.xScroll.Minimum, Math.Min(obj.xScroll.Maximum, value)));
			UIHelper<TextEditor>.AddCoerce(a => a.yScrollValue, (obj, value) => (int)Math.Max(obj.yScroll.Minimum, Math.Min(obj.yScroll.Maximum, value)));
		}

		RunOnceTimer selectionsTimer, searchesTimer, marksTimer, renderTimer;
		Font font = new Font();

		readonly UIHelper<TextEditor> uiHelper;
		public TextEditor(string filename = null, byte[] bytes = null, Coder.Type encoding = Coder.Type.None, int line = -1, int column = -1)
		{
			uiHelper = new UIHelper<TextEditor>(this);
			InitializeComponent();

			undoRedo = new UndoRedo(b => IsModified = b);
			selectionsTimer = new RunOnceTimer(SelectionsInvalidated);
			searchesTimer = new RunOnceTimer(SearchesInvalidated);
			marksTimer = new RunOnceTimer(MarksInvalidated);
			renderTimer = new RunOnceTimer(() => canvas.InvalidateVisual());

			OpenFile(filename, bytes, encoding);
			Goto(line, column);

			Selections.CollectionChanged += (s, e) => selectionsTimer.Start();
			Searches.CollectionChanged += (s, e) => searchesTimer.Start();
			Marks.CollectionChanged += (s, e) => marksTimer.Start();

			UIHelper<TextEditor>.AddCallback(this, Canvas.ActualWidthProperty, () => CalculateBoundaries());
			UIHelper<TextEditor>.AddCallback(this, Canvas.ActualHeightProperty, () => CalculateBoundaries());

			canvas.MouseLeftButtonDown += OnCanvasMouseLeftButtonDown;
			canvas.MouseLeftButtonUp += OnCanvasMouseLeftButtonUp;
			canvas.MouseMove += OnCanvasMouseMove;
			canvas.Render += OnCanvasRender;

			MouseWheel += (s, e) => yScrollValue -= e.Delta / 40;

			Loaded += (s, e) =>
			{
				EnsureVisible();
				renderTimer.Start();
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
			var multiBinding = new MultiBinding { Converter = new NeoEdit.GUI.Common.ExpressionConverter(), ConverterParameter = @"([0]==''?'[Untitled]':FileName([0]))t+([1]?'*':'')" };
			multiBinding.Bindings.Add(new Binding("FileName") { Source = this });
			multiBinding.Bindings.Add(new Binding("IsModified") { Source = this });
			label.SetBinding(Label.ContentProperty, multiBinding);
			return label;
		}

		DateTime fileLastWrite;
		internal void OpenFile(string filename, byte[] bytes = null, Coder.Type encoding = Coder.Type.None)
		{
			FileName = filename;
			if (File.Exists(FileName))
				fileLastWrite = new FileInfo(FileName).LastWriteTime;
			var modified = bytes != null;
			if (bytes == null)
			{
				if (FileName == null)
					bytes = Encoding.UTF8.GetPreamble();
				else
					bytes = File.ReadAllBytes(FileName);
			}
			if (encoding == Coder.Type.None)
				encoding = Coder.GuessEncoding(bytes);
			Data = new TextData(bytes, encoding);
			undoRedo.SetModified(modified);
			CoderUsed = encoding;
			HighlightType = Highlighting.Get(FileName);
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
			if (!IsModified)
				return true;

			switch (new Message
			{
				Title = "Confirm",
				Text = "Do you want to save changes?",
				Options = Message.OptionsEnum.YesNoCancel,
				DefaultCancel = Message.OptionsEnum.Cancel,
			}.Show())
			{
				case Message.OptionsEnum.Cancel: return false;
				case Message.OptionsEnum.No: return true;
				case Message.OptionsEnum.Yes:
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

		internal void Command_File_Save()
		{
			if (FileName == null)
				Command_File_SaveAs();
			else
			{
				File.WriteAllBytes(FileName, Data.GetBytes(CoderUsed));
				fileLastWrite = new FileInfo(FileName).LastWriteTime;
				undoRedo.SetModified(false);
			}
		}

		internal void Command_File_SaveAs()
		{
			var dialog = new SaveFileDialog
			{
				DefaultExt = "txt",
				Filter = "Text files|*.txt|All files|*.*",
				FilterIndex = 2,
				FileName = Path.GetFileName(FileName),
				InitialDirectory = Path.GetDirectoryName(FileName),
			};
			if (dialog.ShowDialog() == true)
			{
				if (Directory.Exists(dialog.FileName))
					throw new Exception("A directory by that name already exists");
				if (!Directory.Exists(Path.GetDirectoryName(dialog.FileName)))
					throw new Exception("Directory doesn't exist");
				FileName = dialog.FileName;
				Command_File_Save();
			}
		}

		internal void Command_File_Refresh()
		{
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

		internal void Command_File_InsertFiles()
		{
			var run = true;

			if (Selections.Count != 1)
			{
				new Message
				{
					Title = "Error",
					Text = "You have more than one selection.",
					Options = Message.OptionsEnum.Ok,
				}.Show();
				run = false;
			}

			if (run)
			{
				var dialog = new OpenFileDialog { DefaultExt = "txt", Filter = "Text files|*.txt|All files|*.*", FilterIndex = 2, Multiselect = true };
				if (dialog.ShowDialog() == true)
				{
					var str = "";
					foreach (var filename in dialog.FileNames)
					{
						var bytes = File.ReadAllBytes(filename);
						var data = new TextData(bytes, Coder.GuessEncoding(bytes));

						var beginOffset = data.GetOffset(0, 0);
						var endOffset = data.GetOffset(data.NumLines - 1, data.GetLineLength(data.NumLines - 1));
						str += data.GetString(beginOffset, endOffset - beginOffset);
					}

					ReplaceSelections(str);
				}
			}
		}

		internal void Command_File_CopyPath()
		{
			ClipboardWindow.SetFiles(new List<string> { FileName }, false);
		}

		internal void Command_File_CopyName()
		{
			Clipboard.SetText(Path.GetFileName(FileName));
		}

		internal void Command_File_Encoding()
		{
			var result = EncodingDialog.Run(CoderUsed, HasBOM, LineEnding);
			if (result == null)
				return;

			CoderUsed = result.Encoding;

			if (Data.BOM != result.BOM)
			{
				if (result.BOM)
					Replace(new ObservableCollection<Range> { new Range(0, 0) }, new List<string> { "\ufeff" });
				else
					Replace(new ObservableCollection<Range> { new Range(0, 1) }, new List<string> { "" });
			}

			if (result.LineEndings != null)
			{
				var lines = Data.NumLines;
				var sel = new ObservableCollection<Range>();
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

		internal void Command_File_BinaryEditor()
		{
			Launcher.Static.LaunchBinaryEditor(FileName, Data.GetBytes(CoderUsed), CoderUsed);
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
			var result = Selections.Select(range => GetString(range)).ToArray();
			if (result.Length != 0)
				ClipboardWindow.Set(result);
			if (isCut)
				ReplaceSelections("");
		}

		internal void Command_Edit_Paste()
		{
			var clipboardStrings = ClipboardWindow.GetStrings().ToList();
			if ((clipboardStrings == null) || (clipboardStrings.Count == 0))
				return;

			if (clipboardStrings.Count == 1)
				clipboardStrings = Selections.Select(str => clipboardStrings[0]).ToList();

			if (Selections.Count == clipboardStrings.Count)
			{
				ReplaceSelections(clipboardStrings, shiftDown);
				return;
			}

			if (Selections.Count != 1)
				throw new Exception(String.Format("You must have either 1 or the number copied selections ({0}).", clipboardStrings.Count));

			clipboardStrings = clipboardStrings.Select(str => str.TrimEnd('\r', '\n') + Data.DefaultEnding).ToList();
			var replace = new List<string> { String.Join("", clipboardStrings) };

			var offset = Selections.First().Start;
			ReplaceSelections(replace);
			Selections.Clear();
			foreach (var str in clipboardStrings)
			{
				Selections.Add(Range.FromIndex(offset, str.Length - Data.DefaultEnding.Length));
				offset += str.Length;
			}
		}

		internal void Command_Edit_ShowClipboard()
		{
			ClipboardWindow.Show();
		}

		internal void Command_Edit_Find()
		{
			var selecting = shiftDown;
			string text = null;
			var selectionOnly = Selections.Any(range => range.HasSelection());

			if (Selections.Count == 1)
			{
				var sel = Selections.First();
				if ((selectionOnly) && (Data.GetOffsetLine(sel.Cursor) == Data.GetOffsetLine(sel.Highlight)) && (sel.Length < 1000))
				{
					selectionOnly = false;
					text = GetString(sel);
				}
			}

			var findResult = FindDialog.Run(text, selectionOnly);
			if (findResult != null)
			{
				RunSearch(findResult);
				if (findResult.SelectAll)
				{
					if (Searches.Count != 0)
						Selections.Replace(Searches);
					Searches.Clear();
				}

				FindNext(true, selecting);
			}
		}

		internal void Command_Edit_FindNextPrev(bool next)
		{
			FindNext(next, shiftDown);
		}

		bool shiftDown { get { return (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None; } }
		bool controlDown { get { return (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None; } }

		internal void Command_Edit_GotoLine()
		{
			var selecting = shiftDown;
			var line = Data.GetOffsetLine(Selections.First().Start);
			var newLine = GotoLineDialog.Run(Data.NumLines, line);
			if (newLine.HasValue)
				Selections.Replace(Selections.Select(range => MoveCursor(range, newLine.Value, 0, false, true, selecting)).ToList());
		}

		internal void Command_Edit_GotoIndex()
		{
			var selecting = shiftDown;
			var offset = Selections.First().Start;
			var line = Data.GetOffsetLine(offset);
			var index = Data.GetOffsetIndex(offset, line);
			var newIndex = GotoIndexDialog.Run(Data.GetLineLength(line) + 1, index);
			if (newIndex.HasValue)
				Selections.Replace(Selections.Select(range => MoveCursor(range, 0, newIndex.Value, true, false, selecting)).ToList());
		}

		internal void Command_Files_CutCopy(bool isCut)
		{
			var result = Selections.Select(range => GetString(range)).ToArray();
			if (result.Length != 0)
				ClipboardWindow.SetFiles(result, isCut);
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
				var files = Selections.Select(range => GetString(range)).ToArray();
				foreach (var file in files)
				{
					if (File.Exists(file))
						File.Delete(file);
					if (Directory.Exists(file))
						Directory.Delete(file, true);
				}
			}
		}

		[Flags]
		internal enum TimestampType
		{
			Write = 1,
			Access = 2,
			Create = 4,
			All = Write | Access | Create,
		}

		internal void Command_Files_Timestamp(TimestampType type)
		{
			var result = ChooseDateTimeDialog.Run(DateTime.Now);
			if (result != null)
			{
				var files = Selections.Select(range => GetString(range)).ToArray();
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
		}

		internal void Command_Files_Path_Simplify()
		{
			ReplaceSelections(Selections.Select(range => Path.GetFullPath(GetString(range))).ToList());
		}

		internal void Command_Files_Path_GetFilePath(GetPathType type)
		{
			Selections.Replace(Selections.Select(range => GetPathRange(type, range)).ToList());
		}

		internal void Command_Files_CreateDirectory()
		{
			var files = Selections.Select(range => GetString(range)).ToArray();
			foreach (var file in files)
				Directory.CreateDirectory(file);
		}

		internal void Command_Files_Information_Size()
		{
			var files = Selections.Select(range => GetString(range)).ToList();
			var strs = new List<string>();
			foreach (var file in files)
			{
				if (File.Exists(file))
				{
					var fileinfo = new FileInfo(file);
					strs.Add(fileinfo.Length.ToString());
				}
				else if (Directory.Exists(file))
					strs.Add("Directory");
				else
					strs.Add("INVALID");
			}
			ReplaceSelections(strs);
		}

		internal void Command_Files_Information_WriteTime()
		{
			var files = Selections.Select(range => GetString(range)).ToList();
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

		internal void Command_Files_Information_AccessTime()
		{
			var files = Selections.Select(range => GetString(range)).ToList();
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

		internal void Command_Files_Information_CreateTime()
		{
			var files = Selections.Select(range => GetString(range)).ToList();
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

		internal void Command_Files_Information_Attributes()
		{
			var files = Selections.Select(range => GetString(range)).ToList();
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

		internal void Command_Files_Information_ReadOnly()
		{
			var files = Selections.Select(range => GetString(range)).ToList();
			var strs = new List<string>();
			foreach (var file in files)
			{
				if (File.Exists(file))
				{
					var fileinfo = new FileInfo(file);
					strs.Add(fileinfo.IsReadOnly.ToString());
				}
				else if (Directory.Exists(file))
					strs.Add("Directory");
				else
					strs.Add("INVALID");
			}
			ReplaceSelections(strs);
		}

		static bool FileOrDirectoryExists(string name)
		{
			return (Directory.Exists(name)) || (File.Exists(name));
		}

		internal void Command_Files_Select_Existing(bool existing)
		{
			var sels = Selections.Where(range => FileOrDirectoryExists(GetString(range)) == existing).ToList();
			Selections.Replace(sels);
		}

		internal void Command_Files_Select_Files()
		{
			var sels = Selections.Where(range => File.Exists(GetString(range))).ToList();
			Selections.Replace(sels);
		}

		internal void Command_Files_Select_Directories()
		{
			var sels = Selections.Where(range => Directory.Exists(GetString(range))).ToList();
			Selections.Replace(sels);
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

			var result = sels.Where(sel => roots.Contains(sel.str) == include).Select(sel => sel.range).ToList();
			Selections.Replace(result);
		}

		internal void Command_Files_RenameKeysToSelections()
		{
			if ((keysAndValues[0].Count == 0) || (Selections.Count == 0))
				throw new Exception("Keys and selections must be set");

			if (keysAndValues[0].Count != Selections.Count)
				throw new Exception("Keys and selections count must match");

			var sels = Selections.Select(range => GetString(range)).ToList();

			if ((keysAndValues[0].Any(a => String.IsNullOrEmpty(a))) || (sels.Any(a => String.IsNullOrEmpty(a))))
				throw new Exception("Can't have empty items in list");

			var invalid = keysAndValues[0].FirstOrDefault(file => !FileOrDirectoryExists(file));
			if (invalid != null)
				throw new Exception(String.Format("File/directory doesn't exist: {0}", invalid));

			invalid = sels.FirstOrDefault(file => FileOrDirectoryExists(file));
			if (invalid != null)
				throw new Exception(String.Format("File/directory already exists: {0}", invalid));

			var paths = sels.Select(path => Path.GetDirectoryName(path)).Distinct().ToList();
			invalid = paths.FirstOrDefault(dir => !Directory.Exists(dir));
			if (invalid != null)
				throw new Exception(String.Format("Directory doesn't exist: {0}", invalid));

			const int numExamples = 10;
			var examples = String.Join("", Enumerable.Range(0, Math.Min(numExamples, sels.Count)).Select(num => keysAndValues[0][num] + " => " + sels[num] + "\n"));
			if (sels.Count > numExamples)
				examples += String.Format(" + {0} more\n", sels.Count - numExamples);

			if (new Message
			{
				Title = "Confirm",
				Text = "Are you sure you want to rename these files?\n" + examples,
				Options = Message.OptionsEnum.YesNo,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.No,
				Width = 900,
			}.Show() != Message.OptionsEnum.Yes)
				return;

			for (var ctr = 0; ctr < sels.Count; ++ctr)
				if (Directory.Exists(keysAndValues[0][ctr]))
					Directory.Move(keysAndValues[0][ctr], sels[ctr]);
				else
					File.Move(keysAndValues[0][ctr], sels[ctr]);
		}

		internal void Command_Data_Case_Upper()
		{
			ReplaceSelections(Selections.Select(range => GetString(range).ToUpperInvariant()).ToList());
		}

		internal void Command_Data_Case_Lower()
		{
			ReplaceSelections(Selections.Select(range => GetString(range).ToLowerInvariant()).ToList());
		}

		internal void Command_Data_Case_Proper()
		{
			ReplaceSelections(Selections.Select(range => GetString(range).ToProper()).ToList());
		}

		internal void Command_Data_Case_Toggle()
		{
			ReplaceSelections(Selections.Select(range => GetString(range).ToToggled()).ToList());
		}

		internal void Command_Data_Hex_ToHex()
		{
			ReplaceSelections(Selections.Select(range => Int64.Parse(GetString(range)).ToString("x")).ToList());
		}

		internal void Command_Data_Hex_FromHex()
		{
			ReplaceSelections(Selections.Select(range => Int64.Parse(GetString(range), NumberStyles.HexNumber).ToString()).ToList());
		}

		internal void Command_Data_Char_ToChar()
		{
			ReplaceSelections(Selections.Select(range => GetString(range).FromUTF8HexString()).ToList());
		}

		internal void Command_Data_Char_FromChar()
		{
			ReplaceSelections(Selections.Select(range => GetString(range).ToUTF8HexString()).ToList());
		}

		internal void Command_Data_DateTime_Insert()
		{
			ReplaceSelections(DateTime.Now.ToString("O"));
		}

		internal void Command_Data_DateTime_Convert()
		{
			var strs = Selections.Select(range => GetString(range)).ToList();
			if (strs.Count < 1)
				return;

			var result = ConvertDateTimeDialog.Run(strs.First());
			if (result == null)
				return;

			strs = strs.Select(str => ConvertDateTimeDialog.ConvertFormat(str, result.InputFormat, result.InputUTC, result.OutputFormat, result.OutputUTC)).ToList();
			ReplaceSelections(strs);

		}

		internal void Command_Data_Length()
		{
			ReplaceSelections(Selections.Select(range => GetString(range).Length.ToString()).ToList());
		}

		internal void Command_Data_Width()
		{
			var strs = Selections.Select(range => GetString(range)).ToList();
			var minWidth = strs.Select(str => str.Length).Max();
			var numeric = strs.All(str => str.IsNumeric());
			var result = WidthDialog.Run(minWidth, numeric ? '0' : ' ', numeric);
			if (result == null)
				return;
			ReplaceSelections(Selections.Select(range => SetWidth(GetString(range), result.Value, result.PadChar, result.Before)).ToList());
		}

		internal void Command_Data_Trim()
		{
			var strs = Selections.Select(range => GetString(range)).ToList();
			if (strs.All(str => str.IsNumeric()))
				strs = strs.Select(str => str.TrimStart('0')).ToList();
			else
				strs = strs.Select(str => str.Trim()).ToList();
			ReplaceSelections(strs);
		}

		internal void Command_Data_EvaluateExpression()
		{
			var strs = Selections.Select(range => GetString(range)).ToList();
			var expression = ExpressionDialog.GetExpression(strs);
			if (expression == null)
				return;

			strs = strs.Select((str, pos) => expression.Evaluate(str, pos + 1).ToString()).ToList();
			ReplaceSelections(strs);
		}

		internal void Command_Data_Series()
		{
			ReplaceSelections(Selections.Select((range, index) => (index + 1).ToString()).ToList());
		}

		internal void Command_Data_Repeat()
		{
			var repeat = RepeatDialog.Run(Selections.Count == 1);
			if (repeat == null)
				return;

			ReplaceSelections(Selections.Select(range => RepeatString(GetString(range), repeat.RepeatCount)).ToList());
			if (repeat.SelectAll)
			{
				var newSelections = new ObservableCollection<Range>();
				foreach (var selection in Selections)
				{
					var len = selection.Length / repeat.RepeatCount;
					for (var index = selection.Start; index < selection.End; index += len)
						newSelections.Add(new Range(index + len, index));
				}
				Selections.Replace(newSelections);
			}
		}

		internal void Command_Data_GUID()
		{
			ReplaceSelections(Selections.Select(range => Guid.NewGuid().ToString()).ToList());
		}

		internal void Command_Data_Random()
		{
			var result = RandomNumberDialog.Run();
			if (result == null)
				return;

			ReplaceSelections(Selections.Select(range => random.Next(result.MinValue, result.MaxValue + 1).ToString()).ToList());
		}

		internal void Command_Data_Escape_XML()
		{
			ReplaceSelections(Selections.Select(range => GetString(range).Replace("&", "&amp;").Replace("'", "&apos;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;")).ToList());
		}

		internal void Command_Data_Escape_Regex()
		{
			ReplaceSelections(Selections.Select(range => Regex.Escape(GetString(range))).ToList());
		}

		internal void Command_Data_Unescape_XML()
		{
			ReplaceSelections(Selections.Select(range => GetString(range).Replace("&apos;", "'").Replace("&quot;", "\"").Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">")).ToList());
		}

		internal void Command_Data_Unescape_Regex()
		{
			ReplaceSelections(Selections.Select(range => Regex.Unescape(GetString(range))).ToList());
		}

		internal void Command_Data_Checksum(Checksum.Type type, Coder.Type coder)
		{
			ReplaceSelections(Selections.Select(range => Checksum.Get(type, Coder.StringToBytes(GetString(range), coder))).ToList());
		}

		internal void Command_Keys_SetValues(int index)
		{
			// Handles keys as well as values
			var values = Selections.Select(range => GetString(range)).ToList();
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
			var ranges = new ObservableCollection<Range>();
			var selections = Selections;
			if ((Selections.Count == 1) && (!Selections[0].HasSelection()))
				selections = new ObservableCollection<Range> { new Range(BeginOffset(), EndOffset()) };
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
			var ranges = new ObservableCollection<Range>();
			var selections = Selections;
			if ((Selections.Count == 1) && (!Selections[0].HasSelection()))
				selections = new ObservableCollection<Range> { new Range(BeginOffset(), EndOffset()) };
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

		internal void Command_Keys_CopyValues(int index)
		{
			ClipboardWindow.Set(keysAndValues[index].ToArray());
		}

		internal void Command_Keys_HitsMisses(int index, bool hits)
		{
			var set = new HashSet<string>(keysAndValues[index]);
			Selections.Replace(Selections.Where(range => set.Contains(GetString(range)) == hits).ToList());
		}

		internal void Command_Keys_Counts()
		{
			var strs = Selections.Select(range => GetString(range)).ToList();
			var group = strs.GroupBy(a => a).Select(a => new { key = a.Key, count = a.Count() }).OrderBy(a => a.count).ToList();
			keysAndValues[0] = group.Select(a => a.key).ToList();
			keysHash = keysAndValues[0].Select((key, pos) => new { key = key, pos = pos }).ToDictionary(entry => entry.key, entry => entry.pos);
			keysAndValues[1] = group.Select(a => a.count.ToString()).ToList();
		}

		internal void Command_SelectMark_Toggle()
		{
			if (Selections.Count > 1)
			{
				Marks.AddRange(Selections);
				Selections.Replace(Selections.First());
			}
			else if (Marks.Count != 0)
			{
				Selections.Replace(Marks);
				Marks.Clear();
			}
		}

		internal void Command_Select_All()
		{
			Selections.Replace(new Range(EndOffset(), BeginOffset()));
		}

		internal void Command_Select_Limit()
		{
			var result = LimitDialog.Run(Selections.Count);
			if (result == null)
				return;

			if (result.IgnoreBlank)
				Selections.Replace(Selections.Where(sel => sel.HasSelection()).ToList());
			if (result.SelMult > 1)
				Selections.Replace(Selections.Where((sel, index) => index % result.SelMult == 0).ToList());
			var sels = Math.Min(Selections.Count, result.NumSels);
			Selections.RemoveRange(sels, Selections.Count - sels);
		}

		internal void Command_Select_Lines()
		{
			var lines = Selections.SelectMany(selection => Enumerable.Range(Data.GetOffsetLine(selection.Start), Data.GetOffsetLine(selection.End - 1) - Data.GetOffsetLine(selection.Start) + 1)).Distinct().OrderBy(lineNum => lineNum).ToList();
			var sels = lines.Select(line => new Range(Data.GetOffset(line, Data.GetLineLength(line)), Data.GetOffset(line, 0))).ToList();
			Selections.Replace(sels);
		}

		internal void Command_Select_NonEmpty(bool include)
		{
			Selections.Replace(Selections.Where(range => range.HasSelection() == include).ToList());
		}

		internal void Command_Select_Unique()
		{
			Selections.Replace(Selections.GroupBy(range => GetString(range)).Select(list => list.First()).ToList());
		}

		internal void Command_Select_Duplicates()
		{
			Selections.Replace(Selections.GroupBy(range => GetString(range)).SelectMany(list => list.Skip(1)).ToList());
		}

		internal void Command_Select_Marks()
		{
			if (Marks.Count != 0)
			{
				Selections.Replace(Marks);
				Marks.Clear();
			}
		}

		internal void Command_Select_Find()
		{
			Selections.Replace(Searches);
			Searches.Clear();
		}

		internal void Command_Select_Min_String()
		{
			var selections = Selections.Where(range => range.HasSelection()).Select(range => new { range = range, str = GetString(range) }).OrderBy(obj => obj.str).ToList();
			var first = selections.First().str;
			Selections.Replace(selections.Where(obj => obj.str == first).Select(obj => obj.range).ToList());
		}

		internal void Command_Select_Min_Numeric()
		{
			var selections = Selections.Where(range => range.HasSelection()).Select(range => new { range = range, str = GetString(range) }).OrderBy(obj => NumericSort(obj.str)).ToList();
			var first = selections.First().str;
			Selections.Replace(selections.Where(obj => obj.str == first).Select(obj => obj.range).ToList());
		}

		internal void Command_Select_Max_String()
		{
			var selections = Selections.Where(range => range.HasSelection()).Select(range => new { range = range, str = GetString(range) }).OrderBy(obj => obj.str).ToList();
			var first = selections.Last().str;
			Selections.Replace(selections.Where(obj => obj.str == first).Select(obj => obj.range).ToList());
		}

		internal void Command_Select_Max_Numeric()
		{
			var selections = Selections.Where(range => range.HasSelection()).Select(range => new { range = range, str = GetString(range) }).OrderBy(obj => NumericSort(obj.str)).ToList();
			var first = selections.Last().str;
			Selections.Replace(selections.Where(obj => obj.str == first).Select(obj => obj.range).ToList());
		}

		internal void Command_Select_ExpressionMatches(bool include)
		{
			var strs = Selections.Select(range => GetString(range)).ToList();
			var expression = ExpressionDialog.GetExpression(strs);
			if (expression != null)
			{
				var sels = new ObservableCollection<Range>();
				for (var ctr = 0; ctr < strs.Count; ++ctr)
					if ((bool)expression.Evaluate(strs[ctr], ctr + 1) == include)
						sels.Add(Selections[ctr]);
				Selections.Replace(sels);
			}
		}

		internal void Command_Select_RegExMatches(bool include)
		{
			var strs = Selections.Select(range => GetString(range)).ToList();
			var expression = ExpressionDialog.GetRegEx(strs);
			if (expression != null)
			{
				var sels = new ObservableCollection<Range>();
				for (var ctr = 0; ctr < strs.Count; ++ctr)
					if (expression.IsMatch(strs[ctr]) == include)
						sels.Add(Selections[ctr]);
				Selections.Replace(sels);
			}
		}

		internal void Command_Select_ShowFirst()
		{
			visibleIndex = 0;
			EnsureVisible(true);
			renderTimer.Start();
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
			renderTimer.Start();
		}

		internal void Command_Select_PrevSelection()
		{
			--visibleIndex;
			if (visibleIndex < 0)
				visibleIndex = Selections.Count - 1;
			EnsureVisible(true);
			renderTimer.Start();
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

		internal void Command_Mark_Selection()
		{
			Marks.AddRange(Selections);
		}

		internal void Command_Mark_Find()
		{
			Marks.AddRange(Searches);
			Searches.Clear();
		}

		internal void Command_Mark_Clear()
		{
			var hasSelection = Selections.Any(range => range.HasSelection());
			if (!hasSelection)
				Marks.Clear();
			else
			{
				foreach (var selection in Selections)
				{
					var toRemove = Marks.Where(mark => (mark.Start >= selection.Start) && (mark.End <= selection.End)).ToList();
					toRemove.ForEach(mark => Marks.Remove(mark));
				}
			}
		}

		internal void Command_Mark_LimitToSelection()
		{
			Marks.Replace(Marks.Where(mark => Selections.Any(selection => (mark.Start >= selection.Start) && (mark.End <= selection.End))).ToList());
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
			selectionsTimer.Stop();
			if (visible != null)
			{
				visibleIndex = Selections.FindIndex(range => (range.Start == visible.Start) && (range.End == visible.End));
				if (visibleIndex < 0)
					visibleIndex = 0;
			}

			EnsureVisible();
			renderTimer.Start();
		}

		void SearchesInvalidated()
		{
			Searches.Replace(Searches.Where(range => range.HasSelection()).ToList());
			Searches.DeOverlap();
			searchesTimer.Stop();
			renderTimer.Start();
		}

		void MarksInvalidated()
		{
			Marks.DeOverlap();
			marksTimer.Stop();
			renderTimer.Start();
		}

		void OnCanvasRender(object sender, DrawingContext dc)
		{
			if ((Data == null) || (yScrollViewportCeiling == 0) || (xScrollViewportCeiling == 0))
				return;

			var brushes = new List<Tuple<ObservableCollection<Range>, Brush>>
			{
				new Tuple<ObservableCollection<Range>, Brush>(Selections, Misc.selectionBrush),
				new Tuple<ObservableCollection<Range>, Brush>(Searches, Misc.searchBrush),
				new Tuple<ObservableCollection<Range>, Brush>(Marks, Misc.markBrush),
			};

			HasBOM = Data.BOM;

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
			var y = lines.ToDictionary(line => line, line => (line - startLine) * font.lineHeight);
			var cursorLineDone = new HashSet<int>();
			var visibleCursor = (visibleIndex >= 0) && (visibleIndex < Selections.Count) ? Selections[visibleIndex] : null;

			foreach (var entry in brushes)
			{
				var hasSelection = entry.Item1.Any(range => range.HasSelection());

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
							dc.DrawRectangle(Misc.visibleCursorBrush, null, new Rect(0, y[cursorLine], canvas.ActualWidth, font.lineHeight));

						if (!cursorLineDone.Contains(cursorLine))
						{
							dc.DrawRectangle(Misc.cursorBrush, Misc.cursorPen, new Rect(0, y[cursorLine], canvas.ActualWidth, font.lineHeight));
							cursorLineDone.Add(cursorLine);
						}

						var cursor = Data.GetOffsetIndex(range.Cursor, cursorLine);
						if ((cursor >= startIndexes[cursorLine]) && (cursor <= endIndexes[cursorLine]))
						{
							cursor = Data.GetColumnFromIndex(cursorLine, cursor);
							dc.DrawRectangle(Brushes.Black, null, new Rect((cursor - startColumn) * font.charWidth - 1, y[cursorLine], 2, font.lineHeight));
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
						var width = end - start;

						var steps = range == visibleCursor ? 2 : 1;
						for (var ctr = 0; ctr < steps; ++ctr)
							dc.DrawRectangle(entry.Item2, null, new Rect(start * font.charWidth, y[line], width * font.charWidth + 1, font.lineHeight));
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
				var text = font.GetText(str);
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

		protected override void OnKeyDown(KeyEventArgs e)
		{
			e.Handled = true;
			switch (e.Key)
			{
				case Key.Back:
				case Key.Delete:
					{
						if (Selections.Any(range => range.HasSelection()))
						{
							ReplaceSelections("");
							break;
						}

						var selections = new ObservableCollection<Range>();
						foreach (var range in Selections)
						{
							var offset = range.Start;

							if (controlDown)
							{
								if (e.Key == Key.Back)
									offset = GetPrevWord(offset);
								else
									offset = GetNextWord(offset);
							}
							else
							{
								var line = Data.GetOffsetLine(offset);
								var index = Data.GetOffsetIndex(offset, line);

								if (e.Key == Key.Back)
									--index;
								else
									++index;

								if (index < 0)
								{
									--line;
									if (line < 0)
										continue;
									index = Data.GetLineLength(line);
								}
								if (index > Data.GetLineLength(line))
								{
									++line;
									if (line >= Data.NumLines)
										continue;
									index = 0;
								}

								offset = Data.GetOffset(line, index);
							}

							selections.Add(new Range(offset, range.Highlight));
						}

						Replace(selections, null);
					}
					break;
				case Key.Escape:
					Searches.Clear();
					break;
				case Key.Left:
					{
						var hasSelection = Selections.Any(range => range.HasSelection());
						for (var ctr = 0; ctr < Selections.Count; ++ctr)
						{
							var line = Data.GetOffsetLine(Selections[ctr].Cursor);
							var index = Data.GetOffsetIndex(Selections[ctr].Cursor, line);
							if (controlDown)
								Selections[ctr] = MoveCursor(Selections[ctr], GetPrevWord(Selections[ctr].Cursor));
							else if ((!shiftDown) && (hasSelection))
								Selections[ctr] = new Range(Selections[ctr].Start);
							else if ((index == 0) && (line != 0))
								Selections[ctr] = MoveCursor(Selections[ctr], -1, Int32.MaxValue, indexRel: false);
							else
								Selections[ctr] = MoveCursor(Selections[ctr], 0, -1);
						}
					}
					break;
				case Key.Right:
					{
						var hasSelection = Selections.Any(range => range.HasSelection());
						for (var ctr = 0; ctr < Selections.Count; ++ctr)
						{
							var line = Data.GetOffsetLine(Selections[ctr].Cursor);
							var index = Data.GetOffsetIndex(Selections[ctr].Cursor, line);
							if (controlDown)
								Selections[ctr] = MoveCursor(Selections[ctr], GetNextWord(Selections[ctr].Cursor));
							else if ((!shiftDown) && (hasSelection))
								Selections[ctr] = new Range(Selections[ctr].End);
							else if ((index == Data.GetLineLength(line)) && (line != Data.NumLines - 1))
								Selections[ctr] = MoveCursor(Selections[ctr], 1, 0, indexRel: false);
							else
								Selections[ctr] = MoveCursor(Selections[ctr], 0, 1);
						}
					}
					break;
				case Key.Up:
				case Key.Down:
					{
						var mult = e.Key == Key.Up ? -1 : 1;
						if (!controlDown)
							Selections.Replace(Selections.Select(range => MoveCursor(range, mult, 0)).ToList());
						else if (!shiftDown)
							yScrollValue += mult;
						else if (e.Key == Key.Down)
							Selections.Add(MoveCursor(Selections.Last(), mult, 0, selecting: false));
						else if (Selections.Count > 1)
							Selections.RemoveAt(Selections.Count - 1);
					}
					break;
				case Key.Home:
					if (controlDown)
						Selections.Replace(Selections.Select(range => MoveCursor(range, BeginOffset())).ToList()); // Have to use MoveCursor for selection
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
							Selections[ctr] = MoveCursor(Selections[ctr], 0, first, indexRel: false);
						}
						if (!changed)
						{
							Selections.Replace(Selections.Select(range => MoveCursor(range, 0, 0, indexRel: false)).ToList());
							xScrollValue = 0;
						}
					}
					break;
				case Key.End:
					if (controlDown)
						Selections.Replace(Selections.Select(range => MoveCursor(range, EndOffset())).ToList()); // Have to use MoveCursor for selection
					else
						Selections.Replace(Selections.Select(range => MoveCursor(range, 0, Int32.MaxValue, indexRel: false)).ToList());
					break;
				case Key.PageUp:
					if (controlDown)
						yScrollValue -= yScrollViewportFloor / 2;
					else
						Selections.Replace(Selections.Select(range => MoveCursor(range, 1 - yScrollViewportFloor, 0)).ToList());
					break;
				case Key.PageDown:
					if (controlDown)
						yScrollValue += yScrollViewportFloor / 2;
					else
						Selections.Replace(Selections.Select(range => MoveCursor(range, yScrollViewportFloor - 1, 0)).ToList());
					break;
				case Key.Tab:
					{
						if (!Selections.Any(range => range.HasSelection()))
						{
							HandleText("\t");
							break;
						}

						var selLines = Selections.Where(a => a.HasSelection()).Select(range => new { start = Data.GetOffsetLine(range.Start), end = Data.GetOffsetLine(range.End - 1) }).ToList();
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
						for (var ctr = 0; ctr < Selections.Count; ++ctr)
						{
							var newPos = Data.GetOppositeBracket(Selections[ctr].Cursor);
							if (newPos == -1)
								continue;

							Selections[ctr] = MoveCursor(Selections[ctr], newPos);
						}
					}
					else
						e.Handled = false;
					break;
				default: e.Handled = false; break;
			}

			if (selectionsTimer.Started())
				EnsureVisible();
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

		Range MoveCursor(Range range, int cursor, bool? selecting = null)
		{
			if (selecting == null)
				selecting = shiftDown;
			if (selecting.Value)
				return new Range(cursor, range.Highlight);

			return new Range(cursor);
		}

		Range MoveCursor(Range range, int line, int index, bool lineRel = true, bool indexRel = true, bool? selecting = null)
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
			var line = Math.Min(Data.NumLines - 1, (int)(mousePos.Y / font.lineHeight) + yScrollValue);
			var index = Math.Min(Data.GetLineLength(line), Data.GetIndexFromColumn(line, (int)(mousePos.X / font.charWidth) + xScrollValue, true));
			var offset = Data.GetOffset(line, index);
			var mouseRange = Selections[visibleIndex];

			if (selecting)
			{
				Selections.Remove(mouseRange);
				Selections.Add(MoveCursor(mouseRange, offset, true));
				visibleIndex = Selections.Count - 1;
				return;
			}

			if (!controlDown)
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
			MouseHandler(e.GetPosition(canvas), e.ClickCount, false);
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

		void RunSearch(FindDialog.Result result)
		{
			if ((result == null) || (result.Regex == null))
				return;

			Searches.Clear();

			var regions = result.SelectionOnly ? Selections : new ObservableCollection<Range> { new Range(EndOffset(), BeginOffset()) };
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

		void ReplaceSelections(string str, bool highlight = true, ReplaceType replaceType = ReplaceType.Normal)
		{
			ReplaceSelections(Selections.Select(range => str).ToList(), highlight, replaceType);
		}

		void ReplaceSelections(List<string> strs, bool highlight = true, ReplaceType replaceType = ReplaceType.Normal)
		{
			Replace(Selections, strs, replaceType);

			if (highlight)
				Selections.Replace(Selections.Select((range, index) => new Range(range.End, range.End - (strs == null ? 0 : strs[index].Length))).ToList());
			else
				Selections.Replace(Selections.Select(range => new Range(range.End)).ToList());
		}

		void Replace(ObservableCollection<Range> ranges, List<string> strs, ReplaceType replaceType = ReplaceType.Normal)
		{
			if (ranges.Count == 0)
				return;

			if (strs == null)
				strs = ranges.Select(range => "").ToList();

			if (ranges.Count != strs.Count)
				throw new Exception("Invalid string count");

			var undoRanges = new ObservableCollection<Range>();
			var undoText = new List<string>();

			var change = 0;
			for (var ctr = 0; ctr < ranges.Count; ++ctr)
			{
				var undoRange = new Range(ranges[ctr].Start + change, ranges[ctr].Start + strs[ctr].Length + change);
				undoRanges.Add(undoRange);
				undoText.Add(GetString(ranges[ctr]));
				change = undoRange.Highlight - ranges[ctr].End;
			}

			var textCanvasUndoRedo = new UndoRedo.UndoRedoStep(undoRanges, undoText);
			switch (replaceType)
			{
				case ReplaceType.Undo: undoRedo.AddUndone(textCanvasUndoRedo); break;
				case ReplaceType.Redo: undoRedo.AddRedone(textCanvasUndoRedo); break;
				case ReplaceType.Normal: undoRedo.AddUndo(textCanvasUndoRedo); break;
			}

			Data.Replace(ranges.Select(range => range.Start).ToList(), ranges.Select(range => range.Length).ToList(), strs);

			var translateNums = RangeExtensions.GetTranslateNums(Selections, Marks, Searches);
			var translateMap = RangeExtensions.GetTranslateMap(translateNums, ranges, strs);
			Selections.Translate(translateMap);
			Marks.Translate(translateMap);
			var searchLens = Searches.Select(range => range.Length).ToList();
			Searches.Translate(translateMap);
			Searches.Replace(Searches.Where((range, index) => searchLens[index] == range.Length).ToList());

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

		string SetWidth(string str, int length, char padChar, bool before)
		{
			var pad = new string(padChar, length - str.Length);
			if (before)
				return pad + str;
			return str + pad;
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

		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			base.OnTextInput(e);
			HandleText(e.Text);
		}

		internal bool Empty()
		{
			return (!IsModified) && (BeginOffset() == EndOffset());
		}

		internal void HandleText(string text)
		{
			if (text.Length == 0)
				return;

			ReplaceSelections(text, false);
			if (selectionsTimer.Started())
				EnsureVisible();
		}

		void CalculateBoundaries()
		{
			if ((canvas.ActualWidth <= 0) || (canvas.ActualHeight <= 0))
				return;

			xScroll.ViewportSize = canvas.ActualWidth / font.charWidth;
			xScroll.Minimum = 0;
			xScroll.Maximum = Data.MaxColumn - xScrollViewportFloor;
			xScroll.SmallChange = 1;
			xScroll.LargeChange = Math.Max(0, xScroll.ViewportSize - 1);
			xScrollValue = xScrollValue;

			yScroll.ViewportSize = canvas.ActualHeight / font.lineHeight;
			yScroll.Minimum = 0;
			yScroll.Maximum = Data.NumLines - yScrollViewportFloor;
			yScroll.SmallChange = 1;
			yScroll.LargeChange = Math.Max(0, yScroll.ViewportSize - 1);
			yScrollValue = yScrollValue;

			LineEnding = Data.OnlyEnding;

			renderTimer.Start();
		}

		public override string ToString()
		{
			return FileName;
		}
	}
}
