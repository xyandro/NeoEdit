using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoEdit.Common.Transform;

namespace NeoEdit.Tests
{
	public partial class UnitTest
	{
		class XMLConverterCustomExporter
		{
			public string MyStr { get; set; }
			[XMLConverter.ToXML]
			XElement ToXML() => new XElement("Value", new XElement("Nested", MyStr));
			[XMLConverter.FromXML]
			static XMLConverterCustomExporter FromXML(XElement xml) => new XMLConverterCustomExporter { MyStr = xml.Element("Value")?.Element("Nested")?.Value };
		}

		class XMLConverterGenericBase<T>
		{
			public T Val1, Val2;
			public Dictionary<string, T> Dict1, Dict2, Dict3;
		}

		class XMLConverterGenericDerived<T> : XMLConverterGenericBase<T> { }

		class XMLConverterBase
		{
			public int Int1, Int2;
			public int? IntNullable1, IntNullable2;
			public byte[] ByteArray1, ByteArray2, ByteArray3;
			public int[] IntArray1, IntArray2, IntArray3;
			public List<int> IntList1, IntList2, IntList3;
			public Regex Regex1, Regex2, Regex3;
			public XMLConverterBase SubClass1, SubClass2, SubClass3;
		}

		class XMLConverterDerived : XMLConverterBase
		{
			public List<XMLConverterBase> Children { get; set; }
			public XMLConverterGenericBase<float?> Generic { get; set; }
			public XMLConverterCustomExporter Exporter { get; set; }
			public XMLConverterDerived Self { get; set; }
		}

