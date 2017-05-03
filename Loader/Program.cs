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
				else if (arg.StartsWith("-Password=", StringComparison.OrdinalIgnoreCase))
					config.Password = arg.Substring("-Password=".Length);
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
			return ResourceReader.ResourceHeaders.Select(resourceHeader => Path.Combine(dir, resourceHeader.Name)).Any(file => (file != self) && (File.Exists(file)));
		}

		static void RunExtractor(string[] args)
		{
			SetupPassword();

			if ((args.Length == 4) && (args[0] == "-extractor"))
			{
				var bitDepth = (BitDepths)Enum.Parse(typeof(BitDepths), args[1]);
				var pid = int.Parse(args[2]);
				var fileName = args[3];
				Extractor.RunExtractor(bitDepth, pid, fileName);
			}
			else if ((args.Length == 3) && (args[0] == "-update"))
			{
				var fileName = args[1];
				var pid = int.Parse(args[2]);
				Extractor.RunUpdate(pid, fileName);
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

				// This is after the dialog because it makes the dialog not get focus
				ClearCapsLock();

				switch (action)
				{
					case ExtractActions.Extract: Extractor.Extract(bitDepth); break;
					case ExtractActions.GUI: Extractor.RunProgram(args); break;
				}
			}
			else
				Extractor.RunProgram(args);
		}

		static void SetupPassword()
		{
			if (ResourceReader.Config.Password == null)
				return;

			if (Native.GetConsoleWindow() == IntPtr.Zero)
			{
				Resource.Password = PasswordDialog.Run();
				return;
			}

			Console.Write("Password: ");

			var password = "";
			while (true)
			{
				var key = Console.ReadKey(true);
				if (key.Key == ConsoleKey.Backspace)
				{
					if (password.Length > 0)
					{
						password = password.Remove(password.Length - 1);
						Console.Write("\b \b");
					}
				}
				else if (key.Key == ConsoleKey.Enter)
				{
					if (password.Length > 0)
					{
						Console.WriteLine();
						break;
					}
				}
				else
				{
					Console.Write("*");
					password += key.KeyChar;
				}
			}

			Resource.Password = password;
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
				if (Native.GetConsoleWindow() != IntPtr.Zero)
					Console.WriteLine(ex.Message);
				else
					MessageBox.Show(ex.Message, "Error");
				Environment.Exit(1);
			}
		}
	}
}
