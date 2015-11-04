using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoEdit.Common.Transform;

namespace NeoEdit.Common.UnitTest
{
	public partial class UnitTest
	{
		[TestMethod]
		public void CryptoGenerateKeys()
		{
			Assert.AreEqual(Cryptor.GetRfc2898Key("Password", "U2FsdHkgR29vZG5lc3M=", 256), "iSE0XUnVlwuKTn0ZWSJo/xE5sZ1vBERwE/PJQ9VPhow=");

			// Make sure keys generate (no exceptions)
			foreach (var type in Helpers.GetValues<Cryptor.Type>())
			{
				if (type == Cryptor.Type.None)
					continue;

				IEnumerable<int> keySizes;
				int defaultKeySize;
				Cryptor.GetKeySizeInfo(type, out keySizes, out defaultKeySize);
				var privKey = Cryptor.GenerateKey(type, defaultKeySize);
				Assert.IsNotNull(privKey);
				Assert.AreNotEqual(privKey, "");
				var pubKey = Cryptor.GetPublicKey(type, privKey);
				Assert.IsNotNull(pubKey);
				Assert.AreNotEqual(pubKey, "");
			}
		}

		static Dictionary<Cryptor.Type, string> privKeys = new Dictionary<Cryptor.Type, string>
		{
			[Cryptor.Type.AES] = "oIlTERIQEfR5ja6gQwppFs98J2ZBw7x5MXSlWIoqmjg=",
			[Cryptor.Type.DES] = "1j6yNCjUPEE=",
			[Cryptor.Type.DES3] = "osbe2uVjfkWAc1rVBzdjNtmf7WwURyad",
			[Cryptor.Type.RSA] = "<RSAKeyValue><Modulus>1zM5YHtiQuDoT1M+m4poOCXG+Myl6n1UZYY4cZ9nb5upnVfouyg47DV8QnvGcE0BkktQkwqWBQN7UIfaR7YcCD3ojpH26RtqYA8qwB2EAzgAY9JeRDAzmpOH0391y9HeZqRkVOn06rKr9bgW8W7YVjP0Ee2akWKsS31hON6v1XU=</Modulus><Exponent>AQAB</Exponent><P>/PdvcBHtLjv01KEqSw0G4LVDcwJOAoqmIgmXkq1N98/1tgB22S+D6ynHK5Esq5olVgokh5CeECrYKFpPODYsVw==</P><Q>2cfaKKJwLlEyps8GjKc+M12aIyzLBsJ6YISH2wHbolMBgjqTftV2Qi2Z7oJbKieyrNOMSMkbKHOcBdmwjpLtEw==</Q><DP>LAudMKM+a+VunLMvHQA4qVmGU/kbwh+IC7cl2Bkq0TI5cyYji29VhdWxYymU4JAnXhLIB36rtt7u4h/PWVc9Yw==</DP><DQ>pCaFg5dcoyzE1wK41w/ATItDATxkE8ZG47VBiYxO9n1GZ9irEDDpoFwq6KINAStG/AnAdaRP+h+Z/Lbm47BPTQ==</DQ><InverseQ>Jhdscm/25d54aCATBR216iuPLJivbFcyQ9Su5IZ0ipVxF3gBDg6pju3Nl6+KCVTx/utIibnRU6sCKDXdtMeltA==</InverseQ><D>Cl9+uYbZAq5CpGg30VmHkoVHpQCAUuA9rDMfD2MGoluXfm0ZW8Qhkkk1tyKuOwWLONkGhj/xYUZ8sDIMNH6ztR9N3vRA3+mQUkTUrklILTsgHONTxrm2TFX7I1HkCDe1gjZtexmzvh0/KXbAZVuquOR21rGJSaIlTqaTYHgleBU=</D></RSAKeyValue>",
			[Cryptor.Type.DSA] = "<DSAKeyValue><P>3f1ZVEXNtwAyCgJY0sNEKcpiZXzmLixW7ACCogXKhCOMYlIY/2ZzkWO3FQajX3JZNn5IevtzK+xLRC/HjXPboDwnG8NI40YFqmMZIenb1A8GwaqwN3Anhyd4zdfE3jkyuJW0lg0IQv1V3h5uU9p7xopAEuB5FCW22ChTkXu7riU=</P><Q>0Y+FdpHxdkfPf15c261zyxw4ffk=</Q><G>Xd5XzIIZ1htjOenbJqOKrNPQhvBKzYtnjJ1f8KtLRmw1751oGAchspwDbaWMPF8XNoIgd48XDDVCPctaIq3s0R2l86d8/hlfg20QPaBhPwRjG2HnoDxDPSgCngGyTJZd7+hwWN0LQk/jqm1p0QNJd7bprQpX3BkEr/kIPQhs+0k=</G><Y>SI93lbmZ2rRZjlCRYGKTwcqw0lBt0wDBrz/ZvatFsDBCsKcNNX12iI2Q/DmCXQVUADvEbLC/rOE9qyaV37AVGPnLAHLiitFyMX2a7HrgQi0MIr+8n/VLsfqYVIFic82a7uhXAGO/l6/v7xFsDc0Q4RLmtKU2YbV2cW7wCdh8Ihc=</Y><J>AAAAAQ8u7obLw26OjdtXc35rwGWzg+t0AfIfuaTmuirEtVXVEJZ+f5zK4a3yBpH63ZV0LpmfgP4ZCUsbpR1yRO/s8CtN1j9W+gtU06/thJ9RQp8BOPhXNzLVSmK/m96096xK/LFfryUvWu5/eHn4RA==</J><Seed>yGOJ5wemfpoGVc8f5PUOSYYOuQQ=</Seed><PgenCounter>dg==</PgenCounter><X>Dq8WLSQpmlIJqNBEL0cIP29Tz+A=</X></DSAKeyValue>",
			[Cryptor.Type.RSAAES] = "<RSAKeyValue><Modulus>uTbY3Fp9kKo1xi7zeisMe2Y7ArAWZCgJc7GgFCfpOO1GDJ7jZfDCOYA6ObUXwz4FL3Kn+9lhzgimOCgTGsBJComCrILAwDhRDTKMT++HV7HnBltMb4He1NhJtnIIr9V/A7DCh8ylBou9dJGZXcoG4smzOz+Z5ujzzPl44x/CtO0=</Modulus><Exponent>AQAB</Exponent><P>4HQdtUSt/9F0+ehwVtZOgH3y7saU+XR4csEy85abOxG9jpBgfRRjOT1mNXUmdgrNyrfX4gU8/vtMT4KMwIiIEw==</P><Q>0z7kZeTsBkInDSNgm4onJJ7J2cCT7km+FVXv2HDkTTyXY/Ipfwv6mGhN3A792C2bbzUHs/ipDtk3/ytOXOlu/w==</Q><DP>h9e+4yZd6KXsFhQHaYbqm/mePcUSBKfo/grPu307FdT21IGs5AaixtHSOihczrRbOIVrsu9YqzmMRdLNPPlk4Q==</DP><DQ>O166rFSJbTzcYtnlhfFvDOC/1D5GlTsOfEqZzRf1Yiu8VK+zr+w68uilsUpZV4+B1uBtmMwzH2Q3U7TlIkHNGw==</DQ><InverseQ>Pe+Ljg8l3iXSdJhjlvgntx5vzVzngUuZ2yTgbJdx3HN3avbtieX/AXaJZ+kAMVBcAyHMr/5EvXp4Z7r54yE0Mg==</InverseQ><D>Y6X6rzOQcxDgtav1GzmmEY5DGYMbyO8xhql+ctm2RQaDta6WsfYCyWUP7pRcIJNxvsF1V67xTCSjKXuGK9JgePxwKkJptAc7v3OkQq55CQ3QmBz7Pq0V95c2XvHlmdMt6hY535kchpr4xH0k7jSKNiUAl7h9UoiSIEqjyb9VKZE=</D></RSAKeyValue>",
		};

