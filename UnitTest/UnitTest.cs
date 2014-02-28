using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoEdit.GUI.Data;

namespace NeoEdit.GUI.UnitTest
{
	[TestClass]
	public partial class UnitTest
	{
		static UnitTest()
		{
			SmallTestData = Encoding.UTF8.GetBytes("This is my small test string.");
			SmallTestDataSHA1 = Checksum.Get(Checksum.Type.SHA1, SmallTestData);

			var sb = new StringBuilder();
			for (var ctr = 0; ctr < 163840; ++ctr)
				sb.Append("This is my string. It's awesome.");
			LargeTestData = Encoding.UTF8.GetBytes(sb.ToString()); // 5 MB
			LargeTestDataSHA1 = Checksum.Get(Checksum.Type.SHA1, LargeTestData);
		}

		static readonly byte[] SmallTestData;
		static readonly string SmallTestDataSHA1;
		static readonly byte[] LargeTestData;
		static readonly string LargeTestDataSHA1;
	}
}
