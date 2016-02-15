using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace Loader
{
	static class Program
	{
		static void BuildLoader(string[] args)
		{
			var config = new Config();

			var loader = typeof(Program).Assembly.Location;

			var go = false;
			foreach (var arg in args)
			{
				if (arg.StartsWith("Start=", StringComparison.OrdinalIgnoreCase))
					config.SetStart(new Uri(new Uri(loader), arg.Substring("Start=".Length)).LocalPath);
				else if (arg.StartsWith("Path=", StringComparison.OrdinalIgnoreCase))
					config.SetPath(new Uri(new Uri(loader), arg.Substring("Path=".Length)).LocalPath);
				else if (arg.StartsWith("Output=", StringComparison.OrdinalIgnoreCase))
					config.Output = new Uri(new Uri(loader), arg.Substring("Output=".Length)).LocalPath;
				else if (arg.StartsWith("Match=", StringComparison.OrdinalIgnoreCase))
					config.Match = arg.Substring("Match=".Length);
				else if (arg.StartsWith("ExtractAction=", StringComparison.OrdinalIgnoreCase))
					config.ExtractAction = (ExtractActions)Enum.Parse(typeof(ExtractActions), arg.Substring("ExtractAction=".Length), true);
				else if (arg.StartsWith("NGen=", StringComparison.OrdinalIgnoreCase))
					switch (arg.Substring("NGen=".Length).ToLowerInvariant()[0])
					{
						case '1': case 'y': case 't': config.NGen = true; break;
					}
				else if (arg.Equals("GO", StringComparison.OrdinalIgnoreCase))
					go = true;
			}

			if ((!go) && (!GetConfig.Run(config)))
				return;

			Builder.Run(config);
		}

		static void RunExtractor(string[] args)
		{
			var extractor = new Extractor();
			if (Path.GetFileNameWithoutExtension(typeof(Program).Assembly.Location).EndsWith(Extractor.ExtractorSuffix))
				extractor.RunExtractor(int.Parse(args[0]), args[1], (BitDepths)Enum.Parse(typeof(BitDepths), args[2]));
			else if ((ResourceReader.Config.ExtractAction != ExtractActions.None) && ((Keyboard.Modifiers.HasFlag(ModifierKeys.Control | ModifierKeys.Shift)) || ((args.Length == 1) && (args[0] == "-extract"))))
			{
				switch (ResourceReader.Config.ExtractAction)
				{
					case ExtractActions.Extract: extractor.Extract(Environment.Is64BitProcess ? BitDepths.x64 : BitDepths.x32); break;
					case ExtractActions.GUI:
						var action = Contents.Run();
						switch (action.Item1)
						{
							case ExtractActions.Extract: extractor.Extract(action.Item2); break;
							case ExtractActions.GUI: extractor.RunProgram(args); break;
						}
						break;
				}
			}
			else
				extractor.RunProgram(args);
		}

		[STAThread]
		static void Main(string[] args)
		{
			try
			{
				if (!ResourceReader.HasData)
					BuildLoader(args);
				else
					RunExtractor(args);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.StackTrace, ex.Message);
				Environment.Exit(1);
			}
		}
	}
}
