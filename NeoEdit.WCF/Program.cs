using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;

namespace NeoEdit.WCF
{
	class Program
	{
		readonly List<AppDomain> appDomains = new List<AppDomain>();
		readonly Dictionary<string, WCFClient> wcfClients = new Dictionary<string, WCFClient>();

		static void Main(string[] args)
		{
			if ((args.Length != 1) || (!int.TryParse(args[0], out var pid)))
				throw new Exception("Must specify PID when executing");

			new Program().Run(pid);
		}

		void Run(int pid)
		{
			using (var eventWaitHandle = EventWaitHandle.OpenExisting(WCFMessage.EventName(pid)))
			{
				eventWaitHandle.Set();

				new Thread(() => ExitWhenProcessExits(pid)).Start();

				while (true)
				{
					var pipe = new NamedPipeServerStream(WCFMessage.PipeName(pid), PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
					pipe.WaitForConnection();
					new Thread(() => HandlePipe(pipe)).Start();
				}
			}
		}

		void ExitWhenProcessExits(int pid)
		{
			try { Process.GetProcessById(pid).WaitForExit(); }
			catch { }
			Environment.Exit(0);
		}

		void HandlePipe(NamedPipeServerStream stream)
		{
			try
			{
				using (stream)
				{
					var command = WCFMessage.GetMessage(stream);
					var response = HandleCommand(command);
					WCFMessage.SendMessage(stream, response);
				}
			}
			catch { }
		}

		string[] HandleCommand(string[] strs)
		{
			try
			{
				switch (strs[0])
				{
					case "StartInterceptCalls": StartInterceptCalls(strs[1], strs[2]); return new string[] { "Success" };
					case "EndInterceptCalls": return new string[] { "Success" }.Concat(EndInterceptCalls(strs[1])).ToArray();
					case "ResetClients": ResetClients(); return new string[] { "Success" };
					case "GetWCFConfig": return new string[] { "Success", GetWCFConfig(strs[1]) };
					case "ExecuteWCF": return new string[] { "Success", ExecuteWCF(strs[1]) };
					default: throw new Exception($"Invalid command: {strs[0]}");
				}
			}
			catch (Exception ex)
			{
				return new string[] { "Error", ex.Message };
			}
		}

		WCFClient GetWCFClient(string serviceURL)
		{
			if (!wcfClients.ContainsKey(serviceURL))
			{
				var appDomain = AppDomain.CreateDomain(serviceURL);
				try
				{
					wcfClients[serviceURL] = appDomain.CreateInstanceFromAndUnwrap(typeof(WCFClient).Assembly.Location, typeof(WCFClient).FullName) as WCFClient;
					wcfClients[serviceURL].Load(serviceURL);
				}
				catch
				{
					wcfClients.Remove(serviceURL);
					AppDomain.Unload(appDomain);
					throw;
				}
				appDomains.Add(appDomain);
			}

			return wcfClients[serviceURL];
		}

		public void StartInterceptCalls(string serviceURL, string interceptURL) => GetWCFClient(serviceURL).DoStartInterceptCalls(interceptURL);

		public List<string> EndInterceptCalls(string serviceURL) => GetWCFClient(serviceURL).DoEndInterceptCalls();

		public void ResetClients()
		{
			foreach (var appDomain in appDomains)
				AppDomain.Unload(appDomain);
			appDomains.Clear();
			wcfClients.Clear();
		}

		public string GetWCFConfig(string serviceURL) => GetWCFClient(serviceURL).DoGetWCFConfig();

		public string ExecuteWCF(string str)
		{
			str = Regex.Replace(str, @"/\*.*?\*/", "", RegexOptions.Singleline | RegexOptions.Multiline);
			var client = GetWCFClient(JsonConvert.DeserializeObject<WCFOperation>(str).ServiceURL);
			return client.DoExecuteWCF(str);
		}
	}
}