		static Dictionary<Cryptor.Type, string> pubKeys = new Dictionary<Cryptor.Type, string>
		{
			[Cryptor.Type.AES] = "oIlTERIQEfR5ja6gQwppFs98J2ZBw7x5MXSlWIoqmjg=",
			[Cryptor.Type.DES] = "1j6yNCjUPEE=",
			[Cryptor.Type.DES3] = "osbe2uVjfkWAc1rVBzdjNtmf7WwURyad",
			[Cryptor.Type.RSA] = "<RSAKeyValue><Modulus>1zM5YHtiQuDoT1M+m4poOCXG+Myl6n1UZYY4cZ9nb5upnVfouyg47DV8QnvGcE0BkktQkwqWBQN7UIfaR7YcCD3ojpH26RtqYA8qwB2EAzgAY9JeRDAzmpOH0391y9HeZqRkVOn06rKr9bgW8W7YVjP0Ee2akWKsS31hON6v1XU=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>",
			[Cryptor.Type.DSA] = "<DSAKeyValue><P>3f1ZVEXNtwAyCgJY0sNEKcpiZXzmLixW7ACCogXKhCOMYlIY/2ZzkWO3FQajX3JZNn5IevtzK+xLRC/HjXPboDwnG8NI40YFqmMZIenb1A8GwaqwN3Anhyd4zdfE3jkyuJW0lg0IQv1V3h5uU9p7xopAEuB5FCW22ChTkXu7riU=</P><Q>0Y+FdpHxdkfPf15c261zyxw4ffk=</Q><G>Xd5XzIIZ1htjOenbJqOKrNPQhvBKzYtnjJ1f8KtLRmw1751oGAchspwDbaWMPF8XNoIgd48XDDVCPctaIq3s0R2l86d8/hlfg20QPaBhPwRjG2HnoDxDPSgCngGyTJZd7+hwWN0LQk/jqm1p0QNJd7bprQpX3BkEr/kIPQhs+0k=</G><Y>SI93lbmZ2rRZjlCRYGKTwcqw0lBt0wDBrz/ZvatFsDBCsKcNNX12iI2Q/DmCXQVUADvEbLC/rOE9qyaV37AVGPnLAHLiitFyMX2a7HrgQi0MIr+8n/VLsfqYVIFic82a7uhXAGO/l6/v7xFsDc0Q4RLmtKU2YbV2cW7wCdh8Ihc=</Y><J>AAAAAQ8u7obLw26OjdtXc35rwGWzg+t0AfIfuaTmuirEtVXVEJZ+f5zK4a3yBpH63ZV0LpmfgP4ZCUsbpR1yRO/s8CtN1j9W+gtU06/thJ9RQp8BOPhXNzLVSmK/m96096xK/LFfryUvWu5/eHn4RA==</J><Seed>yGOJ5wemfpoGVc8f5PUOSYYOuQQ=</Seed><PgenCounter>dg==</PgenCounter></DSAKeyValue>",
			[Cryptor.Type.RSAAES] = "<RSAKeyValue><Modulus>uTbY3Fp9kKo1xi7zeisMe2Y7ArAWZCgJc7GgFCfpOO1GDJ7jZfDCOYA6ObUXwz4FL3Kn+9lhzgimOCgTGsBJComCrILAwDhRDTKMT++HV7HnBltMb4He1NhJtnIIr9V/A7DCh8ylBou9dJGZXcoG4smzOz+Z5ujzzPl44x/CtO0=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>",
		};

