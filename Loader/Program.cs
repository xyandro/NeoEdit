using System;
using System.IO;
using System.Linq;
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
				if (arg.StartsWith("-Start=", StringComparison.OrdinalIgnoreCase))
					config.SetStart(new Uri(new Uri(loader), arg.Substring("-Start=".Length)).LocalPath);
				else if (arg.StartsWith("-Path=", StringComparison.OrdinalIgnoreCase))
					config.SetPath(new Uri(new Uri(loader), arg.Substring("-Path=".Length)).LocalPath);
				else if (arg.StartsWith("-Output=", StringComparison.OrdinalIgnoreCase))
					config.Output = new Uri(new Uri(loader), arg.Substring("-Output=".Length)).LocalPath;
				else if (arg.StartsWith("-Match=", StringComparison.OrdinalIgnoreCase))
					config.Match = arg.Substring("-Match=".Length);
				else if (arg.StartsWith("-ExtractAction=", StringComparison.OrdinalIgnoreCase))
					config.ExtractAction = (ExtractActions)Enum.Parse(typeof(ExtractActions), arg.Substring("-ExtractAction=".Length), true);
				else if (arg.StartsWith("-NGen=", StringComparison.OrdinalIgnoreCase))
					switch (arg.Substring("-NGen=".Length).ToLowerInvariant()[0])
					{
						case '1': case 'y': case 't': config.NGen = true; break;
					}
				else if (arg.Equals("-GO", StringComparison.OrdinalIgnoreCase))
					go = true;
			}

			if ((!go) && (!GetConfig.Run(config)))
				return;

			Builder.Run(config);
		}

		static bool FilesAreExtracted()
		{
			var self = typeof(Extractor).Assembly.Location;
			var dir = Path.GetDirectoryName(self);
			return ResourceReader.Resources.Select(resource => Path.Combine(dir, resource.Name)).Any(file => (file != self) && (File.Exists(file)));
		}

		static void RunExtractor(string[] args)
		{
			var extractor = new Extractor();
			if ((args.Length == 4) && (args[0] == "-extractor"))
			{
				var bitDepth = (BitDepths)Enum.Parse(typeof(BitDepths), args[1]);
				var pid = int.Parse(args[2]);
				var fileName = args[3];
				extractor.RunExtractor(bitDepth, pid, fileName);
			}
			else if ((args.Length == 3) && (args[0] == "-update"))
			{
				var fileName = args[1];
				var pid = int.Parse(args[2]);
				extractor.RunUpdate(pid, fileName);
			}
			else if ((ResourceReader.Config.ExtractAction != ExtractActions.None) && ((Keyboard.GetKeyStates(Key.CapsLock).HasFlag(KeyStates.Down)) || ((args.Length == 1) && (args[0] == "-extract")) || (FilesAreExtracted())))
			{
				var action = ResourceReader.Config.ExtractAction;
				var bitDepth = Environment.Is64BitProcess ? BitDepths.x64 : BitDepths.x32;
				if (action == ExtractActions.GUI)
				{
					var result = Contents.Run();
					action = result?.Item1 ?? ExtractActions.None;
					bitDepth = result?.Item2 ?? bitDepth;
				}

				ClearCapsLock();

				switch (action)
				{
					case ExtractActions.Extract: extractor.Extract(bitDepth); break;
					case ExtractActions.GUI: extractor.RunProgram(args); break;
				}
			}
			else
				extractor.RunProgram(args);
		}

		static void ClearCapsLock()
		{
			if (!Keyboard.GetKeyStates(Key.CapsLock).HasFlag(KeyStates.Toggled))
				return;

			var inputs = new Native.INPUT[]
			{
				new Native.INPUT { type = Native.InputType.KEYBOARD, ki = new Native.INPUT.KEYBDINPUT { wVk = Native.VirtualKeyShort.CAPSLOCK, dwFlags = Native.KEYEVENTF.KEYUP } },
				new Native.INPUT { type = Native.InputType.KEYBOARD, ki = new Native.INPUT.KEYBDINPUT { wVk = Native.VirtualKeyShort.CAPSLOCK, dwFlags = Native.KEYEVENTF.NONE } },
				new Native.INPUT { type = Native.InputType.KEYBOARD, ki = new Native.INPUT.KEYBDINPUT { wVk = Native.VirtualKeyShort.CAPSLOCK, dwFlags = Native.KEYEVENTF.KEYUP } },
			};

			Native.SendInput(inputs.Length, inputs, Native.INPUT.Size);
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
				MessageBox.Show($"{ex.Message}\n\nStack trace:\n{ex.StackTrace}", "Error");
				Environment.Exit(1);
			}
		}
	}
}
