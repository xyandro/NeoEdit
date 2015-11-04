using System;
using System.ComponentModel;
using System.IO.Pipes;

namespace NeoEdit.Console
{
	class ConsoleRunnerPipe : IDisposable
	{
		public delegate void ReadDelegate(Type type, byte[] data);
		public event ReadDelegate Read { add { read += value; } remove { read -= value; } }
		ReadDelegate read = (t, d) => { };

		public enum Type
		{
			None,
			StdIn,
			StdOut,
			StdErr,
		}

		PipeStream readPipe, writePipe;
		public ConsoleRunnerPipe(string name, bool server)
		{
			if (server)
			{
				readPipe = new NamedPipeServerStream($"{name}-read");
				writePipe = new NamedPipeServerStream($"{name}-write");
			}
			else
			{
				readPipe = new NamedPipeClientStream($"{name}-write");
				writePipe = new NamedPipeClientStream($"{name}-read");
				(readPipe as NamedPipeClientStream).Connect();
				(writePipe as NamedPipeClientStream).Connect();
				SetupReader();
			}
		}

		public void Accept()
		{
			var readServer = readPipe as NamedPipeServerStream;
			var writeServer = writePipe as NamedPipeServerStream;
			if (readServer == null)
				return;

			readServer.WaitForConnection();
			writeServer.WaitForConnection();
			SetupReader();
		}

		public void Send(Type type, byte[] data)
		{
			try
			{
				writePipe.WriteByte((byte)type);
				var lenBytes = BitConverter.GetBytes(data.Length);
				writePipe.Write(lenBytes, 0, lenBytes.Length);
				writePipe.Write(data, 0, data.Length);
				writePipe.Flush();
			}
			catch
			{
			}
		}

		void SetupReader()
		{
			var worker = new BackgroundWorker();
			worker.DoWork += (s, e) =>
			{
				try
				{
					while (true)
					{
						var type = (Type)readPipe.ReadByte();
						var buffer = new byte[4];
						var total = 0;
						while (total < buffer.Length)
						{
							var block = readPipe.Read(buffer, total, buffer.Length - total);
							if (block == 0)
								throw new Exception("Failed to read full block");
							total += block;
						}
						buffer = new byte[BitConverter.ToInt32(buffer, 0)];
						total = 0;
						while (total < buffer.Length)
						{
							var block = readPipe.Read(buffer, total, buffer.Length - total);
							if (block == 0)
								throw new Exception("Failed to read full block");
							total += block;
						}
						read(type, buffer);
					}
				}
				catch
				{
					read(Type.None, null);
				}
			};
			worker.RunWorkerAsync();
		}

		public void Kill()
		{
			readPipe.Close();
			writePipe.Close();
		}

		public void Dispose()
		{
			readPipe.Dispose();
			writePipe.Dispose();
		}
	}
}