		[TestMethod]
		public void SmallCryptoEncryptTest()
		{
			foreach (var type in Helpers.GetValues<Cryptor.Type>())
			{
				// DSA doesn't encrypt apparently
				if ((type == Cryptor.Type.None) || (type == Cryptor.Type.DSA))
					continue;

				var encrypted = Cryptor.Encrypt(SmallTestData, type, pubKeys[type]);
				var decrypted = Cryptor.Decrypt(encrypted, type, privKeys[type]);
				var decryptedSHA1 = Hasher.Get(decrypted, Hasher.Type.SHA1);

				Assert.AreEqual(SmallTestDataSHA1, decryptedSHA1);
			}
		}

		[TestMethod]
		public void LargeCryptoEncryptTest()
		{
			foreach (var type in Helpers.GetValues<Cryptor.Type>())
			{
				// RSA/DSA not suited for large-scale compression
				if ((type == Cryptor.Type.None) || (type == Cryptor.Type.RSA) || (type == Cryptor.Type.DSA))
					continue;

				var encrypted = Cryptor.Encrypt(LargeTestData, type, pubKeys[type]);
				var decrypted = Cryptor.Decrypt(encrypted, type, privKeys[type]);
				var decryptedSHA1 = Hasher.Get(decrypted, Hasher.Type.SHA1);

				Assert.AreEqual(LargeTestDataSHA1, decryptedSHA1);
			}
		}

