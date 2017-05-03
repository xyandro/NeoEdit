using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Loader
{
	class Builder
	{
		static IEnumerable<string> GetFiles(string startPath)
		{
			if (!Directory.Exists(startPath))
				yield break;

			var paths = new Queue<string>();
			paths.Enqueue(startPath);
			while (paths.Count != 0)
			{
				var path = paths.Dequeue();
				foreach (var file in Directory.EnumerateFiles(path))
					yield return file.Substring(startPath.Length + 1);
				foreach (var dir in Directory.EnumerateDirectories(path))
					paths.Enqueue(dir);
			}
		}

		public static void Run(Config config)
		{
			if (((!File.Exists(config.X32StartFull)) && (!File.Exists(config.X64StartFull))) || (config.Output == null) || (config.Match == null))
				throw new ArgumentException("Invalid parameter");

			var files = GetFiles(config.X32Path).Concat(GetFiles(config.X64Path)).Distinct(StringComparer.OrdinalIgnoreCase).Where(file => config.IsMatch(file)).ToList();
			// Make sure entry points are found
			if (config.X32Start != null)
				if (files.Count(file => file.Equals(config.X32Start, StringComparison.OrdinalIgnoreCase)) != 1)
					throw new Exception("X32Start must exist");
			if (config.X64Start != null)
				if (files.Count(file => file.Equals(config.X64Start, StringComparison.OrdinalIgnoreCase)) != 1)
					throw new Exception("X64Start must exist");

			var loader = typeof(Program).Assembly.Location;
			var bytes = File.ReadAllBytes(loader);
			var loaderPeInfo = new PEInfo(bytes);

			if (config.X64Path == null)
				loaderPeInfo.CorFlags |= Native.IMAGE_COR20_HEADER_FLAGS.x32BitRequired;

			if (config.IsConsole)
				loaderPeInfo.IsConsole = true;

			File.WriteAllBytes(config.Output, bytes);

			Resource.Password = config.Password;
			using (var writer = new ResourceWriter(config.Output))
			{
				var startFile = config.X64StartFull ?? config.X32StartFull;
				writer.CopyResources(new PEInfo(startFile), new List<IntPtr> { Native.RT_ICON, Native.RT_GROUP_ICON, Native.RT_VERSION });

				var currentID = 1;
				foreach (var file in files)
				{
					var x32File = config.X32Path == null ? null : Path.Combine(config.X32Path, file);
					if ((x32File == null) || (x32File.Equals(loader, StringComparison.OrdinalIgnoreCase)) || (!File.Exists(x32File)))
						x32File = null;
					var x32Res = Resource.CreateFromFile(file, x32File, BitDepths.x32);

					var x64File = config.X64Path == null ? null : Path.Combine(config.X64Path, file);
					if ((x64File == null) || (x64File.Equals(loader, StringComparison.OrdinalIgnoreCase)) || (!File.Exists(x64File)))
						x64File = null;
					var x64Res = x64File == x32File ? x32Res : Resource.CreateFromFile(file, x64File, BitDepths.x64);

					if (Resource.DataMatch(x32Res, x64Res))
						x64Res = x32Res;

					if (x32Res == x64Res)
					{
						x32Res.Header.BitDepth = BitDepths.Any;
						x64Res = null;
					}

					if (x32Res != null)
					{
						x32Res.Header.ResourceID = ++currentID;
						writer.AddBinary(x32Res.Header.ResourceID, x32Res.Data);
						config.ResourceHeaders.Add(x32Res.Header);
					}
					if (x64Res != null)
					{
						x64Res.Header.ResourceID = ++currentID;
						writer.AddBinary(x64Res.Header.ResourceID, x64Res.Data);
						config.ResourceHeaders.Add(x64Res.Header);
					}
				}

				writer.AddBinary(1, config.ToBytes());
			}
		}
	}
}
