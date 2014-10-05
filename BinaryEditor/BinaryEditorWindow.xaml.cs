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
		[DepProp]
		string FileTitle { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		string FileName { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		BinaryData Data { get { return uiHelper.GetPropValue<BinaryData>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool ShowValues { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		Coder.Type CoderUsed { get { return uiHelper.GetPropValue<Coder.Type>(); } set { uiHelper.SetPropValue(value); } }

		static BinaryEditorWindow() { UIHelper<BinaryEditorWindow>.Register(); }

		readonly UIHelper<BinaryEditorWindow> uiHelper;
		BinaryEditorWindow(BinaryData data, Coder.Type encoder = Coder.Type.None)
		{
			uiHelper = new UIHelper<BinaryEditorWindow>(this);
			BinaryEditMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command));
			InitializeComponent();

			Data = data;
			CoderUsed = encoder;
			if (CoderUsed == Coder.Type.None)
				CoderUsed = Data.GuessEncoding();

			MouseWheel += (s, e) => yScroll.Value -= e.Delta;
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
			return new BinaryEditorWindow(new MemoryBinaryData(bytes), encoder) { FileName = filename };
		}

		public static BinaryEditorWindow CreateFromDump(string filename)
		{
			return new BinaryEditorWindow(new DumpBinaryData(filename)) { FileTitle = "Dump: ", FileName = filename };
		}

		public static BinaryEditorWindow CreateFromProcess(int pid)
		{
			var process = Process.GetProcessById(pid);
			if (process == null)
				throw new ArgumentException("Process doesn't exist.");
			if (process.Id == Process.GetCurrentProcess().Id)
				throw new ArgumentException("Can't open current process.");
			return new BinaryEditorWindow(new ProcessBinaryData(pid)) { FileTitle = String.Format("Process {0} ({1}) - ", pid, process.ProcessName) };
		}

		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			base.OnTextInput(e);
			if (e.Handled)
				return;

			if (e.OriginalSource is MenuItem)
				return;

			canvas.HandleText(e.Text);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Handled)
				return;

			switch (e.Key)
			{
				case Key.Escape: canvas.Focus(); e.Handled = true; break;
				default: e.Handled = canvas.HandleKey(e.Key); break;
			}
		}

		void Command_File_New()
		{
			FileTitle = FileName = null;
			Data = new MemoryBinaryData();
		}

		void Command_File_Open()
		{
			var dialog = new OpenFileDialog();
			if (dialog.ShowDialog() != true)
				return;

			FileTitle = null;
			FileName = dialog.FileName;
			Data = new MemoryBinaryData(File.ReadAllBytes(FileName));
		}

		void Command_File_OpenDump()
		{
			var dialog = new OpenFileDialog();
			if (dialog.ShowDialog() != true)
				return;

			FileTitle = "Dump: ";
			FileName = dialog.FileName;
			Data = new DumpBinaryData(FileName);
		}

		void Command_File_Save()
		{
			if (FileName == null)
				Command_File_SaveAs();
			else
				Data.Save(FileName);
		}

		void Command_File_SaveAs()
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

		void RunCommand(BinaryEditCommand command)
		{
			switch (command)
			{
				case BinaryEditCommand.File_New: Command_File_New(); break;
				case BinaryEditCommand.File_Open: Command_File_Open(); break;
				case BinaryEditCommand.File_OpenDump: Command_File_OpenDump(); break;
				case BinaryEditCommand.File_Save: Command_File_Save(); break;
				case BinaryEditCommand.File_SaveAs: Command_File_SaveAs(); break;
				case BinaryEditCommand.File_Encode_Auto: CoderUsed = Data.GuessEncoding(); break;
				case BinaryEditCommand.File_Encode_UTF8: CoderUsed = Coder.Type.UTF8; break;
				case BinaryEditCommand.File_Encode_UTF7: CoderUsed = Coder.Type.UTF7; break;
				case BinaryEditCommand.File_Encode_UTF16LE: CoderUsed = Coder.Type.UTF16LE; break;
				case BinaryEditCommand.File_Encode_UTF16BE: CoderUsed = Coder.Type.UTF16BE; break;
				case BinaryEditCommand.File_Encode_UTF32LE: CoderUsed = Coder.Type.UTF32LE; break;
				case BinaryEditCommand.File_Encode_UTF32BE: CoderUsed = Coder.Type.UTF32BE; break;
				case BinaryEditCommand.File_Encode_Base64: CoderUsed = Coder.Type.Base64; break;
				case BinaryEditCommand.File_TextEditor: Launcher.Static.LaunchTextEditor(FileName, Data.GetAllBytes(), CoderUsed); Close(); break;
				case BinaryEditCommand.File_Exit: Close(); break;
				case BinaryEditCommand.Edit_Undo: canvas.Command_Edit_Undo(); break;
				case BinaryEditCommand.Edit_Redo: canvas.Command_Edit_Redo(); break;
				case BinaryEditCommand.Edit_Copy: canvas.Command_Edit_Copy(command); break;
				case BinaryEditCommand.Edit_Paste: canvas.Command_Edit_Paste(); break;
				case BinaryEditCommand.Edit_ShowClipboard: ClipboardWindow.Show(); break;
				case BinaryEditCommand.Edit_Find: canvas.Command_Edit_Find(); break;
				case BinaryEditCommand.Edit_FindPrev: canvas.Command_Edit_FindPrev(command); break;
				case BinaryEditCommand.Edit_Goto: canvas.Command_Edit_Goto(); break;
				case BinaryEditCommand.Edit_Insert: canvas.Command_Edit_Insert(); break;
				case BinaryEditCommand.View_Values: ShowValues = !ShowValues; break;
				case BinaryEditCommand.View_Refresh: canvas.Command_View_Refresh(); break;
				case BinaryEditCommand.Checksum_MD5: canvas.Command_Checksum(Checksum.Type.MD5); break;
				case BinaryEditCommand.Checksum_SHA1: canvas.Command_Checksum(Checksum.Type.SHA1); break;
				case BinaryEditCommand.Checksum_SHA256: canvas.Command_Checksum(Checksum.Type.SHA256); break;
				case BinaryEditCommand.Compress_GZip: canvas.Command_Compress(true, Compression.Type.GZip); break;
				case BinaryEditCommand.Compress_Deflate: canvas.Command_Compress(true, Compression.Type.Deflate); break;
				case BinaryEditCommand.Decompress_GZip: canvas.Command_Compress(false, Compression.Type.GZip); break;
				case BinaryEditCommand.Decompress_Inflate: canvas.Command_Compress(false, Compression.Type.Deflate); break;
				case BinaryEditCommand.Encrypt_AES: canvas.Command_Encrypt(true, Crypto.Type.AES); break;
				case BinaryEditCommand.Encrypt_DES: canvas.Command_Encrypt(true, Crypto.Type.DES); break;
				case BinaryEditCommand.Encrypt_DES3: canvas.Command_Encrypt(true, Crypto.Type.DES3); break;
				case BinaryEditCommand.Encrypt_RSA: canvas.Command_Encrypt(true, Crypto.Type.RSA); break;
				case BinaryEditCommand.Encrypt_RSAAES: canvas.Command_Encrypt(true, Crypto.Type.RSAAES); break;
				case BinaryEditCommand.Decrypt_AES: canvas.Command_Encrypt(false, Crypto.Type.AES); break;
				case BinaryEditCommand.Decrypt_DES: canvas.Command_Encrypt(false, Crypto.Type.DES); break;
				case BinaryEditCommand.Decrypt_DES3: canvas.Command_Encrypt(false, Crypto.Type.DES3); break;
				case BinaryEditCommand.Decrypt_RSA: canvas.Command_Encrypt(false, Crypto.Type.RSA); break;
				case BinaryEditCommand.Decrypt_RSAAES: canvas.Command_Encrypt(false, Crypto.Type.RSAAES); break;
				case BinaryEditCommand.Sign_RSA: canvas.Command_Sign(true, Crypto.Type.RSA); break;
				case BinaryEditCommand.Sign_DSA: canvas.Command_Sign(true, Crypto.Type.DSA); break;
				case BinaryEditCommand.Verify_RSA: canvas.Command_Sign(false, Crypto.Type.RSA); break;
				case BinaryEditCommand.Verify_DSA: canvas.Command_Sign(false, Crypto.Type.DSA); break;
			}
		}
	}
}
