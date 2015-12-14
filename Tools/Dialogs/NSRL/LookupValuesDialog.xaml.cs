using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml.Linq;
using Microsoft.Win32;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Tools.Dialogs.NSRLTool
{
	partial class LookupValuesDialog
	{
		[DepProp]
		public string NSRLFile { get { return UIHelper<LookupValuesDialog>.GetPropValue<string>(this); } set { UIHelper<LookupValuesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string IndexFile { get { return UIHelper<LookupValuesDialog>.GetPropValue<string>(this); } set { UIHelper<LookupValuesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string HashFile { get { return UIHelper<LookupValuesDialog>.GetPropValue<string>(this); } set { UIHelper<LookupValuesDialog>.SetPropValue(this, value); } }

		static LookupValuesDialog() { UIHelper<LookupValuesDialog>.Register(); }

		LookupValuesDialog()
		{
			InitializeComponent();
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			if (String.IsNullOrEmpty(NSRLFile))
				throw new Exception("NSRL file cannot be empty");
			if (String.IsNullOrEmpty(IndexFile))
				throw new Exception("Index file cannot be empty");
			if (String.IsNullOrEmpty(HashFile))
				throw new Exception("Hash file cannot be empty");

			var len = new FileInfo(NSRLFile).Length;
			var data = XElement.Load(IndexFile).Elements("Value").Select(element => Tuple.Create(element.Attribute("Key").Value, long.Parse(element.Attribute("Start").Value))).ToList();
			var offsets = data.Select((item, index) => new { key = item.Item1, start = item.Item2, size = (index == data.Count - 1 ? len : data[index + 1].Item2) - item.Item2 }).ToDictionary(obj => obj.key, obj => Tuple.Create(obj.start, obj.size));

			var hashes = File.ReadAllLines(HashFile);
			var hashCache = new Dictionary<string, string>();
			using (var file = File.OpenRead(NSRLFile))
			{
				foreach (var group in hashes.Select(hash => hash.ToUpperInvariant()).OrderBy().GroupBy(hash => hash.Substring(0, 4)))
				{
					if (offsets.ContainsKey(group.Key))
					{
						var buf = new byte[offsets[group.Key].Item2];
						file.Position = offsets[group.Key].Item1;
						file.Read(buf, 0, buf.Length);
						var lines = Encoding.UTF8.GetString(buf).SplitByLine().ToList();
						var groupHashes = lines.ToDictionary(line => line.SplitTCSV(',').First()[0].ToUpperInvariant(), line => line);
						foreach (var hash in group)
							if (groupHashes.ContainsKey(hash))
								hashCache[hash] = groupHashes[hash];
					}
				}
			}

			var results = hashes.Select(hash => $"{hash}: " + (hashCache.ContainsKey(hash.ToUpperInvariant()) ? hashCache[hash.ToUpperInvariant()] : "Not found")).ToList();
			File.WriteAllLines(HashFile, results);

			MessageBox.Show("Done!");

			DialogResult = true;
		}

		void BrowseNSRLFile(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog
			{
				DefaultExt = "txt",
				Filter = "Text files|*.txt|All files|*.*",
				FilterIndex = 2,
			};
			if (dialog.ShowDialog() != true)
				return;

			NSRLFile = dialog.FileName;
			if (String.IsNullOrWhiteSpace(IndexFile))
				IndexFile = Path.Combine(Path.GetDirectoryName(NSRLFile), Path.GetFileNameWithoutExtension(NSRLFile) + ".idx");
		}

		void BrowseIndexFile(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog
			{
				DefaultExt = "idx",
				Filter = "Text files|*.idx|All files|*.*",
				FilterIndex = 2,
			};
			if (dialog.ShowDialog() != true)
				return;

			IndexFile = dialog.FileName;
		}

		void BrowseHashFile(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog
			{
				DefaultExt = "txt",
				Filter = "Text files|*.txt|All files|*.*",
				FilterIndex = 2,
			};
			if (dialog.ShowDialog() != true)
				return;

			HashFile = dialog.FileName;
		}

		public static void Run() => new LookupValuesDialog().ShowDialog();
	}
}
