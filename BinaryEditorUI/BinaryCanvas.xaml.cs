using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
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
				_pos1 = Math.Min(Data.Length - 1, Math.Max(0, value));
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
				_pos2 = Math.Min(Data.Length - 1, Math.Max(0, value));

				SelStart = Math.Min(_pos1, _pos2);
				SelEnd = Math.Max(_pos1, _pos2);

				InvalidateVisual();
			}
		}

		[DepProp]
		public bool SelHex { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }

		readonly double charWidth;
		const int minColumns = 4;
		const int maxColumns = Int32.MaxValue;

		bool mouseDown;
		bool? overrideSelecting;
		bool shiftDown { get { return (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None; } }
		bool controlDown { get { return (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None; } }
		bool altDown { get { return (Keyboard.Modifiers & ModifierKeys.Alt) != ModifierKeys.None; } }
		bool controlOnly { get { return (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)) == ModifierKeys.Control; } }
		bool selecting { get { return overrideSelecting.HasValue ? overrideSelecting.Value : ((mouseDown) || (shiftDown)); } }

		int columns;
		long rows;

		// X spacing
		const double xStartSpacing = 10;
		const int xPosColumns = 8;
		const int xPosGap = 2;
		const int xHexSpacing = 1;
		const int xHexGap = 2;
		const double xEndSpacing = xStartSpacing;

		double xStart { get { return 0; } }
		double xPosition { get { return xStart + xStartSpacing; } }
		double xHexViewStart { get { return xPosition + (xPosColumns + xPosGap) * charWidth; } }
		double xHexViewEnd { get { return xHexViewStart + (columns * (2 + xHexSpacing) - xHexSpacing) * charWidth; } }
		double xTextViewStart { get { return xHexViewEnd + xHexGap * charWidth; } }
		double xTextViewEnd { get { return xTextViewStart + columns * charWidth; } }
		double xEnd { get { return xTextViewEnd + xEndSpacing; } }

		// Y spacing
		const double yStartSpacing = 10;
		readonly double rowHeight;
		const double yEndSpacing = yStartSpacing;

		double yStart { get { return 0; } }
		double yLinesStart { get { return yStart + yStartSpacing; } }
		double yLinesEnd { get { return yLinesStart + rows * rowHeight; } }
		double yEnd { get { return yLinesEnd + yEndSpacing; } }

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
			uiHelper.AddCallback(a => a.SelHex, (o, n) => InvalidateVisual());
			uiHelper.AddCallback(a => a.xScrollValue, (o, n) => InvalidateVisual());
			uiHelper.AddCallback(a => a.yScrollValue, (o, n) => InvalidateVisual());
			uiHelper.AddCallback(Canvas.ActualWidthProperty, this, () => InvalidateVisual());
			uiHelper.AddCallback(Canvas.ActualHeightProperty, this, () => { EnsureVisible(Pos1); InvalidateVisual(); });

			Loaded += (s, e) =>
			{
				InvalidateVisual();
				InputCoderType = Coder.Type.UTF8;
				SelStart = SelEnd = 0;
			};
		}

		void EnsureVisible(long position)
		{
			var y = GetYFromRow(position / columns);
			yScrollValue = Math.Min(y, Math.Max(y + rowHeight - ActualHeight, yScrollValue));
		}

		long GetRowFromY(double y)
		{
			return (long)((y - yLinesStart) / rowHeight);
		}

		double GetYFromRow(long row)
		{
			return yLinesStart + row * rowHeight;
		}

		double GetXHexFromColumn(long column)
		{
			return xHexViewStart + column * (2 + xHexSpacing) * charWidth;
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

			columns = Math.Min(maxColumns, Math.Max(minColumns, ((int)((ActualWidth - xStartSpacing - xEndSpacing) / charWidth) - xPosColumns - xPosGap - xHexGap + xHexSpacing) / (3 + xHexSpacing)));
			rows = (Data.Length + columns - 1) / columns;

			xScrollMaximum = xEnd - ActualWidth;
			xScrollSmallChange = charWidth;
			xScrollLargeChange = ActualWidth - xScrollSmallChange;

			yScrollMaximum = yEnd - ActualHeight;
			yScrollSmallChange = rowHeight;
			yScrollLargeChange = ActualHeight - yScrollSmallChange;

			var startRow = Math.Max(0, GetRowFromY(yScrollValue));
			var endRow = Math.Min(rows, GetRowFromY(ActualHeight + rowHeight + yScrollValue));

			for (var row = startRow; row < endRow; ++row)
			{
				var y = yLinesStart - yScrollValue + row * rowHeight;
				var selected = new bool[columns];
				string hex = "", text = "";
				var useColumns = Math.Min(columns, Data.Length - row * columns);
				for (var column = 0; column < useColumns; ++column)
				{
					var pos = row * columns + column;
					if ((pos >= SelStart) && (pos <= SelEnd))
						selected[column] = true;

					var b = Data[pos];
					var c = (char)b;

					hex += String.Format("{0:x2}", b) + new string(' ', xHexSpacing);
					text += Char.IsControl(c) ? '·' : c;
				}

				var posText = new FormattedText(String.Format("{0:x" + xPosColumns.ToString() + "}", row * columns), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
				var hexText = new FormattedText(hex, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
				var textText = new FormattedText(text, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);

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
						overrideSelecting = false;
						if ((SelStart != SelEnd) || (e.Key == Key.Delete))
						{
							Data.Replace(SelStart, SelEnd - SelStart + 1, new byte[0]);
							Pos1 = SelStart;
						}
						else if (e.Key == Key.Back)
						{
							Data.Replace(SelStart - 1, 1, new byte[0]);
							Pos1 = SelStart - 1;
						}
						overrideSelecting = null;
					}
					break;
				case Key.Tab:
					SelHex = !SelHex;
					inHexEdit = false;
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

		bool inHexEdit = false;
		void DoInsert(BinaryData bytes, bool inHex)
		{
			if ((bytes == null) || (bytes.Length == 0))
				return;

			if (Insert)
			{
				if (SelStart != SelEnd)
					Data.Replace(SelStart, SelEnd - SelStart + 1, new byte[0]);
				if ((inHex) && (!inHexEdit))
					Data.Replace(SelStart, bytes.Length, bytes.Data);
				else
					Data.Replace(SelStart, 0, bytes.Data);
			}
			else
			{
				Data.Replace(SelStart, bytes.Length, bytes.Data);
			}

			overrideSelecting = false;
			if (!inHex)
				Pos1 = SelStart + bytes.Length;
			else if (!inHexEdit)
				Pos1 = SelStart + 1;
			overrideSelecting = null;
		}

		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			if ((String.IsNullOrEmpty(e.Text)) || (e.Text == "\u001B"))
				return;

			BinaryData bytes = null;

			if (SelHex)
			{
				var let = Char.ToUpper(e.Text[0]);
				byte val;
				if ((let >= '0') && (let <= '9'))
					val = (byte)(let - '0');
				else if ((let >= 'A') && (let <= 'F'))
					val = (byte)(let - 'A' + 10);
				else
					return;

				if (inHexEdit)
					bytes = new byte[] { (byte)(Data[SelStart] * 16 + val) };
				else
					bytes = new byte[] { val };
				inHexEdit = !inHexEdit;
			}
			else
				bytes = Coder.StringToBytes(e.Text, InputCoderType);

			DoInsert(bytes, SelHex);
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

		void GZip()
		{
			using (var ms = new MemoryStream())
			{
				using (var gz = new GZipStream(ms, CompressionLevel.Optimal, true))
					gz.Write(Data.Data, 0, Data.Data.Length);
				Data = ms.ToArray();
			}
		}

		void GUnzip()
		{
			using (var gz = new GZipStream(new MemoryStream(Data.Data), CompressionMode.Decompress))
			using (var ms = new MemoryStream())
			{
				gz.CopyTo(ms);
				Data = ms.ToArray();
			}
		}

		void Deflate()
		{
			using (var ms = new MemoryStream())
			{
				using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal, true))
					deflate.Write(Data.Data, 0, Data.Data.Length);
				Data = ms.ToArray();
			}
		}

		void Inflate()
		{
			using (var inflate = new DeflateStream(new MemoryStream(Data.Data), CompressionMode.Decompress))
			using (var ms = new MemoryStream())
			{
				inflate.CopyTo(ms);
				Data = ms.ToArray();
			}
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
							var subset = Data.GetSubset(SelStart, SelEnd - SelStart + 1);
							Clipboard.Current.Set(subset, SelHex);
							if ((command.Name == BinaryEditor.Edit_Cut) && (Insert))
								Data.Replace(SelStart, SelEnd - SelStart + 1, new byte[0]);
						}
						break;
					case BinaryEditor.Edit_Paste:
						DoInsert(Clipboard.Current.GetBinaryData(InputCoderType), false);
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
							Checksums.Type checksumType = Checksums.Type.None;
							switch (command.Name)
							{
								case BinaryEditor.Checksum_MD5: checksumType = Checksums.Type.MD5; break;
								case BinaryEditor.Checksum_SHA1: checksumType = Checksums.Type.SHA1; break;
								case BinaryEditor.Checksum_SHA256: checksumType = Checksums.Type.SHA256; break;
							}
							new Message
							{
								Title = "Result",
								Text = Checksums.Get(checksumType, Data.Data),
								Options = Message.OptionsEnum.Ok
							}.Show();
						}
						break;
					case BinaryEditor.Compress_GZip: GZip(); break;
					case BinaryEditor.Decompress_GZip: GUnzip(); break;
					case BinaryEditor.Compress_Deflate: Deflate(); break;
					case BinaryEditor.Decompress_Inflate: Inflate(); break;
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

									var encryptedData = Crypto.Encrypt(Crypto.CryptoType.AES, Data.Data, aesKey);
									ms.Write(encryptedData, 0, encryptedData.Length);

									Data = ms.ToArray();
								}
							}
						}
						break;
					case BinaryEditor.Decrypt_RSAAES:
						{
							var keyDialog = new AsymmetricKeyDialog { Type = Crypto.CryptoType.RSA, Public = false };
							if (keyDialog.ShowDialog() == true)
							{
								var encryptedAesKey = new byte[BitConverter.ToInt32(Data.Data, 0)];
								Array.Copy(Data.Data, sizeof(int), encryptedAesKey, 0, encryptedAesKey.Length);
								var aesKey = Encoding.UTF8.GetString(Crypto.Decrypt(Crypto.CryptoType.RSA, encryptedAesKey, keyDialog.Key));
								var data = Data.Data.Skip(sizeof(int) + encryptedAesKey.Length).ToArray();
								Data = Crypto.Decrypt(Crypto.CryptoType.AES, data, aesKey);
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
								Data = Crypto.Encrypt(type, Data.Data, keyDialog.Key);
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
								Data = Crypto.Decrypt(type, Data.Data, keyDialog.Key);
						}
						break;
					case BinaryEditor.Encrypt_RSA:
						{
							var type = Crypto.CryptoType.RSA;
							var keyDialog = new AsymmetricKeyDialog { Type = type, Public = true, CanGenerate = true };
							if (keyDialog.ShowDialog() == true)
								Data = Crypto.Encrypt(type, Data.Data, keyDialog.Key);
						}
						break;
					case BinaryEditor.Decrypt_RSA:
						{
							var type = Crypto.CryptoType.RSA;
							var keyDialog = new AsymmetricKeyDialog { Type = type, Public = false };
							if (keyDialog.ShowDialog() == true)
								Data = Crypto.Decrypt(type, Data.Data, keyDialog.Key);
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
									Text = Crypto.Sign(type, Data.Data, keyDialog.Key, keyDialog.Hash),
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
								var result = Crypto.Verify(type, Data.Data, keyDialog.Key, keyDialog.Hash, keyDialog.Signature);
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
				overrideSelecting = true;
				Pos2 = start;
				EnsureVisible(Pos2);
				Pos1 = end;
				overrideSelecting = null;
			}
		}
	}
}
