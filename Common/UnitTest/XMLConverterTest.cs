using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoEdit.Common.Transform;

namespace NeoEdit.Common.UnitTest
{
	public partial class UnitTest
	{
		class XMLConverterGenericBase<T>
		{
			public T Val1, Val2;
		}

		class XMLConverterGenericDerived<T> : XMLConverterGenericBase<T> { }

		class XMLConverterBase
		{
			public int Int1, Int2;
			public int? IntNullable1, IntNullable2;
			public Regex Regex1, Regex2;
			public XMLConverterBase SubClass1, SubClass2, SubClass3;
		}

		class XMLConverterDerived : XMLConverterBase
		{
			public List<XMLConverterBase> Children { get; set; }
			public XMLConverterGenericBase<float?> Generic { get; set; }
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
				Regex1 = new Regex("Pattern", RegexOptions.Compiled | RegexOptions.IgnoreCase),
				Regex2 = null,
				SubClass1 = new XMLConverterBase { Int1 = 3 },
				SubClass2 = new XMLConverterDerived { Int1 = 4 },
				SubClass3 = null,
				Children = new List<XMLConverterBase> { new XMLConverterBase { Int1 = 5 }, new XMLConverterDerived { Int1 = 6 }, null },
				Generic = new XMLConverterGenericDerived<float?> { Val1 = 5.5F, Val2 = null }
			};
			test1.Self = test1;
			var xml = XMLConverter.ToXML(test1);
			var test2 = XMLConverter.FromXML<XMLConverterBase>(xml) as XMLConverterDerived;

			Assert.IsNotNull(test1);
			Assert.IsNotNull(test2);

			Assert.AreEqual(test1.Int1, test2.Int1);
			Assert.AreEqual(test1.Int2, test2.Int2);
			Assert.AreEqual(test1.IntNullable1, test2.IntNullable1);
			Assert.AreEqual(test1.IntNullable2, test2.IntNullable2);
			Assert.AreEqual(test1.Regex1.ToString(), test2.Regex1.ToString());
			Assert.AreEqual(test1.Regex1.Options, test2.Regex1.Options);
			Assert.AreEqual(test1.Regex2, test2.Regex2);
			Assert.AreEqual(test1.SubClass1.GetType(), test2.SubClass1.GetType());
			Assert.AreEqual(test1.SubClass2.GetType(), test2.SubClass2.GetType());
			Assert.AreEqual(test1.SubClass1.Int1, test2.SubClass1.Int1);
			Assert.AreEqual(test1.SubClass2.Int1, test2.SubClass2.Int1);
			Assert.AreEqual(test1.SubClass3, test2.SubClass3);
			Assert.AreEqual(test1.Children[0].GetType(), test2.Children[0].GetType());
			Assert.AreEqual(test1.Children[1].GetType(), test2.Children[1].GetType());
			Assert.AreEqual(test1.Children.Count, test2.Children.Count);
			Assert.AreEqual(test1.Children[0].Int1, test2.Children[0].Int1);
			Assert.AreEqual(test1.Children[1].Int1, test2.Children[1].Int1);
			Assert.AreEqual(test1.Children[2], test2.Children[2]);
			Assert.AreEqual(test1.Generic.GetType(), test2.Generic.GetType());
			Assert.AreEqual(test1.Generic.Val1, test2.Generic.Val1);
			Assert.AreEqual(test1.Generic.Val2, test2.Generic.Val2);
			Assert.IsTrue(test1 == test1.Self);
			Assert.IsTrue(test2 == test2.Self);
		}
	}
}
