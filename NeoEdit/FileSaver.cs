using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program
{
	public static class FileSaver
	{
		readonly static byte[] EncryptedHeader = Encoding.UTF8.GetBytes("\u0000NEAES\u0000");
		readonly static byte[] EncryptedValidate = Encoding.UTF8.GetBytes("\u0000VALID\u0000");
		readonly static byte[] CompressedHeader = Encoding.UTF8.GetBytes("\u0000NEGZIP\u0000");
		readonly static HashSet<string> keyVault = new HashSet<string>();

		public static byte[] Encrypt(byte[] data, string AESKey)
		{
			if (string.IsNullOrEmpty(AESKey))
				return data;

			keyVault.Add(AESKey);
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

		public static void HandleDecrypt(TabsWindow window, ref byte[] bytes, out string AESKey)
		{
			AESKey = null;
			if ((bytes.Length < EncryptedHeader.Length) || (!bytes.Equal(EncryptedHeader, EncryptedHeader.Length)))
				return;

			bytes = bytes.Skip(EncryptedHeader.Length).ToArray();

			foreach (var key in keyVault)
			{
				var output = Decrypt(bytes, key);
				if (output == null)
					continue;

				bytes = output;
				AESKey = key;
				return;
			}

			while (true)
			{
				var key = window.RunCryptorKeyDialog(Cryptor.Type.AES, false);
				if (string.IsNullOrEmpty(key))
					throw new Exception("Failed to decrypt file");

				var output = Decrypt(bytes, key);
				if (output == null)
					continue;

				keyVault.Add(key);
				bytes = output;
				AESKey = key;
				return;
			}
		}

		public static byte[] Compress(byte[] bytes, bool compress)
		{
			if (!compress)
				return bytes;

			return CompressedHeader.Concat(Compressor.Compress(bytes, Compressor.Type.GZip)).ToArray();
		}

		public static byte[] Decompress(byte[] bytes, out bool compressed)
		{
			compressed = (bytes.Length >= CompressedHeader.Length) && (bytes.Equal(CompressedHeader, CompressedHeader.Length));
			if (!compressed)
				return bytes;
			return Compressor.Decompress(bytes.Skip(CompressedHeader.Length).ToArray(), Compressor.Type.GZip);
		}
	}
}
