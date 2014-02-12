using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml.Linq;

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

		static string SerializeRSAParameters(RSAParameters rsaParameters)
		{
			var data = new Dictionary<string, byte[]> {
				{ "D", rsaParameters.D },
				{ "DP", rsaParameters.DP },
				{ "DQ", rsaParameters.DQ },
				{ "Exponent", rsaParameters.Exponent },
				{ "InverseQ", rsaParameters.InverseQ },
				{ "Modulus", rsaParameters.Modulus },
				{ "P", rsaParameters.P },
				{ "Q", rsaParameters.Q },
			};
			var xml = new XElement("RSA", data.Where(entry => entry.Value != null).Select(entry => new XElement(entry.Key, Convert.ToBase64String(entry.Value))));
			return xml.ToString(SaveOptions.DisableFormatting);
		}

		static RSAParameters DeserializeRSAParameters(string xmlStr)
		{
			var xml = XElement.Parse(xmlStr);
			return new RSAParameters
			{
				D = xml.Element("D") == null ? null : Convert.FromBase64String(xml.Element("D").Value),
				DP = xml.Element("DP") == null ? null : Convert.FromBase64String(xml.Element("DP").Value),
				DQ = xml.Element("DQ") == null ? null : Convert.FromBase64String(xml.Element("DQ").Value),
				Exponent = xml.Element("Exponent") == null ? null : Convert.FromBase64String(xml.Element("Exponent").Value),
				InverseQ = xml.Element("InverseQ") == null ? null : Convert.FromBase64String(xml.Element("InverseQ").Value),
				Modulus = xml.Element("Modulus") == null ? null : Convert.FromBase64String(xml.Element("Modulus").Value),
				P = xml.Element("P") == null ? null : Convert.FromBase64String(xml.Element("P").Value),
				Q = xml.Element("Q") == null ? null : Convert.FromBase64String(xml.Element("Q").Value),
			};
		}

		public static string CreateRSAPrivateKey(int keySize)
		{
			var rsa = new RSACryptoServiceProvider(keySize);
			return SerializeRSAParameters(rsa.ExportParameters(true));
		}

		public static string GetRSAPublicKey(string privKey)
		{
			var rsaParameters = DeserializeRSAParameters(privKey);
			var rsa = new RSACryptoServiceProvider();
			rsa.ImportParameters(rsaParameters);
			rsaParameters = rsa.ExportParameters(false);
			return SerializeRSAParameters(rsaParameters);
		}

		public static byte[] EncryptRSA(byte[] data, string pubKey)
		{
			var rsa = new RSACryptoServiceProvider();
			rsa.ImportParameters(DeserializeRSAParameters(pubKey));
			return rsa.Encrypt(data, false);
		}

		public static byte[] DecryptRSA(byte[] data, string privKey)
		{
			var rsa = new RSACryptoServiceProvider();
			rsa.ImportParameters(DeserializeRSAParameters(privKey));
			return rsa.Decrypt(data, false);
		}
	}
}