		[TestMethod]
		public void SmallCryptoSignTest()
		{
			var correctSignature = new Dictionary<string, string>
			{
				["RSA/SHA1"] = "iilrLSxFCe/SYFoNKDjeomjE/zJoJzrU433NSPJ85TKzFNahbthbhsjijfTfq16shXQabEr5V3IWMQ702dk+GtWx7dgK5qUVr8mXGi3A9ZzGdKxghbY6a308vEC+tr4JD2HUQOY9gW5cQdPxZt7Jkm/9A5W70vdYtE+2h5/X+qs=",
				["RSA/SHA256"] = "YrCf/Qwj3UltmL7XYqIVLC+9xyxiHUy5p4YfSKjl3GKcROPqp4gLsuAE+/vpXIGxr8NBp1bCGWRTAApK0ckBmsyVyzQC/MPEXliXj6qXr/aodLRuWU1m9/oYOttSLg6r4DqEWQbfJjBAdy1GIA3sPhLYRgfz4Sa1VH0IHkSE0so=",
				["RSA/SHA512"] = "dmhEcGFugupRADBMGP8iDaMxwl6e+X3f5Wge3LaJqJVHZDyPP0Mh+mimGgkVGF8hFKcXQTvlAxDBB4Wwia8qNcWkzMoyVhWgUS7DwXzzfplFcp1rvb3PPXFCVFo80j5LYLbWB2TCDL+FP8GhuaLCrR4kSWb5feQUyQ6wCqoW9g4=",
				["RSA/MD5"] = "IilgnaFfbhLhE6u9c6nUPnIpRpiYG0nz9OH3IP1UPEd8AmggrWF2tQx6lwlN6nAEkHoKfW+qwFjXZeANKgPLFiuYygzu3gC97nBuyZEj7kXcSr/5oPstf+g8tzyPSvPUOp7+i4PhfYoDdvIusBMcbeDm1OdN2H57j7kCBYoIT1w=",
			};

			foreach (var type in Helpers.GetValues<Cryptor.Type>())
			{
				if ((type == Cryptor.Type.None) || (type == Cryptor.Type.RSAAES) || (type.IsSymmetric()))
					continue;

				var hashes = type.SigningHashes();
				foreach (var hash in hashes)
				{
					var signature = Cryptor.Sign(SmallTestData, type, privKeys[type], hash);
					if (type != Cryptor.Type.DSA) // DSA generates a new one every time.
						Assert.AreEqual(signature, correctSignature[$"{type}/{hash}"]);
					Assert.IsTrue(Cryptor.Verify(SmallTestData, type, pubKeys[type], hash, signature));
				}
			}
		}

		[TestMethod]
		public void LargeCryptoSignTest()
		{
			var correctSignature = new Dictionary<string, string>
			{
				["RSA/SHA1"] = "Gdx77jzSC6LWUpEPUORd/3srsIJKLd7DLlwigA28bHsrrA1xgrXRpXzgjGZ8K8m4Y0BuhSg+WpCoQAv7hnutlWiHeFHtSVlVR8DQXyHwQEA1uxdEXHcoISGGY0+W0kNizWuqTysMjL8CNcz06/uMGbHanDAiFeq5MUmIBNhGtso=",
				["RSA/SHA256"] = "nt9rbYhDU3svyVf5RioGmDS+R+muXhopC3H8o/ovjLxIZBmSQ5Q5aMkE04h4vsEcTaIgQUiCfFCrGraSx+yFajjGO2I9yEsFU+BHWTqdMSllF6eNfJU1Jiu5gV0CNNwEBiPzPdwmcZ1kZpw1W0Fhr1/dfHPX66iS3TFVsfLh9lo=",
				["RSA/SHA512"] = "L6hHPvnZC5AmCDOAMZMEezVcJB1TG+f2TERIDDdsGvF24bX2dhPYz2hoZL7U9+XlEnXmXX+8chlCndj9/aKx9qL2ia23YOhyk6plKID7aPXiHi5j246/l4wUEMyUUnJ4C3xLVElhzS90hEJqknsCWLgokbDDzoP0SDLVP514b4c=",
				["RSA/MD5"] = "wXJS8uJA98fo/J17HIp1OgvO8PSU7sTHFZKgG/i/9m1hISrZY8p8AlZC7CbxNI9BgP9I1kTHtj6ZnUUU5Kcqi6stKurYQLJiIgCd/2uUd+qy56/XA8ydkYT6TFZ4z7nD1N1VnuAQqWH+/42yeX8x1tV132Kf8ApnSsXnQgY53t8=",
			};

			foreach (var type in Helpers.GetValues<Cryptor.Type>())
			{
				if ((type == Cryptor.Type.None) || (type == Cryptor.Type.RSAAES) || (type.IsSymmetric()))
					continue;

				var hashes = type.SigningHashes();
				foreach (var hash in hashes)
				{
					var signature = Cryptor.Sign(LargeTestData, type, privKeys[type], hash);
					if (type != Cryptor.Type.DSA) // DSA generates a new one every time.
						Assert.AreEqual(signature, correctSignature[$"{type}/{hash}"]);
					Assert.IsTrue(Cryptor.Verify(LargeTestData, type, pubKeys[type], hash, signature));
				}
			}
		}
	}
}
