using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoEdit.Common.Transform;

namespace NeoEdit.Common.UnitTest
{
	public partial class UnitTest
	{
		[TestMethod]
		public void SmallHashTest()
		{
			var correctHash = new Dictionary<Hash.Type, string>
			{
				{ Hash.Type.MD5, "c19068ee30362fe24e76a2c331d77cbe" },
				{ Hash.Type.SHA1, "ed7e5031c215d97cbaa19d021ddf381a86b8d02e" },
				{ Hash.Type.SHA256, "e6066932333b5a9653da045fc9540c9f4b275a29d0f1297d740b176980f28203" },
			};

			foreach (var type in Helpers.GetValues<Hash.Type>())
			{
				if (type == Hash.Type.None)
					continue;

				Assert.AreEqual(Hash.Get(type, SmallTestData), correctHash[type]);
			}
		}

		[TestMethod]
		public void LargeHashTest()
		{
			var correctHash = new Dictionary<Hash.Type, string>
			{
				{ Hash.Type.MD5, "ae015635f4df3f5140679f65e1b1f561" },
				{ Hash.Type.SHA1, "30823c2b6ecac2aa0003ebc29da3aa7ea3a4d97f" },
				{ Hash.Type.SHA256, "bb946c87630d140d9fbdd863867d3b76666984842200131c8eb847cd88e24979" },
			};

			foreach (var type in Helpers.GetValues<Hash.Type>())
			{
				if (type == Hash.Type.None)
					continue;

				Assert.AreEqual(Hash.Get(type, LargeTestData), correctHash[type]);
			}
		}
	}
}
