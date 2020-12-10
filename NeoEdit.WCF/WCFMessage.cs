using System;
using System.IO;
using System.Text;

namespace NeoEdit.WCF
{
	static class WCFMessage
	{
		public static string PipeName(int pid) => $"NeoEdit.WCF.Pipe-{{98c475f8-395a-438c-89a9-14901e1c6986}}-{pid}";

		public static string EventName(int pid) => $"NeoEdit.WCF.Event-{{98c475f8-395a-438c-89a9-14901e1c6986}}-{pid}";

		static byte[] ReadAll(Stream stream, int size)
		{
			var buffer = new byte[size];
			var total = 0;
			while (total < size)
			{
				var read = stream.Read(buffer, total, size - total);
				if (read == 0)
					throw new Exception("End of stream");
				total += read;
			}
			return buffer;
		}

		static void WriteAll(Stream stream, byte[] buffer) => stream.Write(buffer, 0, buffer.Length);

		static string[] SplitStrings(byte[] buffer)
		{
			using (var ms = new MemoryStream(buffer))
			{
				var count = BitConverter.ToInt32(ReadAll(ms, sizeof(int)), 0);
				var strs = new string[count];
				for (var ctr = 0; ctr < count; ++ctr)
				{
					var length = BitConverter.ToInt32(ReadAll(ms, sizeof(int)), 0);
					var bytes = ReadAll(ms, length);
					strs[ctr] = Encoding.UTF8.GetString(bytes);
				}
				return strs;
			}
		}

		static byte[] CombineStrings(string[] strs)
		{
			using (var ms = new MemoryStream())
			{
				WriteAll(ms, BitConverter.GetBytes(strs.Length));
				foreach (var str in strs)
				{
					var bytes = Encoding.UTF8.GetBytes(str);
					WriteAll(ms, BitConverter.GetBytes(bytes.Length));
					WriteAll(ms, bytes);
				}
				return ms.ToArray();
			}
		}

		public static string[] GetMessage(Stream stream)
		{
			var length = BitConverter.ToInt32(ReadAll(stream, sizeof(int)), 0);
			var message = ReadAll(stream, length);
			return SplitStrings(message);
		}

		public static void SendMessage(Stream stream, string[] strs)
		{
			var message = CombineStrings(strs);
			WriteAll(stream, BitConverter.GetBytes(message.Length));
			WriteAll(stream, message);
		}
	}
}
