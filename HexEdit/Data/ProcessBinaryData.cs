using System;
using System.Collections.Generic;
using System.IO;
using NeoEdit.GUI.Dialogs;
using NeoEdit.Win32;

namespace NeoEdit.HexEdit.Data
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
				Length = (long)Interop.GetProcessMemoryLength(handle);
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

		protected override void VirtRead(long index, out byte[] block, out long blockStart, out long blockEnd)
		{
			using (Suspend())
			using (Open())
			{
				block = null;
				blockStart = blockEnd = index;

				var queryInfo = Interop.VirtualQuery(handle, index);
				if (queryInfo == null)
				{
					blockEnd = Length;
					return;
				}

				blockEnd = queryInfo.EndAddress.ToInt64();

				if ((!queryInfo.Committed) || ((queryInfo.Mapped) && queryInfo.NoAccess))
				{
					blockStart = queryInfo.StartAddress.ToInt64();
					return;
				}

				blockEnd = Math.Min(blockEnd, blockStart + 65536);

				block = new byte[blockEnd - blockStart];

				using (Interop.SetProtect(handle, queryInfo, false))
					Interop.ReadProcessMemory(handle, index, block, (int)(index - blockStart), (int)(blockEnd - index));
			}
		}

		public override bool Find(BinaryFindDialog.Result currentFind, long index, out long start, out long end, bool forward = true)
		{
			using (Suspend())
			using (Open())
				return base.Find(currentFind, index, out start, out end, forward);
		}

		protected override void VirtWrite(long index, long count, byte[] bytes)
		{
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
		}

		public override void Save(string fileName)
		{
			using (Suspend())
			using (Open())
			using (var output = File.Create(fileName))
			{
				long sectionStart = -1, sectionEnd = -1;
				var start = new List<long>();
				var end = new List<long>();

				byte[] block = null;
				long blockStart = 0, blockEnd = 0;
				long index = 0;
				while (true)
				{
					block = null;
					if (index < Length)
						VirtRead(index, out block, out blockStart, out blockEnd);

					if (block != null)
					{
						if (sectionStart == -1)
							sectionStart = blockStart;
						sectionEnd = blockEnd;
						output.Write(block, 0, block.Length);
					}
					else if (sectionStart != -1)
					{
						start.Add(sectionStart);
						end.Add(sectionEnd);
						sectionStart = sectionEnd = -1;
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

					index = blockEnd;
				}
			}
		}
	}
}
