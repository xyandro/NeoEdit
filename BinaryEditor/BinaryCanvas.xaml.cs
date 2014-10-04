using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NeoEdit.BinaryEditor.Data;
using NeoEdit.BinaryEditor.Dialogs;
using NeoEdit.Common.Transform;
using NeoEdit.GUI;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.BinaryEditor
{
	partial class BinaryCanvas : Canvas
	{
		[DepProp]
		internal BinaryData Data { get { return uiHelper.GetPropValue<BinaryData>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long ChangeCount { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public double xScrollMaximum { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public double xScrollSmallChange { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public double xScrollLargeChange { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public double xScrollValue { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public double yScrollMaximum { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public double yScrollSmallChange { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public double yScrollLargeChange { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public double yScrollValue { get { return uiHelper.GetPropValue<double>(); } set { uiHelper.SetPropValue(value); } }
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
				InvalidateVisual();
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

				InvalidateVisual();
			}
		}
		long Length { get { return SelEnd - SelStart; } }
		bool sexHex;
		bool SelHex
		{
			get { return sexHex; }
			set
			{
				InvalidateVisual();
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

		class BinaryCanvasUndoRedo
		{
			public long index, count;
			public byte[] bytes;

			public BinaryCanvasUndoRedo(long _position, long _length, byte[] _bytes)
			{
				index = _position;
				count = _length;
				bytes = _bytes;
			}
		}

		List<BinaryCanvasUndoRedo> undo = new List<BinaryCanvasUndoRedo>();
		List<BinaryCanvasUndoRedo> redo = new List<BinaryCanvasUndoRedo>();

		static BinaryCanvas()
		{
			UIHelper<BinaryCanvas>.Register();
			UIHelper<BinaryCanvas>.AddCallback(a => a.Data, (obj, o, n) =>
			{
				obj.InvalidateVisual();
				obj.undo.Clear();
				obj.redo.Clear();
			});
			UIHelper<BinaryCanvas>.AddCallback(a => a.ChangeCount, (obj, o, n) => obj.InvalidateVisual());
			UIHelper<BinaryCanvas>.AddCallback(a => a.xScrollValue, (obj, o, n) => obj.InvalidateVisual());
			UIHelper<BinaryCanvas>.AddCallback(a => a.yScrollValue, (obj, o, n) => obj.InvalidateVisual());
		}

		readonly UIHelper<BinaryCanvas> uiHelper;
		public BinaryCanvas()
		{
			uiHelper = new UIHelper<BinaryCanvas>(this);
			InitializeComponent();

			var fontFamily = new FontFamily(new Uri("pack://application:,,,/GUI;component/"), "./Resources/#Anonymous Pro");
			typeface = fontFamily.GetTypefaces().First();
			fontSize = 14;
			rowHeight = fontSize;

			var example = "0123456789 abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ !@#$%^&*()";
			var formattedText = new FormattedText(example, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
			charWidth = formattedText.Width / example.Length;

			UIHelper<BinaryCanvas>.AddCallback(this, Canvas.ActualWidthProperty, () => InvalidateVisual());
			UIHelper<BinaryCanvas>.AddCallback(this, Canvas.ActualHeightProperty, () => { EnsureVisible(Pos1); InvalidateVisual(); });

			Loaded += (s, e) =>
			{
				InvalidateVisual();
				Pos1 = Pos2 = 0;
			};
		}

		void EnsureVisible(long position)
		{
			var y = GetYFromRow(position / columns);
			yScrollValue = Math.Min(y, Math.Max(y + rowHeight - ActualHeight, yScrollValue));
		}

		long GetRowFromY(double y)
		{
			return (long)(y / rowHeight);
		}

		double GetYFromRow(long row)
		{
			return row * rowHeight;
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

		void SetBounds()
		{
			columns = Math.Min(maxColumns, Math.Max(minColumns, ((int)(ActualWidth / charWidth) - xPosColumns - xPosGap - xHexGap + xHexSpacing) / (3 + xHexSpacing)));
			rows = Data.Length / columns + 1;

			xScrollMaximum = xEnd - ActualWidth;
			xScrollSmallChange = charWidth;
			xScrollLargeChange = ActualWidth - xScrollSmallChange;

			yScrollMaximum = rows * rowHeight - ActualHeight;
			yScrollSmallChange = rowHeight;
			yScrollLargeChange = ActualHeight - yScrollSmallChange;
		}

		static Brush selectionActiveBrush = new SolidColorBrush(Color.FromArgb(128, 58, 143, 205)); //9cc7e6
		static Brush selectionInactiveBrush = new SolidColorBrush(Color.FromArgb(128, 197, 205, 173)); //e2e6d6
		void HighlightSelection(DrawingContext dc, long row)
		{
			var y = row * rowHeight - yScrollValue;
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

				dc.DrawRectangle(SelHex ? selectionActiveBrush : selectionInactiveBrush, null, new Rect(GetXHexFromColumn(first) - xScrollValue, y, (count * (2 + xHexSpacing) - xHexSpacing) * charWidth, rowHeight));
				dc.DrawRectangle(SelHex ? selectionInactiveBrush : selectionActiveBrush, null, new Rect(GetXTextFromColumn(first) - xScrollValue, y, count * charWidth, rowHeight));
			}

			var selRow = Pos1 / columns;
			if (selRow == row)
			{
				var selCol = (int)(Pos1 % columns);
				dc.DrawRectangle(SelHex ? Brushes.Black : Brushes.Gray, null, new Rect(GetXHexFromColumn(selCol) - xScrollValue, y, 1, rowHeight));
				dc.DrawRectangle(SelHex ? Brushes.Gray : Brushes.Black, null, new Rect(GetXTextFromColumn(selCol) - xScrollValue, y, 1, rowHeight));
			}
		}

		void DrawPos(DrawingContext dc, long row)
		{
			var y = row * rowHeight - yScrollValue;
			var posText = new FormattedText(String.Format("{0:x" + xPosColumns.ToString() + "}", row * columns), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
			dc.DrawText(posText, new Point(xPosition - xScrollValue, y));
		}

		void DrawHex(DrawingContext dc, long row)
		{
			var y = row * rowHeight - yScrollValue;
			var hex = new StringBuilder();
			var useColumns = Math.Min(columns, Data.Length - row * columns);
			for (var column = 0; column < useColumns; ++column)
			{
				var b = Data[row * columns + column];

				hex.Append(b.ToString("x2"));
				hex.Append(' ', xHexSpacing);
			}

			var hexText = new FormattedText(hex.ToString(), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
			dc.DrawText(hexText, new Point(xHexViewStart - xScrollValue, y));
		}

		void DrawText(DrawingContext dc, long row)
		{
			var y = row * rowHeight - yScrollValue;
			var text = new StringBuilder();
			var useColumns = Math.Min(columns, Data.Length - row * columns);
			for (var column = 0; column < useColumns; ++column)
			{
				var c = (char)Data[row * columns + column];
				text.Append(Char.IsControl(c) ? '·' : c);
			}

			var textText = new FormattedText(text.ToString(), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
			dc.DrawText(textText, new Point(xTextViewStart - xScrollValue, y));
		}

		protected override void OnRender(DrawingContext dc)
		{
			base.OnRender(dc);

			if (Data == null)
				return;

			SetBounds();

			var startRow = Math.Max(0, GetRowFromY(yScrollValue));
			var endRow = Math.Min(rows - 1, GetRowFromY(yScrollValue + ActualHeight));

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
					if (VerifyInsert())
					{
						if (SelStart != SelEnd)
						{
							Replace(null);
							break;
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
						{
							yScrollValue += rowHeight * mult;
							var row = Pos1 / columns;
							var adj = Math.Min(0, row - GetRowFromY(yScrollValue + rowHeight - 1)) + Math.Max(0, row - GetRowFromY(yScrollValue + ActualHeight - rowHeight));
							Pos1 -= adj * columns;
						}
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
						Pos1 -= (Pos1 / columns - GetRowFromY(yScrollValue + rowHeight - 1)) * columns;
					else
						Pos1 -= (long)(ActualHeight / rowHeight - 1) * columns;
					break;
				case Key.PageDown:
					if (controlDown)
						Pos1 += (GetRowFromY(ActualHeight + yScrollValue - rowHeight) - Pos1 / columns) * columns;
					else
						Pos1 += (long)(ActualHeight / rowHeight - 1) * columns;
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
		void AddUndoRedo(BinaryCanvasUndoRedo current, ReplaceType replaceType)
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
			AddUndoRedo(new BinaryCanvasUndoRedo(index, bytes.Length, Data.GetSubset(index, count)), replaceType);
			Data.Replace(index, count, bytes);
			++ChangeCount;
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
			var x = mousePos.X + xScrollValue;
			var row = GetRowFromY(mousePos.Y + yScrollValue);
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

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			MouseHandler(e.GetPosition(this));
			mouseDown = e.ButtonState == MouseButtonState.Pressed;
			if (mouseDown)
				CaptureMouse();
			else
				ReleaseMouseCapture();
			e.Handled = true;
		}

		protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			OnMouseLeftButtonDown(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (!mouseDown)
				return;

			MouseHandler(e.GetPosition(this));
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

		static Dictionary<ICommand, Checksum.Type> ChecksumType = new Dictionary<ICommand, Checksum.Type>
		{
			{ BinaryEditorWindow.Command_Checksum_MD5, Checksum.Type.MD5 },
			{ BinaryEditorWindow.Command_Checksum_SHA1, Checksum.Type.SHA1 },
			{ BinaryEditorWindow.Command_Checksum_SHA256, Checksum.Type.SHA256 },
		};

		bool HandleChecksum(ICommand command)
		{
			if (!ChecksumType.ContainsKey(command))
				return false;

			new Message
			{
				Title = "Result",
				Text = Checksum.Get(ChecksumType[command], Data.GetAllBytes()),
				Options = Message.OptionsEnum.Ok
			}.Show();

			return true;
		}

		static Dictionary<ICommand, Compression.Type> CompressType = new Dictionary<ICommand, Compression.Type>
		{
			{ BinaryEditorWindow.Command_Compress_GZip, Compression.Type.GZip },
			{ BinaryEditorWindow.Command_Decompress_GZip, Compression.Type.GZip },
			{ BinaryEditorWindow.Command_Compress_Deflate, Compression.Type.Deflate },
			{ BinaryEditorWindow.Command_Decompress_Inflate, Compression.Type.Deflate },
		};

		static Dictionary<ICommand, bool> IsCompress = new Dictionary<ICommand, bool>
		{
			{ BinaryEditorWindow.Command_Compress_GZip, true },
			{ BinaryEditorWindow.Command_Decompress_GZip, false },
			{ BinaryEditorWindow.Command_Compress_Deflate, true },
			{ BinaryEditorWindow.Command_Decompress_Inflate, false },
		};

		bool HandleCompress(ICommand command)
		{
			if (!CompressType.ContainsKey(command))
				return false;

			if (!VerifyInsert())
				return true;

			if (IsCompress[command])
				ReplaceAll(Compression.Compress(CompressType[command], Data.GetAllBytes()));
			else
				ReplaceAll(Compression.Decompress(CompressType[command], Data.GetAllBytes()));

			return true;
		}

		static Dictionary<ICommand, Crypto.Type> EncryptType = new Dictionary<ICommand, Crypto.Type>
		{
			{ BinaryEditorWindow.Command_Encrypt_AES, Crypto.Type.AES },
			{ BinaryEditorWindow.Command_Decrypt_AES, Crypto.Type.AES },
			{ BinaryEditorWindow.Command_Encrypt_DES, Crypto.Type.DES },
			{ BinaryEditorWindow.Command_Decrypt_DES, Crypto.Type.DES },
			{ BinaryEditorWindow.Command_Encrypt_DES3, Crypto.Type.DES3 },
			{ BinaryEditorWindow.Command_Decrypt_DES3, Crypto.Type.DES3 },
			{ BinaryEditorWindow.Command_Encrypt_RSA, Crypto.Type.RSA },
			{ BinaryEditorWindow.Command_Decrypt_RSA, Crypto.Type.RSA },
			{ BinaryEditorWindow.Command_Encrypt_RSAAES, Crypto.Type.RSAAES },
			{ BinaryEditorWindow.Command_Decrypt_RSAAES, Crypto.Type.RSAAES },
		};

		static Dictionary<ICommand, bool> IsEncrypt = new Dictionary<ICommand, bool>
		{
			{ BinaryEditorWindow.Command_Encrypt_AES, true },
			{ BinaryEditorWindow.Command_Decrypt_AES, false },
			{ BinaryEditorWindow.Command_Encrypt_DES, true },
			{ BinaryEditorWindow.Command_Decrypt_DES, false },
			{ BinaryEditorWindow.Command_Encrypt_DES3, true },
			{ BinaryEditorWindow.Command_Decrypt_DES3, false },
			{ BinaryEditorWindow.Command_Encrypt_RSA, true },
			{ BinaryEditorWindow.Command_Decrypt_RSA, false },
			{ BinaryEditorWindow.Command_Encrypt_RSAAES, true },
			{ BinaryEditorWindow.Command_Decrypt_RSAAES, false },
		};

		bool HandleEncrypt(ICommand command)
		{
			if (!EncryptType.ContainsKey(command))
				return false;

			if (!VerifyInsert())
				return true;

			string key;
			var type = EncryptType[command];
			var isEncrypt = IsEncrypt[command];
			if (type.IsSymmetric())
			{
				var keyDialog = new SymmetricKeyDialog { Type = type };
				if (keyDialog.ShowDialog() != true)
					return true;
				key = keyDialog.Key;
			}
			else
			{
				var keyDialog = new AsymmetricKeyDialog { Type = type, Public = isEncrypt, CanGenerate = isEncrypt };
				if (keyDialog.ShowDialog() != true)
					return true;
				key = keyDialog.Key;
			}

			if (isEncrypt)
				ReplaceAll(Crypto.Encrypt(type, Data.GetAllBytes(), key));
			else
				ReplaceAll(Crypto.Decrypt(type, Data.GetAllBytes(), key));

			return true;
		}


		static Dictionary<ICommand, Crypto.Type> SignType = new Dictionary<ICommand, Crypto.Type>
		{
			{ BinaryEditorWindow.Command_Sign_RSA, Crypto.Type.RSA },
			{ BinaryEditorWindow.Command_Verify_RSA, Crypto.Type.RSA },
			{ BinaryEditorWindow.Command_Sign_DSA, Crypto.Type.DSA },
			{ BinaryEditorWindow.Command_Verify_DSA, Crypto.Type.DSA },
		};

		static Dictionary<ICommand, bool> IsSign = new Dictionary<ICommand, bool>
		{
			{ BinaryEditorWindow.Command_Sign_RSA, true },
			{ BinaryEditorWindow.Command_Verify_RSA, false },
			{ BinaryEditorWindow.Command_Sign_DSA, true },
			{ BinaryEditorWindow.Command_Verify_DSA, false },
		};

		bool HandleSign(ICommand command)
		{
			if (!SignType.ContainsKey(command))
				return false;

			var type = SignType[command];
			var sign = IsSign[command];

			var keyDialog = new AsymmetricKeyDialog { Type = type, Public = !sign, GetHash = true, CanGenerate = sign, GetSignature = !sign };
			if (keyDialog.ShowDialog() != true)
				return true;

			string text;
			if (IsSign[command])
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

			return true;
		}

		public void RunCommand(ICommand command)
		{
			if (HandleChecksum(command))
				return;
			if (HandleCompress(command))
				return;
			if (HandleEncrypt(command))
				return;
			if (HandleSign(command))
				return;

			if (command == BinaryEditorWindow.Command_Edit_Undo)
			{
				if (undo.Count == 0)
					return;

				var step = undo.Last();
				undo.Remove(step);
				Replace(step.index, step.count, step.bytes, ReplaceType.Undo);

				Pos1 = step.index;
				Pos2 = Pos1 + step.bytes.Length;
			}
			else if (command == BinaryEditorWindow.Command_Edit_Redo)
			{
				if (redo.Count == 0)
					return;

				var step = redo.Last();
				redo.Remove(step);
				Replace(step.index, step.count, step.bytes, ReplaceType.Redo);

				Pos1 = Pos2 = step.index + step.bytes.Length;
			}
			else if ((command == BinaryEditorWindow.Command_Edit_Cut) || (command == BinaryEditorWindow.Command_Edit_Copy))
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
				if ((command == BinaryEditorWindow.Command_Edit_Cut) && (Insert))
					Replace(null);
			}
			else if (command == BinaryEditorWindow.Command_Edit_Paste)
			{
				var bytes = ClipboardWindow.GetBytes();
				if (bytes == null)
				{
					var str = ClipboardWindow.GetString();
					if (str != null)
						bytes = Coder.StringToBytes(str, CoderUsed);
				}
				if ((bytes != null) && (bytes.Length != 0))
					Replace(bytes);
			}
			else if (command == BinaryEditorWindow.Command_Edit_Find)
			{
				var results = FindDialog.Run();
				if (results != null)
				{
					currentFind = results;
					FoundText = currentFind.Text;
					DoFind();
				}
			}
			else if ((command == BinaryEditorWindow.Command_Edit_FindNext) || (command == BinaryEditorWindow.Command_Edit_FindPrev))
				DoFind(command == BinaryEditorWindow.Command_Edit_FindNext);
			else if (command == BinaryEditorWindow.Command_Edit_Goto)
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
			else if (command == BinaryEditorWindow.Command_Edit_Insert)
			{
				if (Data.CanInsert())
					Insert = !Insert;
			}
			else if (command == BinaryEditorWindow.Command_View_Refresh)
			{
				Data.Refresh();
				++ChangeCount;
			}
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
