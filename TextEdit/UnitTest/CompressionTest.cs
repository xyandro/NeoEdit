using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoEdit.TextEdit.Transform;

namespace NeoEdit.TextEdit.UnitTest
{
	public partial class UnitTest
	{
		[TestMethod]
		public void SmallCompressionTest()
		{
			var hash = new Dictionary<Compressor.Type, string>
			{
				[Compressor.Type.GZip] = "1552138f83454e9026acdbd74fc9ae1c8c0d9656",
				[Compressor.Type.Deflate] = "9e3a8cc0d4d7e26ca28256a7859419255862f9fb",
			};

			foreach (var type in Helpers.GetValues<Compressor.Type>())
			{
				if (type == Compressor.Type.None)
					continue;

				var compressed = Compressor.Compress(SmallTestData, type);
				var compressedSHA1 = Hasher.Get(compressed, Hasher.Type.SHA1);
				Assert.AreEqual(compressedSHA1, hash[type]);
				Assert.AreNotEqual(SmallTestDataSHA1, compressedSHA1);

				var decompressed = Compressor.Decompress(compressed, type);
				var decompressedSHA1 = Hasher.Get(decompressed, Hasher.Type.SHA1);
				Assert.AreEqual(SmallTestDataSHA1, decompressedSHA1);
			}
		}

		[TestMethod]
		public void LargeCompressionTest()
		{
			var hash = new Dictionary<Compressor.Type, string>
			{
				[Compressor.Type.GZip] = "465a4bed533d8c9273c5a0bd8dd8fe76876b2d26",
				[Compressor.Type.Deflate] = "92b5bbdf0c9f7793f4c8c4b1b494d511bead2074",
			};

			foreach (var type in Helpers.GetValues<Compressor.Type>())
			{
				if (type == Compressor.Type.None)
					continue;

				var compressed = Compressor.Compress(LargeTestData, type);
				var compressedSHA1 = Hasher.Get(compressed, Hasher.Type.SHA1);
				Assert.AreEqual(compressedSHA1, hash[type]);
				Assert.AreNotEqual(LargeTestDataSHA1, compressedSHA1);

				var decompressed = Compressor.Decompress(compressed, type);
				var decompressedSHA1 = Hasher.Get(decompressed, Hasher.Type.SHA1);
				Assert.AreEqual(LargeTestDataSHA1, decompressedSHA1);
			}
		}
	}
}
