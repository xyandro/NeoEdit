using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Win32;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Data;
using NeoEdit.GUI.Records;

namespace NeoEdit.GUI.BinaryEditorUI
{
	public partial class BinaryEditor : Window
	{
		public static RoutedUICommand Command_File_New = new RoutedUICommand { Text = "_New" };
		public static RoutedUICommand Command_File_Open = new RoutedUICommand { Text = "_Open" };
		public static RoutedUICommand Command_File_Save = new RoutedUICommand { Text = "_Save" };
		public static RoutedUICommand Command_File_SaveAs = new RoutedUICommand { Text = "Save _As" };
		public static RoutedUICommand Command_File_Exit = new RoutedUICommand { Text = "_Exit" };
		public static RoutedUICommand Command_Edit_Undo = new RoutedUICommand { Text = "_Undo" };
		public static RoutedUICommand Command_Edit_Cut = new RoutedUICommand { Text = "Cu_t" };
		public static RoutedUICommand Command_Edit_Copy = new RoutedUICommand { Text = "_Copy" };
		public static RoutedUICommand Command_Edit_Paste = new RoutedUICommand { Text = "_Paste" };
		public static RoutedUICommand Command_Edit_ShowClipboard = new RoutedUICommand { Text = "_Show Clipboard" };
		public static RoutedUICommand Command_Edit_Find = new RoutedUICommand { Text = "_Find" };
		public static RoutedUICommand Command_Edit_FindNext = new RoutedUICommand { Text = "Find _Next" };
		public static RoutedUICommand Command_Edit_FindPrev = new RoutedUICommand { Text = "Find _Previous" };
		public static RoutedUICommand Command_Edit_Goto = new RoutedUICommand { Text = "_Goto" };
		public static RoutedUICommand Command_Edit_Insert = new RoutedUICommand { Text = "_Insert" };
		public static RoutedUICommand Command_View_Values = new RoutedUICommand { Text = "_Values" };
		public static RoutedUICommand Command_View_Refresh = new RoutedUICommand { Text = "_Refresh" };
		public static RoutedUICommand Command_Checksum_MD5 = new RoutedUICommand { Text = "_MD5" };
		public static RoutedUICommand Command_Checksum_SHA1 = new RoutedUICommand { Text = "SHA_1" };
		public static RoutedUICommand Command_Checksum_SHA256 = new RoutedUICommand { Text = "SHA_256" };
		public static RoutedUICommand Command_Compress_GZip = new RoutedUICommand { Text = "_GZip" };
		public static RoutedUICommand Command_Decompress_GZip = new RoutedUICommand { Text = "_GZip" };
		public static RoutedUICommand Command_Compress_Deflate = new RoutedUICommand { Text = "_Deflate" };
		public static RoutedUICommand Command_Decompress_Inflate = new RoutedUICommand { Text = "_Inflate" };
		public static RoutedUICommand Command_Encrypt_AES = new RoutedUICommand { Text = "_AES" };
		public static RoutedUICommand Command_Decrypt_AES = new RoutedUICommand { Text = "_AES" };
		public static RoutedUICommand Command_Encrypt_DES = new RoutedUICommand { Text = "_DES" };
		public static RoutedUICommand Command_Decrypt_DES = new RoutedUICommand { Text = "_DES" };
		public static RoutedUICommand Command_Encrypt_DES3 = new RoutedUICommand { Text = "_3DES" };
		public static RoutedUICommand Command_Decrypt_DES3 = new RoutedUICommand { Text = "_3DES" };
		public static RoutedUICommand Command_Encrypt_RSA = new RoutedUICommand { Text = "_RSA" };
		public static RoutedUICommand Command_Decrypt_RSA = new RoutedUICommand { Text = "_RSA" };
		public static RoutedUICommand Command_Encrypt_RSAAES = new RoutedUICommand { Text = "RSA/AES" };
		public static RoutedUICommand Command_Decrypt_RSAAES = new RoutedUICommand { Text = "RSA/AES" };
		public static RoutedUICommand Command_Sign_RSA = new RoutedUICommand { Text = "_RSA" };
		public static RoutedUICommand Command_Verify_RSA = new RoutedUICommand { Text = "_RSA" };
		public static RoutedUICommand Command_Sign_DSA = new RoutedUICommand { Text = "_DSA" };
		public static RoutedUICommand Command_Verify_DSA = new RoutedUICommand { Text = "_DSA" };

