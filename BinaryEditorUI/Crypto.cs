using System;
using System.IO;
using System.Security.Cryptography;

namespace NeoEdit.BinaryEditorUI
{
	static class Crypto
	{
		public static string GetRfc2898Key(string password, string salt, int keySize)
		{
			using (var byteGenerator = new Rfc2898DeriveBytes(password, Convert.FromBase64String(salt)))
				return Convert.ToBase64String(byteGenerator.GetBytes(keySize / 8));
		}

		public static byte[] EncryptAES(byte[] data, string key)
		{
			using (var aesAlg = new RijndaelManaged { Key = Convert.FromBase64String(key) })
			using (var encryptor = aesAlg.CreateEncryptor())
			using (var ms = new MemoryStream())
			{
				ms.WriteByte((byte)aesAlg.IV.Length);
				ms.Write(aesAlg.IV, 0, aesAlg.IV.Length);
				var encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);
				ms.Write(encrypted, 0, encrypted.Length);
				return ms.ToArray();
			}
		}

		public static byte[] DecryptAES(byte[] data, string key)
		{
			try
			{
				var iv = new byte[data[0]];
				Array.Copy(data, 1, iv, 0, iv.Length);

				using (var aesAlg = new RijndaelManaged { Key = Convert.FromBase64String(key), IV = iv })
				using (var decryptor = aesAlg.CreateDecryptor())
					return decryptor.TransformFinalBlock(data, iv.Length + 1, data.Length - iv.Length - 1);
			}
			catch (Exception ex) { throw new Exception(String.Format("Decryption failed: {0}", ex.Message), ex); }
		}
	}
}
