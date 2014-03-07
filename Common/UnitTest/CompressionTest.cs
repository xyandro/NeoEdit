using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoEdit.Common.Transform;

namespace NeoEdit.Common.UnitTest
{
	public partial class UnitTest
	{
		[TestMethod]
		public void SmallCompressionTest()
		{
			var checksum = new Dictionary<Compression.Type, string>
			{
				{ Compression.Type.GZip, "1552138f83454e9026acdbd74fc9ae1c8c0d9656" },
				{ Compression.Type.Deflate, "9e3a8cc0d4d7e26ca28256a7859419255862f9fb" },
			};

			foreach (var type in Helpers.GetValues<Compression.Type>())
			{
				if (type == Compression.Type.None)
					continue;

				var compressed = Compression.Compress(type, SmallTestData);
				var compressedSHA1 = Checksum.Get(Checksum.Type.SHA1, compressed);
				Assert.AreEqual(compressedSHA1, checksum[type]);
				Assert.AreNotEqual(SmallTestDataSHA1, compressedSHA1);

				var decompressed = Compression.Decompress(type, compressed);
				var decompressedSHA1 = Checksum.Get(Checksum.Type.SHA1, decompressed);
				Assert.AreEqual(SmallTestDataSHA1, decompressedSHA1);
			}
		}

		[TestMethod]
		public void LargeCompressionTest()
		{
			var checksum = new Dictionary<Compression.Type, string>
			{
				{ Compression.Type.GZip, "465a4bed533d8c9273c5a0bd8dd8fe76876b2d26" },
				{ Compression.Type.Deflate, "92b5bbdf0c9f7793f4c8c4b1b494d511bead2074" },
			};

			foreach (var type in Helpers.GetValues<Compression.Type>())
			{
				if (type == Compression.Type.None)
					continue;

				var compressed = Compression.Compress(type, LargeTestData);
				var compressedSHA1 = Checksum.Get(Checksum.Type.SHA1, compressed);
				Assert.AreEqual(compressedSHA1, checksum[type]);
				Assert.AreNotEqual(LargeTestDataSHA1, compressedSHA1);

				var decompressed = Compression.Decompress(type, compressed);
				var decompressedSHA1 = Checksum.Get(Checksum.Type.SHA1, decompressed);
				Assert.AreEqual(LargeTestDataSHA1, decompressedSHA1);
			}
		}
	}
}
