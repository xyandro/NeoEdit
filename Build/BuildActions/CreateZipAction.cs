using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Build.BuildActions
{
	class CreateZipAction : BaseAction
	{
		public override string Name => "Create zip";

		public List<string> FindFiles(string path)
		{
			var files = new List<string>();
			var paths = new Queue<string>();
			paths.Enqueue(path);
			while (paths.Any())
			{
				path = paths.Dequeue();
				foreach (var dir in Directory.GetDirectories(path))
					paths.Enqueue(dir);
				foreach (var file in Directory.GetFiles(path))
					files.Add(file);
			}
			return files;
		}

		public override void Run(WriteTextDelegate writeText)
		{
			var ignoreExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdb" };
			var path = $@"{App.Location}\NeoEdit\bin\Release\net5.0-windows";
			var files = FindFiles(path);
			files = files.Where(file => !ignoreExtensions.Contains(Path.GetExtension(file))).ToList();
			using var archiveFile = File.Create($@"{App.Location}\NeoEdit.Setup\Release\NeoEdit.zip");
			using var archive = new ZipArchive(archiveFile, ZipArchiveMode.Create);
			foreach (var file in files)
			{
				writeText($"Adding file {file}...");
				archive.CreateEntryFromFile(file, file.Substring(path.Length + 1));
			}
		}
	}
}
