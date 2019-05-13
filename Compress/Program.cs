using System;
using System.IO;
using System.IO.Compression;

namespace Compress
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length != 2)
			{
				Console.WriteLine($"Usage: {Path.GetFileName(typeof(Program).Assembly.Location)} <Input> <Output>");
				return;
			}

			using (var output = File.Create(args[1]))
			using (var gz = new GZipStream(output, CompressionLevel.Optimal, true))
			using (var input = File.OpenRead(args[0]))
				input.CopyTo(gz);
		}
	}
}
