using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using NeoEdit.Common.Transform;

namespace NeoEdit.GUI.Records.Disk
{
	class Identifier
	{
		static string magicPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Magic");
		static Identifier()
		{
			string Header = "NeoEdit.GUI.Magic.";

			Directory.CreateDirectory(magicPath);
			foreach (var file in typeof(Identifier).Assembly.GetManifestResourceNames())
			{
				if (!file.StartsWith(Header))
					continue;

				using (var stream = typeof(Identifier).Assembly.GetManifestResourceStream(file))
				{
					byte[] data;
					using (var ms = new MemoryStream())
					{
						stream.CopyTo(ms);
						data = ms.ToArray();
					}
					data = Compression.Decompress(Compression.Type.GZip, data);
					File.WriteAllBytes(Path.Combine(magicPath, file.Substring(Header.Length)), data);
				}
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
