using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

		public static string CreateRSAPrivateKey(int keySize)
		{
			return new RSACryptoServiceProvider(keySize).ToXmlString(true);
		}

		public static string GetRSAPublicKey(string privKey)
		{
			var rsa = new RSACryptoServiceProvider();
			rsa.FromXmlString(privKey);
			return rsa.ToXmlString(false);
		}

		public static void GetRSAKeySizeInfo(out IEnumerable<int> keySizes, out int defaultKeySize)
		{
			var keySet = new HashSet<int>();
			var rsa = new RSACryptoServiceProvider();
			defaultKeySize = rsa.KeySize;
			foreach (var keySize in rsa.LegalKeySizes)
			{
				for (var size = 1; size <= keySize.MaxSize; size <<= 1)
					if (size >= keySize.MinSize)
						keySet.Add(size);
			}
			keySizes = keySet.OrderBy(size => size).ToList();
		}

		public static byte[] EncryptRSA(byte[] data, string pubKey)
		{
			var rsa = new RSACryptoServiceProvider();
			rsa.FromXmlString(pubKey);
			return rsa.Encrypt(data, false);
		}

		public static byte[] DecryptRSA(byte[] data, string privKey)
		{
			var rsa = new RSACryptoServiceProvider();
			rsa.FromXmlString(privKey);
			return rsa.Decrypt(data, false);
		}

		public static string SignRSA(byte[] data, string privKey, string hash)
		{
			var rsa = new RSACryptoServiceProvider();
			rsa.FromXmlString(privKey);
			return Convert.ToBase64String(rsa.SignData(data, hash));
		}

		public static bool VerifyRSA(byte[] data, string pubKey, string hash, string signature)
		{
			var rsa = new RSACryptoServiceProvider();
			rsa.FromXmlString(pubKey);
			return rsa.VerifyData(data, hash, Convert.FromBase64String(signature));
		}
	}
}
