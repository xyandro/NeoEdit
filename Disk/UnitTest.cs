using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NeoEdit.Disk.UnitTest
{
	[TestClass]
	public class UnitTest
	{
		List<DiskItem> GetAllChildren(DiskItem item)
		{
			var result = new List<DiskItem>();
			if (item.CanGetChildren())
			{
				foreach (DiskItem child in item.GetChildren())
				{
					result.Add(child);
					result.AddRange(GetAllChildren(child));
				}
			}
			return result;
		}

		[TestMethod]
		public void TestDisk()
		{
			var testLocation = Path.GetFullPath(Path.Combine(typeof(UnitTest).Assembly.Location, "..", "..", "..", "Disk", "Test"));
			var location = Path.Combine(testLocation, "Test.7z");
			var item = DiskItem.GetRoot().GetChild(location) as DiskItem;
			var children = GetAllChildren(item).OrderBy(child => child.FullName).ToList();
			var childrenDict = children.ToDictionary(file => file.FullName.Substring(location.Length + 1), file => file);

			var xml = XElement.Load(Path.Combine(testLocation, "Files.xml"));
			var files = xml.Elements().ToDictionary(file => file.Attribute("FullName").Value, file => file.Attribute("MD5").Value);

			var extra = childrenDict.Keys.Where(key => !files.ContainsKey(key)).ToList();
			Assert.AreEqual(extra.Count, 0);

			var missing = files.Keys.Where(key => !childrenDict.ContainsKey(key)).ToList();
			Assert.AreEqual(missing.Count, 0);

			foreach (var entry in files)
			{
				if (String.IsNullOrEmpty(entry.Value))
					continue;

				item = childrenDict[entry.Key];
				item.CalcMD5();
				Assert.AreEqual(item.MD5, entry.Value);
			}
		}
	}
}
