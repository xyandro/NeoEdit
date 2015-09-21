using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoEdit.Common.Transform;

namespace NeoEdit.Common.UnitTest
{
	public partial class UnitTest
	{
		readonly byte[] key = Encoding.UTF8.GetBytes("123456789012345678901234");
		[TestMethod]
		public void SmallHashTest()
		{
			var correctHash = new Dictionary<Hasher.Type, string>
			{
				{ Hasher.Type.MD2, "aa559d38f52f5929a078ba56053d74d1" },
				{ Hasher.Type.MD4, "524b4b5013d193b57f0cbef0b43b2b8c" },
				{ Hasher.Type.MD5, "c19068ee30362fe24e76a2c331d77cbe" },
				{ Hasher.Type.SHA1, "ed7e5031c215d97cbaa19d021ddf381a86b8d02e" },
				{ Hasher.Type.SHA256, "e6066932333b5a9653da045fc9540c9f4b275a29d0f1297d740b176980f28203" },
				{ Hasher.Type.SHA384, "6ed7011416421afc1f1c0bf2a27f2dbef8181aaed00344f796e56261f34a142362870e317689f3de1c1737caba0cfa4e" },
				{ Hasher.Type.SHA512, "f1bd696650488e8c8ce53e96d6ea05d34f55e80483496d0b015e7917c2d8dc0199210abe785f0f7c2fdc69d1fcb1f61d420c978537fc28f5a94e96379db1c193" },
				{ Hasher.Type.HMACMD5, "ea0ac01afa0d5c701238835342c8df66" },
				{ Hasher.Type.HMACRIPEMD160, "4db031d3586f21ee21cbe28b5a818e1235f71c85" },
				{ Hasher.Type.HMACSHA1, "0287346bf1b0d59f1f75f4c22940b5c623df0c82" },
				{ Hasher.Type.HMACSHA256, "43f0f121c21fadf8a17e79a0342fcf1afb8cd055c135eb912b3e5f3e9e7ca983" },
				{ Hasher.Type.HMACSHA384, "8df2622c01588a63e0e1a6443b090bf1941bddc293e555e825c535ec797db1450507d3d44f86eaf38e81881395b93245" },
				{ Hasher.Type.HMACSHA512, "e2684bad1db07a583c529e3767b47f047cf175a9c16cc6749fb61f9daa69e36ae14f510f618dc93f736216c678d6e43a810ce12002d3dd05da2029a07d4bb5b4" },
				{ Hasher.Type.MACTripleDES, "07a3fece2acbcdcb" },
				{ Hasher.Type.RIPEMD160, "2cb680ef7399293856e1681024d348bebfd3cb60" },
				{ Hasher.Type.QuickHash, "527bcbba6be1165bc51417d11e2056fa" },
			};

			foreach (var type in Helpers.GetValues<Hasher.Type>())
			{
				if (type == Hasher.Type.None)
					continue;

				var value = Hasher.Get(SmallTestData, type, key);
				if (correctHash.ContainsKey(type))
					Assert.AreEqual(value, correctHash[type]);
			}
		}

		[TestMethod]
		public void LargeHashTest()
		{
			var correctHash = new Dictionary<Hasher.Type, string>
			{
				{ Hasher.Type.MD2, "2c5b8ed33e40dcc03d5ed11c7d7d5b16" },
				{ Hasher.Type.MD4, "daaf7c75c462beaf306461825cbf3a92" },
				{ Hasher.Type.MD5, "ae015635f4df3f5140679f65e1b1f561" },
				{ Hasher.Type.SHA1, "30823c2b6ecac2aa0003ebc29da3aa7ea3a4d97f" },
				{ Hasher.Type.SHA256, "bb946c87630d140d9fbdd863867d3b76666984842200131c8eb847cd88e24979" },
				{ Hasher.Type.SHA384, "073ccc7a81781fefd861743ce4465cb5aea6bdbd8b9e49b2bbbc0e1c6f9b2a32d9c57463bd72a6478ff9b448b7dbdb5d" },
				{ Hasher.Type.SHA512, "7753e58f7cf5c6560517acdd72d2fb3d280b86f1496424c7d5c2d8a05651fe876fd3c156a65c2a69ab10e54733b7cc48944e0fd877cfe7612f69c0eaf1699b66" },
				{ Hasher.Type.HMACMD5, "abd6f9dd37bdc75a854747735afb8c84" },
				{ Hasher.Type.HMACRIPEMD160, "bf9885ba85ed2939e31425ff644b527f0ffd18f2" },
				{ Hasher.Type.HMACSHA1, "ff4b4b61daf929f02756d0df2b68dbeffd05785d" },
				{ Hasher.Type.HMACSHA256, "c895b74875511082604afa1ecd753a247d036174bfd5bcd65de19a29fb877bc2" },
				{ Hasher.Type.HMACSHA384, "675f7b7e1f9c8059db6f449d9aef07e6d57bd01c1352ea92977e84cc6117f36f1ad20219c08948dfc1d872333d8698f3" },
				{ Hasher.Type.HMACSHA512, "0d1ad01b513e5a9c155b04f0ad8d9befc1824bf130143b04b8f91257af87dc0fd9ef9a7aa0cf8801cc9542b0f645f7290235ac88193c5436619080568e47f36b" },
				{ Hasher.Type.MACTripleDES, "2b2f2b4f5159e1e7" },
				{ Hasher.Type.RIPEMD160, "753bd55a8bfcd5c56fb74a04d9596498778847b9" },
				{ Hasher.Type.QuickHash, "d3fef6970c2473874a1d4745798498f2" },
			};

			foreach (var type in Helpers.GetValues<Hasher.Type>())
			{
				if (type == Hasher.Type.None)
					continue;

				var value = Hasher.Get(LargeTestData, type, key);
				if (correctHash.ContainsKey(type))
					Assert.AreEqual(value, correctHash[type]);
			}
		}
	}
}
