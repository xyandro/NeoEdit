using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml.Linq;
using Microsoft.Win32;
using NeoEdit.Common;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Tools.Dialogs.NSRLTool
{
	partial class LookupFilesDialog
	{
		[DepProp]
		public string NSRLFile { get { return UIHelper<LookupFilesDialog>.GetPropValue<string>(this); } set { UIHelper<LookupFilesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string IndexFile { get { return UIHelper<LookupFilesDialog>.GetPropValue<string>(this); } set { UIHelper<LookupFilesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string RootDir { get { return UIHelper<LookupFilesDialog>.GetPropValue<string>(this); } set { UIHelper<LookupFilesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string OutputFile { get { return UIHelper<LookupFilesDialog>.GetPropValue<string>(this); } set { UIHelper<LookupFilesDialog>.SetPropValue(this, value); } }

		static LookupFilesDialog() { UIHelper<LookupFilesDialog>.Register(); }

		LookupFilesDialog()
		{
			InitializeComponent();
			NSRLFile = @"C:\Users\rspackma\Downloads\NSRL\NSRLFile.txt";
			IndexFile = @"C:\Users\rspackma\Downloads\NSRL\NSRLFile.idx";
			RootDir = @"C:\Program Files\Windows Media Player";
			OutputFile = @"C:\Users\rspackma\Downloads\NSRL\Files.txt";
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(NSRLFile))
				throw new Exception("NSRL file cannot be empty");
			if (string.IsNullOrEmpty(IndexFile))
				throw new Exception("Index file cannot be empty");
			if (string.IsNullOrEmpty(RootDir))
				throw new Exception("Root dir cannot be empty");
			if (string.IsNullOrEmpty(OutputFile))
				throw new Exception("Output file cannot be empty");

			var nsrlFile = NSRLFile;
			var indexFile = IndexFile;
			var rootDir = RootDir;

			var files = ProgressDialog.Run(this, "Finding files...", callback => LookupFiles(rootDir, callback));
			var hashes = ProgressDialog.Run(this, "Getting hashes...", callback => LookupHashes(files, callback));
			var results = ProgressDialog.Run(this, "Lookup up NSRL data...", callback => LookupNSRLData(hashes, nsrlFile, indexFile, callback));

			if (results != null)
				File.WriteAllLines(OutputFile, results);

			MessageBox.Show("Done!");

			DialogResult = true;
		}

		List<string> LookupFiles(string rootDir, Func<int, bool, bool> callback)
		{
			var files = new List<string>();
			var dirs = new List<string> { rootDir };
			for (var ctr = 0; ctr < dirs.Count; ++ctr)
			{
				if (callback(ctr * 100 / dirs.Count, false))
					return null;
				try { files.AddRange(Directory.EnumerateFiles(dirs[ctr])); } catch { }
				try { dirs.AddRange(Directory.EnumerateDirectories(dirs[ctr])); } catch { }
			}
			callback(100, true);
			return files;
		}

		List<Tuple<string, string, string>> LookupHashes(List<string> files, Func<int, bool, bool> callback)
		{
			if (files == null)
				return null;
			var results = new List<Tuple<string, string, string>>();
			for (var ctr = 0; ctr < files.Count; ++ctr)
			{
				if (callback(ctr * 100 / files.Count, false))
					return null;
				try { results.Add(Tuple.Create(files[ctr], Hasher.Get(files[ctr], Hasher.Type.SHA1).ToUpperInvariant(), default(string))); }
				catch (Exception ex) { results.Add(Tuple.Create(files[ctr], default(string), $"Error: {ex.Message}")); }
			}
			return results;
		}

		private List<string> LookupNSRLData(List<Tuple<string, string, string>> data, string nsrlFile, string indexFile, Func<int, bool, bool> callback)
		{
			if (data == null)
				return null;

			var len = new FileInfo(nsrlFile).Length;
			var indexData = XElement.Load(indexFile).Elements("Value").Select(element => Tuple.Create(element.Attribute("Key").Value, long.Parse(element.Attribute("Start").Value))).ToList();
			var offsets = indexData.Select((item, index) => new { key = item.Item1, start = item.Item2, size = (index == indexData.Count - 1 ? len : indexData[index + 1].Item2) - item.Item2 }).ToDictionary(obj => obj.key, obj => Tuple.Create(obj.start, obj.size));

			var hashCache = new Dictionary<string, string>();
			var groups = data.Select(tuple => tuple.Item2.ToUpperInvariant()).Where(hash => !string.IsNullOrEmpty(hash)).Distinct().OrderBy().GroupBy(hash => hash.Substring(0, 4)).ToList();
			using (var file = File.OpenRead(nsrlFile))
			{
				for (var ctr = 0; ctr < groups.Count; ++ctr)
				{
					if (callback(ctr * 100 / groups.Count, false))
						return null;

					if (offsets.ContainsKey(groups[ctr].Key))
					{
						var buf = new byte[offsets[groups[ctr].Key].Item2];
						file.Position = offsets[groups[ctr].Key].Item1;
						file.Read(buf, 0, buf.Length);
						var lines = Encoding.UTF8.GetString(buf).SplitByLine().ToList();
						var groupHashes = lines.ToDictionary(line => line.SplitTCSV(',').First()[0].ToUpperInvariant(), line => line);
						foreach (var hash in groups[ctr])
							if (groupHashes.ContainsKey(hash))
								hashCache[hash] = groupHashes[hash];
					}
				}
				callback(100, true);
			}

			var results = new List<string>();
			foreach (var item in data)
			{
				if (string.IsNullOrEmpty(item.Item2))
					results.Add($"{item.Item1}: Error: {item.Item3}");
				else if (hashCache.ContainsKey(item.Item2))
					results.Add($"{item.Item1}: Found: {item.Item2}, {hashCache[item.Item2]}");
				else
					results.Add($"{item.Item1}: Not found: {item.Item2}");
			}

			return results;
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
			if (string.IsNullOrWhiteSpace(IndexFile))
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

		void BrowseRootDir(object sender, RoutedEventArgs e)
		{
			var dialog = new System.Windows.Forms.FolderBrowserDialog();
			if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
				return;

			RootDir = dialog.SelectedPath;
		}

		void BrowseOutputFile(object sender, RoutedEventArgs e)
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
			if (string.IsNullOrWhiteSpace(IndexFile))
				IndexFile = Path.Combine(Path.GetDirectoryName(NSRLFile), Path.GetFileNameWithoutExtension(NSRLFile) + ".idx");
		}

		public static void Run() => new LookupFilesDialog().ShowDialog();
	}
}
