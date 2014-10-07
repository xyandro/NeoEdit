using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using NeoEdit.BinaryEditor.Data;
using NeoEdit.BinaryEditor.Dialogs;
using NeoEdit.Common.Transform;
using NeoEdit.GUI;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.BinaryEditor
{
	partial class BinaryEditor
	{
		[DepProp]
		public string FileTitle { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public string FileName { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public BinaryData Data { get { return uiHelper.GetPropValue<BinaryData>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool ShowValues { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long ChangeCount { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long SelStart { get { return uiHelper.GetPropValue<long>(); } set { ++internalChangeCount; uiHelper.SetPropValue(value); --internalChangeCount; } }
		[DepProp]
		public long SelEnd { get { return uiHelper.GetPropValue<long>(); } set { ++internalChangeCount; uiHelper.SetPropValue(value); --internalChangeCount; } }
		[DepProp]
		public bool Insert { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public Coder.Type CoderUsed { get { return uiHelper.GetPropValue<Coder.Type>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public string FoundText { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long yScrollValue { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }

		long yScrollViewportFloor { get { return (long)Math.Floor(yScroll.ViewportSize); } }
		long yScrollViewportCeiling { get { return (long)Math.Ceiling(yScroll.ViewportSize); } }

		int internalChangeCount = 0;

		long _pos1, _pos2;

		long Pos1
		{
			get { return _pos1; }
			set
			{
				_pos1 = Math.Min(Data.Length, Math.Max(0, value));
				if (!selecting)
					_pos2 = _pos1;

				inHexEdit = false;

				SelStart = Math.Min(_pos1, _pos2);
				SelEnd = Math.Max(_pos1, _pos2);

				EnsureVisible(Pos1);
				canvas.InvalidateVisual();
			}
		}

		long Pos2
		{
			get { return _pos2; }
			set
			{
				_pos2 = Math.Min(Data.Length, Math.Max(0, value));

				SelStart = Math.Min(_pos1, _pos2);
				SelEnd = Math.Max(_pos1, _pos2);

				canvas.InvalidateVisual();
			}
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

		readonly double charWidth;
		const int minColumns = 4;
		const int maxColumns = Int32.MaxValue;

		bool mouseDown;
		bool shiftDown { get { return (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None; } }
		bool controlDown { get { return (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None; } }
		bool altDown { get { return (Keyboard.Modifiers & ModifierKeys.Alt) != ModifierKeys.None; } }
		bool controlOnly { get { return (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)) == ModifierKeys.Control; } }
		bool selecting { get { return (mouseDown) || (shiftDown); } }

		int columns;
		long rows;

		// X spacing
		const int xPosColumns = 12;
		const int xPosGap = 2;
		const int xHexSpacing = 1;
		const int xHexGap = 2;

		double xPosition { get { return 0; } }
		double xHexViewStart { get { return xPosition + (xPosColumns + xPosGap) * charWidth; } }
		double xHexViewEnd { get { return xHexViewStart + (columns * (2 + xHexSpacing) - xHexSpacing) * charWidth; } }
		double xTextViewStart { get { return xHexViewEnd + xHexGap * charWidth; } }
		double xTextViewEnd { get { return xTextViewStart + columns * charWidth; } }
		double xEnd { get { return xTextViewEnd; } }

		// Y spacing
		readonly double rowHeight;

		readonly Typeface typeface;
		readonly double fontSize;

		class BinaryEditorUndoRedo
		{
			public long index, count;
			public byte[] bytes;

			public BinaryEditorUndoRedo(long _position, long _length, byte[] _bytes)
			{
				index = _position;
				count = _length;
				bytes = _bytes;
			}
		}

		List<BinaryEditorUndoRedo> undo = new List<BinaryEditorUndoRedo>();
		List<BinaryEditorUndoRedo> redo = new List<BinaryEditorUndoRedo>();

		static BinaryEditor()
		{
			UIHelper<BinaryEditor>.Register();
			UIHelper<BinaryEditor>.AddCallback(a => a.Data, (obj, o, n) =>
			{
				obj.canvas.InvalidateVisual();
				obj.undo.Clear();
				obj.redo.Clear();
			});
			UIHelper<BinaryEditor>.AddCallback(a => a.ChangeCount, (obj, o, n) => { obj.Focus(); obj.canvas.InvalidateVisual(); });
			UIHelper<BinaryEditor>.AddCallback(a => a.yScrollValue, (obj, o, n) => obj.canvas.InvalidateVisual());
			UIHelper<BinaryEditor>.AddCoerce(a => a.yScrollValue, (obj, value) => (int)Math.Max(obj.yScroll.Minimum, Math.Min(obj.yScroll.Maximum, value)));
		}

		readonly UIHelper<BinaryEditor> uiHelper;
		public BinaryEditor()
		{
			uiHelper = new UIHelper<BinaryEditor>(this);
			InitializeComponent();

			var fontFamily = new FontFamily(new Uri("pack://application:,,,/GUI;component/"), "./Resources/#Anonymous Pro");
			typeface = fontFamily.GetTypefaces().First();
			fontSize = 14;
			rowHeight = fontSize;

			var example = "0123456789 abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ !@#$%^&*()";
			var formattedText = new FormattedText(example, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
			charWidth = formattedText.Width / example.Length;

			UIHelper<BinaryEditor>.AddCallback(canvas, Canvas.ActualWidthProperty, () => canvas.InvalidateVisual());
			UIHelper<BinaryEditor>.AddCallback(canvas, Canvas.ActualHeightProperty, () => { EnsureVisible(Pos1); canvas.InvalidateVisual(); });

			canvas.MouseLeftButtonDown += OnCanvasMouseLeftButtonDown;
			canvas.MouseLeftButtonUp += OnCanvasMouseLeftButtonDown;
			canvas.MouseMove += OnCanvasMouseMove;
			canvas.Render += OnCanvasRender;

			Loaded += (s, e) =>
			{
				canvas.InvalidateVisual();
				Pos1 = Pos2 = 0;
			};
		}

		internal void SetData(BinaryData data, Coder.Type encoder = Coder.Type.None, string filename = null, string filetitle = null)
		{
			Data = data;
			CoderUsed = encoder;
			if (CoderUsed == Coder.Type.None)
				CoderUsed = Data.GuessEncoding();
			FileName = filename;
			FileTitle = filetitle;
		}

		void EnsureVisible(long position)
		{
			var row = position / columns;
			yScrollValue = Math.Min(row, Math.Max(row - yScrollViewportFloor + 1, yScrollValue));
		}

		double GetXHexFromColumn(int column)
		{
			return xHexViewStart + (column * (2 + xHexSpacing) + (inHexEdit ? 1 : 0)) * charWidth;
		}

		int GetColumnFromXHex(double x)
		{
			return (int)((x - xHexViewStart) / (2 + xHexSpacing) / charWidth);
		}

		double GetXTextFromColumn(int column)
		{
			return xTextViewStart + column * charWidth;
		}

		int GetColumnFromXText(double x)
		{
			return (int)((x - xTextViewStart) / charWidth);
		}

		void CalculateBoundaries()
		{
			columns = Math.Min(maxColumns, Math.Max(minColumns, ((int)(canvas.ActualWidth / charWidth) - xPosColumns - xPosGap - xHexGap + xHexSpacing) / (3 + xHexSpacing)));
			rows = Data.Length / columns + 1;

			yScroll.ViewportSize = canvas.ActualHeight / rowHeight;
			yScroll.Minimum = 0;
			yScroll.Maximum = rows - yScrollViewportFloor;
			yScroll.SmallChange = 1;
			yScroll.LargeChange = Math.Max(0, yScrollViewportFloor - 1);
		}

		void HighlightSelection(DrawingContext dc, long row)
		{
			var y = (row - yScrollValue) * rowHeight;
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

				dc.DrawRectangle(SelHex ? Misc.selectionActiveBrush : Misc.selectionInactiveBrush, null, new Rect(GetXHexFromColumn(first), y, (count * (2 + xHexSpacing) - xHexSpacing) * charWidth, rowHeight));
				dc.DrawRectangle(SelHex ? Misc.selectionInactiveBrush : Misc.selectionActiveBrush, null, new Rect(GetXTextFromColumn(first), y, count * charWidth, rowHeight));
			}

			var selRow = Pos1 / columns;
			if (selRow == row)
			{
				var selCol = (int)(Pos1 % columns);
				dc.DrawRectangle(SelHex ? Brushes.Black : Brushes.Gray, null, new Rect(GetXHexFromColumn(selCol), y, 1, rowHeight));
				dc.DrawRectangle(SelHex ? Brushes.Gray : Brushes.Black, null, new Rect(GetXTextFromColumn(selCol), y, 1, rowHeight));
			}
		}

		void DrawPos(DrawingContext dc, long row)
		{
			var y = (row - yScrollValue) * rowHeight;
			var posText = new FormattedText(String.Format("{0:x" + xPosColumns.ToString() + "}", row * columns), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
			dc.DrawText(posText, new Point(xPosition, y));
		}

		void DrawHex(DrawingContext dc, long row)
		{
			var y = (row - yScrollValue) * rowHeight;
			var hex = new StringBuilder();
			var useColumns = Math.Min(columns, Data.Length - row * columns);
			for (var column = 0; column < useColumns; ++column)
			{
				var b = Data[row * columns + column];

				hex.Append(b.ToString("x2"));
				hex.Append(' ', xHexSpacing);
			}

			var hexText = new FormattedText(hex.ToString(), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
			dc.DrawText(hexText, new Point(xHexViewStart, y));
		}

		void DrawText(DrawingContext dc, long row)
		{
			var y = (row - yScrollValue) * rowHeight;
			var text = new StringBuilder();
			var useColumns = Math.Min(columns, Data.Length - row * columns);
			for (var column = 0; column < useColumns; ++column)
			{
				var c = (char)Data[row * columns + column];
				text.Append(Char.IsControl(c) ? '·' : c);
			}

			var textText = new FormattedText(text.ToString(), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
			dc.DrawText(textText, new Point(xTextViewStart, y));
		}

		internal void HandleMouseWheel(int delta)
		{
			yScrollValue -= delta / 40;
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

		public bool HandleKey(Key key)
		{
			var ret = true;
			switch (key)
			{
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
							Pos1 = SelStart - 1;
						}
						if (SelStart >= Data.Length)
							break;
						Pos2 = Pos1 + 1;
						Replace(null);
					}
					break;
				case Key.Tab:
					if (inHexEdit)
						Pos1++;
					SelHex = !SelHex;
					break;
				case Key.Up:
				case Key.Down:
					{
						var mult = key == Key.Up ? -1 : 1;
						if (controlDown)
							yScrollValue += mult;
						else
							Pos1 += columns * mult;
					}
					break;
				case Key.Left: --Pos1; break;
				case Key.Right: ++Pos1; break;
				case Key.Home:
					if (controlDown)
						Pos1 = 0;
					else
						Pos1 -= Pos1 % columns;
					break;
				case Key.End:
					if (controlDown)
						Pos1 = Data.Length;
					else
						Pos1 += columns - Pos1 % columns - 1;
					break;
				case Key.PageUp:
					if (controlDown)
						Pos1 = yScrollValue * columns + Pos1 % columns;
					else
						Pos1 -= (yScrollViewportFloor - 1) * columns;
					break;
				case Key.PageDown:
					if (controlDown)
						Pos1 = (yScrollValue + yScrollViewportFloor - 1) * columns + Pos1 % columns;
					else
						Pos1 += (yScrollViewportFloor - 1) * columns;
					break;
				case Key.A:
					if (controlOnly)
					{
						Pos1 = Data.Length;
						Pos2 = 0;
					}
					else
						ret = false;
					break;
				default: ret = false; break;
			}
			return ret;
		}

		void ReplaceAll(byte[] bytes)
		{
			Pos1 = 0;
			Pos2 = Data.Length;
			Replace(bytes, true);
			Pos1 = Pos2 = 0;
		}

		enum ReplaceType
		{
			Normal,
			Undo,
			Redo,
		}

		const int maxUndoBytes = 1048576 * 10;
		void AddUndoRedo(BinaryEditorUndoRedo current, ReplaceType replaceType)
		{
			switch (replaceType)
			{
				case ReplaceType.Undo:
					redo.Add(current);
					break;
				case ReplaceType.Redo:
					undo.Add(current);
					break;
				case ReplaceType.Normal:
					redo.Clear();

					// See if we can add this one to the last one
					var done = false;
					if (undo.Count != 0)
					{
						var last = undo.Last();
						if (last.index + last.count == current.index)
						{
							last.count += current.count;
							var oldSize = last.bytes.LongLength;
							Array.Resize(ref last.bytes, (int)(last.bytes.LongLength + current.bytes.LongLength));
							Array.Copy(current.bytes, 0, last.bytes, oldSize, current.bytes.LongLength);
							done = true;
						}
					}

					if (!done)
						undo.Add(current);

					while (true)
					{
						var totalChars = undo.Sum(undoItem => undoItem.bytes.LongLength);
						if (totalChars <= maxUndoBytes)
							break;
						undo.RemoveAt(0);
					}
					break;
			}
		}

		bool inHexEdit = false;
		void Replace(byte[] bytes, bool useAllBytes = false)
		{
			if (bytes == null)
				bytes = new byte[0];

			long count;
			if (Insert)
				count = Length;
			else
			{
				if ((useAllBytes) && (bytes.Length < Data.Length - SelStart))
					throw new InvalidOperationException("This operation can only be done in insert mode.");
				Array.Resize(ref bytes, (int)Math.Min(bytes.Length, Data.Length - SelStart));
				count = bytes.Length;
			}

			Replace(SelStart, count, bytes);

			Pos1 = Pos2 = SelStart + bytes.Length;
		}

		void Replace(long index, long count, byte[] bytes, ReplaceType replaceType = ReplaceType.Normal)
		{
			AddUndoRedo(new BinaryEditorUndoRedo(index, bytes.Length, Data.GetSubset(index, count)), replaceType);
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

		public void HandleText(string str)
		{
			if ((String.IsNullOrEmpty(str)) || (str == "\u001B"))
				return;

			if (!SelHex)
			{
				Replace(Coder.StringToBytes(str, CoderUsed));
				return;
			}

			var let = Char.ToUpper(str[0]);
			byte val;
			if ((let >= '0') && (let <= '9'))
				val = (byte)(let - '0');
			else if ((let >= 'A') && (let <= 'F'))
				val = (byte)(let - 'A' + 10);
			else
				return;

			var saveInHexEdit = inHexEdit;
			if (saveInHexEdit)
			{
				val = (byte)(Data[SelStart] * 16 + val);
				++Pos2;
			}

			Replace(new byte[] { val });

			if (!saveInHexEdit)
			{
				Pos1 = Pos2 = SelStart - 1;
				inHexEdit = true;
			}
		}

		void MouseHandler(Point mousePos)
		{
			var x = mousePos.X;
			var row = (long)(mousePos.Y / rowHeight) + yScrollValue;
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
			Pos1 = pos;
		}

		void OnCanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			Focus();
			MouseHandler(e.GetPosition(canvas));
			mouseDown = e.ButtonState == MouseButtonState.Pressed;
			if (mouseDown)
				canvas.CaptureMouse();
			else
				canvas.ReleaseMouseCapture();
			e.Handled = true;
		}

		void OnCanvasMouseMove(object sender, MouseEventArgs e)
		{
			if (!mouseDown)
				return;

			MouseHandler(e.GetPosition(canvas));
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
				Data.Save(FileName);
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

		internal void Command_File_CopyPath()
		{
			ClipboardWindow.SetFiles(new List<string> { FileName }, false);
		}

		internal void Command_File_CopyName()
		{
			Clipboard.SetText(Path.GetFileName(FileName));
		}

		internal void Command_File_Encode(Coder.Type type)
		{
			CoderUsed = type;
			if (CoderUsed == Coder.Type.None)
				CoderUsed = Data.GuessEncoding();
		}

		internal void Command_Edit_Undo()
		{
			if (undo.Count == 0)
				return;

			var step = undo.Last();
			undo.Remove(step);
			Replace(step.index, step.count, step.bytes, ReplaceType.Undo);

			Pos1 = step.index;
			Pos2 = Pos1 + step.bytes.Length;
		}

		internal void Command_Edit_Redo()
		{
			if (redo.Count == 0)
				return;

			var step = redo.Last();
			redo.Remove(step);
			Replace(step.index, step.count, step.bytes, ReplaceType.Redo);

			Pos1 = Pos2 = step.index + step.bytes.Length;
		}

		internal void Command_Edit_Copy(BinaryEditCommand command)
		{
			if (SelStart == SelEnd)
				return;

			var bytes = Data.GetSubset(SelStart, Length);
			string str;
			if (SelHex)
				str = Coder.BytesToString(bytes, Coder.Type.Hex);
			else
			{
				var sb = new StringBuilder(bytes.Length);
				for (var ctr = 0; ctr < bytes.Length; ctr++)
					sb.Append((char)bytes[ctr]);
				str = sb.ToString();
			}
			ClipboardWindow.Set(bytes, str);
			if ((command == BinaryEditCommand.Edit_Cut) && (Insert))
				Replace(null);
		}


		internal void Command_Edit_Paste()
		{
			var bytes = ClipboardWindow.GetBytes();
			if (bytes == null)
			{
				var str = ClipboardWindow.GetString();
				if (str != null)
					bytes = Coder.TryStringToBytes(str, CoderUsed);
			}
			if ((bytes != null) && (bytes.Length != 0))
				Replace(bytes);
		}

		internal void Command_Edit_Find()
		{
			var results = FindDialog.Run();
			if (results != null)
			{
				currentFind = results;
				FoundText = currentFind.Text;
				DoFind();
			}
		}

		internal void Command_Edit_FindPrev(BinaryEditCommand command)
		{
			DoFind(command == BinaryEditCommand.Edit_FindNext);
		}

		internal void Command_Edit_Goto()
		{
			var getNumDialog = new GetNumDialog
			{
				Title = "Go to position",
				Text = String.Format("Go to position: (0 - {0})", Data.Length),
				MinValue = 0,
				MaxValue = Data.Length,
				Value = Pos1,
			};
			if (getNumDialog.ShowDialog() == true)
				Pos1 = Pos2 = getNumDialog.Value;
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
		}

		internal void Command_Checksum(Checksum.Type type)
		{
			new Message
			{
				Title = "Result",
				Text = Checksum.Get(type, Data.GetAllBytes()),
				Options = Message.OptionsEnum.Ok
			}.Show();
		}

		internal void Command_Compress(bool compress, Compression.Type type)
		{
			if (!VerifyInsert())
				return;

			if (compress)
				ReplaceAll(Compression.Compress(type, Data.GetAllBytes()));
			else
				ReplaceAll(Compression.Decompress(type, Data.GetAllBytes()));
		}

		internal void Command_Encrypt(bool isEncrypt, Crypto.Type type)
		{
			if (!VerifyInsert())
				return;

			string key;
			if (type.IsSymmetric())
			{
				var keyDialog = new SymmetricKeyDialog { Type = type };
				if (keyDialog.ShowDialog() != true)
					return;
				key = keyDialog.Key;
			}
			else
			{
				var keyDialog = new AsymmetricKeyDialog { Type = type, Public = isEncrypt, CanGenerate = isEncrypt };
				if (keyDialog.ShowDialog() != true)
					return;
				key = keyDialog.Key;
			}

			if (isEncrypt)
				ReplaceAll(Crypto.Encrypt(type, Data.GetAllBytes(), key));
			else
				ReplaceAll(Crypto.Decrypt(type, Data.GetAllBytes(), key));
		}

		internal void Command_Sign(bool sign, Crypto.Type type)
		{
			var keyDialog = new AsymmetricKeyDialog { Type = type, Public = !sign, GetHash = true, CanGenerate = sign, GetSignature = !sign };
			if (keyDialog.ShowDialog() != true)
				return;

			string text;
			if (sign)
				text = Crypto.Sign(type, Data.GetAllBytes(), keyDialog.Key, keyDialog.Hash);
			else if (Crypto.Verify(type, Data.GetAllBytes(), keyDialog.Key, keyDialog.Hash, keyDialog.Signature))
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

		FindData currentFind;
		void DoFind(bool forward = true)
		{
			if (currentFind == null)
				return;

			long index = SelStart, start, end;

			while (true)
			{
				if (Data.Find(currentFind, index, out start, out end, forward))
				{
					EnsureVisible(start);
					Pos1 = end;
					Pos2 = start;
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
	}
}
