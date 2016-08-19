using System;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using NeoEdit.Common.Transform;

namespace NeoEdit
{
	class InstanceManager
	{
		const string IPCName = "NeoEdit-{1e5bef22-1257-4cbd-a84b-36679ed79b07}";

		static Mutex mutex = new Mutex(false, IPCName);

		[STAThread]
		static void Main()
		{
			ClearModifierKeys();

			var args = Environment.GetCommandLineArgs();
			var multi = args.Any(arg => arg == "-multi");

			if ((multi) || (mutex.WaitOne(TimeSpan.Zero, true)))
			{
				var app = new App();
				if (!multi)
					SetupPipeWait(app);
				app.Run();
				if (!multi)
					mutex.ReleaseMutex();
				return;
			}

			// Server already exists; connect and send command line
			var pipeClient = new NamedPipeClientStream(".", IPCName, PipeDirection.InOut);
			pipeClient.Connect();
			var buf = Coder.StringToBytes(string.Join(" ", args.Skip(1).Select(arg => $"\"{arg.Replace(@"""", @"""""")}\"")), Coder.CodePage.UTF8);
			var size = BitConverter.GetBytes(buf.Length);
			pipeClient.Write(size, 0, size.Length);
			pipeClient.Write(buf, 0, buf.Length);
		}

		static void SetupPipeWait(App app)
		{
			var pipe = new NamedPipeServerStream(IPCName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
			pipe.BeginWaitForConnection(result =>
			{
				pipe.EndWaitForConnection(result);

				var buf = new byte[sizeof(int)];
				pipe.Read(buf, 0, buf.Length);
				var len = BitConverter.ToInt32(buf, 0);
				buf = new byte[len];
				pipe.Read(buf, 0, buf.Length);
				var commandLine = Coder.BytesToString(buf, Coder.CodePage.UTF8);

				app.Dispatcher.Invoke(() => app.CreateWindowsFromArgs(commandLine));

				SetupPipeWait(app);
			}, null);
		}

		[StructLayout(LayoutKind.Sequential)]
		struct INPUT
		{
			public InputType type;
			[StructLayout(LayoutKind.Sequential)]
			public struct KEYBDINPUT
			{
				public VirtualKeyShort wVk;
				public short wScan;
				public KEYEVENTF dwFlags;
				public int time;
				public UIntPtr dwExtraInfo;
			}
			public KEYBDINPUT ki;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
			public byte[] padding;

			public static int Size
			{
				get { return Marshal.SizeOf(typeof(INPUT)); }
			}
		}

		enum InputType : uint
		{
			KEYBOARD = 1,
		}

		[Flags]
		enum KEYEVENTF : uint
		{
			EXTENDEDKEY = 0x0001,
			KEYUP = 0x0002,
		}

		enum VirtualKeyShort : short
		{
			LSHIFT = 0xA0,
			RSHIFT = 0xA1,
			LCONTROL = 0xA2,
			RCONTROL = 0xA3,
		}

		[DllImport("user32.dll")]
		static extern uint SendInput(int nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

		static void ClearModifierKeys()
		{
			var inputs = new INPUT[4]
			{
				new INPUT { type = InputType.KEYBOARD, ki = new INPUT.KEYBDINPUT { wVk = VirtualKeyShort.LSHIFT, dwFlags = KEYEVENTF.KEYUP } },
				new INPUT { type = InputType.KEYBOARD, ki = new INPUT.KEYBDINPUT { wVk = VirtualKeyShort.RSHIFT, dwFlags = KEYEVENTF.KEYUP } },
				new INPUT { type = InputType.KEYBOARD, ki = new INPUT.KEYBDINPUT { wVk = VirtualKeyShort.LCONTROL, dwFlags = KEYEVENTF.KEYUP } },
				new INPUT { type = InputType.KEYBOARD, ki = new INPUT.KEYBDINPUT { wVk = VirtualKeyShort.RCONTROL, dwFlags = KEYEVENTF.KEYUP | KEYEVENTF.EXTENDEDKEY } },
			};

			SendInput(inputs.Length, inputs, INPUT.Size);
		}
	}
}
