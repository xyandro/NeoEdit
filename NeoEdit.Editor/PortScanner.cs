using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using NeoEdit.Common;
using NeoEdit.Common.Parsing;

namespace NeoEdit.Editor
{
	class PortScanner
	{
		const int SOCKET_ERROR = -1;
		const int SOCK_STREAM = 1;
		const int IPPROTO_TCP = 6;
		const int WSAEWOULDBLOCK = 10035;
		const int FIONBIO = -2147195266;

		[StructLayout(LayoutKind.Sequential)]
		internal struct sockaddr_in
		{
			internal short sin_family;
			internal ushort sin_port;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			internal byte[] sin_addr;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
			internal byte[] sin_zero;

			public sockaddr_in(IPAddress address, int port)
			{
				sin_family = checked((short)address.AddressFamily);
				sin_port = htons(checked((ushort)port));
				sin_addr = address.GetAddressBytes();
				sin_zero = new byte[8];
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct sockaddr_in6
		{
			internal short sin6_family;
			internal ushort sin6_port;
			internal uint sin6_flowinfo;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			internal byte[] sin6_addr;
			internal uint sin6_scope_id;

			public sockaddr_in6(IPAddress address, int port)
			{
				sin6_family = checked((short)address.AddressFamily);
				sin6_port = htons(checked((ushort)port));
				sin6_flowinfo = 0;
				sin6_addr = address.GetAddressBytes();
				sin6_scope_id = htonl((uint)address.ScopeId);
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct timeval
		{
			public int tv_sec;
			public int tv_usec;

			public timeval(TimeSpan timespan)
			{
				var milliseconds = Math.Max(0, (long)timespan.TotalMilliseconds);
				tv_sec = (int)(milliseconds / 1000);
				tv_usec = (int)(milliseconds % 1000 * 1000);
			}
		};

		[DllImport("ws2_32.dll")]
		public static extern IntPtr socket(int af, int socket_type, int protocol);

		[DllImport("Ws2_32.dll")]
		public static extern int ioctlsocket(IntPtr s, int cmd, ref int argp);

		[DllImport("Ws2_32.dll")]
		public static extern ushort htons(ushort hostshort);

		[DllImport("Ws2_32.dll")]
		public static extern uint htonl(uint hostlong);

		[DllImport("Ws2_32.dll")]
		public static extern int connect(IntPtr s, sockaddr_in addr, int addrsize);

		[DllImport("Ws2_32.dll")]
		public static extern int connect(IntPtr s, sockaddr_in6 addr, int addrsize);

		[DllImport("ws2_32.dll")]
		static extern int WSAGetLastError();

		[DllImport("Ws2_32.dll")]
		public static extern int select(int nfds, IntPtr readfds, IntPtr writefds, IntPtr exceptfds, ref timeval timeout);

		[DllImport("ws2_32.dll")]
		public static extern int closesocket(IntPtr s);

		static GCHandle ToFDSet(List<IntPtr> list)
		{
			object obj;
			switch (IntPtr.Size)
			{
				case 4: obj = new List<int> { list.Count }.Concat(list.Select(item => item.ToInt32())).SelectMany(item => BitConverter.GetBytes(item)).ToArray(); break;
				case 8: obj = new List<long> { list.Count }.Concat(list.Select(item => item.ToInt64())).SelectMany(item => BitConverter.GetBytes(item)).ToArray(); break;
				default: throw new ArgumentException("I'm not sure how THIS happened...");
			}
			return GCHandle.Alloc(obj, GCHandleType.Pinned);
		}

		static HashSet<IntPtr> FromFDSet(GCHandle handle)
		{
			var ptr = handle.Target as byte[];
			List<IntPtr> list;
			switch (IntPtr.Size)
			{
				case 4: list = Enumerable.Range(0, ptr.Length / IntPtr.Size).Select(index => index * IntPtr.Size).Select(index => new IntPtr(BitConverter.ToInt32(ptr, index))).ToList(); break;
				case 8: list = Enumerable.Range(0, ptr.Length / IntPtr.Size).Select(index => index * IntPtr.Size).Select(index => new IntPtr(BitConverter.ToInt64(ptr, index))).ToList(); break;
				default: throw new ArgumentException("I'm not sure how THIS happened...");
			}
			handle.Free();
			list = list.Skip(1).Take(list[0].ToInt32()).ToList();
			return new HashSet<IntPtr>(list);
		}

		class ScanInfo
		{
			public IPAddress Host { get; }
			public int Port { get; }
			public DateTime Timeout { get; set; }
			public int Attempts { get; set; }
			public IntPtr Socket { get; set; }
			public bool Success { get; set; }

			public ScanInfo(IPAddress host, int port, int attempts)
			{
				Host = host;
				Port = port;
				Attempts = attempts;
				Socket = IntPtr.Zero;
			}
		}

		public static List<List<int>> ScanPorts(List<IPAddress> hosts, List<Tuple<int, int>> portsRanges, int attempts, TimeSpan timeout, int concurrency)
		{
			var scanInfo = hosts.Distinct().SelectMany(host => portsRanges.SelectMany(portRange => Enumerable.Range(portRange.Item1, portRange.Item2 - portRange.Item1 + 1).Select(port => new ScanInfo(host, port, attempts)))).ToList();

			var workingSet = new List<ScanInfo>();
			while (true)
			{
				var now = DateTime.UtcNow;

				var toAdd = scanInfo.Where(item => (item.Attempts > 0) && (item.Socket == IntPtr.Zero)).OrderByDescending(item => item.Attempts).Take(concurrency - workingSet.Count).ToList();
				var itemTimeout = now + timeout;
				foreach (var item in toAdd)
				{
					item.Socket = socket((int)item.Host.AddressFamily, SOCK_STREAM, IPPROTO_TCP);
					int mode = 1;
					if (ioctlsocket(item.Socket, FIONBIO, ref mode) == SOCKET_ERROR)
						throw new Exception("Failed to put sock in non-blocking mode");

					int result;
					switch (item.Host.AddressFamily)
					{
						case AddressFamily.InterNetwork: result = connect(item.Socket, new sockaddr_in(item.Host, item.Port), Marshal.SizeOf<sockaddr_in>()); break;
						case AddressFamily.InterNetworkV6: result = connect(item.Socket, new sockaddr_in6(item.Host, item.Port), Marshal.SizeOf<sockaddr_in6>()); break;
						default: throw new ArgumentException("Invalid address");
					}
					if ((result == SOCKET_ERROR) && (WSAGetLastError() != WSAEWOULDBLOCK))
						throw new Exception("Connect failed");
					item.Timeout = itemTimeout;
				}
				workingSet.AddRange(toAdd);

				if (!workingSet.Any())
					break;

				var writefds = ToFDSet(workingSet.Select(item => item.Socket).ToList());
				var errorfds = ToFDSet(workingSet.Select(item => item.Socket).ToList());
				var timeSpan = workingSet.Min(item => item.Timeout) - now;
				var timeval = new timeval(timeSpan);
				if (select(0, IntPtr.Zero, writefds.AddrOfPinnedObject(), errorfds.AddrOfPinnedObject(), ref timeval) == SOCKET_ERROR)
					throw new Exception("Select failed");
				var writeList = FromFDSet(writefds);
				var errorList = FromFDSet(errorfds);

				now = DateTime.UtcNow;

				foreach (var item in workingSet.ToList())
				{
					var done = false;
					if (writeList.Contains(item.Socket))
					{
						item.Attempts = 0;
						item.Success = true;
						done = true;
					}
					else if (errorList.Contains(item.Socket))
					{
						// Actively refused; don't try again
						item.Attempts = 0;
						done = true;
					}
					else if (now >= item.Timeout)
					{
						--item.Attempts;
						done = true;
					}
					if (done)
					{
						closesocket(item.Socket);
						item.Socket = IntPtr.Zero;
						workingSet.Remove(item);
					}
				}
			}

			var scanInfoByHost = scanInfo.GroupBy(item => item.Host).ToDictionary(group => group.Key);
			return hosts.Select(host => scanInfoByHost[host].Where(item => item.Success).Select(item => item.Port).OrderBy().ToList()).ToList();
		}
	}
}
