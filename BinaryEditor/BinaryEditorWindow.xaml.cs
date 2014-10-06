using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using NeoEdit.BinaryEditor.Data;
using NeoEdit.Common.Transform;
using NeoEdit.GUI;
using NeoEdit.GUI.Common;

namespace NeoEdit.BinaryEditor
{
	public partial class BinaryEditorWindow
	{
		static BinaryEditorWindow() { UIHelper<BinaryEditorWindow>.Register(); }

		readonly UIHelper<BinaryEditorWindow> uiHelper;
		BinaryEditorWindow(BinaryData data, Coder.Type encoder = Coder.Type.None, string filename = null, string filetitle = null)
		{
			uiHelper = new UIHelper<BinaryEditorWindow>(this);
			BinaryEditMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command));
			InitializeComponent();

			active.SetData(data, encoder, filename, filetitle);

			MouseWheel += (s, e) => active.HandleMouseWheel(e.Delta);
		}

		public static BinaryEditorWindow CreateFromFile(string filename = null, byte[] bytes = null, Coder.Type encoder = Coder.Type.None)
		{
			if (bytes == null)
			{
				if (filename == null)
					bytes = new byte[0];
				else
					bytes = File.ReadAllBytes(filename);
			}
			return new BinaryEditorWindow(new MemoryBinaryData(bytes), encoder, filename);
		}

		public static BinaryEditorWindow CreateFromDump(string filename)
		{
			return new BinaryEditorWindow(new DumpBinaryData(filename), filename: filename, filetitle: "Dump: ");
		}

		public static BinaryEditorWindow CreateFromProcess(int pid)
		{
			var process = Process.GetProcessById(pid);
			if (process == null)
				throw new ArgumentException("Process doesn't exist.");
			if (process.Id == Process.GetCurrentProcess().Id)
				throw new ArgumentException("Can't open current process.");
			return new BinaryEditorWindow(new ProcessBinaryData(pid), filetitle: String.Format("Process {0} ({1}) - ", pid, process.ProcessName));
		}

		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			base.OnTextInput(e);
			if (e.Handled)
				return;

			if (e.OriginalSource is MenuItem)
				return;

			active.HandleText(e.Text);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Handled)
				return;

			switch (e.Key)
			{
				case Key.Escape: active.Focus(); e.Handled = true; break;
				default: e.Handled = active.HandleKey(e.Key); break;
			}
		}

		void Command_File_New()
		{
			active.SetData(new MemoryBinaryData());
		}

		void Command_File_Open()
		{
			var dialog = new OpenFileDialog();
			if (dialog.ShowDialog() != true)
				return;

			active.SetData(new MemoryBinaryData(File.ReadAllBytes(dialog.FileName)), filename: dialog.FileName);
		}

		void Command_File_OpenDump()
		{
			var dialog = new OpenFileDialog();
			if (dialog.ShowDialog() != true)
				return;

			active.SetData(new DumpBinaryData(dialog.FileName), filetitle: "Dump: ", filename: dialog.FileName);
		}

		void RunCommand(BinaryEditCommand command)
		{
			switch (command)
			{
				case BinaryEditCommand.File_New: Command_File_New(); break;
				case BinaryEditCommand.File_Open: Command_File_Open(); break;
				case BinaryEditCommand.File_OpenDump: Command_File_OpenDump(); break;
				case BinaryEditCommand.File_Save: active.Command_File_Save(); break;
				case BinaryEditCommand.File_SaveAs: active.Command_File_SaveAs(); break;
				case BinaryEditCommand.File_CopyPath: active.Command_File_CopyPath(); break;
				case BinaryEditCommand.File_CopyName: active.Command_File_CopyName(); break;
				case BinaryEditCommand.File_Encode_Auto: active.Command_File_Encode(Coder.Type.None); break;
				case BinaryEditCommand.File_Encode_UTF8: active.Command_File_Encode(Coder.Type.UTF8); break;
				case BinaryEditCommand.File_Encode_UTF7: active.Command_File_Encode(Coder.Type.UTF7); break;
				case BinaryEditCommand.File_Encode_UTF16LE: active.Command_File_Encode(Coder.Type.UTF16LE); break;
				case BinaryEditCommand.File_Encode_UTF16BE: active.Command_File_Encode(Coder.Type.UTF16BE); break;
				case BinaryEditCommand.File_Encode_UTF32LE: active.Command_File_Encode(Coder.Type.UTF32LE); break;
				case BinaryEditCommand.File_Encode_UTF32BE: active.Command_File_Encode(Coder.Type.UTF32BE); break;
				case BinaryEditCommand.File_Encode_Base64: active.Command_File_Encode(Coder.Type.Base64); break;
				case BinaryEditCommand.File_TextEditor: Launcher.Static.LaunchTextEditor(active.FileName, active.Data.GetAllBytes(), active.CoderUsed); Close(); break;
				case BinaryEditCommand.File_Exit: Close(); break;
				case BinaryEditCommand.Edit_Undo: active.Command_Edit_Undo(); break;
				case BinaryEditCommand.Edit_Redo: active.Command_Edit_Redo(); break;
				case BinaryEditCommand.Edit_Copy: active.Command_Edit_Copy(command); break;
				case BinaryEditCommand.Edit_Paste: active.Command_Edit_Paste(); break;
				case BinaryEditCommand.Edit_ShowClipboard: ClipboardWindow.Show(); break;
				case BinaryEditCommand.Edit_Find: active.Command_Edit_Find(); break;
				case BinaryEditCommand.Edit_FindPrev: active.Command_Edit_FindPrev(command); break;
				case BinaryEditCommand.Edit_Goto: active.Command_Edit_Goto(); break;
				case BinaryEditCommand.Edit_Insert: active.Command_Edit_Insert(); break;
				case BinaryEditCommand.View_Values: active.Command_View_Values(); break;
				case BinaryEditCommand.View_Refresh: active.Command_View_Refresh(); break;
				case BinaryEditCommand.Checksum_MD5: active.Command_Checksum(Checksum.Type.MD5); break;
				case BinaryEditCommand.Checksum_SHA1: active.Command_Checksum(Checksum.Type.SHA1); break;
				case BinaryEditCommand.Checksum_SHA256: active.Command_Checksum(Checksum.Type.SHA256); break;
				case BinaryEditCommand.Compress_GZip: active.Command_Compress(true, Compression.Type.GZip); break;
				case BinaryEditCommand.Compress_Deflate: active.Command_Compress(true, Compression.Type.Deflate); break;
				case BinaryEditCommand.Decompress_GZip: active.Command_Compress(false, Compression.Type.GZip); break;
				case BinaryEditCommand.Decompress_Inflate: active.Command_Compress(false, Compression.Type.Deflate); break;
				case BinaryEditCommand.Encrypt_AES: active.Command_Encrypt(true, Crypto.Type.AES); break;
				case BinaryEditCommand.Encrypt_DES: active.Command_Encrypt(true, Crypto.Type.DES); break;
				case BinaryEditCommand.Encrypt_DES3: active.Command_Encrypt(true, Crypto.Type.DES3); break;
				case BinaryEditCommand.Encrypt_RSA: active.Command_Encrypt(true, Crypto.Type.RSA); break;
				case BinaryEditCommand.Encrypt_RSAAES: active.Command_Encrypt(true, Crypto.Type.RSAAES); break;
				case BinaryEditCommand.Decrypt_AES: active.Command_Encrypt(false, Crypto.Type.AES); break;
				case BinaryEditCommand.Decrypt_DES: active.Command_Encrypt(false, Crypto.Type.DES); break;
				case BinaryEditCommand.Decrypt_DES3: active.Command_Encrypt(false, Crypto.Type.DES3); break;
				case BinaryEditCommand.Decrypt_RSA: active.Command_Encrypt(false, Crypto.Type.RSA); break;
				case BinaryEditCommand.Decrypt_RSAAES: active.Command_Encrypt(false, Crypto.Type.RSAAES); break;
				case BinaryEditCommand.Sign_RSA: active.Command_Sign(true, Crypto.Type.RSA); break;
				case BinaryEditCommand.Sign_DSA: active.Command_Sign(true, Crypto.Type.DSA); break;
				case BinaryEditCommand.Verify_RSA: active.Command_Sign(false, Crypto.Type.RSA); break;
				case BinaryEditCommand.Verify_DSA: active.Command_Sign(false, Crypto.Type.DSA); break;
			}
		}
	}
}
