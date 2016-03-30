using System;
using System.IO.Pipes;
using System.Linq;
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
	}
}
