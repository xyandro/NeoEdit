using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
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
				undo.Clear();
				redo.Clear();
				CalculateBoundaries();
			}
		}

		[DepProp]
		public string FileName { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int ModifiedSteps { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public Highlighting.HighlightingType HighlightType { get { return uiHelper.GetPropValue<Highlighting.HighlightingType>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public Coder.Type CoderUsed { get { return uiHelper.GetPropValue<Coder.Type>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool HasBOM { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool CheckUpdates { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int Line { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int Column { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int Index { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int NumSelections { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int xScrollValue { get { return uiHelper.GetPropValue<int>(); } set { value = Math.Max(0, Math.Min(xScrollMax, value)); uiHelper.SetPropValue(value); } }
		[DepProp]
		public int yScrollValue { get { return uiHelper.GetPropValue<int>(); } set { value = Math.Max(0, Math.Min(yScrollMax, value)); uiHelper.SetPropValue(value); } }
		[DepProp]
		public double xScrollViewport { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public double yScrollViewport { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int xScrollMax { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int yScrollMax { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }

		int xScrollViewportFloor { get { return (int)Math.Floor(xScrollViewport); } }
		int xScrollViewportCeiling { get { return (int)Math.Ceiling(xScrollViewport); } }
		int yScrollViewportFloor { get { return (int)Math.Floor(yScrollViewport); } }
		int yScrollViewportCeiling { get { return (int)Math.Ceiling(yScrollViewport); } }

		new readonly TextEditorParent Parent;
		readonly RangeList Selections = new RangeList();
		readonly RangeList Searches = new RangeList();
		readonly RangeList Marks = new RangeList();

		Random random = new Random();

		static TextEditor()
		{
			UIHelper<TextEditor>.Register();
			UIHelper<TextEditor>.AddCallback(a => a.FileName, (obj, o, n) => obj.FilenameChanged());
			UIHelper<TextEditor>.AddCallback(a => a.xScrollValue, (obj, o, n) => obj.InvalidateRender());
			UIHelper<TextEditor>.AddCallback(a => a.yScrollValue, (obj, o, n) => obj.InvalidateRender());
			UIHelper<TextEditor>.AddCallback(a => a.xScrollViewport, (obj, o, n) => obj.InvalidateRender());
			UIHelper<TextEditor>.AddCallback(a => a.yScrollViewport, (obj, o, n) => obj.InvalidateRender());
			UIHelper<TextEditor>.AddCallback(a => a.HighlightType, (obj, o, n) => obj.InvalidateRender());
		}

		readonly UIHelper<TextEditor> uiHelper;
		public TextEditor(TextEditorParent _parent, string filename = null, byte[] bytes = null, Coder.Type encoding = Coder.Type.None, int line = -1, int column = -1)
		{
			Parent = _parent;
			uiHelper = new UIHelper<TextEditor>(this);
			InitializeComponent();

			OpenFile(filename, bytes, encoding);
			Goto(line, column);

			CheckUpdates = true;

			Selections.CollectionChanged += () => InvalidateSelections();
			Searches.CollectionChanged += () => InvalidateSearches();
			Marks.CollectionChanged += () => InvalidateMarks();

			var fontFamily = new FontFamily(new Uri("pack://application:,,,/GUI;component/"), "./Resources/#Anonymous Pro");
			typeface = fontFamily.GetTypefaces().First();
			fontSize = 14;
			lineHeight = fontSize;

			var example = "0123456789 abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ !@#$%^&*()";
			var formattedText = new FormattedText(example, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
			charWidth = formattedText.Width / example.Length;

			UIHelper<TextEditor>.AddCallback(this, Canvas.ActualWidthProperty, () => CalculateBoundaries());
			UIHelper<TextEditor>.AddCallback(this, Canvas.ActualHeightProperty, () => CalculateBoundaries());

			canvas.MouseLeftButtonDown += OnCanvasMouseLeftButtonDown;
			canvas.MouseLeftButtonUp += OnCanvasMouseLeftButtonUp;
			canvas.MouseMove += OnCanvasMouseMove;
			canvas.Render += OnCanvasRender;

			Loaded += (s, e) =>
			{
				EnsureVisible();
				InvalidateRender();
			};
		}

		internal void Goto(int line, int column)
		{
			line = Math.Max(0, Math.Min(line, Data.NumLines) - 1);
			var index = Data.GetIndexFromColumn(line, Math.Max(0, column - 1), true);
			Selections.Add(new Range(Data.GetOffset(line, index)));

		}

		internal void HandleMouseWheel(int delta)
		{
			yScrollValue -= delta * yScrollViewportFloor / 480;
		}

		FileSystemWatcher watcher;
		DateTime fileLastWrite;
		bool checkWatcher = false; // Make sure we only create dialog once
		void FilenameChanged()
		{
			if (watcher != null)
			{
				watcher.EnableRaisingEvents = false;
				watcher.Dispose();
				watcher = null;
			}

			if (String.IsNullOrEmpty(FileName))
				return;

			watcher = new FileSystemWatcher(Path.GetDirectoryName(FileName), Path.GetFileName(FileName));
			watcher.NotifyFilter = NotifyFilters.LastWrite;
			var dispatcher = Dispatcher.CurrentDispatcher;
			watcher.Changed += (s, e) =>
			{
				var watcherTimer = new DispatcherTimer(DispatcherPriority.Normal, dispatcher);
				watcherTimer.Tick += (s2, e2) =>
				{
					watcherTimer.Stop();
					watcherTimer = null;

					if ((!CheckUpdates) || (checkWatcher))
						return;

					checkWatcher = true;
					try
					{
						var lastWrite = new FileInfo(FileName).LastWriteTime;
						if (fileLastWrite != lastWrite)
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
						fileLastWrite = lastWrite;
					}
					finally
					{
						checkWatcher = false;
					}
				};
				watcherTimer.Start();
			};
			watcher.EnableRaisingEvents = true;
		}

		internal void OpenFile(string filename, byte[] bytes = null, Coder.Type encoding = Coder.Type.None)
		{
			FileName = filename;
			if (File.Exists(FileName))
				fileLastWrite = new FileInfo(FileName).LastWriteTime;
			if (bytes == null)
			{
				if (FileName == null)
					bytes = new byte[0];
				else
					bytes = File.ReadAllBytes(FileName);
			}
			if (encoding == Coder.Type.None)
				encoding = Coder.GuessEncoding(bytes);
			Data = new TextData(bytes, encoding);
			CoderUsed = encoding;
			HighlightType = Highlighting.Get(FileName);
			ModifiedSteps = 0;
		}

		int BeginOffset()
		{
			return Data.GetOffset(0, 0);
		}

		int EndOffset()
		{
			return Data.GetOffset(Data.NumLines - 1, Data.GetLineLength(Data.NumLines - 1));
		}

		string EscapeXML(string str)
		{
			return str.Replace("&", "&amp;").Replace("'", "&apos;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");
		}

		string UnescapeXML(string str)
		{
			return str.Replace("&apos;", "'").Replace("&quot;", "\"").Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">");
		}

		string EscapeRegex(string str)
		{
			return Regex.Escape(str);
		}

		string UnescapeRegex(string str)
		{
			return Regex.Unescape(str);
		}

		bool ConfirmModified()
		{
			if (ModifiedSteps == 0)
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
					return ModifiedSteps == 0;
			}
			return false;
		}

		//protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		//{
		//	if (!ConfirmModified())
		//	{
		//		e.Cancel = true;
		//		return;
		//	}

		//	base.OnClosing(e);
		//}

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
				ModifiedSteps = 0;
			}
		}

		internal void Command_File_SaveAs()
		{
			var dialog = new SaveFileDialog { DefaultExt = "txt", Filter = "Text files|*.txt|All files|*.*", FilterIndex = 2 };
			if (dialog.ShowDialog() == true)
			{
				if (Directory.Exists(dialog.FileName))
					throw new Exception("A directory by that name already exists.");
				if (!Directory.Exists(Path.GetDirectoryName(dialog.FileName)))
					throw new Exception("Directory doesn't exist.");
				FileName = dialog.FileName;
				Command_File_Save();
			}
		}

		internal void Command_File_Revert()
		{
			var run = true;

			if ((run) && (ModifiedSteps != 0))
			{
				run = new Message
				{
					Title = "Confirm",
					Text = "You have unsaved changes.  Are you sure you want to reload?",
					Options = Message.OptionsEnum.YesNo,
					DefaultAccept = Message.OptionsEnum.Yes,
					DefaultCancel = Message.OptionsEnum.No,
				}.Show() == Message.OptionsEnum.Yes;
			}

			if (run)
				OpenFile(FileName);
		}

		internal void Command_File_CheckUpdates()
		{
			CheckUpdates = !CheckUpdates;
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

					Replace(Selections, new List<string> { str }, true);
				}
			}
		}

		internal void Command_File_CopyPath()
		{
			Clipboard.SetText(FileName);
		}

		internal void Command_File_CopyName()
		{
			Clipboard.SetText(Path.GetFileName(FileName));
		}

		internal void Command_File_BinaryEditor()
		{
			Launcher.Static.LaunchBinaryEditor(FileName, Data.GetBytes(CoderUsed));
		}

		internal void Command_File_BOM()
		{
			if (Data.BOM)
				Replace(new RangeList { new Range(0, 1) }, new List<string> { "" }, true);
			else
				Replace(new RangeList { new Range(0, 0) }, new List<string> { "\ufeff" }, true);
		}

		internal void Command_Edit_Undo()
		{
			if (undo.Count != 0)
			{
				var undoStep = undo.Last();
				undo.Remove(undoStep);
				Replace(undoStep.ranges, undoStep.text, true, ReplaceType.Undo);
			}
		}

		internal void Command_Edit_Redo()
		{
			if (redo.Count != 0)
			{
				var redoStep = redo.Last();
				redo.Remove(redoStep);
				Replace(redoStep.ranges, redoStep.text, true, ReplaceType.Redo);
			}
		}

		internal void Command_Edit_CutCopy(bool isCut)
		{
			var result = Selections.Select(range => GetString(range)).ToArray();
			if (result.Length != 0)
				ClipboardWindow.Set(result);
			if (isCut)
				Replace(Selections, null, false);
		}

		internal void Command_Edit_Paste()
		{
			var result = ClipboardWindow.GetStrings().ToList();
			if ((Selections.Count == 1) && (result.Count != 1))
				result = result.Select(str => str + Data.DefaultEnding).ToList();
			if ((result != null) && (result.Count != 0))
			{
				while (result.Count > Selections.Count)
				{
					result[result.Count - 2] += result[result.Count - 1];
					result.RemoveAt(result.Count - 1);
				}
				while (result.Count < Selections.Count)
					result.Add(result.Last());

				Replace(Selections, result, false);
			}
		}

		internal void Command_Edit_ShowClipboard()
		{
			ClipboardWindow.Show();
		}

		internal void Command_Edit_Find()
		{
			string text = null;
			var selectionOnly = Selections.Any(range => range.HasSelection());

			if (Selections.Count == 1)
			{
				var sel = Selections.First();
				if ((sel.HasSelection()) && (Data.GetOffsetLine(sel.Cursor) == Data.GetOffsetLine(sel.Highlight)) && (sel.Length < 1000))
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

				FindNext(true);
			}
		}

		internal void Command_Edit_FindNextPrev(bool next)
		{
			FindNext(next);
		}

		bool shiftDown { get { return Parent.shiftDown; } }
		bool? shiftOverride { get { return Parent.shiftOverride; } set { Parent.shiftOverride = value; } }
		bool controlDown { get { return Parent.controlDown; } }

		internal void Command_Edit_GotoLine()
		{
			var shift = shiftDown;
			var line = Data.GetOffsetLine(Selections.First().Start);
			var newLine = GotoLineDialog.Run(Data.NumLines, line);
			if (newLine.HasValue)
			{
				shiftOverride = shift;
				Selections.Replace(Selections.Select(range => MoveCursor(range, newLine.Value, 0, false, true)).ToList());
				shiftOverride = null;
			}
		}

		internal void Command_Edit_GotoIndex()
		{
			var shift = shiftDown;
			var offset = Selections.First().Start;
			var line = Data.GetOffsetLine(offset);
			var index = Data.GetOffsetIndex(offset, line);
			var newIndex = GotoIndexDialog.Run(Data.GetLineLength(line) + 1, index);
			if (newIndex.HasValue)
			{
				shiftOverride = shift;
				Selections.Replace(Selections.Select(range => MoveCursor(range, 0, newIndex.Value, true, false)).ToList());
				shiftOverride = null;
			}
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
					if ((!File.Exists(file)) && (!Directory.Exists(file)))
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
			var strs = Selections.Select(range => Path.GetFullPath(GetString(range))).ToList();
			Replace(Selections, strs, true);
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
			Replace(Selections, strs, true);
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
			Replace(Selections, strs, true);
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
			Replace(Selections, strs, true);
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
			Replace(Selections, strs, true);
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
			Replace(Selections, strs, true);
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
			Replace(Selections, strs, true);
		}

		internal void Command_Data_Case_Upper()
		{
			var strs = Selections.Select(range => GetString(range).ToUpperInvariant()).ToList();
			Replace(Selections, strs, true);
		}

		internal void Command_Data_Case_Lower()
		{
			var strs = Selections.Select(range => GetString(range).ToLowerInvariant()).ToList();
			Replace(Selections, strs, true);
		}

		internal void Command_Data_Case_Proper()
		{
			var strs = Selections.Select(range => GetString(range).ToProper()).ToList();
			Replace(Selections, strs, true);
		}

		internal void Command_Data_Case_Toggle()
		{
			var strs = Selections.Select(range => GetString(range).ToToggled()).ToList();
			Replace(Selections, strs, true);
		}

		internal void Command_Data_Hex_ToHex()
		{
			var selections = Selections.Where(range => range.HasSelection()).ToList();
			var strs = selections.Select(range => Int64.Parse(GetString(range)).ToString("x")).ToList();
			Replace(selections, strs, true);
		}

		internal void Command_Data_Hex_FromHex()
		{
			var selections = Selections.Where(range => range.HasSelection()).ToList();
			var strs = selections.Select(range => Int64.Parse(GetString(range), NumberStyles.HexNumber).ToString()).ToList();
			Replace(selections, strs, true);
		}

		internal void Command_Data_Char_ToChar()
		{
			var selections = Selections.Where(range => range.HasSelection()).ToList();
			var strs = selections.Select(range => GetString(range).FromUTF8HexString()).ToList();
			Replace(selections, strs, true);
		}

		internal void Command_Data_Char_FromChar()
		{
			var selections = Selections.Where(range => range.HasSelection()).ToList();
			var strs = selections.Select(range => GetString(range).ToUTF8HexString()).ToList();
			Replace(selections, strs, true);
		}

		internal void Command_Data_DateTime_Insert()
		{
			var now = DateTime.Now.ToString("O");
			Replace(Selections, Selections.Select(range => now).ToList(), true);
		}

		internal void Command_Data_DateTime_Convert()
		{
			var strs = Selections.Select(range => GetString(range)).ToList();
			if (strs.Count >= 1)
			{
				string inputFormat, outputFormat;
				bool inputUTC, outputUTC;
				if (ConvertDateTimeDialog.Run(strs.First(), out inputFormat, out inputUTC, out outputFormat, out outputUTC))
				{
					strs = strs.Select(str => ConvertDateTimeDialog.ConvertFormat(str, inputFormat, inputUTC, outputFormat, outputUTC)).ToList();
					Replace(Selections, strs, true);
				}
			}

		}

		internal void Command_Data_Length()
		{
			var selections = Selections.Where(range => range.HasSelection()).ToList();
			var strs = selections.Select(range => GetString(range).Length.ToString()).ToList();
			Replace(selections, strs, true);
		}

		internal void Command_Data_Width()
		{
			var minWidth = Selections.Select(range => range.Length).Max();
			var text = String.Join("", Selections.Select(range => GetString(range)));
			var numeric = Regex.IsMatch(text, "^[0-9a-fA-F]+$");
			var widthDialog = new WidthDialog(minWidth, numeric ? '0' : ' ', numeric);
			if (widthDialog.ShowDialog() == true)
				Replace(Selections, Selections.Select(range => SetWidth(GetString(range), widthDialog.Value, widthDialog.PadChar, widthDialog.Before)).ToList(), true);
		}

		internal void Command_Data_Trim()
		{
			var selections = Selections.Where(range => range.HasSelection()).ToList();
			var strs = selections.Select(range => GetString(range).Trim().TrimStart('0')).ToList();
			Replace(selections, strs, true);
		}

		internal void Command_Data_EvaluateExpression()
		{
			var strs = Selections.Select(range => GetString(range)).ToList();
			var expression = ExpressionDialog.GetExpression(strs);
			if (expression != null)
			{
				strs = strs.Select((str, pos) => expression.Evaluate(str, pos + 1).ToString()).ToList();
				Replace(Selections, strs, true);
			}
		}

		internal void Command_Data_Series()
		{
			var strs = Enumerable.Range(1, Selections.Count).Select(num => num.ToString()).ToList();
			Replace(Selections, strs, true);
		}

		internal void Command_Data_Repeat()
		{
			var repeat = RepeatDialog.Run(Selections.Count == 1);
			if (repeat != null)
			{
				var strs = Selections.Select(range => RepeatString(GetString(range), repeat.RepeatCount)).ToList();
				Replace(Selections, strs, true);
				if (repeat.SelectAll)
				{
					var newSelections = new RangeList();
					foreach (var selection in Selections)
					{
						var len = selection.Length / repeat.RepeatCount;
						for (var index = selection.Start; index < selection.End; index += len)
							newSelections.Add(new Range(index + len, index));
					}
					Selections.Replace(newSelections);
				}
			}
		}

		internal void Command_Data_GUID()
		{
			var strs = Selections.Select(range => Guid.NewGuid().ToString()).ToList();
			Replace(Selections, strs, true);
		}

		internal void Command_Data_Random()
		{
			int minValue, maxValue;
			if (RandomNumberDialog.Run(out minValue, out maxValue))
			{
				var strs = Selections.Select(range => random.Next(minValue, maxValue + 1).ToString()).ToList();
				Replace(Selections, strs, true);
			}
		}

		internal void Command_Data_Escape_XML()
		{
			var strs = Selections.Select(range => EscapeXML(GetString(range))).ToList();
			Replace(Selections, strs, true);
		}

		internal void Command_Data_Escape_Regex()
		{
			var strs = Selections.Select(range => EscapeRegex(GetString(range))).ToList();
			Replace(Selections, strs, true);
		}

		internal void Command_Data_Unescape_XML()
		{
			var strs = Selections.Select(range => UnescapeXML(GetString(range))).ToList();
			Replace(Selections, strs, true);
		}

		internal void Command_Data_Unescape_Regex()
		{
			var strs = Selections.Select(range => UnescapeRegex(GetString(range))).ToList();
			Replace(Selections, strs, true);
		}

		internal void Command_Data_Checksum(Checksum.Type type, Coder.Type coder)
		{
			var strs = Selections.Select(range => Checksum.Get(type, Coder.StringToBytes(GetString(range), coder))).ToList();
			Replace(Selections, strs, true);
		}

		internal void Command_Keys_SetValues(int index)
		{
			// Handles keys as well as values
			var values = Selections.Select(range => GetString(range)).ToList();
			if ((index == 0) && (values.Distinct().Count() != values.Count))
				throw new ArgumentException("Cannot have duplicate keys.");
			keysAndValues[index] = values;
			if (index == 0)
				keysHash = values.Select((key, pos) => new { key = key, pos = pos }).ToDictionary(entry => entry.key, entry => entry.pos);
		}

		internal void Command_Keys_SelectionReplace(int index)
		{
			if (keysAndValues[0].Count != keysAndValues[index].Count)
				throw new Exception("Keys and values count must match.");

			var strs = new List<string>();
			foreach (var range in Selections)
			{
				var str = GetString(range);
				if (!keysHash.ContainsKey(str))
					strs.Add(str);
				else
					strs.Add(keysAndValues[index][keysHash[str]]);
			}
			Replace(Selections, strs, true);
		}

		internal void Command_Keys_GlobalFind(int index)
		{
			if (keysAndValues[0].Count != keysAndValues[index].Count)
				throw new Exception("Keys and values count must match.");

			var searcher = Searcher.Create(keysAndValues[0]);
			var ranges = new RangeList();
			var selections = Selections;
			if ((Selections.Count == 1) && (!Selections[0].HasSelection()))
				selections = new RangeList { new Range(BeginOffset(), EndOffset()) };
			foreach (var selection in selections)
				ranges.AddRange(Data.StringMatches(searcher, selection.Start, selection.Length).Select(tuple => Range.FromIndex(tuple.Item1, tuple.Item2)));

			ranges = ranges.OrderBy(range => range.Start).ToList();
			Selections.Replace(ranges);
		}

		internal void Command_Keys_GlobalReplace(int index)
		{
			if (keysAndValues[0].Count != keysAndValues[index].Count)
				throw new Exception("Keys and values count must match.");

			var searcher = Searcher.Create(keysAndValues[0]);
			var ranges = new RangeList();
			var selections = Selections;
			if ((Selections.Count == 1) && (!Selections[0].HasSelection()))
				selections = new RangeList { new Range(BeginOffset(), EndOffset()) };
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
			Replace(ranges, strs, true);
		}

		internal void Command_Keys_CopyValues(int index)
		{
			ClipboardWindow.Set(keysAndValues[index].ToArray());
		}

		internal void Command_Keys_HitsValues(int index)
		{
			Selections.Replace(Selections.Where(range => keysAndValues[index].Contains(GetString(range))).ToList());
		}

		internal void Command_Keys_MissesValues(int index)
		{
			Selections.Replace(Selections.Where(range => !keysAndValues[index].Contains(GetString(range))).ToList());
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
			var numSels = LimitDialog.Run(Selections.Count);
			if (numSels.HasValue)
				Selections.RemoveRange(numSels.Value, Selections.Count - numSels.Value);
		}

		internal void Command_Select_AllLines()
		{
			var lines = Selections.SelectMany(selection => Enumerable.Range(Data.GetOffsetLine(selection.Start), Data.GetOffsetLine(selection.End - 1) - Data.GetOffsetLine(selection.Start) + 1)).Distinct().OrderBy(lineNum => lineNum).ToList();
			var sels = lines.Select(line => new Range(Data.GetOffset(line, Data.GetLineLength(line)), Data.GetOffset(line, 0))).ToList();
			Selections.Replace(sels);
		}

		internal void Command_Select_Lines()
		{
			int lineMult;
			bool ignoreBlankLines;
			if (SelectLinesDialog.Run(out lineMult, out ignoreBlankLines))
			{
				var selections = Selections;
				if ((selections.Count == 1) && (!selections[0].HasSelection()))
					selections = new RangeList { new Range(BeginOffset(), EndOffset()) };
				var lines = selections.SelectMany(selection => Enumerable.Range(Data.GetOffsetLine(selection.Start), Data.GetOffsetLine(selection.End - 1) - Data.GetOffsetLine(selection.Start) + 1)).Distinct().OrderBy(lineNum => lineNum).ToList();
				var sels = lines.Select(line => new Range(Data.GetOffset(line, Data.GetLineLength(line)), Data.GetOffset(line, 0))).ToList();
				if (ignoreBlankLines)
					sels = sels.Where(sel => sel.Cursor != sel.Highlight).ToList();
				if (lineMult > 1)
					sels = sels.Where((sel, index) => index % lineMult == 0).ToList();
				Selections.Replace(sels);
			}
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

		internal void Command_Select_RemoveEmpty()
		{
			Selections.Replace(Selections.Where(range => range.HasSelection()).ToList());
		}

		internal void Command_Select_Unique()
		{
			Selections.Replace(Selections.GroupBy(range => GetString(range)).Select(list => list.First()).ToList());
		}

		internal void Command_Select_Duplicates()
		{
			Selections.Replace(Selections.GroupBy(range => GetString(range)).SelectMany(list => list.Skip(1)).ToList());
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

		internal void Command_Select_ExpressionMatches()
		{
			var strs = Selections.Select(range => GetString(range)).ToList();
			var expression = ExpressionDialog.GetExpression(strs);
			if (expression != null)
			{
				var sels = new RangeList();
				for (var ctr = 0; ctr < strs.Count; ++ctr)
					if ((bool)expression.Evaluate(strs[ctr], ctr + 1))
						sels.Add(Selections[ctr]);
				Selections.Replace(sels);
			}
		}

		internal void Command_Select_RegExMatches()
		{
			var strs = Selections.Select(range => GetString(range)).ToList();
			var expression = ExpressionDialog.GetRegEx(strs);
			if (expression != null)
			{
				var sels = new RangeList();
				for (var ctr = 0; ctr < strs.Count; ++ctr)
					if (expression.IsMatch(strs[ctr]))
						sels.Add(Selections[ctr]);
				Selections.Replace(sels);
			}
		}

		internal void Command_Select_RegExNonMatches()
		{
			var strs = Selections.Select(range => GetString(range)).ToList();
			var expression = ExpressionDialog.GetRegEx(strs);
			if (expression != null)
			{
				var sels = new RangeList();
				for (var ctr = 0; ctr < strs.Count; ++ctr)
					if (!expression.IsMatch(strs[ctr]))
						sels.Add(Selections[ctr]);
				Selections.Replace(sels);
			}
		}

		internal void Command_Select_ShowFirst()
		{
			visibleIndex = 0;
			EnsureVisible(true);
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
		}

		internal void Command_Select_PrevSelection()
		{
			--visibleIndex;
			if (visibleIndex < 0)
				visibleIndex = Selections.Count - 1;
			EnsureVisible(true);
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

		internal readonly double charWidth;
		internal readonly double lineHeight;

		readonly Typeface typeface;
		readonly double fontSize;

		class TextCanvasUndoRedo
		{
			public RangeList ranges { get; private set; }
			public List<string> text { get; private set; }

			public TextCanvasUndoRedo(RangeList _ranges, List<string> _text)
			{
				ranges = _ranges;
				text = _text;
			}
		}

		List<TextCanvasUndoRedo> undo = new List<TextCanvasUndoRedo>();
		List<TextCanvasUndoRedo> redo = new List<TextCanvasUndoRedo>();

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

		DispatcherTimer selectionsTimer = null;
		internal bool SelectionsInvalidated()
		{
			return selectionsTimer != null;
		}

		void InvalidateSelections()
		{
			if (SelectionsInvalidated())
				return;

			selectionsTimer = new DispatcherTimer();
			selectionsTimer.Tick += (s, e) =>
			{
				selectionsTimer.Stop();
				if (Selections.Count == 0)
				{
					Selections.Add(new Range(BeginOffset()));
					EnsureVisible();
				}
				var visible = (visibleIndex >= 0) && (visibleIndex < Selections.Count) ? Selections[visibleIndex] : null;
				Selections.DeOverlap();
				if (visible != null)
				{
					visibleIndex = Selections.FindIndex(range => (range.Start == visible.Start) && (range.End == visible.End));
					if (visibleIndex < 0)
						visibleIndex = 0;
				}

				selectionsTimer = null;
				InvalidateRender();
			};
			selectionsTimer.Start();
		}

		DispatcherTimer searchesTimer = null;
		void InvalidateSearches()
		{
			if (searchesTimer != null)
				return;

			searchesTimer = new DispatcherTimer();
			searchesTimer.Tick += (s, e) =>
			{
				searchesTimer.Stop();
				Searches.Replace(Searches.Where(range => range.HasSelection()).ToList());
				Searches.DeOverlap();

				searchesTimer = null;
				InvalidateRender();
			};
			searchesTimer.Start();
		}

		DispatcherTimer marksTimer = null;
		void InvalidateMarks()
		{
			if (marksTimer != null)
				return;

			marksTimer = new DispatcherTimer();
			marksTimer.Tick += (s, e) =>
			{
				marksTimer.Stop();
				Marks.DeOverlap();

				marksTimer = null;
				InvalidateRender();
			};
			marksTimer.Start();
		}

		DispatcherTimer renderTimer = null;
		internal void InvalidateRender()
		{
			if (renderTimer != null)
				return;

			renderTimer = new DispatcherTimer();
			renderTimer.Tick += (s, e) =>
			{
				renderTimer.Stop();
				renderTimer = null;

				canvas.InvalidateVisual();
			};
			renderTimer.Start();
		}

		void OnCanvasRender(DrawingContext dc)
		{
			if ((Data == null) || (yScrollViewportCeiling == 0) || (xScrollViewportCeiling == 0))
				return;

			var brushes = new List<Tuple<RangeList, Brush>>
			{
				new Tuple<RangeList, Brush>(Selections, Misc.selectionBrush),
				new Tuple<RangeList, Brush>(Searches, Misc.searchBrush),
				new Tuple<RangeList, Brush>(Marks, Misc.markBrush),
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
			var y = lines.ToDictionary(line => line, line => (line - startLine) * lineHeight);
			var cursorLineDone = new HashSet<int>();
			var visibleCursor = (visibleIndex >= 0) && (visibleIndex < Selections.Count) ? Selections[visibleIndex] : null;

			foreach (var entry in brushes)
			{
				foreach (var range in entry.Item1)
				{
					if ((range.End < screenStart) || (range.Start > screenEnd))
						continue;

					var entryStartLine = Data.GetOffsetLine(range.Start);
					var entryEndLine = Data.GetOffsetLine(range.End);
					var cursorLine = range.Cursor == range.Start ? entryStartLine : entryEndLine;
					entryStartLine = Math.Max(startLine, entryStartLine);
					entryEndLine = Math.Min(endLine, entryEndLine + 1);

					if ((entry.Item1 == Selections) && (!range.HasSelection()) && (cursorLine >= entryStartLine) && (cursorLine < entryEndLine))
					{
						if (range == visibleCursor)
							dc.DrawRectangle(Misc.visibleCursorBrush, null, new Rect(0, y[cursorLine], canvas.ActualWidth, lineHeight));

						if (!cursorLineDone.Contains(cursorLine))
						{
							dc.DrawRectangle(Misc.cursorBrush, Misc.cursorPen, new Rect(0, y[cursorLine], canvas.ActualWidth, lineHeight));
							cursorLineDone.Add(cursorLine);
						}

						var cursor = Data.GetOffsetIndex(range.Cursor, cursorLine);
						if ((cursor >= startIndexes[cursorLine]) && (cursor <= endIndexes[cursorLine]))
						{
							cursor = Data.GetColumnFromIndex(cursorLine, cursor);
							dc.DrawRectangle(Brushes.Black, null, new Rect((cursor - startColumn) * charWidth - 1, y[cursorLine], 2, lineHeight));
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
							dc.DrawRectangle(entry.Item2, null, new Rect(start * charWidth, y[line], width * charWidth + 1, lineHeight));
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
				var text = new FormattedText(str, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
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

		internal bool HandleKey(Key key)
		{
			shiftOverride = shiftDown;
			try
			{
				var ret = true;
				switch (key)
				{
					case Key.Back:
					case Key.Delete:
						{
							var selections = new RangeList();
							foreach (var range in Selections)
							{
								if (range.HasSelection())
								{
									selections.Add(range);
									continue;
								}

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

							Replace(selections, null, false);
						}
						break;
					case Key.Escape:
						Searches.Clear();
						break;
					case Key.Left:
						{
							for (var ctr = 0; ctr < Selections.Count; ++ctr)
							{
								var line = Data.GetOffsetLine(Selections[ctr].Cursor);
								var index = Data.GetOffsetIndex(Selections[ctr].Cursor, line);
								if (controlDown)
									Selections[ctr] = MoveCursor(Selections[ctr], GetPrevWord(Selections[ctr].Cursor));
								else if ((!shiftDown) && (Selections[ctr].HasSelection()))
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
							for (var ctr = 0; ctr < Selections.Count; ++ctr)
							{
								var line = Data.GetOffsetLine(Selections[ctr].Cursor);
								var index = Data.GetOffsetIndex(Selections[ctr].Cursor, line);
								if (controlDown)
									Selections[ctr] = MoveCursor(Selections[ctr], GetNextWord(Selections[ctr].Cursor));
								else if ((!shiftDown) && (Selections[ctr].HasSelection()))
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
							var mult = key == Key.Up ? -1 : 1;
							if (controlDown)
								yScrollValue += mult;
							else
								Selections.Replace(Selections.Select(range => MoveCursor(range, mult, 0)).ToList());
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
							Replace(sels, insert, true);
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
							ret = false;
						break;
					default: ret = false; break;
				}

				if (SelectionsInvalidated())
					EnsureVisible();

				return ret;
			}
			finally { shiftOverride = null; }
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

		Range MoveCursor(Range range, int cursor)
		{
			if (shiftDown)
				return new Range(cursor, range.Highlight);

			return new Range(cursor);
		}

		Range MoveCursor(Range range, int line, int index, bool lineRel = true, bool indexRel = true)
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
			return MoveCursor(range, Data.GetOffset(line, index));
		}

		void MouseHandler(Point mousePos, int clickCount)
		{
			var line = Math.Min(Data.NumLines - 1, (int)(mousePos.Y / lineHeight) + yScrollValue);
			var index = Math.Min(Data.GetLineLength(line), Data.GetIndexFromColumn(line, (int)(mousePos.X / charWidth) + xScrollValue, true));
			var offset = Data.GetOffset(line, index);
			var mouseRange = Selections[visibleIndex];

			if (shiftDown)
			{
				Selections.Remove(mouseRange);
				Selections.Add(MoveCursor(mouseRange, offset));
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
			MouseHandler(e.GetPosition(canvas), e.ClickCount);
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

			shiftOverride = true;
			MouseHandler(e.GetPosition(canvas), 0);
			shiftOverride = null;
			e.Handled = true;
		}

		void RunSearch(FindDialog.Result result)
		{
			if ((result == null) || (result.Regex == null))
				return;

			Searches.Clear();

			var regions = result.SelectionOnly ? Selections : new RangeList { new Range(EndOffset(), BeginOffset()) };
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

		const int maxUndoChars = 1048576 * 10;
		void AddUndoRedo(TextCanvasUndoRedo current, ReplaceType replaceType)
		{
			switch (replaceType)
			{
				case ReplaceType.Undo:
					redo.Add(current);
					--ModifiedSteps;
					break;
				case ReplaceType.Redo:
					undo.Add(current);
					++ModifiedSteps;
					break;
				case ReplaceType.Normal:
					if (ModifiedSteps < 0)
						ModifiedSteps = Int32.MinValue / 2; // Should never reach 0 again

					redo.Clear();

					// See if we can add this one to the last one
					bool done = false;
					if ((ModifiedSteps != 0) && (undo.Count != 0))
					{
						var last = undo.Last();
						if (last.ranges.Count == current.ranges.Count)
						{
							var change = 0;
							done = true;
							for (var num = 0; num < last.ranges.Count; ++num)
							{
								if (last.ranges[num].End + change != current.ranges[num].Start)
								{
									done = false;
									break;
								}
								change += current.ranges[num].Length - current.text[num].Length;
							}

							if (done)
							{
								change = 0;
								for (var num = 0; num < last.ranges.Count; ++num)
								{
									last.ranges[num] = new Range(last.ranges[num].Start + change, last.ranges[num].End + current.ranges[num].Length + change);
									last.text[num] += current.text[num];
									change += current.ranges[num].Length - current.text[num].Length;
								}
							}
						}
					}

					if (!done)
					{
						undo.Add(current);
						++ModifiedSteps;
					}

					// Limit undo buffer
					while (true)
					{
						var totalChars = undo.Sum(undoItem => undoItem.text.Sum(textItem => textItem.Length));
						if (totalChars <= maxUndoChars)
							break;
						undo.RemoveAt(0);
					}
					break;
			}
		}

		void Replace(RangeList ranges, List<string> strs, bool leaveHighlighted, ReplaceType replaceType = ReplaceType.Normal)
		{
			if (strs == null)
				strs = ranges.Select(range => "").ToList();

			var undoRanges = new RangeList();
			var undoText = new List<string>();

			var change = 0;
			for (var ctr = 0; ctr < ranges.Count; ++ctr)
			{
				var undoRange = new Range(ranges[ctr].Start + change, ranges[ctr].Start + strs[ctr].Length + change);
				undoRanges.Add(undoRange);
				undoText.Add(GetString(ranges[ctr]));
				change = undoRange.Highlight - ranges[ctr].End;
			}

			AddUndoRedo(new TextCanvasUndoRedo(undoRanges, undoText), replaceType);

			Data.Replace(ranges.Select(range => range.Start).ToList(), ranges.Select(range => range.Length).ToList(), strs);

			Searches.Clear();

			var translateNums = RangeExtensions.GetTranslateNums(Selections, Marks, Searches);
			var translateMap = RangeExtensions.GetTranslateMap(translateNums, ranges, strs);
			Selections.Translate(translateMap);
			Marks.Translate(translateMap);
			Searches.Translate(translateMap);

			if (!leaveHighlighted)
				Selections.Replace(Selections.Select(range => new Range(range.End)).ToList());

			CalculateBoundaries();
		}

		void FindNext(bool forward)
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

				if (!shiftDown)
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

		internal void HandleText(string text)
		{
			if (text.Length == 0)
				return;

			Replace(Selections, Selections.Select(range => text).ToList(), false);
			if (SelectionsInvalidated())
				EnsureVisible();
		}

		void CalculateBoundaries()
		{
			if ((canvas.ActualWidth <= 0) || (canvas.ActualHeight <= 0))
				return;

			xScrollViewport = canvas.ActualWidth / charWidth;
			xScrollMax = Data.MaxColumn - xScrollViewportFloor;
			xScrollValue = xScrollValue;

			yScrollViewport = canvas.ActualHeight / lineHeight;
			yScrollMax = Data.NumLines - yScrollViewportFloor;
			yScrollValue = yScrollValue;

			InvalidateRender();
		}
	}
}
