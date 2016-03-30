using System;
using System.Linq;
using System.Text;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.GUI.Misc
{
	public static class FileEncryptor
	{
		readonly static byte[] EncryptedHeader = Encoding.UTF8.GetBytes("\u0000NEAES\u0000");
		readonly static byte[] EncryptedValidate = Encoding.UTF8.GetBytes("\u0000VALID\u0000");

		public static string GetKey(Window windowParent = null)
		{
			var result = SymmetricKeyDialog.Run(windowParent, Cryptor.Type.AES, true);
			if (result == null)
				return null;
			return result.Key;
		}

		public static byte[] Encrypt(byte[] data, string AESKey)
		{
			if (string.IsNullOrEmpty(AESKey))
				return data;

			return EncryptedHeader.Concat(Cryptor.Encrypt(EncryptedValidate.Concat(data).ToArray(), Cryptor.Type.AES, AESKey)).ToArray();
		}

		static byte[] Decrypt(byte[] data, string key)
		{
			try
			{
				data = Cryptor.Decrypt(data, Cryptor.Type.AES, key);
				if ((data.Length < EncryptedValidate.Length) || (!data.Equal(EncryptedValidate, EncryptedValidate.Length)))
					return null;
				data = data.Skip(EncryptedValidate.Length).ToArray();
				return data;
			}
			catch { return null; }
		}

		public static void HandleDecrypt(ref byte[] bytes, out string AESKey)
		{
			AESKey = null;
			if ((bytes.Length < EncryptedHeader.Length) || (!bytes.Equal(EncryptedHeader, EncryptedHeader.Length)))
				return;

			bytes = bytes.Skip(EncryptedHeader.Length).ToArray();
			var key = GetKey();
			if (string.IsNullOrEmpty(key))
				throw new Exception("Failed to decrypt file");

			var result = Decrypt(bytes, key);
			if (result == null)
				throw new Exception("Failed to decrypt file");

			bytes = result;
			AESKey = key;
		}
	}
}
