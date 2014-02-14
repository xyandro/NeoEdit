using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NeoEdit.BinaryEditorUI.Dialogs;
using NeoEdit.Common;
using NeoEdit.Data;
using NeoEdit.Dialogs;

namespace NeoEdit.BinaryEditorUI
{
	public partial class BinaryCanvas : Canvas
	{
		[DepProp]
		public BinaryData Data { get { return uiHelper.GetPropValue<BinaryData>(); } set { uiHelper.SetPropValue(value); } }
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
		public Coder.Type InputCoderType { get { return uiHelper.GetPropValue<Coder.Type>(); } set { uiHelper.SetPropValue(value); } }
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
		const int xPosColumns = 8;
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

		static BinaryCanvas() { UIHelper<BinaryCanvas>.Register(); }

		readonly UIHelper<BinaryCanvas> uiHelper;
		public BinaryCanvas()
		{
			uiHelper = new UIHelper<BinaryCanvas>(this);
			InitializeComponent();

			var fontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./Resources/#Anonymous Pro");
			typeface = fontFamily.GetTypefaces().First();
			fontSize = 14;
			rowHeight = fontSize;

			var example = "0123456789 abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ !@#$%^&*()";
			var formattedText = new FormattedText(example, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
			charWidth = formattedText.Width / example.Length;

			uiHelper.AddCallback(a => a.Data, (o, n) => InvalidateVisual());
			uiHelper.AddCallback(a => a.ChangeCount, (o, n) => InvalidateVisual());
			uiHelper.AddCallback(a => a.xScrollValue, (o, n) => InvalidateVisual());
			uiHelper.AddCallback(a => a.yScrollValue, (o, n) => InvalidateVisual());
			uiHelper.AddCallback(Canvas.ActualWidthProperty, this, () => InvalidateVisual());
			uiHelper.AddCallback(Canvas.ActualHeightProperty, this, () => { EnsureVisible(Pos1); InvalidateVisual(); });

			Loaded += (s, e) =>
			{
				InvalidateVisual();
				InputCoderType = Coder.Type.UTF8;
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

		protected override void OnRender(DrawingContext dc)
		{
			base.OnRender(dc);

			if (Data == null)
				return;

			columns = Math.Min(maxColumns, Math.Max(minColumns, ((int)(ActualWidth / charWidth) - xPosColumns - xPosGap - xHexGap + xHexSpacing) / (3 + xHexSpacing)));
			rows = Data.Length / columns + 1;

			xScrollMaximum = xEnd - ActualWidth;
			xScrollSmallChange = charWidth;
			xScrollLargeChange = ActualWidth - xScrollSmallChange;

			yScrollMaximum = rows * rowHeight - ActualHeight;
			yScrollSmallChange = rowHeight;
			yScrollLargeChange = ActualHeight - yScrollSmallChange;

			var startRow = Math.Max(0, GetRowFromY(yScrollValue));
			var endRow = Math.Min(rows - 1, GetRowFromY(yScrollValue + ActualHeight));

			for (var row = startRow; row <= endRow; ++row)
			{
				var y = row * rowHeight - yScrollValue;
				var selected = new bool[columns];
				var hex = new StringBuilder();
				var text = new StringBuilder();
				var useColumns = Math.Min(columns, Data.Length - row * columns);
				for (var column = 0; column < useColumns; ++column)
				{
					var pos = row * columns + column;
					if ((pos >= SelStart) && (pos < SelEnd))
						selected[column] = true;

					var b = Data[pos];
					var c = (char)b;

					hex.Append(b.ToString("x2"));
					hex.Append(' ', xHexSpacing);
					text.Append(Char.IsControl(c) ? '·' : c);
				}

				var posText = new FormattedText(String.Format("{0:x" + xPosColumns.ToString() + "}", row * columns), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
				var hexText = new FormattedText(hex.ToString(), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
				var textText = new FormattedText(text.ToString(), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);

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

					hexText.SetForegroundBrush(Brushes.White, first * (xHexSpacing + 2), count * (2 + xHexSpacing));
					dc.DrawRectangle(SelHex ? Brushes.CornflowerBlue : Brushes.Gray, null, new Rect(GetXHexFromColumn(first) - xScrollValue, y, (count * (2 + xHexSpacing) - xHexSpacing) * charWidth, rowHeight));

					textText.SetForegroundBrush(Brushes.White, first, count);
					dc.DrawRectangle(SelHex ? Brushes.Gray : Brushes.CornflowerBlue, null, new Rect(GetXTextFromColumn(first) - xScrollValue, y, count * charWidth, rowHeight));
				}

				var selRow = Pos1 / columns;
				if (selRow == row)
				{
					var selCol = (int)(Pos1 % columns);
					dc.DrawRectangle(SelHex ? Brushes.Black : Brushes.Gray, null, new Rect(GetXHexFromColumn(selCol) - xScrollValue, y, 1, rowHeight));
					dc.DrawRectangle(SelHex ? Brushes.Gray : Brushes.Black, null, new Rect(GetXTextFromColumn(selCol) - xScrollValue, y, 1, rowHeight));
				}

				dc.DrawText(posText, new Point(xPosition - xScrollValue, y));
				dc.DrawText(hexText, new Point(xHexViewStart - xScrollValue, y));
				dc.DrawText(textText, new Point(xTextViewStart - xScrollValue, y));
			}
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			e.Handled = true;
			switch (e.Key)
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

						if (e.Key == Key.Back)
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
						var mult = e.Key == Key.Up ? -1 : 1;
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
						e.Handled = false;
					break;
				default: e.Handled = false; break;
			}
		}

		void ReplaceAll(byte[] bytes)
		{
			Pos1 = 0;
			Pos2 = Data.Length;
			Replace(bytes, true);
			Pos1 = Pos2 = 0;
		}

		bool inHexEdit = false;
		void Replace(byte[] bytes, bool useAllBytes = false)
		{
			if (bytes == null)
				bytes = new byte[0];

			long len;
			if (Insert)
				len = SelEnd - SelStart;
			else
			{
				if ((useAllBytes) && (bytes.Length < Data.Length - SelStart))
					throw new InvalidOperationException("Unable to do this operation in insert mode.");
				Array.Resize(ref bytes, (int)Math.Min(bytes.Length, Data.Length - SelStart));
				len = bytes.Length;
			}

			Data.Replace(SelStart, len, bytes);

			Pos1 = Pos2 = SelStart + bytes.Length;
		}

		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			if ((String.IsNullOrEmpty(e.Text)) || (e.Text == "\u001B"))
				return;

			if (!SelHex)
			{
				Replace(Coder.StringToBytes(e.Text, InputCoderType));
				return;
			}

			var let = Char.ToUpper(e.Text[0]);
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

		static Dictionary<string, Checksum.Type> ChecksumType= new Dictionary<string,Checksum.Type>
		{
			{ BinaryEditor.Checksum_MD5, Checksum.Type.MD5 },
			{ BinaryEditor.Checksum_SHA1, Checksum.Type.SHA1 },
			{ BinaryEditor.Checksum_SHA256, Checksum.Type.SHA256 },
		};

		bool HandleChecksum(string command)
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

		static Dictionary<string, Compression.Type> CompressType = new Dictionary<string, Compression.Type>
		{
			{ BinaryEditor.Compress_GZip, Compression.Type.GZip },
			{ BinaryEditor.Decompress_GZip, Compression.Type.GZip },
			{ BinaryEditor.Compress_Deflate, Compression.Type.Deflate },
			{ BinaryEditor.Decompress_Inflate, Compression.Type.Deflate },
		};

		static Dictionary<string, bool> IsCompress = new Dictionary<string, bool>
		{
			{ BinaryEditor.Compress_GZip, true },
			{ BinaryEditor.Decompress_GZip, false },
			{ BinaryEditor.Compress_Deflate, true },
			{ BinaryEditor.Decompress_Inflate, false },
		};

		bool HandleCompress(string command)
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

		static Dictionary<string, Crypto.Type> EncryptType = new Dictionary<string, Crypto.Type>
		{
			{ BinaryEditor.Encrypt_AES, Crypto.Type.AES },
			{ BinaryEditor.Decrypt_AES, Crypto.Type.AES },
			{ BinaryEditor.Encrypt_DES, Crypto.Type.DES },
			{ BinaryEditor.Decrypt_DES, Crypto.Type.DES },
			{ BinaryEditor.Encrypt_DES3, Crypto.Type.DES3 },
			{ BinaryEditor.Decrypt_DES3, Crypto.Type.DES3 },
			{ BinaryEditor.Encrypt_RSA, Crypto.Type.RSA },
			{ BinaryEditor.Decrypt_RSA, Crypto.Type.RSA },
			{ BinaryEditor.Encrypt_RSAAES, Crypto.Type.RSAAES },
			{ BinaryEditor.Decrypt_RSAAES, Crypto.Type.RSAAES },
		};

		static Dictionary<string, bool> IsEncrypt = new Dictionary<string, bool>
		{
			{ BinaryEditor.Encrypt_AES, true },
			{ BinaryEditor.Decrypt_AES, false },
			{ BinaryEditor.Encrypt_DES, true },
			{ BinaryEditor.Decrypt_DES, false },
			{ BinaryEditor.Encrypt_DES3, true },
			{ BinaryEditor.Decrypt_DES3, false },
			{ BinaryEditor.Encrypt_RSA, true },
			{ BinaryEditor.Decrypt_RSA, false },
			{ BinaryEditor.Encrypt_RSAAES, true },
			{ BinaryEditor.Decrypt_RSAAES, false },
		};

		bool HandleEncrypt(string command)
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


		static Dictionary<string, Crypto.Type> SignType = new Dictionary<string, Crypto.Type>
		{
			{ BinaryEditor.Sign_RSA, Crypto.Type.RSA },
			{ BinaryEditor.Verify_RSA, Crypto.Type.RSA },
			{ BinaryEditor.Sign_DSA, Crypto.Type.DSA },
			{ BinaryEditor.Verify_DSA, Crypto.Type.DSA },
		};

		static Dictionary<string, bool> IsSign = new Dictionary<string, bool>
		{
			{ BinaryEditor.Sign_RSA, true },
			{ BinaryEditor.Verify_RSA, false },
			{ BinaryEditor.Sign_DSA, true },
			{ BinaryEditor.Verify_DSA, false },
		};

		bool HandleSign(string command)
		{
			if (!SignType.ContainsKey(command))
				return false;

			var type = SignType[command];
			var sign = IsSign[command];

			var keyDialog = new AsymmetricKeyDialog { Type = type, Public = !sign, GetHash = Crypto.UseSigningHash(type), CanGenerate = sign, GetSignature = !sign };
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

		public void CommandRun(UICommand command, object parameter)
		{
			try
			{
				if (HandleChecksum(command.Name))
					return;
				if (HandleCompress(command.Name))
					return;
				if (HandleEncrypt(command.Name))
					return;
				if (HandleSign(command.Name))
					return;

				switch (command.Name)
				{
					case BinaryEditor.Edit_Cut:
					case BinaryEditor.Edit_Copy:
						{
							if (SelStart == SelEnd)
								break;

							var subset = Data.GetSubset(SelStart, SelEnd - SelStart);
							Clipboard.Current.Set(subset, SelHex);
							if ((command.Name == BinaryEditor.Edit_Cut) && (Insert))
								Replace(null);
						}
						break;
					case BinaryEditor.Edit_Paste:
						{
							var bytes = Clipboard.Current.GetBytes(InputCoderType);
							if ((bytes == null) || (bytes.Length == 0))
								break;
							Replace(bytes);
						}
						break;
					case BinaryEditor.Edit_Find:
						{
							var results = FindDialog.Run();
							if (results != null)
							{
								currentFind = results;
								FoundText = currentFind.Text;
								DoFind();
							}
						}
						break;
					case BinaryEditor.Edit_FindNext:
					case BinaryEditor.Edit_FindPrev:
						DoFind(command.Name == BinaryEditor.Edit_FindNext);
						break;
					case BinaryEditor.Edit_Insert: Insert = !Insert; break;
				}
			}
			catch (Exception ex) { MessageBox.Show(ex.Message, "Error"); }
		}

		public bool CommandCanRun(UICommand command, object parameter)
		{
			return true;
		}

		FindData currentFind;
		void DoFind(bool forward = true)
		{
			if (currentFind == null)
				return;

			long start = SelStart;
			long end = SelEnd;
			if (Data.Find(currentFind, SelStart, out start, out end, forward))
			{
				EnsureVisible(start);
				Pos1 = end;
				Pos2 = start;
			}
		}
	}
}