		[DepProp]
		public BinaryData Data { get { return uiHelper.GetPropValue<BinaryData>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long ChangeCount { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool ShowValues { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long SelStart { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long SelEnd { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public string FoundText { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool Insert { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public Coder.Type InputCoderType { get { return uiHelper.GetPropValue<Coder.Type>(); } set { uiHelper.SetPropValue(value); } }

		static BinaryEditor() { UIHelper<BinaryEditor>.Register(); }

		readonly UIHelper<BinaryEditor> uiHelper;
		Record record;
		public BinaryEditor(Record _record = null, BinaryData data = null)
		{
			uiHelper = new UIHelper<BinaryEditor>(this);
			InitializeComponent();

			record = _record;
			if (data == null)
			{
				if (record == null)
					data = new MemoryBinaryData();
				else
					data = record.Read();
			}
			Data = data;
			BinaryData.BinaryDataChangedDelegate changed = () => ++ChangeCount;
			Data.Changed += changed;
			Closed += (s, e) => Data.Changed -= changed;

			MouseWheel += (s, e) => uiHelper.RaiseEvent(yScroll, e);
			yScroll.MouseWheel += (s, e) => (s as ScrollBar).Value -= e.Delta;

			Show();
		}

		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			base.OnTextInput(e);
			if (e.Handled)
				return;

			if (e.OriginalSource is MenuItem)
				return;

			uiHelper.RaiseEvent(canvas, e);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Handled)
				return;

			switch (e.Key)
			{
				case Key.Escape: canvas.Focus(); break;
				default: uiHelper.RaiseEvent(canvas, e); break;
			}
		}

		void Command_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			RunCommand(e.Command);
		}

		void RunCommand(ICommand command)
		{
			canvas.RunCommand(command);

			if (command == Command_File_New)
			{
				record = null;
				Data = new MemoryBinaryData();
			}
			else if (command == Command_File_Open)
			{
				{
					var dialog = new OpenFileDialog();
					if (dialog.ShowDialog() == true)
					{
						record = new Root().GetRecord(dialog.FileName);
						Data = record.Read();
					}
				}
			}
			else if (command == Command_File_Save)
			{
				if (record == null)
					RunCommand(Command_File_SaveAs);
				else
					record.Write(Data);
			}
			else if (command == Command_File_SaveAs)
			{
				{
					var dialog = new SaveFileDialog();
					if (dialog.ShowDialog() == true)
					{
						if (Directory.Exists(dialog.FileName))
							throw new Exception("A directory by that name already exists.");
						if (!Directory.Exists(Path.GetDirectoryName(dialog.FileName)))
							throw new Exception("Directory doesn't exist.");
						var dir = new Root().GetRecord(Path.GetDirectoryName(dialog.FileName));
						record = dir.CreateFile(Path.GetFileName(dialog.FileName));
						RunCommand(Command_File_Save);
					}
				}
			}
			else if (command == Command_File_Exit) { Close(); }
			else if (command == Command_View_Values) { ShowValues = !ShowValues; }
			else if (command == Command_Edit_ShowClipboard) { Clipboard.Show(); }
		}

		void InputEncodingClick(object sender, RoutedEventArgs e)
		{
			canvas.InputCoderType = Helpers.ParseEnum<Coder.Type>(((MenuItem)e.Source).Header as string);
		}

		void EncodeClick(object sender, RoutedEventArgs e)
		{
			var header = (e.OriginalSource as MenuItem).Header as string;
			var encoding = Coder.Type.None;
			if (header != "Auto")
				encoding = Helpers.ParseEnum<Coder.Type>(header);
			var data = new TextData(Data.GetAllBytes(), encoding);
			new TextEditorUI.TextEditor(record, data);
			this.Close();
		}
	}
}
