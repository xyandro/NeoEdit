﻿using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Win32;
using NeoEdit.Common;
using NeoEdit.Common.Data;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Common;
using NeoEdit.Records;

namespace NeoEdit.GUI.BinaryEditorUI
{
	public partial class BinaryEditor : Window
	{
		public static RoutedCommand Command_File_New = new RoutedCommand();
		public static RoutedCommand Command_File_Open = new RoutedCommand();
		public static RoutedCommand Command_File_Save = new RoutedCommand();
		public static RoutedCommand Command_File_SaveAs = new RoutedCommand();
		public static RoutedCommand Command_File_Exit = new RoutedCommand();
		public static RoutedCommand Command_Edit_Undo = new RoutedCommand();
		public static RoutedCommand Command_Edit_Redo = new RoutedCommand();
		public static RoutedCommand Command_Edit_Cut = new RoutedCommand();
		public static RoutedCommand Command_Edit_Copy = new RoutedCommand();
		public static RoutedCommand Command_Edit_Paste = new RoutedCommand();
		public static RoutedCommand Command_Edit_ShowClipboard = new RoutedCommand();
		public static RoutedCommand Command_Edit_Find = new RoutedCommand();
		public static RoutedCommand Command_Edit_FindNext = new RoutedCommand();
		public static RoutedCommand Command_Edit_FindPrev = new RoutedCommand();
		public static RoutedCommand Command_Edit_Goto = new RoutedCommand();
		public static RoutedCommand Command_Edit_Insert = new RoutedCommand();
		public static RoutedCommand Command_View_Values = new RoutedCommand();
		public static RoutedCommand Command_View_Refresh = new RoutedCommand();
		public static RoutedCommand Command_Checksum_MD5 = new RoutedCommand();
		public static RoutedCommand Command_Checksum_SHA1 = new RoutedCommand();
		public static RoutedCommand Command_Checksum_SHA256 = new RoutedCommand();
		public static RoutedCommand Command_Compress_GZip = new RoutedCommand();
		public static RoutedCommand Command_Decompress_GZip = new RoutedCommand();
		public static RoutedCommand Command_Compress_Deflate = new RoutedCommand();
		public static RoutedCommand Command_Decompress_Inflate = new RoutedCommand();
		public static RoutedCommand Command_Encrypt_AES = new RoutedCommand();
		public static RoutedCommand Command_Decrypt_AES = new RoutedCommand();
		public static RoutedCommand Command_Encrypt_DES = new RoutedCommand();
		public static RoutedCommand Command_Decrypt_DES = new RoutedCommand();
		public static RoutedCommand Command_Encrypt_DES3 = new RoutedCommand();
		public static RoutedCommand Command_Decrypt_DES3 = new RoutedCommand();
		public static RoutedCommand Command_Encrypt_RSA = new RoutedCommand();
		public static RoutedCommand Command_Decrypt_RSA = new RoutedCommand();
		public static RoutedCommand Command_Encrypt_RSAAES = new RoutedCommand();
		public static RoutedCommand Command_Decrypt_RSAAES = new RoutedCommand();
		public static RoutedCommand Command_Sign_RSA = new RoutedCommand();
		public static RoutedCommand Command_Verify_RSA = new RoutedCommand();
		public static RoutedCommand Command_Sign_DSA = new RoutedCommand();
		public static RoutedCommand Command_Verify_DSA = new RoutedCommand();

		[DepProp]
		BinaryData Data { get { return uiHelper.GetPropValue<BinaryData>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool ShowValues { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		Coder.Type InputCoderType { get { return uiHelper.GetPropValue<Coder.Type>(); } set { uiHelper.SetPropValue(value); } }

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
