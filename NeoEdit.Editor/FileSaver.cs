﻿using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeoEdit.Common;
using NeoEdit.Common.Transform;

namespace NeoEdit.Editor
{
	public static class FileSaver
	{
		static readonly byte[] EncryptedHeader = Encoding.UTF8.GetBytes("\u0000NEAES\u0000");
		static readonly byte[] EncryptedValidate = Encoding.UTF8.GetBytes("\u0000VALID\u0000");
		static readonly byte[] CompressedHeader = Encoding.UTF8.GetBytes("\u0000NEGZIP\u0000");

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

			var toDecrypt = bytes.Skip(EncryptedHeader.Length).ToArray();
			while (true)
			{
				string key;
				try { key = INEWindowUI.GetDecryptKeyStatic(Cryptor.Type.AES); }
				catch (TaskCanceledException) { return; }
				var output = Decrypt(toDecrypt, key);
				if (output == null)
					continue;

				bytes = output;
				AESKey = key;
				break;
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
