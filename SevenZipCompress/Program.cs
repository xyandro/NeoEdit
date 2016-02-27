using System;
using System.Collections.Generic;
using System.IO;
using NeoEdit.SevenZip;

namespace SevenZipCompress
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

			using (var archive = SevenZipArchive.OpenWrite(args[1]))
				archive.Add(args[0], new List<string> { args[0] });
		}
	}
}
