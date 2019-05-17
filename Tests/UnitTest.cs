using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoEdit.Common.Transform;

namespace NeoEdit.UnitTest
{
	[TestClass]
	public partial class UnitTest
	{
		static UnitTest()
		{
			SmallTestData = Encoding.UTF8.GetBytes("This is my small test string.");
			SmallTestDataSHA1 = Hasher.Get(SmallTestData, Hasher.Type.SHA1);

			var sb = new StringBuilder();
			for (var ctr = 0; ctr < 163840; ++ctr)
				sb.Append("This is my string. It's awesome.");
			LargeTestData = Encoding.UTF8.GetBytes(sb.ToString()); // 5 MB
			LargeTestDataSHA1 = Hasher.Get(LargeTestData, Hasher.Type.SHA1);
		}

		static readonly byte[] SmallTestData;
		static readonly string SmallTestDataSHA1;
		static readonly byte[] LargeTestData;
		static readonly string LargeTestDataSHA1;
	}
}
