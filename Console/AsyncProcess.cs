using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace NeoEdit.Console
{
	class AsyncProcess : IDisposable
	{
		public delegate void DataDelegate(object sender, string data, bool newline);
		public delegate void ExitDelegate(object sender);

		public event DataDelegate StdOutData { add { stdOutData += value; } remove { stdOutData -= value; } }
		public event DataDelegate StdErrData { add { stdErrData += value; } remove { stdErrData -= value; } }
		public event ExitDelegate Exit { add { exit += value; } remove { exit -= value; } }

		DataDelegate stdOutData = (s, t, f) => { };
		DataDelegate stdErrData = (s, t, f) => { };
		ExitDelegate exit = s => { };

		readonly Process process;
		public AsyncProcess(string command)
		{
			process = new Process();
			process.StartInfo.FileName = command;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.EnableRaisingEvents = true;
			process.Exited += process_Exited;
		}

		public void Start()
		{
			process.Start();
			SetupEventHandler(process.StandardOutput, stdOutData);
			SetupEventHandler(process.StandardError, stdErrData);
		}

		void SetupEventHandler(StreamReader reader, DataDelegate handler)
		{
			var eols = new char[] { '\r', '\n' };
			var worker = new BackgroundWorker();
			worker.DoWork += (s, e) =>
			{
				try
				{
					var stream = reader.BaseStream;
					var encoding = reader.CurrentEncoding;

					var buffer = new byte[2048];
					var lastCR = false;
					while (true)
					{
						var block = stream.Read(buffer, 0, buffer.Length);
						if (block == 0)
							break;
						var str = encoding.GetString(buffer, 0, block);
						var index = 0;
						while (index < str.Length)
						{
							var prevCR = lastCR;
							lastCR = false;

							var endIndex = str.IndexOfAny(eols, index);
							var newline = endIndex != -1;
							if (!newline)
								endIndex = str.Length;
							else if ((prevCR) && (index == endIndex) && (str[endIndex] == '\n'))
							{
								++index;
								continue;
							}
							else if (str[endIndex] == '\r')
								lastCR = true;

							handler(this, str.Substring(index, endIndex - index), newline);
							index = endIndex + (newline ? 1 : 0);
						}
					}
				}
				catch { }
				Finished();
			};
			worker.RunWorkerAsync();
		}

		void process_Exited(object sender, EventArgs e)
		{
			Finished();
		}

		object finishLock = new object();
		int finishCount = 0;
		void Finished()
		{
			lock (finishLock)
			{
				if (++finishCount == 3) // StdOut, StdErr, Process all finished
					exit(this);
			}
		}

		public void Dispose()
		{
			process.Dispose();
		}

		public void Write(byte data)
		{
			process.StandardInput.Write(data);
		}
	}
}
