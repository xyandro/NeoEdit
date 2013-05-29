using System;
namespace NeoEdit.DiskModule
{
	public interface IFile : IDisposable
	{
		long length { get; }
		byte[] Read(long position, int length);
	}
}
