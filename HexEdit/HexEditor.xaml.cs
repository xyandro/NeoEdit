﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using NeoEdit.Common;
using NeoEdit.Common.Transform;
using NeoEdit.GUI;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Converters;
using NeoEdit.GUI.Dialogs;
using NeoEdit.GUI.Misc;
using NeoEdit.HexEdit.Data;
using NeoEdit.HexEdit.Dialogs;
using NeoEdit.HexEdit.Dialogs.Models;
using NeoEdit.HexEdit.Models;

namespace NeoEdit.HexEdit
{
	partial class HexEditor
	{
		[DepProp]
		public string TabLabel { get { return UIHelper<HexEditor>.GetPropValue<string>(this); } set { UIHelper<HexEditor>.SetPropValue(this, value); } }
		[DepProp]
		public string FileTitle { get { return UIHelper<HexEditor>.GetPropValue<string>(this); } set { UIHelper<HexEditor>.SetPropValue(this, value); } }
		[DepProp]
		public string FileName { get { return UIHelper<HexEditor>.GetPropValue<string>(this); } set { UIHelper<HexEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsModified { get { return UIHelper<HexEditor>.GetPropValue<bool>(this); } set { UIHelper<HexEditor>.SetPropValue(this, value); } }
		[DepProp]
		public BinaryData Data { get { return UIHelper<HexEditor>.GetPropValue<BinaryData>(this); } set { UIHelper<HexEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowValues { get { return UIHelper<HexEditor>.GetPropValue<bool>(this); } set { UIHelper<HexEditor>.SetPropValue(this, value); } }
		[DepProp]
		public long ChangeCount { get { return UIHelper<HexEditor>.GetPropValue<long>(this); } set { UIHelper<HexEditor>.SetPropValue(this, value); } }
		[DepProp]
		public long SelStart { get { return UIHelper<HexEditor>.GetPropValue<long>(this); } set { ++internalChangeCount; UIHelper<HexEditor>.SetPropValue(this, value); --internalChangeCount; } }
		[DepProp]
		public long SelEnd { get { return UIHelper<HexEditor>.GetPropValue<long>(this); } set { ++internalChangeCount; UIHelper<HexEditor>.SetPropValue(this, value); --internalChangeCount; } }
		[DepProp]
		public bool Insert { get { return UIHelper<HexEditor>.GetPropValue<bool>(this); } set { UIHelper<HexEditor>.SetPropValue(this, value); } }
		[DepProp]
		public Coder.CodePage CodePage { get { return UIHelper<HexEditor>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<HexEditor>.SetPropValue(this, value); } }
		[DepProp]
		public string FoundText { get { return UIHelper<HexEditor>.GetPropValue<string>(this); } set { UIHelper<HexEditor>.SetPropValue(this, value); } }
		[DepProp]
		public long yScrollValue { get { return UIHelper<HexEditor>.GetPropValue<long>(this); } set { UIHelper<HexEditor>.SetPropValue(this, value); } }

		long yScrollViewportFloor { get { return (long)Math.Floor(yScroll.ViewportSize); } }
		long yScrollViewportCeiling { get { return (long)Math.Ceiling(yScroll.ViewportSize); } }

		int internalChangeCount = 0;

		DateTime fileLastWrite;

		long _pos1, _pos2;

		long Pos1 { get { return _pos1; } }
		long Pos2 { get { return _pos2; } }

		void MoveCursor(long value, bool selecting, bool rel = true)
		{
			if (rel)
				value += _pos1;
			_pos1 = Math.Min(Data.Length, Math.Max(0, value));
			if (!selecting)
				_pos2 = _pos1;

			inHexEdit = false;

			SelStart = Math.Min(_pos1, _pos2);
			SelEnd = Math.Max(_pos1, _pos2);

			EnsureVisible(Pos1);
			canvas.InvalidateVisual();
		}

		long Length { get { return SelEnd - SelStart; } }
		bool sexHex;
		bool SelHex
		{
			get { return sexHex; }
			set
			{
				canvas.InvalidateVisual();
				inHexEdit = false;
				sexHex = value;
			}
		}

		const int minColumns = 4;
		const int maxColumns = Int32.MaxValue;

		int columns = minColumns;
		long rows;

		// X spacing
		const int xPosColumns = 12;
		const int xPosGap = 2;
		const int xHexSpacing = 1;
		const int xHexGap = 2;

		double xPosition { get { return 0; } }
		double xHexViewStart { get { return xPosition + (xPosColumns + xPosGap) * Font.charWidth; } }
		double xHexViewEnd { get { return xHexViewStart + (columns * (2 + xHexSpacing) - xHexSpacing) * Font.charWidth; } }
		double xTextViewStart { get { return xHexViewEnd + xHexGap * Font.charWidth; } }
		double xTextViewEnd { get { return xTextViewStart + columns * Font.charWidth; } }
		double xEnd { get { return xTextViewEnd; } }

		ModelData modelData = new ModelData();

		readonly UndoRedo undoRedo;
		static HexEditor()
		{
			UIHelper<HexEditor>.Register();
			UIHelper<HexEditor>.AddCallback(a => a.Data, (obj, o, n) =>
			{
				obj.canvas.InvalidateVisual();
				obj.undoRedo.Clear();
			});
			UIHelper<HexEditor>.AddCallback(a => a.ChangeCount, (obj, o, n) => { obj.Focus(); obj.canvas.InvalidateVisual(); });
			UIHelper<HexEditor>.AddCallback(a => a.yScrollValue, (obj, o, n) => obj.canvas.InvalidateVisual());
			UIHelper<HexEditor>.AddCallback(a => a.canvas, Canvas.ActualWidthProperty, obj => obj.canvas.InvalidateVisual());
			UIHelper<HexEditor>.AddCallback(a => a.canvas, Canvas.ActualHeightProperty, obj => { obj.EnsureVisible(obj.Pos1); obj.canvas.InvalidateVisual(); });
			UIHelper<HexEditor>.AddCoerce(a => a.yScrollValue, (obj, value) => (int)Math.Max(obj.yScroll.Minimum, Math.Min(obj.yScroll.Maximum, value)));
		}

		List<PropertyChangeNotifier> localCallbacks;
		public HexEditor(BinaryData data, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, string filename = null, string filetitle = null, bool modified = false)
		{
			InitializeComponent();

			var multiBinding = new MultiBinding { Converter = new NEExpressionConverter(), ConverterParameter = @"([0] == null?""[Untitled]"":FileName([0]))+([1]?""*"":"""")" };
			multiBinding.Bindings.Add(new Binding("FileName") { Source = this });
			multiBinding.Bindings.Add(new Binding("IsModified") { Source = this });
			SetBinding(UIHelper<HexEditor>.GetProperty(a => a.TabLabel), multiBinding);

			undoRedo = new UndoRedo(b => IsModified = b);

			localCallbacks = UIHelper<HexEditor>.GetLocalCallbacks(this);

			canvas.MouseLeftButtonDown += OnCanvasMouseLeftButtonDown;
			canvas.MouseLeftButtonUp += OnCanvasMouseLeftButtonUp;
			canvas.MouseMove += OnCanvasMouseMove;
			canvas.Render += OnCanvasRender;

			Data = data;
			CodePage = codePage;
			if (CodePage == Coder.CodePage.AutoByBOM)
				CodePage = Data.CodePageFromBOM();
			FileName = filename;
			if (File.Exists(FileName))
				fileLastWrite = new FileInfo(FileName).LastWriteTime;
			FileTitle = filetitle;
			undoRedo.SetModified(modified);

			MouseWheel += (s, e) => yScrollValue -= e.Delta / 40;
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

		internal void Close()
		{
		}

		void EnsureVisible(long position)
		{
			var row = position / columns;
			yScrollValue = Math.Min(row, Math.Max(row - yScrollViewportFloor + 1, yScrollValue));
		}

		double GetXHexFromColumn(int column)
		{
			return xHexViewStart + (column * (2 + xHexSpacing) + (inHexEdit ? 1 : 0)) * Font.charWidth;
		}

		int GetColumnFromXHex(double x)
		{
			return (int)((x - xHexViewStart) / (2 + xHexSpacing) / Font.charWidth);
		}

		double GetXTextFromColumn(int column)
		{
			return xTextViewStart + column * Font.charWidth;
		}

		int GetColumnFromXText(double x)
		{
			return (int)((x - xTextViewStart) / Font.charWidth);
		}

		void CalculateBoundaries()
		{
			columns = Math.Min(maxColumns, Math.Max(minColumns, ((int)(canvas.ActualWidth / Font.charWidth) - xPosColumns - xPosGap - xHexGap + xHexSpacing) / (3 + xHexSpacing)));
			rows = Data.Length / columns + 1;

			yScroll.ViewportSize = canvas.ActualHeight / Font.lineHeight;
			yScroll.Minimum = 0;
			yScroll.Maximum = rows - yScrollViewportFloor;
			yScroll.SmallChange = 1;
			yScroll.LargeChange = Math.Max(0, yScrollViewportFloor - 1);
		}

		void HighlightSelection(DrawingContext dc, long row)
		{
			var y = (row - yScrollValue) * Font.lineHeight;
			var selected = new bool[columns];
			var useColumns = Math.Min(columns, Data.Length - row * columns);
			for (var column = 0; column < useColumns; ++column)
			{
				var pos = row * columns + column;
				if ((pos >= SelStart) && (pos < SelEnd))
					selected[column] = true;
			}

			for (var first = 0; first < selected.Length; first++)
			{
				if (!selected[first])
					continue;

				int last;
				for (last = first; last < selected.Length; last++)
				{
					if (!selected[last])
						break;
					selected[last] = false;
				}

				var count = last - first;

				dc.DrawRectangle(SelHex ? Misc.selectionActiveBrush : Misc.selectionInactiveBrush, null, new Rect(GetXHexFromColumn(first), y, (count * (2 + xHexSpacing) - xHexSpacing) * Font.charWidth, Font.lineHeight));
				dc.DrawRectangle(SelHex ? Misc.selectionInactiveBrush : Misc.selectionActiveBrush, null, new Rect(GetXTextFromColumn(first), y, count * Font.charWidth, Font.lineHeight));
			}

			var selRow = Pos1 / columns;
			if (selRow == row)
			{
				var selCol = (int)(Pos1 % columns);
				dc.DrawRectangle(SelHex ? Brushes.Blue : Brushes.Gray, null, new Rect(GetXHexFromColumn(selCol) - 1, y, 2, Font.lineHeight));
				dc.DrawRectangle(SelHex ? Brushes.Gray : Brushes.Blue, null, new Rect(GetXTextFromColumn(selCol) - 1, y, 2, Font.lineHeight));
			}
		}

		void DrawPos(DrawingContext dc, long row)
		{
			var y = (row - yScrollValue) * Font.lineHeight;
			var posText = Font.GetText(String.Format("{0:x" + xPosColumns.ToString() + "}", row * columns));
			dc.DrawText(posText, new Point(xPosition, y));
		}

		void DrawHex(DrawingContext dc, long row)
		{
			var y = (row - yScrollValue) * Font.lineHeight;
			var hex = new StringBuilder();
			var useColumns = Math.Min(columns, Data.Length - row * columns);
			for (var column = 0; column < useColumns; ++column)
			{
				var b = Data[row * columns + column];

				hex.Append(b.ToString("x2"));
				hex.Append(' ', xHexSpacing);
			}

			var hexText = Font.GetText(hex.ToString());
			dc.DrawText(hexText, new Point(xHexViewStart, y));
		}

		void DrawText(DrawingContext dc, long row)
		{
			var y = (row - yScrollValue) * Font.lineHeight;
			var text = new StringBuilder();
			var useColumns = Math.Min(columns, Data.Length - row * columns);
			for (var column = 0; column < useColumns; ++column)
			{
				var c = (char)Data[row * columns + column];
				text.Append((Char.IsControl(c) || c == 0xad) ? '·' : c); // ad = soft hyphen, won't show
			}

			var textText = Font.GetText(text.ToString());
			dc.DrawText(textText, new Point(xTextViewStart, y));
		}

		void OnCanvasRender(object sender, DrawingContext dc)
		{
			if (Data == null)
				return;

			CalculateBoundaries();

			var startRow = yScrollValue;
			var endRow = Math.Min(rows - 1, startRow + yScrollViewportCeiling - 1);

			for (var row = startRow; row <= endRow; ++row)
			{
				HighlightSelection(dc, row);
				DrawPos(dc, row);
				DrawHex(dc, row);
				DrawText(dc, row);
			}
		}

		internal bool HandleKey(Key key, bool shiftDown, bool controlDown)
		{
			var ret = true;
			switch (key)
			{
				case Key.Escape: Focus(); break;
				case Key.Back:
				case Key.Delete:
					{
						if (!VerifyInsert())
							break;

						if (SelStart != SelEnd)
						{
							Replace(null);
							break;
						}

						if (inHexEdit)
						{
							++SelStart;
							inHexEdit = false;
						}

						if (key == Key.Back)
						{
							if (SelStart <= 0)
								break;
							MoveCursor(-1, false);
						}
						if (SelStart >= Data.Length)
							break;
						MoveCursor(1, true);
						Replace(null);
					}
					break;
				case Key.Tab:
					if (inHexEdit)
						MoveCursor(1, shiftDown);
					SelHex = !SelHex;
					break;
				case Key.Up:
				case Key.Down:
					{
						var mult = key == Key.Up ? -1 : 1;
						if (controlDown)
							yScrollValue += mult;
						else
							MoveCursor(columns * mult, shiftDown);
					}
					break;
				case Key.Left: MoveCursor(-1, shiftDown); break;
				case Key.Right: MoveCursor(1, shiftDown); break;
				case Key.Home:
					if (controlDown)
						MoveCursor(0, shiftDown, false);
					else
						MoveCursor(-(Pos1 % columns), shiftDown);
					break;
				case Key.End:
					if (controlDown)
						MoveCursor(Data.Length, shiftDown, false);
					else
						MoveCursor(columns - Pos1 % columns - 1, shiftDown);
					break;
				case Key.PageUp:
					if (controlDown)
						MoveCursor(yScrollValue * columns + Pos1 % columns, shiftDown, false);
					else
						MoveCursor(-(yScrollViewportFloor - 1) * columns, shiftDown);
					break;
				case Key.PageDown:
					if (controlDown)
						MoveCursor((yScrollValue + yScrollViewportFloor - 1) * columns + Pos1 % columns, shiftDown, false);
					else
						MoveCursor((yScrollViewportFloor - 1) * columns, shiftDown);
					break;
				case Key.A:
					if (controlDown)
						SelectAll();
					else
						ret = false;
					break;
				default: ret = false; break;
			}

			return ret;
		}

		void SelectAll()
		{
			MoveCursor(0, false, false);
			MoveCursor(Data.Length, true, false);
		}

		enum ReplaceType
		{
			Normal,
			Undo,
			Redo,
		}

		bool inHexEdit = false;
		void Replace(byte[] bytes)
		{
			if (bytes == null)
				bytes = new byte[0];

			Replace(SelStart, Insert ? Length : bytes.Length, bytes);

			MoveCursor(SelStart, false, false);
			MoveCursor(SelStart + bytes.Length, bytes.Length != 1, false);
		}

		void Replace(long index, long count, byte[] bytes, ReplaceType replaceType = ReplaceType.Normal)
		{
			if ((!Insert) && ((count != bytes.Length) || (index + count > Data.Length)))
				throw new InvalidOperationException("This operation can only be done in insert mode.");

			var undoRedoStep = new UndoRedo.UndoRedoStep(index, bytes.Length, Data.GetSubset(index, count));
			switch (replaceType)
			{
				case ReplaceType.Undo: undoRedo.AddUndone(undoRedoStep); break;
				case ReplaceType.Redo: undoRedo.AddRedone(undoRedoStep); break;
				case ReplaceType.Normal: undoRedo.AddUndo(undoRedoStep); break;
			}
			Data.Replace(index, count, bytes);
			++ChangeCount;
		}

		internal void DisplayValuesReplace(byte[] bytes)
		{
			var useLen = (int)Math.Min(bytes.Length, Data.Length - SelStart);
			if (Length > 0)
				useLen = (int)Math.Min(Length, useLen);
			Array.Resize(ref bytes, useLen);
			Replace(SelStart, useLen, bytes);
		}

		internal bool HandleText(string text)
		{
			if (String.IsNullOrEmpty(text))
				return true;

			if (!SelHex)
			{
				Replace(Coder.StringToBytes(text, CodePage));
				return true;
			}

			var let = Char.ToUpper(text[0]);
			byte val;
			if ((let >= '0') && (let <= '9'))
				val = (byte)(let - '0');
			else if ((let >= 'A') && (let <= 'F'))
				val = (byte)(let - 'A' + 10);
			else
				return true;

			var saveInHexEdit = inHexEdit;
			if (saveInHexEdit)
			{
				val = (byte)(Data[SelStart] * 16 + val);
				MoveCursor(1, true);
			}

			Replace(new byte[] { val });

			if (!saveInHexEdit)
			{
				MoveCursor(SelStart - 1, false, false);
				inHexEdit = true;
			}

			return true;
		}

		void MouseHandler(Point mousePos, bool selecting)
		{
			var x = mousePos.X;
			var row = (long)(mousePos.Y / Font.lineHeight) + yScrollValue;
			int column;
			bool isHex;

			if ((x >= xHexViewStart) && (x <= xHexViewEnd))
			{
				isHex = true;
				column = GetColumnFromXHex(x);
			}
			else if ((x >= xTextViewStart) && (x <= xTextViewEnd))
			{
				isHex = false;
				column = GetColumnFromXText(x);
			}
			else
				return;

			if ((column < 0) || (column >= columns))
				return;

			var pos = row * columns + column;
			if ((pos < 0) || (pos > Data.Length))
				return;

			SelHex = isHex;
			MoveCursor(pos, selecting, false);
		}

		void OnCanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			Focus();
			canvas.CaptureMouse();
			MouseHandler(e.GetPosition(canvas), (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None);
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

			MouseHandler(e.GetPosition(canvas), true);
			e.Handled = true;
		}

		bool VerifyInsert()
		{
			if (Insert)
				return true;

			new Message
			{
				Title = "Error",
				Text = "This operation can only be performed in insert mode.",
				Options = Message.OptionsEnum.Ok
			}.Show();

			return false;
		}

		internal void Command_File_Save()
		{
			if (FileName == null)
				Command_File_SaveAs();
			else
			{
				Data.Save(FileName);
				undoRedo.SetModified(false);
			}
		}

		internal void Command_File_SaveAs()
		{
			var dialog = new SaveFileDialog();
			if (dialog.ShowDialog() != true)
				return;

			if (Directory.Exists(dialog.FileName))
				throw new Exception("A directory by that name already exists.");
			if (!Directory.Exists(Path.GetDirectoryName(dialog.FileName)))
				throw new Exception("Directory doesn't exist.");
			FileName = dialog.FileName;
			Command_File_Save();
		}

		internal void Command_File_Revert()
		{
			if (!Data.CanReload())
				return;

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

			undoRedo.Clear();
			Data.Replace(0, Data.Length, File.ReadAllBytes(FileName));
			if (File.Exists(FileName))
				fileLastWrite = new FileInfo(FileName).LastWriteTime;

			MoveCursor(0, false, false);

			++ChangeCount;
		}

		internal void Command_File_CopyPath()
		{
			NEClipboard.SetFiles(new List<string> { FileName }, false);
		}

		internal void Command_File_CopyName()
		{
			Clipboard.SetText(Path.GetFileName(FileName));
		}

		internal void Command_File_Encoding()
		{
			var result = EncodingDialog.Run(UIHelper.FindParent<Window>(this), CodePage, Data.CodePageFromBOM());
			if (result == null)
				return;
			CodePage = result.CodePage;
		}

		internal bool Command_File_TextEditor()
		{
			var bytes = Data.GetAllBytes();
			if (!Coder.CanFullyEncode(bytes, CodePage))
			{
				if (new Message
				{
					Title = "Confirm",
					Text = String.Format("The current encoding cannot represent all bytes.  Continue anyway?"),
					Options = Message.OptionsEnum.YesNoCancel,
					DefaultAccept = Message.OptionsEnum.Yes,
					DefaultCancel = Message.OptionsEnum.Cancel,
				}.Show() != GUI.Dialogs.Message.OptionsEnum.Yes)
					return false;
			}

			Launcher.Static.LaunchTextEditor(FileName, Data.GetAllBytes(), CodePage, IsModified);
			return true;
		}

		internal void Command_Edit_Undo()
		{
			var step = undoRedo.GetUndo();
			if (step == null)
				return;

			Replace(step.index, step.count, step.bytes, ReplaceType.Undo);

			MoveCursor(step.index, false, false);
			MoveCursor(step.index + step.bytes.Length, true, false);
		}

		internal void Command_Edit_Redo()
		{
			var step = undoRedo.GetRedo();
			if (step == null)
				return;

			Replace(step.index, step.count, step.bytes, ReplaceType.Redo);

			MoveCursor(step.index, false, false);
			MoveCursor(step.index + step.bytes.Length, step.bytes.Length != 1, false);
		}

		internal void Command_Edit_CutCopy(bool isCut)
		{
			if (SelStart == SelEnd)
				return;

			var bytes = Data.GetSubset(SelStart, Length);
			string str;
			if (SelHex)
				str = Coder.BytesToString(bytes, Coder.CodePage.Hex);
			else
			{
				var sb = new char[bytes.Length];
				for (var ctr = 0; ctr < bytes.Length; ctr++)
					sb[ctr] = (char)bytes[ctr];
				str = new string(sb);
			}
			NEClipboard.Set(bytes, str);
			if ((isCut) && (Insert))
				Replace(null);
		}


		internal void Command_Edit_Paste()
		{
			var bytes = NEClipboard.Data as byte[];
			if (bytes == null)
			{
				var str = NEClipboard.Text;
				if (str != null)
					bytes = Coder.TryStringToBytes(str, CodePage);
			}
			if ((bytes != null) && (bytes.Length != 0))
				Replace(bytes);
		}

		internal void Command_Edit_Find(bool shiftDown)
		{
			var results = FindBinaryDialog.Run(UIHelper.FindParent<Window>(this));
			if (results != null)
			{
				currentFind = results;
				FoundText = currentFind.Text;
				DoFind(shiftDown);
			}
		}

		internal void Command_Edit_FindNextPrev(bool forward, bool shiftDown)
		{
			DoFind(shiftDown, forward);
		}

		internal void Command_Edit_Goto(bool shiftDown)
		{
			var position = GotoDialog.Run(UIHelper.FindParent<Window>(this), Pos1);
			if (position == null)
				return;
			if (position.Relative)
				position.Value += Pos1;
			MoveCursor(position.Value, shiftDown, false);
		}

		internal void Command_Edit_Insert()
		{
			if (Data.CanInsert())
				Insert = !Insert;
		}

		internal void Command_View_Values()
		{
			ShowValues = !ShowValues;
		}

		internal void Command_View_Refresh()
		{
			Data.Refresh();
			++ChangeCount;

			if ((!Data.CanReload()) || (String.IsNullOrEmpty(FileName)))
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

		internal void Command_Data_Hash(Hasher.Type type)
		{
			if (Length == 0)
				SelectAll();

			var data = Data.GetSubset(SelStart, Length);
			new Message
			{
				Title = "Result",
				Text = Hasher.Get(data, type),
				Options = Message.OptionsEnum.Ok
			}.Show();
		}

		internal void Command_Data_Compress(bool compress, Compressor.Type type)
		{
			if (!VerifyInsert())
				return;

			if (Length == 0)
				SelectAll();

			if (compress)
				Replace(Compressor.Compress(Data.GetSubset(SelStart, SelEnd - SelStart), type));
			else
				Replace(Compressor.Decompress(Data.GetSubset(SelStart, SelEnd - SelStart), type));
		}

		internal void Command_Data_Encrypt(bool isEncrypt, Cryptor.Type type)
		{
			if (!VerifyInsert())
				return;

			string key;
			if (type.IsSymmetric())
			{
				var result = SymmetricKeyDialog.Run(UIHelper.FindParent<Window>(this), type);
				if (result == null)
					return;
				key = result.Key;
			}
			else
			{
				var result = AsymmetricKeyDialog.Run(UIHelper.FindParent<Window>(this), type, isEncrypt, isEncrypt);
				if (result == null)
					return;
				key = result.Key;
			}

			if (Length == 0)
				SelectAll();

			if (isEncrypt)
				Replace(Cryptor.Encrypt(Data.GetSubset(SelStart, SelEnd - SelStart), type, key));
			else
				Replace(Cryptor.Decrypt(Data.GetSubset(SelStart, SelEnd - SelStart), type, key));
		}

		internal void Command_Data_Sign(bool sign, Cryptor.Type type)
		{
			var keyResult = AsymmetricKeyDialog.Run(UIHelper.FindParent<Window>(this), type, !sign, sign, true, !sign);
			if (keyResult == null)
				return;

			if (Length == 0)
				SelectAll();

			string text;
			if (sign)
				text = Cryptor.Sign(Data.GetSubset(SelStart, SelEnd - SelStart), type, keyResult.Key, keyResult.Hash);
			else if (Cryptor.Verify(Data.GetSubset(SelStart, SelEnd - SelStart), type, keyResult.Key, keyResult.Hash, keyResult.Signature))
				text = "Matched.";
			else
				text = "ERROR: Signature DOES NOT match.";

			new Message
			{
				Title = "Signature:",
				Text = text,
				Options = Message.OptionsEnum.Ok,
			}.Show();
		}

		internal void Command_Data_Fill()
		{
			var fill = FillDialog.Run(UIHelper.FindParent<Window>(this));
			if (fill == null)
				return;
			var data = Enumerable.Range(0, (int)(SelEnd - SelStart)).Select(a => fill.Value).ToArray();
			Replace(SelStart, SelEnd - SelStart, data);
		}

		internal void Command_Data_Models_Define()
		{
			if (new ModelDataVM(modelData).EditDialog())
				Command_Data_Models_Save();
		}

		internal void Command_Data_Models_Save()
		{
			var dialog = new SaveFileDialog
			{
				DefaultExt = "xml",
				Filter = "Model files|*.xml|All files|*.*",
				FileName = modelData.FileName,
			};
			if (dialog.ShowDialog() != true)
				return;

			modelData.Save(dialog.FileName);
		}

		internal void Command_Data_Models_Load()
		{
			var dialog = new OpenFileDialog
			{
				DefaultExt = "xml",
				Filter = "Model files|*.xml|All files|*.*",
				FileName = modelData.FileName,
			};
			if (dialog.ShowDialog() != true)
				return;

			modelData = ModelData.Load(dialog.FileName);
		}

		internal void Command_Data_Models_ExtractData()
		{
			var start = SelStart;
			var end = SelEnd;
			if (start == end)
			{
				start = 0;
				end = Data.Length;
			}
			var results = ModelDataExtractor.ExtractData(Data, start, Pos1, end, modelData, modelData.Default);
			ModelResultsDialog.Run(results,
				changeSelection: (selStart, selEnd) => { MoveCursor(selStart, false, false); MoveCursor(selEnd, true, false); },
				getByte: Pos => Data[Pos],
				changeBytes: (startPos, endPos, data) => Replace(startPos, endPos - startPos, data)
			);
		}

		FindBinaryDialog.Result currentFind;
		void DoFind(bool shiftDown, bool forward = true)
		{
			if (currentFind == null)
				return;

			long index = SelEnd, start, end;

			while (true)
			{
				if (Data.Find(currentFind, index, out start, out end, forward))
				{
					EnsureVisible(start);
					if (!shiftDown)
						MoveCursor(start, false, false);
					MoveCursor(end, true, false);
					return;
				}

				if (((forward) && (index <= 0)) || ((!forward) && (index >= Data.Length)))
				{
					new Message
					{
						Title = "Info",
						Text = "Not found.",
						Options = Message.OptionsEnum.Ok,
					}.Show();
					return;
				}

				if (forward)
				{
					if (new Message
					{
						Title = "Info",
						Text = "Not found.  Search from beginning?",
						Options = Message.OptionsEnum.YesNo,
						DefaultAccept = Message.OptionsEnum.Yes,
						DefaultCancel = Message.OptionsEnum.No,
					}.Show() == Message.OptionsEnum.Yes)
					{
						index = -1;
						continue;
					}
				}
				else
				{
					if (new Message
					{
						Title = "Info",
						Text = "Not found.  Search from end?",
						Options = Message.OptionsEnum.YesNo,
						DefaultAccept = Message.OptionsEnum.Yes,
						DefaultCancel = Message.OptionsEnum.No,
					}.Show() == Message.OptionsEnum.Yes)
					{
						index = Data.Length;
						continue;
					}
				}

				return;
			}
		}

		public override string ToString()
		{
			return FileName;
		}
	}
}
