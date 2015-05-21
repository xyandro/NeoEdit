using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
	public class Tabs : Tabs<HexEditor> { }

	public partial class HexEditTabs
	{
		[DepProp]
		public ObservableCollection<HexEditor> HexEditors { get { return UIHelper<HexEditTabs>.GetPropValue<ObservableCollection<HexEditor>>(this); } set { UIHelper<HexEditTabs>.SetPropValue(this, value); } }
		[DepProp]
		public HexEditor Active { get { return UIHelper<HexEditTabs>.GetPropValue<HexEditor>(this); } set { UIHelper<HexEditTabs>.SetPropValue(this, value); } }
		[DepProp]
		public Tabs.ViewType View { get { return UIHelper<HexEditTabs>.GetPropValue<Tabs.ViewType>(this); } set { UIHelper<HexEditTabs>.SetPropValue(this, value); } }

		static HexEditTabs() { UIHelper<HexEditTabs>.Register(); }

		static void Create(BinaryData data, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, string filename = null, string filetitle = null, bool modified = false, bool createNew = false, HexEditTabs hexEditTabs = null)
		{
			if ((hexEditTabs == null) && (!createNew))
				hexEditTabs = UIHelper<HexEditTabs>.GetNewest();
			if (hexEditTabs == null)
				hexEditTabs = new HexEditTabs();
			hexEditTabs.Add(new HexEditor(data, codePage, filename, filetitle, modified));
		}

		HexEditTabs()
		{
			HexEditMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command, shiftDown));
			InitializeComponent();

			HexEditors = new ObservableCollection<HexEditor>();
		}

		bool shiftDown { get { return (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None; } }
		bool controlDown { get { return (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None; } }

		void Add(HexEditor hexEditor)
		{
			HexEditors.Add(hexEditor);
			Active = hexEditor;
		}

		public static void CreateFromFile(string filename = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, bool modified = false, bool createNew = false)
		{
			if (bytes == null)
			{
				if (filename == null)
					bytes = new byte[0];
				else
					bytes = File.ReadAllBytes(filename);
			}
			Create(new MemoryBinaryData(bytes), codePage, filename, modified: modified, createNew: createNew);
		}

		public static void CreateFromDump(string filename, bool createNew = false)
		{
			Create(new DumpBinaryData(filename), filename: filename, filetitle: "Dump: ", createNew: createNew);
		}

		public static void CreateFromProcess(int pid, bool createNew = false)
		{
			var process = Process.GetProcessById(pid);
			if (process == null)
				throw new ArgumentException("Process doesn't exist.");
			if (process.Id == Process.GetCurrentProcess().Id)
				throw new ArgumentException("Can't open current process.");
			Create(new ProcessBinaryData(pid), filetitle: String.Format("Process {0} ({1}) - ", pid, process.ProcessName), createNew: createNew);
		}

		Label GetLabel(HexEditor hexEditor)
		{
			return hexEditor.GetLabel();
		}

		void Command_File_New(bool newWindow)
		{
			Create(new MemoryBinaryData(), createNew: newWindow, hexEditTabs: newWindow ? null : this);
		}

		void Command_File_Open()
		{
			var dir = Active != null ? Path.GetDirectoryName(Active.FileName) : null;
			var dialog = new OpenFileDialog
			{
				Multiselect = true,
				InitialDirectory = dir,
			};
			if (dialog.ShowDialog() != true)
				return;

			foreach (var filename in dialog.FileNames)
				Add(new HexEditor(new MemoryBinaryData(File.ReadAllBytes(filename)), filename: filename));
		}

		void Command_File_OpenDump()
		{
			var dialog = new OpenFileDialog();
			if (dialog.ShowDialog() != true)
				return;

			Add(new HexEditor(new DumpBinaryData(dialog.FileName), filetitle: "Dump: ", filename: dialog.FileName));
		}

		void Command_File_OpenCopiedCutFiles()
		{
			var files = NEClipboard.GetStrings();
			if ((files == null) || (files.Count < 0))
				return;

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
				Add(new HexEditor(new MemoryBinaryData(File.ReadAllBytes(file)), filename: file));
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			var active = Active;
			foreach (var hexEditor in HexEditors)
			{
				Active = hexEditor;
				if (!hexEditor.CanClose())
				{
					e.Cancel = true;
					return;
				}
			}
			Active = active;
			HexEditors.ToList().ForEach(hexEditor => hexEditor.Close());
			base.OnClosing(e);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Handled)
				return;

			var shiftDown = this.shiftDown;
			var controlDown = this.controlDown;

			e.Handled = HandleKey(e.Key, shiftDown, controlDown);
		}

		internal bool HandleKey(Key key, bool shiftDown, bool controlDown)
		{
			if (Active == null)
				return false;
			return Active.HandleKey(key, shiftDown, controlDown);
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

		internal bool HandleText(string text)
		{
			if (Active == null)
				return false;
			return Active.HandleText(text);
		}

		void RunCommand(HexEditCommand command, bool shiftDown)
		{
			switch (command)
			{
				case HexEditCommand.File_New: Command_File_New(shiftDown); break;
				case HexEditCommand.File_Open: Command_File_Open(); break;
				case HexEditCommand.File_OpenDump: Command_File_OpenDump(); break;
				case HexEditCommand.File_OpenCopiedCutFiles: Command_File_OpenCopiedCutFiles(); break;
				case HexEditCommand.File_Exit: Close(); break;
				case HexEditCommand.View_Tiles: View = View == Tabs.ViewType.Tiles ? Tabs.ViewType.Tabs : Tabs.ViewType.Tiles; break;
			}

			if (Active == null)
				return;

			switch (command)
			{
				case HexEditCommand.File_Save: Active.Command_File_Save(); break;
				case HexEditCommand.File_SaveAs: Active.Command_File_SaveAs(); break;
				case HexEditCommand.File_Close: if (Active.CanClose()) { Active.Close(); HexEditors.Remove(Active); } break;
				case HexEditCommand.File_Revert: Active.Command_File_Revert(); break;
				case HexEditCommand.File_CopyPath: Active.Command_File_CopyPath(); break;
				case HexEditCommand.File_CopyName: Active.Command_File_CopyName(); break;
				case HexEditCommand.File_Encoding: Active.Command_File_Encoding(); break;
				case HexEditCommand.File_TextEditor: if (Active.Command_File_TextEditor()) { HexEditors.Remove(Active); if (HexEditors.Count == 0) Close(); } break;
				case HexEditCommand.Edit_Undo: Active.Command_Edit_Undo(); break;
				case HexEditCommand.Edit_Redo: Active.Command_Edit_Redo(); break;
				case HexEditCommand.Edit_Cut: Active.Command_Edit_CutCopy(true); break;
				case HexEditCommand.Edit_Copy: Active.Command_Edit_CutCopy(false); break;
				case HexEditCommand.Edit_Paste: Active.Command_Edit_Paste(); break;
				case HexEditCommand.Edit_Find: Active.Command_Edit_Find(shiftDown); break;
				case HexEditCommand.Edit_FindNext: Active.Command_Edit_FindNextPrev(true, shiftDown); break;
				case HexEditCommand.Edit_FindPrev: Active.Command_Edit_FindNextPrev(false, shiftDown); break;
				case HexEditCommand.Edit_Goto: Active.Command_Edit_Goto(shiftDown); break;
				case HexEditCommand.Edit_Insert: Active.Command_Edit_Insert(); break;
				case HexEditCommand.View_Values: Active.Command_View_Values(); break;
				case HexEditCommand.View_Refresh: Active.Command_View_Refresh(); break;
				case HexEditCommand.Data_Hash_MD5: Active.Command_Data_Hash(Hash.Type.MD5); break;
				case HexEditCommand.Data_Hash_SHA1: Active.Command_Data_Hash(Hash.Type.SHA1); break;
				case HexEditCommand.Data_Hash_SHA256: Active.Command_Data_Hash(Hash.Type.SHA256); break;
				case HexEditCommand.Data_Compress_GZip: Active.Command_Data_Compress(true, Compression.Type.GZip); break;
				case HexEditCommand.Data_Compress_Deflate: Active.Command_Data_Compress(true, Compression.Type.Deflate); break;
				case HexEditCommand.Data_Decompress_GZip: Active.Command_Data_Compress(false, Compression.Type.GZip); break;
				case HexEditCommand.Data_Decompress_Inflate: Active.Command_Data_Compress(false, Compression.Type.Deflate); break;
				case HexEditCommand.Data_Encrypt_AES: Active.Command_Data_Encrypt(true, Crypto.Type.AES); break;
				case HexEditCommand.Data_Encrypt_DES: Active.Command_Data_Encrypt(true, Crypto.Type.DES); break;
				case HexEditCommand.Data_Encrypt_3DES: Active.Command_Data_Encrypt(true, Crypto.Type.DES3); break;
				case HexEditCommand.Data_Encrypt_RSA: Active.Command_Data_Encrypt(true, Crypto.Type.RSA); break;
				case HexEditCommand.Data_Encrypt_RSAAES: Active.Command_Data_Encrypt(true, Crypto.Type.RSAAES); break;
				case HexEditCommand.Data_Decrypt_AES: Active.Command_Data_Encrypt(false, Crypto.Type.AES); break;
				case HexEditCommand.Data_Decrypt_DES: Active.Command_Data_Encrypt(false, Crypto.Type.DES); break;
				case HexEditCommand.Data_Decrypt_3DES: Active.Command_Data_Encrypt(false, Crypto.Type.DES3); break;
				case HexEditCommand.Data_Decrypt_RSA: Active.Command_Data_Encrypt(false, Crypto.Type.RSA); break;
				case HexEditCommand.Data_Decrypt_RSAAES: Active.Command_Data_Encrypt(false, Crypto.Type.RSAAES); break;
				case HexEditCommand.Data_Sign_RSA: Active.Command_Data_Sign(true, Crypto.Type.RSA); break;
				case HexEditCommand.Data_Sign_DSA: Active.Command_Data_Sign(true, Crypto.Type.DSA); break;
				case HexEditCommand.Data_Verify_RSA: Active.Command_Data_Sign(false, Crypto.Type.RSA); break;
				case HexEditCommand.Data_Verify_DSA: Active.Command_Data_Sign(false, Crypto.Type.DSA); break;
				case HexEditCommand.Data_Fill: Active.Command_Data_Fill(); break;
				case HexEditCommand.Data_Models_Define: Active.Command_Data_Models_Define(); break;
				case HexEditCommand.Data_Models_Save: Active.Command_Data_Models_Save(); break;
				case HexEditCommand.Data_Models_Load: Active.Command_Data_Models_Load(); break;
				case HexEditCommand.Data_Models_ExtractData: Active.Command_Data_Models_ExtractData(); break;
			}
		}
	}
}
