using System;
using System.Globalization;
using System.IO;
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

		double GetXHexFromColumn(long column)
		{
			return xHexViewStart + (column * (2 + xHexSpacing) + (inHexEdit ? 1 : 0)) * charWidth;
		}

		long GetColumnFromXHex(double x)
		{
			return (long)((x - xHexViewStart) / (2 + xHexSpacing) / charWidth);
		}

		double GetXTextFromColumn(long column)
		{
			return xTextViewStart + column * charWidth;
		}

		long GetColumnFromXText(double x)
		{
			return (long)((x - xTextViewStart) / charWidth);
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
					var selCol = Pos1 % columns;
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
					if (Insert)
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
			Replace(bytes);
		}

		bool inHexEdit = false;
		void Replace(byte[] bytes)
		{
			if (bytes == null)
				bytes = new byte[0];

			long len;
			if (Insert)
				len = SelEnd - SelStart;
			else
			{
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
			long column;
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

		public void CommandRun(UICommand command, object parameter)
		{
			try
			{
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
					case BinaryEditor.Checksum_MD5:
					case BinaryEditor.Checksum_SHA1:
					case BinaryEditor.Checksum_SHA256:
						{
							Checksum.Type checksumType = Checksum.Type.None;
							switch (command.Name)
							{
								case BinaryEditor.Checksum_MD5: checksumType = Checksum.Type.MD5; break;
								case BinaryEditor.Checksum_SHA1: checksumType = Checksum.Type.SHA1; break;
								case BinaryEditor.Checksum_SHA256: checksumType = Checksum.Type.SHA256; break;
							}
							new Message
							{
								Title = "Result",
								Text = Checksum.Get(checksumType, Data.GetAllBytes()),
								Options = Message.OptionsEnum.Ok
							}.Show();
						}
						break;
					case BinaryEditor.Compress_GZip: Data.Replace(Compression.Compress(Compression.Type.GZip, Data.GetAllBytes())); break;
					case BinaryEditor.Decompress_GZip: Data.Replace(Compression.Decompress(Compression.Type.GZip, Data.GetAllBytes())); break;
					case BinaryEditor.Compress_Deflate: Data.Replace(Compression.Compress(Compression.Type.Deflate, Data.GetAllBytes())); break;
					case BinaryEditor.Decompress_Inflate: Data.Replace(Compression.Decompress(Compression.Type.Deflate, Data.GetAllBytes())); break;
					case BinaryEditor.Encrypt_RSAAES:
						{
							var keyDialog = new AsymmetricKeyDialog { Type = Crypto.CryptoType.RSA, Public = true, CanGenerate = true };
							if (keyDialog.ShowDialog() == true)
							{
								var aesKey = Crypto.GenerateKey(Crypto.CryptoType.AES, 0);
								using (var ms = new MemoryStream())
								{
									var encryptedAesKey = Crypto.Encrypt(Crypto.CryptoType.RSA, Encoding.UTF8.GetBytes(aesKey), keyDialog.Key);
									ms.Write(BitConverter.GetBytes(encryptedAesKey.Length), 0, sizeof(int));
									ms.Write(encryptedAesKey, 0, encryptedAesKey.Length);

									var encryptedData = Crypto.Encrypt(Crypto.CryptoType.AES, Data.GetAllBytes(), aesKey);
									ms.Write(encryptedData, 0, encryptedData.Length);

									ReplaceAll(ms.ToArray());
								}
							}
						}
						break;
					case BinaryEditor.Decrypt_RSAAES:
						{
							var keyDialog = new AsymmetricKeyDialog { Type = Crypto.CryptoType.RSA, Public = false };
							if (keyDialog.ShowDialog() == true)
							{
								var encryptedAesKey = Data.GetSubset(sizeof(int), BitConverter.ToInt32(Data.GetSubset(0, sizeof(int)), 0));
								var aesKey = Encoding.UTF8.GetString(Crypto.Decrypt(Crypto.CryptoType.RSA, encryptedAesKey, keyDialog.Key));
								var skip = sizeof(int) + encryptedAesKey.Length;
								ReplaceAll(Crypto.Decrypt(Crypto.CryptoType.AES, Data.GetSubset(skip, Data.Length - skip).ToArray(), aesKey));
							}
						}
						break;
					case BinaryEditor.Encrypt_AES:
					case BinaryEditor.Encrypt_DES:
					case BinaryEditor.Encrypt_DES3:
						{
							Crypto.CryptoType type;
							switch (command.Name)
							{
								case BinaryEditor.Encrypt_AES: type = Crypto.CryptoType.AES; break;
								case BinaryEditor.Encrypt_DES: type = Crypto.CryptoType.DES; break;
								case BinaryEditor.Encrypt_DES3: type = Crypto.CryptoType.DES3; break;
								default: throw new Exception();
							}

							var keyDialog = new SymmetricKeyDialog { Type = type };
							if (keyDialog.ShowDialog() == true)
								ReplaceAll(Crypto.Encrypt(type, Data.GetAllBytes(), keyDialog.Key));
						}
						break;
					case BinaryEditor.Decrypt_AES:
					case BinaryEditor.Decrypt_DES:
					case BinaryEditor.Decrypt_DES3:
						{
							Crypto.CryptoType type;
							switch (command.Name)
							{
								case BinaryEditor.Decrypt_AES: type = Crypto.CryptoType.AES; break;
								case BinaryEditor.Decrypt_DES: type = Crypto.CryptoType.DES; break;
								case BinaryEditor.Decrypt_DES3: type = Crypto.CryptoType.DES3; break;
								default: throw new Exception();
							}

							var keyDialog = new SymmetricKeyDialog { Type = type }; ;
							if (keyDialog.ShowDialog() == true)
								ReplaceAll(Crypto.Decrypt(type, Data.GetAllBytes(), keyDialog.Key));
						}
						break;
					case BinaryEditor.Encrypt_RSA:
						{
							var type = Crypto.CryptoType.RSA;
							var keyDialog = new AsymmetricKeyDialog { Type = type, Public = true, CanGenerate = true };
							if (keyDialog.ShowDialog() == true)
								ReplaceAll(Crypto.Encrypt(type, Data.GetAllBytes(), keyDialog.Key));
						}
						break;
					case BinaryEditor.Decrypt_RSA:
						{
							var type = Crypto.CryptoType.RSA;
							var keyDialog = new AsymmetricKeyDialog { Type = type, Public = false };
							if (keyDialog.ShowDialog() == true)
								ReplaceAll(Crypto.Decrypt(type, Data.GetAllBytes(), keyDialog.Key));
						}
						break;
					case BinaryEditor.Sign_RSA:
					case BinaryEditor.Sign_DSA:
						{
							var type = command.Name == BinaryEditor.Sign_RSA ? Crypto.CryptoType.RSA : Crypto.CryptoType.DSA;
							var keyDialog = new AsymmetricKeyDialog { Type = type, Public = false, GetHash = Crypto.UseSigningHash(type), CanGenerate = true };
							if (keyDialog.ShowDialog() == true)
							{
								new Message
								{
									Title = "Signature:",
									Text = Crypto.Sign(type, Data.GetAllBytes(), keyDialog.Key, keyDialog.Hash),
									Options = Message.OptionsEnum.Ok,
								}.Show();
							}
						}
						break;
					case BinaryEditor.Verify_RSA:
					case BinaryEditor.Verify_DSA:
						{
							var type = command.Name == BinaryEditor.Verify_RSA ? Crypto.CryptoType.RSA : Crypto.CryptoType.DSA;
							var keyDialog = new AsymmetricKeyDialog { Type = type, Public = true, GetHash = Crypto.UseSigningHash(type), GetSignature = true };
							if (keyDialog.ShowDialog() == true)
							{
								var result = Crypto.Verify(type, Data.GetAllBytes(), keyDialog.Key, keyDialog.Hash, keyDialog.Signature);
								new Message
								{
									Title = "Signature:",
									Text = result ? "Matched." : "ERROR: Signature DOES NOT match.",
									Options = Message.OptionsEnum.Ok,
								}.Show();
							}
						}
						break;
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
