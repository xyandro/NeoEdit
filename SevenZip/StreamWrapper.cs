using System;
using System.IO;
using System.Runtime.InteropServices;

namespace NeoEdit.SevenZip
{
	class StreamWrapper : IDisposable
	{
		protected Stream baseStream;

		protected StreamWrapper(Stream baseStream)
		{
			this.baseStream = baseStream;
		}

		public void Dispose() => baseStream.Close();

		public virtual void Seek(long offset, SeekOrigin seekOrigin, IntPtr newPosition)
		{
			var position = baseStream.Seek(offset, seekOrigin);
			if (newPosition != IntPtr.Zero)
				Marshal.WriteInt64(newPosition, position);
		}
	}
}
