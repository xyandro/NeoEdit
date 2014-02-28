using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace NeoEdit
{
	class Program
	{
		[STAThread]
		static void Main()
		{
			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
			AppDomain.CurrentDomain.ExecuteAssemblyByName("GUI");
		}

		static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			var name = "NeoEdit." + args.Name.Split(',')[0];
			var stream = typeof(Program).Assembly.GetManifestResourceStream(name);
			if (stream == null)
				return null;

			using (var gz = new GZipStream(stream, CompressionMode.Decompress))
			using (var ms = new MemoryStream())
			{
				gz.CopyTo(ms);
				return Assembly.Load(ms.ToArray());
			}
		}
	}
}
