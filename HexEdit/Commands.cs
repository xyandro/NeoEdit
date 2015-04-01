﻿using System.Windows.Input;
using NeoEdit.GUI.Common;

namespace NeoEdit.HexEdit
{
	enum HexEditCommand
	{
		None,
		[KeyGesture(Key.N, ModifierKeys.Control)] [KeyGesture(Key.T, ModifierKeys.Control)] File_New,
		[KeyGesture(Key.O, ModifierKeys.Control)] File_Open,
		File_OpenDump,
		File_OpenCopiedCutFiles,
		[KeyGesture(Key.S, ModifierKeys.Control)] File_Save,
		File_SaveAs,
		[KeyGesture(Key.F4, ModifierKeys.Control)] [KeyGesture(Key.W, ModifierKeys.Control)] File_Close,
		File_Revert,
		File_CopyPath,
		File_CopyName,
		File_Encoding,
		[KeyGesture(Key.T, ModifierKeys.Control | ModifierKeys.Alt)] File_TextEditor,
		File_Exit,
		[KeyGesture(Key.Z, ModifierKeys.Control)] Edit_Undo,
		[KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Shift)] Edit_Redo,
		[KeyGesture(Key.X, ModifierKeys.Control)] Edit_Cut,
		[KeyGesture(Key.C, ModifierKeys.Control)] Edit_Copy,
		[KeyGesture(Key.V, ModifierKeys.Control)] Edit_Paste,
		[KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift)] Edit_ShowClipboard,
		[KeyGesture(Key.F, ModifierKeys.Control)] [KeyGesture(Key.F, ModifierKeys.Control | ModifierKeys.Shift)] Edit_Find,
		[KeyGesture(Key.F3)] [KeyGesture(Key.F3, ModifierKeys.Shift)] Edit_FindNext,
		[KeyGesture(Key.F3, ModifierKeys.Control)] [KeyGesture(Key.F3, ModifierKeys.Control | ModifierKeys.Shift)] Edit_FindPrev,
		[KeyGesture(Key.G, ModifierKeys.Control)] [KeyGesture(Key.G, ModifierKeys.Control | ModifierKeys.Shift)] Edit_Goto,
		[KeyGesture(Key.Insert)] Edit_Insert,
		View_Values,
		[KeyGesture(Key.F5)] View_Refresh,
		View_Tiles,
		Data_Hash_MD5,
		Data_Hash_SHA1,
		Data_Hash_SHA256,
		Data_Compress_GZip,
		Data_Compress_Deflate,
		Data_Decompress_GZip,
		Data_Decompress_Inflate,
		Data_Encrypt_AES,
		Data_Encrypt_DES,
		Data_Encrypt_3DES,
		Data_Encrypt_RSA,
		Data_Encrypt_RSAAES,
		Data_Decrypt_AES,
		Data_Decrypt_DES,
		Data_Decrypt_3DES,
		Data_Decrypt_RSA,
		Data_Decrypt_RSAAES,
		Data_Sign_RSA,
		Data_Sign_DSA,
		Data_Verify_RSA,
		Data_Verify_DSA,
		Data_Fill,
		[KeyGesture(Key.D, ModifierKeys.Control)] Data_Models_Define,
		Data_Models_Save,
		Data_Models_Load,
		[KeyGesture(Key.E, ModifierKeys.Control)] Data_Models_ExtractData,
	}
}
