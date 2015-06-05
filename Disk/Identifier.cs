using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using NeoEdit.Common.Transform;

namespace NeoEdit.Disk
{
	class Identifier
	{
		static string magicPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Magic");
		static Identifier()
		{
			string Header = typeof(Identifier).Namespace + ".Magic.";
			var resources = typeof(Identifier).Assembly.GetManifestResourceNames().Where(name => name.StartsWith(Header)).ToList();
			Directory.CreateDirectory(magicPath);
			foreach (var resource in resources)
			{
				using (var stream = typeof(Identifier).Assembly.GetManifestResourceStream(resource))
				{
					byte[] data;
					using (var ms = new MemoryStream())
					{
						stream.CopyTo(ms);
						data = ms.ToArray();
					}
					data = Compression.Decompress(Compression.Type.GZip, data);
					var fileName = resource.Substring(Header.Length, resource.Length - Header.Length - ".gz".Length);
					File.WriteAllBytes(Path.Combine(magicPath, fileName), data);
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
