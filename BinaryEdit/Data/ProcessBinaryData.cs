using System;
using System.Collections.Generic;
using System.IO;
using NeoEdit.GUI.Dialogs;
using NeoEdit.Win32;

namespace NeoEdit.BinaryEdit.Data
{
	class ProcessBinaryData : BinaryData
	{
		class OnCloseAction : IDisposable
		{
			Action closeAction;
			public OnCloseAction(Action _closeAction)
			{
				closeAction = _closeAction;
			}

			public void Dispose()
			{
				closeAction();
			}
		}

		OnCloseAction Suspend()
		{
			SuspendProcess();
			return new OnCloseAction(() => ResumeProcess());
		}

		OnCloseAction Open()
		{
			OpenProcess();
			return new OnCloseAction(() => CloseProcess());
		}

		readonly int pid;
		public ProcessBinaryData(int pid)
		{
			this.pid = pid;

			using (Suspend())
			using (Open())
				length = (long)Interop.GetProcessMemoryLength(handle);
		}

		int suspendCount = 0;
		void SuspendProcess()
		{
			if (suspendCount++ != 0)
				return;

			Interop.SuspendProcess(pid);
		}

		void ResumeProcess()
		{
			if (--suspendCount != 0)
				return;

			Interop.ResumeProcess(pid);
		}

		Handle handle;
		int openCount = 0;
		void OpenProcess()
		{
			if (openCount++ != 0)
				return;

			handle = Interop.OpenReadMemoryProcess(pid);
		}

		void CloseProcess()
		{
			if (--openCount != 0)
				return;

			handle.Dispose();
		}

		protected override void SetCache(long index, int count)
		{
			if ((index >= cacheStart) && (index + count <= cacheEnd))
				return;

			if (count > cache.Length)
				throw new ArgumentException("count");

			using (Suspend())
			using (Open())
			{
				cacheStart = cacheEnd = index;
				cacheHasData = false;

				while (cacheEnd - cacheStart < count)
				{
					bool hasData;

					var queryInfo = Interop.VirtualQuery(handle, index);
					if (queryInfo != null)
					{
						hasData = queryInfo.Committed;
						if ((hasData) && (queryInfo.Mapped))
						{
							if (queryInfo.NoAccess)
								hasData = false;
						}
						cacheEnd = queryInfo.EndAddress.ToInt64();
					}
					else
					{
						hasData = false;
						cacheEnd = Length;
					}

					if ((!hasData) && (!cacheHasData) && (cacheEnd - cacheStart >= count))
						return;

					cacheHasData = true;
					cacheEnd = Math.Min(cacheEnd, cacheStart + cache.Length);

					if (!hasData)
						Array.Clear(cache, (int)(index - cacheStart), (int)(cacheEnd - index));
					else
						using (Interop.SetProtect(handle, queryInfo, false))
							Interop.ReadProcessMemory(handle, index, cache, (int)(index - cacheStart), (int)(cacheEnd - index));

					index = cacheEnd;
				}
			}
		}

		public override bool Find(BinaryFindDialog.Result currentFind, long index, out long start, out long end, bool forward = true)
		{
			using (Suspend())
			using (Open())
				return base.Find(currentFind, index, out start, out end, forward);
		}

		public override void Replace(long index, long count, byte[] bytes)
		{
			if (count != bytes.Length)
				throw new Exception("Cannot change byte count.");

			using (Suspend())
			using (Open())
			{
				while (bytes.Length > 0)
				{
					var queryInfo = Interop.VirtualQuery(handle, index);
					if ((queryInfo == null) || (!queryInfo.Committed))
						throw new Exception("Cannot write to this memory");

					var end = queryInfo.EndAddress.ToInt64();
					var numBytes = (int)Math.Min(bytes.Length, end - index);

					using (Interop.SetProtect(handle, queryInfo, true))
						Interop.WriteProcessMemory(handle, index, bytes, numBytes);

					index += numBytes;
					Array.Copy(bytes, numBytes, bytes, 0, bytes.Length - numBytes);
					Array.Resize(ref bytes, bytes.Length - numBytes);
				}
			}

			Refresh();
		}

		public override void Refresh()
		{
			cacheStart = cacheEnd = 0;
			base.Refresh();
		}

		public override void Save(string fileName)
		{
			using (Suspend())
			using (Open())
			using (var output = File.Create(fileName))
			{
				long startBlock = -1, endBlock = -1;
				var start = new List<long>();
				var end = new List<long>();

				cacheStart = cacheEnd = 0;
				long index = 0;
				while (true)
				{
					var hasData = false;
					if (index < Length)
					{
						SetCache(index, 1);
						hasData = cacheHasData;
					}

					if (hasData)
					{
						if (startBlock == -1)
							startBlock = cacheStart;
						endBlock = cacheEnd;
						output.Write(cache, 0, (int)(cacheEnd - cacheStart));
					}
					else if (startBlock != -1)
					{
						start.Add(startBlock);
						end.Add(endBlock);
						startBlock = endBlock = -1;
					}
					else if (index >= Length)
					{
						for (var ctr = 0; ctr < start.Count; ++ctr)
						{
							var startBytes = BitConverter.GetBytes(start[ctr]);
							output.Write(startBytes, 0, startBytes.Length);
							var endBytes = BitConverter.GetBytes(end[ctr]);
							output.Write(endBytes, 0, endBytes.Length);
						}
						var countBytes = BitConverter.GetBytes(start.Count);
						output.Write(countBytes, 0, countBytes.Length);
						break;
					}

					index = cacheEnd;
				}
			}
		}
	}
}
