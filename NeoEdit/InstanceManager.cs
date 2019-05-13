using System;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Interop;
using NeoEdit.TextEdit.Transform;

namespace NeoEdit
{
	class InstanceManager
	{
		const string IPCName = "NeoEdit-{1e5bef22-1257-4cbd-a84b-36679ed79b07}";
		const string ShutdownEventName = "NeoEdit-Wait-{0}";

		static MemoryMappedFile mmfile;

		[DllImport("user32.dll", SetLastError = true)]
		static extern bool AllowSetForegroundWindow(int dwProcessId);

		[STAThread]
		static void Main()
		{
			ClearModifierKeys();

			var args = Environment.GetCommandLineArgs().Skip(1).ToList();
			var multi = args.Any(arg => arg == "-multi");

			var masterPid = default(int?);
			try
			{
				if (!multi)
					using (var oldMMFile = MemoryMappedFile.OpenExisting(IPCName))
					using (var va = oldMMFile.CreateViewAccessor())
						masterPid = va.ReadInt32(0);
			}
			catch { }

			if (!masterPid.HasValue)
			{
				var app = new App();
				if (!multi)
				{
					mmfile = MemoryMappedFile.CreateNew(IPCName, 4);
					using (var va = mmfile.CreateViewAccessor())
						va.Write(0, Process.GetCurrentProcess().Id);
					SetupPipeWait(app);
				}
				app.Run();
				return;
			}

			var proc = Process.GetProcessById(masterPid.Value);
			AllowSetForegroundWindow(proc.Id);

			// Server already exists; connect and send command line
			var waitEvent = default(EventWaitHandle);
			if (args.Any(arg => arg == "-wait"))
			{
				args.RemoveAll(arg => arg == "-wait");
				var name = string.Format(ShutdownEventName, Guid.NewGuid());
				args.Add("-wait");
				args.Add(name);
				waitEvent = new EventWaitHandle(false, EventResetMode.ManualReset, name);
			}
			var pipeClient = new NamedPipeClientStream(".", IPCName, PipeDirection.InOut);
			pipeClient.Connect();
			var buf = Coder.StringToBytes(string.Join(" ", args.Select(arg => $"\"{arg.Replace(@"""", @"""""")}\"")), Coder.CodePage.UTF8);
			var size = BitConverter.GetBytes(buf.Length);
			pipeClient.Write(size, 0, size.Length);
			pipeClient.Write(buf, 0, buf.Length);

			while ((waitEvent?.WaitOne(1000) == false) && (!proc.HasExited)) { }
		}

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool SetForegroundWindow(IntPtr hWnd);

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

				app.Dispatcher.Invoke(() =>
				{
					var window = app.CreateWindowsFromArgs(commandLine);
					if (window != null)
					{
						window.Activate();
						window.Show();
						SetForegroundWindow(new WindowInteropHelper(window).Handle);
					}
				});

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
