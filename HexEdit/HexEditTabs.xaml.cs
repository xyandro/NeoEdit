using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using NeoEdit.Common;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;
using NeoEdit.HexEdit.Data;

namespace NeoEdit.HexEdit
{
	public class Tabs : Tabs<HexEditor, HexEditCommand> { }
	public class TabsWindow : TabsWindow<HexEditor, HexEditCommand> { }

	public partial class HexEditTabs
	{
		static HexEditTabs() { UIHelper<HexEditTabs>.Register(); }

		static void Create(BinaryData data, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, string filename = null, string filetitle = null, bool modified = false, HexEditTabs hexEditTabs = null, bool forceCreate = false)
		{
			CreateTab(new HexEditor(data, codePage, filename, filetitle, modified), hexEditTabs, forceCreate);
		}

		HexEditTabs()
		{
			HexEditMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command, shiftDown));
			InitializeComponent();
			ItemTabs = tabs;
			UIHelper.AuditMenu(menu);
		}

		public static void CreateFromFile(string filename = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, bool modified = false, bool forceCreate = false)
		{
			if (bytes == null)
			{
				if (filename == null)
					bytes = new byte[0];
				else
					bytes = File.ReadAllBytes(filename);
			}
			Create(new MemoryBinaryData(bytes), codePage, filename, modified: modified, forceCreate: forceCreate);
		}

		public static void CreateFromDump(string filename, bool forceCreate = false)
		{
			Create(new DumpBinaryData(filename), filename: filename, filetitle: "Dump: ", forceCreate: forceCreate);
		}

		public static void CreateFromProcess(int pid, bool forceCreate = false)
		{
			var process = Process.GetProcessById(pid);
			if (process == null)
				throw new ArgumentException("Process doesn't exist.");
			if (process.Id == Process.GetCurrentProcess().Id)
				throw new ArgumentException("Can't open current process.");
			Create(new ProcessBinaryData(pid), filetitle: String.Format("Process {0} ({1}) - ", pid, process.ProcessName), forceCreate: forceCreate);
		}

		void Command_File_New(bool newWindow)
		{
			Create(new MemoryBinaryData(), hexEditTabs: this, forceCreate: newWindow);
		}

		void Command_File_Open_Open()
		{
			var dir = ItemTabs.TopMost != null ? Path.GetDirectoryName(ItemTabs.TopMost.FileName) : null;
			var dialog = new OpenFileDialog
			{
				Multiselect = true,
				InitialDirectory = dir,
			};
			if (dialog.ShowDialog() != true)
				return;

			foreach (var filename in dialog.FileNames)
				tabs.CreateTab(new HexEditor(new MemoryBinaryData(File.ReadAllBytes(filename)), filename: filename));
		}

		void Command_File_Open_OpenDump()
		{
			var dialog = new OpenFileDialog();
			if (dialog.ShowDialog() != true)
				return;

			tabs.CreateTab(new HexEditor(new DumpBinaryData(dialog.FileName), filetitle: "Dump: ", filename: dialog.FileName));
		}

		void Command_File_Open_OpenCopiedCutFiles()
		{
			var files = NEClipboard.Strings;

			if ((files.Count > 5) && (new Message
			{
				Title = "Confirm",
				Text = String.Format("Are you sure you want to open these {0} files?", files.Count),
				Options = Message.OptionsEnum.YesNoCancel,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.Cancel,
			}.Show() != Message.OptionsEnum.Yes))
				return;

			foreach (var file in files)
				tabs.CreateTab(new HexEditor(new MemoryBinaryData(File.ReadAllBytes(file)), filename: file));
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Handled)
				return;

			var shiftDown = this.shiftDown;
			var controlDown = this.controlDown;
			var altDown = this.altDown;

			e.Handled = HandleKey(e.Key, shiftDown, controlDown, altDown);
		}

		public override bool HandleKey(Key key, bool shiftDown, bool controlDown, bool altDown)
		{
			if (ItemTabs.TopMost == null)
				return false;
			return ItemTabs.TopMost.HandleKey(key, shiftDown, controlDown, altDown);
		}

		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			base.OnTextInput(e);
			if (e.Handled)
				return;

			if (e.Source is MenuItem)
				return;

			e.Handled = HandleText(e.Text);
		}

		public override bool HandleText(string text)
		{
			if (ItemTabs.TopMost == null)
				return false;
			return ItemTabs.TopMost.HandleText(text);
		}

		void RunCommand(HexEditCommand command, bool shiftDown)
		{
			switch (command)
			{
				case HexEditCommand.File_New: Command_File_New(shiftDown); break;
				case HexEditCommand.File_Open_Open: Command_File_Open_Open(); break;
				case HexEditCommand.File_Open_OpenCopiedCutFiles: Command_File_Open_OpenCopiedCutFiles(); break;
				case HexEditCommand.File_Open_OpenDump: Command_File_Open_OpenDump(); break;
				case HexEditCommand.File_Exit: Close(); break;
			}

			if (ItemTabs.TopMost == null)
				return;

			switch (command)
			{
				case HexEditCommand.File_Save_Save: ItemTabs.TopMost.Command_File_Save_Save(); break;
				case HexEditCommand.File_Save_SaveAs: ItemTabs.TopMost.Command_File_Save_SaveAs(); break;
				case HexEditCommand.File_Close: if (ItemTabs.TopMost.CanClose()) Remove(ItemTabs.TopMost); break;
				case HexEditCommand.File_Refresh: ItemTabs.TopMost.Command_File_Refresh(); break;
				case HexEditCommand.File_Revert: ItemTabs.TopMost.Command_File_Revert(); break;
				case HexEditCommand.File_Copy_CopyPath: ItemTabs.TopMost.Command_File_Copy_CopyPath(); break;
				case HexEditCommand.File_Copy_CopyName: ItemTabs.TopMost.Command_File_Copy_CopyName(); break;
				case HexEditCommand.File_Encoding: ItemTabs.TopMost.Command_File_Encoding(); break;
				case HexEditCommand.File_TextEditor: if (ItemTabs.TopMost.Command_File_TextEditor()) { ItemTabs.Items.Remove(ItemTabs.TopMost); if (ItemTabs.Items.Count == 0) Close(); } break;
				case HexEditCommand.Edit_Undo: ItemTabs.TopMost.Command_Edit_Undo(); break;
				case HexEditCommand.Edit_Redo: ItemTabs.TopMost.Command_Edit_Redo(); break;
				case HexEditCommand.Edit_Cut: ItemTabs.TopMost.Command_Edit_CutCopy(true); break;
				case HexEditCommand.Edit_Copy: ItemTabs.TopMost.Command_Edit_CutCopy(false); break;
				case HexEditCommand.Edit_Paste: ItemTabs.TopMost.Command_Edit_Paste(); break;
				case HexEditCommand.Edit_Find_Find: ItemTabs.TopMost.Command_Edit_Find_Find(shiftDown); break;
				case HexEditCommand.Edit_Find_Next: ItemTabs.TopMost.Command_Edit_FindNextPrev(true, shiftDown); break;
				case HexEditCommand.Edit_Find_Previous: ItemTabs.TopMost.Command_Edit_FindNextPrev(false, shiftDown); break;
				case HexEditCommand.Edit_Goto: ItemTabs.TopMost.Command_Edit_Goto(shiftDown); break;
				case HexEditCommand.Edit_Insert: ItemTabs.TopMost.Command_Edit_Insert(); break;
				case HexEditCommand.View_Values: ItemTabs.TopMost.Command_View_Values(); break;
				case HexEditCommand.Data_Hash_MD5: ItemTabs.TopMost.Command_Data_Hash(Hasher.Type.MD5); break;
				case HexEditCommand.Data_Hash_SHA1: ItemTabs.TopMost.Command_Data_Hash(Hasher.Type.SHA1); break;
				case HexEditCommand.Data_Hash_SHA256: ItemTabs.TopMost.Command_Data_Hash(Hasher.Type.SHA256); break;
				case HexEditCommand.Data_Compress_GZip: ItemTabs.TopMost.Command_Data_Compress(true, Compressor.Type.GZip); break;
				case HexEditCommand.Data_Compress_Deflate: ItemTabs.TopMost.Command_Data_Compress(true, Compressor.Type.Deflate); break;
				case HexEditCommand.Data_Decompress_GZip: ItemTabs.TopMost.Command_Data_Compress(false, Compressor.Type.GZip); break;
				case HexEditCommand.Data_Decompress_Inflate: ItemTabs.TopMost.Command_Data_Compress(false, Compressor.Type.Deflate); break;
				case HexEditCommand.Data_Encrypt_AES: ItemTabs.TopMost.Command_Data_Encrypt(true, Cryptor.Type.AES); break;
				case HexEditCommand.Data_Encrypt_DES: ItemTabs.TopMost.Command_Data_Encrypt(true, Cryptor.Type.DES); break;
				case HexEditCommand.Data_Encrypt_3DES: ItemTabs.TopMost.Command_Data_Encrypt(true, Cryptor.Type.DES3); break;
				case HexEditCommand.Data_Encrypt_RSA: ItemTabs.TopMost.Command_Data_Encrypt(true, Cryptor.Type.RSA); break;
				case HexEditCommand.Data_Encrypt_RSAAES: ItemTabs.TopMost.Command_Data_Encrypt(true, Cryptor.Type.RSAAES); break;
				case HexEditCommand.Data_Decrypt_AES: ItemTabs.TopMost.Command_Data_Encrypt(false, Cryptor.Type.AES); break;
				case HexEditCommand.Data_Decrypt_DES: ItemTabs.TopMost.Command_Data_Encrypt(false, Cryptor.Type.DES); break;
				case HexEditCommand.Data_Decrypt_3DES: ItemTabs.TopMost.Command_Data_Encrypt(false, Cryptor.Type.DES3); break;
				case HexEditCommand.Data_Decrypt_RSA: ItemTabs.TopMost.Command_Data_Encrypt(false, Cryptor.Type.RSA); break;
				case HexEditCommand.Data_Decrypt_RSAAES: ItemTabs.TopMost.Command_Data_Encrypt(false, Cryptor.Type.RSAAES); break;
				case HexEditCommand.Data_Sign_RSA: ItemTabs.TopMost.Command_Data_Sign(true, Cryptor.Type.RSA); break;
				case HexEditCommand.Data_Sign_DSA: ItemTabs.TopMost.Command_Data_Sign(true, Cryptor.Type.DSA); break;
				case HexEditCommand.Data_Verify_RSA: ItemTabs.TopMost.Command_Data_Sign(false, Cryptor.Type.RSA); break;
				case HexEditCommand.Data_Verify_DSA: ItemTabs.TopMost.Command_Data_Sign(false, Cryptor.Type.DSA); break;
				case HexEditCommand.Data_Fill: ItemTabs.TopMost.Command_Data_Fill(); break;
				case HexEditCommand.Models_Define: ItemTabs.TopMost.Command_Models_Define(); break;
				case HexEditCommand.Models_Save: ItemTabs.TopMost.Command_Models_Save(); break;
				case HexEditCommand.Models_Load: ItemTabs.TopMost.Command_Models_Load(); break;
				case HexEditCommand.Models_ExtractData: ItemTabs.TopMost.Command_Models_ExtractData(); break;
			}
		}
	}
}
