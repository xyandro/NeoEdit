using System;
using System.Net.Sockets;

namespace NetworkTest
{
	class Program
	{
		static void Main(string[] args)
		{
			var port = int.Parse(args[0]);
			var server = TcpListener.Create(port);
			server.Start();
			Console.WriteLine($"Waiting for connections (port {port})...");
			while (true)
			{
				try
				{
					using (var client = server.AcceptTcpClient())
					{
						Console.WriteLine("Got connection!");
						var s = client.GetStream();
						while (true)
						{
							var b = s.ReadByte();
							if (b == -1)
								break;
							if (b == 'q')
							{
								client.Close();
								break;
							}
							Console.WriteLine($"Got {b}, sending {b + 1}");
							s.WriteByte((byte)(b + 1));
						}
						Console.WriteLine("Connection closed.");
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Exception: {ex.Message}");
				}
			}
		}
	}
}
