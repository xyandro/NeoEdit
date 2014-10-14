using System;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace NeoEdit.Console
{
	public class ConsoleRunner
	{
		readonly Semaphore endSem = new Semaphore(0, 1);
		readonly IntPtr console;
		readonly ConsoleRunnerPipe pipe;
		readonly Process process;
		public ConsoleRunner(string pipeName)
		{
			try
			{
				//System.Windows.MessageBox.Show("ConsoleRunner!!!");

				pipe = new ConsoleRunnerPipe(pipeName, false);
				pipe.Read += OnStdIn;

				console = NeoEdit.Win32.Interop.CreateHiddenConsole();

				process = new Process();
				process.StartInfo.FileName = @"E:\Dev\Misc\NeoEdit - Work\Test\bin\Debug\Test.exe";
				//process.StartInfo.FileName = @"C:\Documents\Cpp\NeoEdit - Work\x64\Debug\Test2.exe";
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true;
				process.EnableRaisingEvents = true;
				process.Exited += (s, e) => Finished();
				process.Start();
				SetupEventHandler(process.StandardOutput, ConsoleRunnerPipe.Type.StdOut);
				SetupEventHandler(process.StandardError, ConsoleRunnerPipe.Type.StdErr);
				endSem.WaitOne();
			}
			catch (Exception ex)
			{
				pipe.Send(ConsoleRunnerPipe.Type.StdErr, Encoding.ASCII.GetBytes(ex.Message));
			}
		}

		void OnStdIn(ConsoleRunnerPipe.Type type, byte[] data)
		{
			try
			{
				foreach (var c in data)
					NeoEdit.Win32.Interop.SendChar(console, c);
			}
			catch { Terminate(); }
		}

		void SetupEventHandler(StreamReader reader, ConsoleRunnerPipe.Type type)
		{
			var worker = new BackgroundWorker();
			worker.DoWork += (s, e) =>
			{
				try
				{
					var stream = reader.BaseStream;
					var encoding = reader.CurrentEncoding;

					while (true)
					{
						var buffer = new byte[2048];
						var block = stream.Read(buffer, 0, buffer.Length);
						if (block == 0)
							break;
						Array.Resize(ref buffer, block);
						lock (pipe)
							pipe.Send(type, buffer);
					}
				}
				catch { Terminate(); }
				Finished();
			};
			worker.RunWorkerAsync();
		}

		object finishLock = new object();
		int finishCount = 0;
		void Finished()
		{
			lock (finishLock)
			{
				if (++finishCount == 3) // StdOut, StdErr, Process all finished
					endSem.Release();
			}
		}

		void Terminate()
		{
			process.Kill();
			Environment.Exit(0);
		}
	}
}
