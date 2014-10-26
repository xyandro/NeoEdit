﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using Microsoft.Win32;
using NeoEdit.BinaryEditor.Data;
using NeoEdit.Common.Transform;
using NeoEdit.GUI;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.BinaryEditor
{
	public class Tabs : Tabs<BinaryEditor> { }

	public partial class BinaryEditorTabs
	{
		[DepProp]
		public ObservableCollection<BinaryEditor> BinaryEditors { get { return UIHelper<BinaryEditorTabs>.GetPropValue<ObservableCollection<BinaryEditor>>(this); } set { UIHelper<BinaryEditorTabs>.SetPropValue(this, value); } }
		[DepProp]
		public BinaryEditor Active { get { return UIHelper<BinaryEditorTabs>.GetPropValue<BinaryEditor>(this); } set { UIHelper<BinaryEditorTabs>.SetPropValue(this, value); } }
		[DepProp]
		public Tabs.ViewType View { get { return UIHelper<BinaryEditorTabs>.GetPropValue<Tabs.ViewType>(this); } set { UIHelper<BinaryEditorTabs>.SetPropValue(this, value); } }

		static BinaryEditorTabs() { UIHelper<BinaryEditorTabs>.Register(); }

		static BinaryEditorTabs Create(BinaryData data, Coder.Type encoder = Coder.Type.None, string filename = null, string filetitle = null, bool createNew = false)
		{
			var binaryEditorTabs = (!createNew ? UIHelper<BinaryEditorTabs>.GetNewest() : null) ?? new BinaryEditorTabs();
			binaryEditorTabs.Add(new BinaryEditor(data, encoder, filename, filetitle));
			return binaryEditorTabs;
		}

		BinaryEditorTabs()
		{
			BinaryEditMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command));
			InitializeComponent();

			BinaryEditors = new ObservableCollection<BinaryEditor>();
		}

		void Add(BinaryEditor binaryEditor)
		{
			BinaryEditors.Add(binaryEditor);
			Active = binaryEditor;
		}

		public static BinaryEditorTabs CreateFromFile(string filename = null, byte[] bytes = null, Coder.Type encoder = Coder.Type.None, bool createNew = false)
		{
			if (bytes == null)
			{
				if (filename == null)
					bytes = new byte[0];
				else
					bytes = File.ReadAllBytes(filename);
			}
			return Create(new MemoryBinaryData(bytes), encoder, filename, createNew: createNew);
		}

		public static BinaryEditorTabs CreateFromDump(string filename, bool createNew = false)
		{
			return Create(new DumpBinaryData(filename), filename: filename, filetitle: "Dump: ", createNew: createNew);
		}

		public static BinaryEditorTabs CreateFromProcess(int pid, bool createNew = false)
		{
			var process = Process.GetProcessById(pid);
			if (process == null)
				throw new ArgumentException("Process doesn't exist.");
			if (process.Id == Process.GetCurrentProcess().Id)
				throw new ArgumentException("Can't open current process.");
			return Create(new ProcessBinaryData(pid), filetitle: String.Format("Process {0} ({1}) - ", pid, process.ProcessName), createNew: createNew);
		}

		Label GetLabel(BinaryEditor binaryEditor)
		{
			return binaryEditor.GetLabel();
		}

		void Command_File_New()
		{
			Add(new BinaryEditor(new MemoryBinaryData()));
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
				Add(new BinaryEditor(new MemoryBinaryData(File.ReadAllBytes(filename)), filename: filename));
		}

		void Command_File_OpenDump()
		{
			var dialog = new OpenFileDialog();
			if (dialog.ShowDialog() != true)
				return;

			Add(new BinaryEditor(new DumpBinaryData(dialog.FileName), filetitle: "Dump: ", filename: dialog.FileName));
		}

		void Command_File_OpenCopied()
		{
			var files = ClipboardWindow.GetFiles();
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
				Add(new BinaryEditor(new MemoryBinaryData(File.ReadAllBytes(file)), filename: file));
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			var active = Active;
			foreach (var binaryEditor in BinaryEditors)
			{
				Active = binaryEditor;
				if (!binaryEditor.CanClose())
				{
					e.Cancel = true;
					return;
				}
			}
			Active = active;
			BinaryEditors.ToList().ForEach(binaryEditor => binaryEditor.Close());
			base.OnClosing(e);
		}

		void RunCommand(BinaryEditCommand command)
		{
			switch (command)
			{
				case BinaryEditCommand.File_New: Command_File_New(); break;
				case BinaryEditCommand.File_Open: Command_File_Open(); break;
				case BinaryEditCommand.File_OpenDump: Command_File_OpenDump(); break;
				case BinaryEditCommand.File_OpenCopiedCutFiles: Command_File_OpenCopied(); break;
				case BinaryEditCommand.File_Exit: Close(); break;
				case BinaryEditCommand.Edit_ShowClipboard: ClipboardWindow.Show(); break;
				case BinaryEditCommand.View_Tiles: View = View == Tabs.ViewType.Tiles ? Tabs.ViewType.Tabs : Tabs.ViewType.Tiles; break;
			}

			if (Active == null)
				return;

			switch (command)
			{
				case BinaryEditCommand.File_Save: Active.Command_File_Save(); break;
				case BinaryEditCommand.File_SaveAs: Active.Command_File_SaveAs(); break;
				case BinaryEditCommand.File_Close: if (Active.CanClose()) { Active.Close(); BinaryEditors.Remove(Active); } break;
				case BinaryEditCommand.File_CopyPath: Active.Command_File_CopyPath(); break;
				case BinaryEditCommand.File_CopyName: Active.Command_File_CopyName(); break;
				case BinaryEditCommand.File_Encoding_Auto: Active.Command_File_Encode(Coder.Type.None); break;
				case BinaryEditCommand.File_Encoding_UTF8: Active.Command_File_Encode(Coder.Type.UTF8); break;
				case BinaryEditCommand.File_Encoding_UTF7: Active.Command_File_Encode(Coder.Type.UTF7); break;
				case BinaryEditCommand.File_Encoding_UTF16LE: Active.Command_File_Encode(Coder.Type.UTF16LE); break;
				case BinaryEditCommand.File_Encoding_UTF16BE: Active.Command_File_Encode(Coder.Type.UTF16BE); break;
				case BinaryEditCommand.File_Encoding_UTF32LE: Active.Command_File_Encode(Coder.Type.UTF32LE); break;
				case BinaryEditCommand.File_Encoding_UTF32BE: Active.Command_File_Encode(Coder.Type.UTF32BE); break;
				case BinaryEditCommand.File_Encoding_Base64: Active.Command_File_Encode(Coder.Type.Base64); break;
				case BinaryEditCommand.File_Encoding_Hex: Active.Command_File_Encode(Coder.Type.Hex); break;
				case BinaryEditCommand.File_TextEditor: Launcher.Static.LaunchTextEditor(Active.FileName, Active.Data.GetAllBytes(), Active.CoderUsed); BinaryEditors.Remove(Active); if (BinaryEditors.Count == 0) Close(); break;
				case BinaryEditCommand.Edit_Undo: Active.Command_Edit_Undo(); break;
				case BinaryEditCommand.Edit_Redo: Active.Command_Edit_Redo(); break;
				case BinaryEditCommand.Edit_Cut: Active.Command_Edit_Copy(true); break;
				case BinaryEditCommand.Edit_Copy: Active.Command_Edit_Copy(false); break;
				case BinaryEditCommand.Edit_Paste: Active.Command_Edit_Paste(); break;
				case BinaryEditCommand.Edit_Find: Active.Command_Edit_Find(); break;
				case BinaryEditCommand.Edit_FindNext: Active.Command_Edit_FindNextPrev(true); break;
				case BinaryEditCommand.Edit_FindPrevious: Active.Command_Edit_FindNextPrev(false); break;
				case BinaryEditCommand.Edit_Goto: Active.Command_Edit_Goto(); break;
				case BinaryEditCommand.Edit_Insert: Active.Command_Edit_Insert(); break;
				case BinaryEditCommand.View_Values: Active.Command_View_Values(); break;
				case BinaryEditCommand.View_Refresh: Active.Command_View_Refresh(); break;
				case BinaryEditCommand.Data_Checksum_MD5: Active.Command_Checksum(Checksum.Type.MD5); break;
				case BinaryEditCommand.Data_Checksum_SHA1: Active.Command_Checksum(Checksum.Type.SHA1); break;
				case BinaryEditCommand.Data_Checksum_SHA256: Active.Command_Checksum(Checksum.Type.SHA256); break;
				case BinaryEditCommand.Data_Compress_GZip: Active.Command_Compress(true, Compression.Type.GZip); break;
				case BinaryEditCommand.Data_Compress_Deflate: Active.Command_Compress(true, Compression.Type.Deflate); break;
				case BinaryEditCommand.Data_Decompress_GZip: Active.Command_Compress(false, Compression.Type.GZip); break;
				case BinaryEditCommand.Data_Decompress_Inflate: Active.Command_Compress(false, Compression.Type.Deflate); break;
				case BinaryEditCommand.Data_Encrypt_AES: Active.Command_Encrypt(true, Crypto.Type.AES); break;
				case BinaryEditCommand.Data_Encrypt_DES: Active.Command_Encrypt(true, Crypto.Type.DES); break;
				case BinaryEditCommand.Data_Encrypt_3DES: Active.Command_Encrypt(true, Crypto.Type.DES3); break;
				case BinaryEditCommand.Data_Encrypt_RSA: Active.Command_Encrypt(true, Crypto.Type.RSA); break;
				case BinaryEditCommand.Data_Encrypt_RSAAES: Active.Command_Encrypt(true, Crypto.Type.RSAAES); break;
				case BinaryEditCommand.Data_Decrypt_AES: Active.Command_Encrypt(false, Crypto.Type.AES); break;
				case BinaryEditCommand.Data_Decrypt_DES: Active.Command_Encrypt(false, Crypto.Type.DES); break;
				case BinaryEditCommand.Data_Decrypt_3DES: Active.Command_Encrypt(false, Crypto.Type.DES3); break;
				case BinaryEditCommand.Data_Decrypt_RSA: Active.Command_Encrypt(false, Crypto.Type.RSA); break;
				case BinaryEditCommand.Data_Decrypt_RSAAES: Active.Command_Encrypt(false, Crypto.Type.RSAAES); break;
				case BinaryEditCommand.Data_Sign_RSA: Active.Command_Sign(true, Crypto.Type.RSA); break;
				case BinaryEditCommand.Data_Sign_DSA: Active.Command_Sign(true, Crypto.Type.DSA); break;
				case BinaryEditCommand.Data_Verify_RSA: Active.Command_Sign(false, Crypto.Type.RSA); break;
				case BinaryEditCommand.Data_Verify_DSA: Active.Command_Sign(false, Crypto.Type.DSA); break;
				case BinaryEditCommand.Data_Fill: Active.Command_Data_Fill(); break;
			}
		}
	}
}
