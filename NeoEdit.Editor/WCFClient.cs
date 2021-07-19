using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using NeoEdit.WCF;

namespace NeoEdit.Editor
{
	static class WCFClient
	{
		static object lockObj = new object();
		static readonly int PID = Process.GetCurrentProcess().Id;
		static void StartWCFClient()
		{
			using (var eventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset, WCFMessage.EventName(PID), out var created))
			{
				if (created)
				{
					Process.Start(new ProcessStartInfo
					{
						FileName = Path.Combine(Path.GetDirectoryName(typeof(WCFClient).Assembly.Location), @"WCF\NeoEdit.WCF.exe"),
						Arguments = PID.ToString()
					});
				}
				eventWaitHandle.WaitOne();
			}
		}

		static string[] HandleCommand(params string[] command)
		{
			StartWCFClient();
			using (var stream = new NamedPipeClientStream(".", WCFMessage.PipeName(PID), PipeDirection.InOut))
			{
				stream.Connect(500);
				WCFMessage.SendMessage(stream, command);
				var response = WCFMessage.GetMessage(stream);
				switch (response[0])
				{
					case "Success": return response;
					case "Error": throw new Exception(response[1]);
					default: throw new Exception($"Invalid response: {response[0]}");
				}
			}
		}

		public static void StartInterceptCalls(string serviceURL, string interceptURL) => HandleCommand(nameof(StartInterceptCalls), serviceURL, interceptURL);
		public static List<string> EndInterceptCalls(string serviceURL) => HandleCommand(nameof(EndInterceptCalls), serviceURL).Skip(1).ToList();
		public static void ResetClients() => HandleCommand(nameof(ResetClients));
		public static string GetWCFConfig(string serviceURL) => HandleCommand(nameof(GetWCFConfig), serviceURL)[1];
		public static string ExecuteWCF(string str) => HandleCommand(nameof(ExecuteWCF), str)[1];
	}
}
