using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Build.BuildActions
{
	class LoaderAction : BaseAction
	{
		public override string Name => "Loader";

		public override void Run(WriteTextDelegate writeText, string configuration, List<string> platforms)
		{
			writeText($"Building loader for {configuration} platforms {string.Join(", ", platforms)}...");

			var loader = $@"{App.Location}\bin\{configuration}.AnyCPU\Loader.exe";
			if (!File.Exists(loader))
				throw new Exception($"Loader ({loader}) not found.");

			var files = platforms.Select(platform => $@"{App.Location}\bin\{configuration}.{platform}\NeoEdit.exe").ToList();
			var missing = files.Where(file => !File.Exists(file)).ToList();
			if (missing.Any())
				throw new Exception("Missing files:\n" + string.Join("\n", missing));

			var fileList = string.Join(" ", files.Select(file => $@"-Start=""{file}"""));
			var output = $@"{App.Location}\Release\NeoEdit.exe";
			Directory.CreateDirectory(Path.GetDirectoryName(output));

			var arguments = $@"{fileList} -output=""{output}"" -ngen=1 -extractaction=gui -go";
			RunCommand(writeText, loader, arguments);
		}
	}
}
