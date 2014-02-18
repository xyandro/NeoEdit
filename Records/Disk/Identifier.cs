using System;
using System.Diagnostics;
using System.IO;

namespace NeoEdit.Records.Disk
{
	class Identifier
	{
		static string magicPath = Path.Combine(Path.GetDirectoryName(typeof(Identifier).Assembly.Location), "Magic");
		static Identifier()
		{
			string Header = "NeoEdit.Magic.";

			Directory.CreateDirectory(magicPath);
			foreach (var file in typeof(Identifier).Assembly.GetManifestResourceNames())
			{
				if (!file.StartsWith(Header))
					continue;

				using (var output = File.OpenWrite(Path.Combine(magicPath, file.Substring(Header.Length))))
				using (var stream = typeof(Identifier).Assembly.GetManifestResourceStream(file))
					stream.CopyTo(output);
			}
		}

		public static string Identify(string fileName)
		{
			var process = new Process
			{
				StartInfo = new ProcessStartInfo(Path.Combine(magicPath, "file.exe"), String.Format("-m \"{0}\" -0 \"{1}\"", Path.Combine(magicPath, "magic.mgc"), fileName))
				{
					UseShellExecute = false,
					RedirectStandardOutput = true,
					CreateNoWindow = true,
				},
			};
			process.Start();
			string result = process.StandardOutput.ReadToEnd();
			process.WaitForExit();

			var idx = result.IndexOf((char)0);
			if (idx == -1)
				throw new Exception("Failed to identify file");

			return result.Substring(idx + 1).Trim();
		}
	}
}