		[TestMethod]
		public void XMLConverterTest()
		{
			var test1 = new XMLConverterDerived
			{
				Int1 = 1,
				Int2 = default(int),
				IntNullable1 = 2,
				IntNullable2 = null,
				ByteArray1 = new byte[] { 45, 57 },
				ByteArray3 = null,
				IntArray1 = new int[] { 500, 600 },
				IntArray3 = null,
				IntList1 = new List<int> { 500, 600 },
				IntList3 = null,
				Regex1 = new Regex("Pattern", RegexOptions.Compiled | RegexOptions.IgnoreCase),
				Regex3 = null,
				SubClass1 = new XMLConverterBase { Int1 = 3 },
				SubClass2 = new XMLConverterDerived { Int1 = 4 },
				SubClass3 = null,
				Children = new List<XMLConverterBase> { new XMLConverterBase { Int1 = 5 }, new XMLConverterDerived { Int1 = 6 }, null },
				Generic = new XMLConverterGenericDerived<float?>
				{
					Val1 = 5.5F,
					Val2 = null,
					Dict1 = new Dictionary<string, float?>
					{
						["Test1"] = 5.4F,
						["Test2"] = 5.6F,
					},
					Dict3 = null,
				},
				Exporter = new XMLConverterCustomExporter { MyStr = "Test string" },
			};
			test1.ByteArray2 = test1.ByteArray1;
			test1.IntArray2 = test1.IntArray1;
			test1.IntList2 = test1.IntList1;
			test1.Regex2 = test1.Regex1;
			test1.Generic.Dict2 = test1.Generic.Dict1;
			test1.Self = test1;
			var xml = XMLConverter.ToXML(test1);
			var test2 = XMLConverter.FromXML<XMLConverterBase>(xml) as XMLConverterDerived;

			Assert.IsNotNull(test1);
			Assert.IsNotNull(test2);

			Assert.AreEqual(test1.Int1, test2.Int1);
			Assert.AreEqual(test1.Int2, test2.Int2);
			Assert.AreEqual(test1.IntNullable1, test2.IntNullable1);
			Assert.IsNull(test1.IntNullable2);
			Assert.IsNull(test2.IntNullable2);
			Assert.AreEqual(test1.ByteArray1.Length, test2.ByteArray1.Length);
			Assert.AreEqual(test1.ByteArray1[0], test2.ByteArray1[0]);
			Assert.AreEqual(test1.ByteArray1[1], test2.ByteArray1[1]);
			Assert.AreEqual(test1.ByteArray1, test1.ByteArray2);
			Assert.AreEqual(test2.ByteArray1, test2.ByteArray2);
			Assert.IsNull(test1.ByteArray3);
			Assert.IsNull(test2.ByteArray3);
			Assert.AreEqual(test1.IntArray1.Length, test2.IntArray1.Length);
			Assert.AreEqual(test1.IntArray1[0], test2.IntArray1[0]);
			Assert.AreEqual(test1.IntArray1[1], test2.IntArray1[1]);
			Assert.AreEqual(test1.IntArray1, test1.IntArray2);
			Assert.AreEqual(test2.IntArray1, test2.IntArray2);
			Assert.IsNull(test1.IntArray3);
			Assert.IsNull(test2.IntArray3);
			Assert.AreEqual(test1.IntList1.Count, test2.IntList1.Count);
			Assert.AreEqual(test1.IntList1[0], test2.IntList1[0]);
			Assert.AreEqual(test1.IntList1[1], test2.IntList1[1]);
			Assert.AreEqual(test1.IntList1, test1.IntList2);
			Assert.AreEqual(test2.IntList1, test2.IntList2);
			Assert.IsNull(test1.IntList3);
			Assert.IsNull(test2.IntList3);
			Assert.AreEqual(test1.Regex1.ToString(), test2.Regex1.ToString());
			Assert.AreEqual(test1.Regex1.Options, test2.Regex1.Options);
			Assert.AreEqual(test1.Regex1, test1.Regex2);
			Assert.AreEqual(test2.Regex1, test2.Regex2);
			Assert.IsNull(test1.Regex3);
			Assert.IsNull(test2.Regex3);
			Assert.AreEqual(test1.SubClass1.GetType(), test2.SubClass1.GetType());
			Assert.AreEqual(test1.SubClass2.GetType(), test2.SubClass2.GetType());
			Assert.AreEqual(test1.SubClass1.Int1, test2.SubClass1.Int1);
			Assert.AreEqual(test1.SubClass2.Int1, test2.SubClass2.Int1);
			Assert.IsNull(test1.SubClass3);
			Assert.IsNull(test2.SubClass3);
			Assert.AreEqual(test1.Children[0].GetType(), test2.Children[0].GetType());
			Assert.AreEqual(test1.Children[1].GetType(), test2.Children[1].GetType());
			Assert.AreEqual(test1.Children.Count, test2.Children.Count);
			Assert.AreEqual(test1.Children[0].Int1, test2.Children[0].Int1);
			Assert.AreEqual(test1.Children[1].Int1, test2.Children[1].Int1);
			Assert.AreEqual(test1.Children[2], test2.Children[2]);
			Assert.AreEqual(test1.Generic.GetType(), test2.Generic.GetType());
			Assert.AreEqual(test1.Generic.Val1, test2.Generic.Val1);
			Assert.AreEqual(test1.Generic.Val2, test2.Generic.Val2);
			Assert.AreEqual(test1.Generic.Dict1.Count, test2.Generic.Dict1.Count);
			Assert.AreEqual(test1.Generic.Dict1.ContainsKey("Test1"), test2.Generic.Dict1.ContainsKey("Test1"));
			Assert.AreEqual(test1.Generic.Dict1.ContainsKey("Test2"), test2.Generic.Dict1.ContainsKey("Test2"));
			Assert.AreEqual(test1.Generic.Dict1.ContainsKey("Test3"), test2.Generic.Dict1.ContainsKey("Test3"));
			Assert.AreEqual(test1.Generic.Dict1["Test1"], test2.Generic.Dict1["Test1"]);
			Assert.AreEqual(test1.Generic.Dict1["Test2"], test2.Generic.Dict1["Test2"]);
			Assert.AreEqual(test1.Generic.Dict1, test1.Generic.Dict2);
			Assert.AreEqual(test2.Generic.Dict1, test2.Generic.Dict2);
			Assert.IsNull(test1.Generic.Dict3);
			Assert.IsNull(test2.Generic.Dict3);
			Assert.AreEqual(test1.Exporter.MyStr, test2.Exporter.MyStr);
			Assert.AreEqual(test1, test1.Self);
			Assert.AreEqual(test2, test2.Self);
		}
	}
}
