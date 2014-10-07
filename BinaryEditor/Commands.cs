﻿using System.Windows.Input;
using NeoEdit.GUI.Common;

namespace NeoEdit.BinaryEditor
{
	class BinaryEditMenuItem : NEMenuItem<BinaryEditCommand> { }

	enum BinaryEditCommand
	{
		None,
		[Header("_New")] [KeyGesture(Key.N, ModifierKeys.Control)] File_New,
		[Header("_Open")] [KeyGesture(Key.O, ModifierKeys.Control)] File_Open,
		[Header("Open _Dump")] File_OpenDump,
		[Header("Open Copied/Cut Files")] File_OpenCopied,
		[Header("_Save")] [KeyGesture(Key.S, ModifierKeys.Control)] File_Save,
		[Header("Save _As")] File_SaveAs,
		[Header("_Close")] [KeyGesture(Key.W, ModifierKeys.Control)] [KeyGesture(Key.F4, ModifierKeys.Control)] File_Close,
		[Header("Copy _Path")] File_CopyPath,
		[Header("Copy Name")] File_CopyName,
		[Header("Auto")] File_Encode_Auto,
		[Header("UTF8")] File_Encode_UTF8,
		[Header("UTF7")] File_Encode_UTF7,
		[Header("UTF16LE")] File_Encode_UTF16LE,
		[Header("UTF16BE")] File_Encode_UTF16BE,
		[Header("UTF32LE")] File_Encode_UTF32LE,
		[Header("UTF32BE")] File_Encode_UTF32BE,
		[Header("Base64")] File_Encode_Base64,
		[Header("Text Editor")] File_TextEditor,
		[Header("E_xit")] File_Exit,
		[Header("_Undo")] [KeyGesture(Key.Z, ModifierKeys.Control)] Edit_Undo,
		[Header("_Redo")] [KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Shift)] Edit_Redo,
		[Header("Cu_t")] [KeyGesture(Key.X, ModifierKeys.Control)] Edit_Cut,
		[Header("_Copy")] [KeyGesture(Key.C, ModifierKeys.Control)] Edit_Copy,
		[Header("_Paste")] [KeyGesture(Key.V, ModifierKeys.Control)] Edit_Paste,
		[Header("_Show Clipboard")] [KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift)] Edit_ShowClipboard,
		[Header("_Find")] [KeyGesture(Key.F, ModifierKeys.Control)] Edit_Find,
		[Header("Find _Next")] [KeyGesture(Key.F3)] Edit_FindNext,
		[Header("Find _Previous")] [KeyGesture(Key.F3, ModifierKeys.Shift)] Edit_FindPrev,
		[Header("_Goto")] [KeyGesture(Key.G, ModifierKeys.Control)] Edit_Goto,
		[Header("_Insert")] [KeyGesture(Key.Insert)] Edit_Insert,
		[Header("_Values")] View_Values,
		[Header("_Refresh")] [KeyGesture(Key.F5)] View_Refresh,
		[Header("_Tiles")] [KeyGesture(Key.T, ModifierKeys.Control)] View_Tiles,
		[Header("_MD5")] Checksum_MD5,
		[Header("SHA_1")] Checksum_SHA1,
		[Header("SHA_256")] Checksum_SHA256,
		[Header("_GZip")] Compress_GZip,
		[Header("_Deflate")] Compress_Deflate,
		[Header("_GZip")] Decompress_GZip,
		[Header("_Inflate")] Decompress_Inflate,
		[Header("_AES")] Encrypt_AES,
		[Header("_DES")] Encrypt_DES,
		[Header("_3DES")] Encrypt_DES3,
		[Header("_RSA")] Encrypt_RSA,
		[Header("RSA/AES")] Encrypt_RSAAES,
		[Header("_AES")] Decrypt_AES,
		[Header("_DES")] Decrypt_DES,
		[Header("_3DES")] Decrypt_DES3,
		[Header("_RSA")] Decrypt_RSA,
		[Header("RSA/AES")] Decrypt_RSAAES,
		[Header("_RSA")] Sign_RSA,
		[Header("_DSA")] Sign_DSA,
		[Header("_RSA")] Verify_RSA,
		[Header("_DSA")] Verify_DSA,
	}
}
