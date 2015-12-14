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

namespace NeoEdit.Tools.Dialogs
{
	partial class CreateNSRLIndexDialog
	{
		[DepProp]
		public string NSRLFile { get { return UIHelper<CreateNSRLIndexDialog>.GetPropValue<string>(this); } set { UIHelper<CreateNSRLIndexDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string IndexFile { get { return UIHelper<CreateNSRLIndexDialog>.GetPropValue<string>(this); } set { UIHelper<CreateNSRLIndexDialog>.SetPropValue(this, value); } }

		static CreateNSRLIndexDialog() { UIHelper<CreateNSRLIndexDialog>.Register(); }

		CreateNSRLIndexDialog()
		{
			InitializeComponent();
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

		IEnumerable<Tuple<string, long>> GetFileLines(string filename, Func<int, bool, bool> progressCallback)
		{
			using (var file = File.OpenRead(filename))
			{
				long blockPos = 0;
				var size = file.Length;
				var buf = new byte[16384];
				var bufSize = 0;

				while (blockPos != size)
				{
					var newBufSize = (int)Math.Min(size - blockPos, buf.Length);
					file.Read(buf, bufSize, newBufSize - bufSize);
					bufSize = newBufSize;
					var pos = 0;

					if (progressCallback((int)(blockPos * 100 / size), false))
					{
						yield return null;
						yield break;
					}

					while (true)
					{
						while ((pos < bufSize) && ((buf[pos] == '\r') || (buf[pos] == '\n')))
							++pos;
						var newLine = Array.FindIndex(buf, pos, bufSize - pos, ch => (ch == '\r') || (ch == '\n'));
						if (newLine == -1)
						{
							if (pos == 0)
								throw new Exception("Cannot find newline");
							Array.Copy(buf, pos, buf, 0, bufSize - pos);
							blockPos += pos;
							bufSize -= pos;
							break;
						}

						yield return Tuple.Create(Encoding.UTF8.GetString(buf, pos, newLine - pos), blockPos + pos);
						pos = newLine;
					}
				}

				progressCallback(100, true);
			}
		}

		const string KeyField = "SHA-1";
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (String.IsNullOrEmpty(NSRLFile))
				throw new Exception("NSRL file cannot be empty");
			if (String.IsNullOrEmpty(IndexFile))
				throw new Exception("Index file cannot be empty");

			// Copy because they'll be used from another thread
			var nsrlFile = NSRLFile;
			var indexFile = IndexFile;
			ProgressDialog.Run(this, "Reading NSRL data...", callback => ReadNSRLData(nsrlFile, indexFile, callback));
		}

		void ReadNSRLData(string nsrlFile, string indexFile, Func<int, bool, bool> callback)
		{
			var headers = default(Dictionary<string, int>);
			var result = new Dictionary<string, long>();
			foreach (var tuple in GetFileLines(nsrlFile, callback))
			{
				if (tuple == null)
					return;

				var fields = tuple.Item1.SplitTCSV(',').First().ToList();
				if (headers == null)
				{
					headers = fields.Select((field, index) => new { field, index }).ToDictionary(obj => obj.field, obj => obj.index);
					continue;
				}

				var key = fields[headers[KeyField]].Substring(0, 4).ToUpperInvariant();
				if (!result.ContainsKey(key))
					result[key] = tuple.Item2;
			}

			var xml = new XElement("Index", result.Select(pair => new XElement("Value", new XAttribute("Key", pair.Key), new XAttribute("Start", pair.Value))));
			xml.Save(indexFile);
		}

		public static void Run() => new CreateNSRLIndexDialog().ShowDialog();
	}
}
