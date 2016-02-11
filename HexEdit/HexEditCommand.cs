﻿using System.Windows.Input;
using NeoEdit.GUI.Controls;

namespace NeoEdit.HexEdit
{
	public enum HexEditCommand
	{
		None,
		[KeyGesture(Key.N, ModifierKeys.Control)] [KeyGesture(Key.N, ModifierKeys.Control | ModifierKeys.Shift, false)] File_New,
		[KeyGesture(Key.O, ModifierKeys.Control)] File_Open_Open,
		File_Open_OpenCopiedCutFiles,
		File_Open_OpenDump,
		[KeyGesture(Key.D, ModifierKeys.Control | ModifierKeys.Alt)] File_OpenWith_Disk,
		[KeyGesture(Key.T, ModifierKeys.Control | ModifierKeys.Alt)] File_OpenWith_TextEditor,
		[KeyGesture(Key.S, ModifierKeys.Control)] File_Save_Save,
		File_Save_SaveAs,
		File_Operations_Rename,
		File_Operations_Delete,
		File_Operations_Explore,
		[KeyGesture(Key.F4, ModifierKeys.Control)] File_Close,
		[KeyGesture(Key.F5)] File_Refresh,
		File_Revert,
		File_Copy_CopyPath,
		File_Copy_CopyName,
		File_Encoding,
		File_Exit,
		[KeyGesture(Key.Z, ModifierKeys.Control)] Edit_Undo,
		[KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Shift)] Edit_Redo,
		[KeyGesture(Key.X, ModifierKeys.Control)] Edit_Cut,
		[KeyGesture(Key.C, ModifierKeys.Control)] Edit_Copy,
		[KeyGesture(Key.V, ModifierKeys.Control)] Edit_Paste,
		[KeyGesture(Key.F, ModifierKeys.Control)] [KeyGesture(Key.F, ModifierKeys.Control | ModifierKeys.Shift, false)] Edit_Find_Find,
		[KeyGesture(Key.F3)] [KeyGesture(Key.F3, ModifierKeys.Shift, false)] Edit_Find_Next,
		[KeyGesture(Key.F3, ModifierKeys.Control)] [KeyGesture(Key.F3, ModifierKeys.Control | ModifierKeys.Shift, false)] Edit_Find_Previous,
		[KeyGesture(Key.G, ModifierKeys.Control)] [KeyGesture(Key.G, ModifierKeys.Control | ModifierKeys.Shift, false)] Edit_Goto,
		[KeyGesture(Key.Insert)] Edit_Insert,
		View_Values,
		View_Full,
		View_Grid,
		View_CustomGrid,
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
		[KeyGesture(Key.D, ModifierKeys.Control)] Models_Define,
		Models_Save,
		Models_Load,
		[KeyGesture(Key.E, ModifierKeys.Control)] Models_ExtractData,
	}
}
