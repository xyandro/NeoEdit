using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace NeoEdit.SevenZip
{
	public class SevenZipArchive : IDisposable, IEnumerable<SevenZipEntry>
	{
		static IntPtr LibHandle;

		delegate int CreateObjectDelegate([In] ref Guid classID, [In] ref Guid interfaceID, [MarshalAs(UnmanagedType.Interface)] out object outObject);

		static CreateObjectDelegate CreateObject;

		static SevenZipArchive()
		{
			LibHandle = LoadLibrary("7z.dll");
			if (LibHandle == IntPtr.Zero)
				throw new Win32Exception();

			var createObjectAddr = GetProcAddress(LibHandle, "CreateObject");
			if (createObjectAddr == IntPtr.Zero)
				throw new Win32Exception();
			CreateObject = Marshal.GetDelegateForFunctionPointer<CreateObjectDelegate>(createObjectAddr);
		}

		Formats format;
		IInArchive inArchive;
		InStreamWrapper inStream;
		SevenZipArchive(Formats format, IInArchive inArchive, Stream stream)
		{
			this.format = format;
			this.inArchive = inArchive;
			inStream = new InStreamWrapper(stream);
			ulong checkPos = 1 << 15;
			if (inArchive.Open(inStream, ref checkPos, IntPtr.Zero) != 0)
				throw new Exception("Failed to open archive");
		}

		IOutArchive outArchive;
		OutStreamWrapper outStream;
		SevenZipArchive(Formats format, IOutArchive outArchive, Stream stream)
		{
			this.format = format;
			this.outArchive = outArchive;
			outStream = new OutStreamWrapper(stream);

			var names = GCHandle.Alloc(new IntPtr[] { Marshal.StringToBSTR("x") }, GCHandleType.Pinned);
			var values = GCHandle.Alloc(new PropVariant[] { new PropVariant { VarType = VarEnum.VT_UI4, IntValue = 9 } }, GCHandleType.Pinned);
			try { ((ISetProperties)outArchive).SetProperties(names.AddrOfPinnedObject(), values.AddrOfPinnedObject(), (new IntPtr[] { Marshal.StringToBSTR("x") }).Length); }
			finally
			{
				names.Free();
				values.Free();
			}
		}

		~SevenZipArchive() { Dispose(); }

		public void Dispose()
		{
			if (inArchive != null)
			{
				Marshal.ReleaseComObject(inArchive);
				inStream.Dispose();
				inArchive = null;
				inStream = null;
			}

			if (outArchive != null)
			{
				Marshal.ReleaseComObject(outArchive);
				outStream.Dispose();
				outArchive = null;
				outStream = null;
			}
		}

		static public bool IsArchive(string fileName) => Format.GetExtensionFormat(fileName) != Formats.None;

		static public SevenZipArchive OpenRead(string fileName)
		{
			var stream = File.OpenRead(fileName);
			var format = Format.GetStreamFormat(stream);

			if (format == Formats.None)
				format = Format.GetExtensionFormat(fileName);

			if (format == Formats.None)
				throw new Exception($"{fileName} is not an archive");

			var formatGuid = format.Guid();
			var archiveGuid = typeof(IInArchive).GUID;
			object inArchive;
			CreateObject(ref formatGuid, ref archiveGuid, out inArchive);
			if (inArchive == null)
				throw new Exception("Failed to open archive");

			try
			{
				return new SevenZipArchive(format, inArchive as IInArchive, stream);
			}
			catch
			{
				Marshal.ReleaseComObject(inArchive);
				throw;
			}
		}

		static public SevenZipArchive OpenWrite(string fileName)
		{
			var stream = File.Open(fileName, FileMode.OpenOrCreate);
			var format = Format.GetStreamFormat(stream);

			if (format == Formats.None)
				format = Format.GetExtensionFormat(fileName);

			if (format == Formats.None)
				throw new Exception($"{fileName} is not an archive");

			var formatGuid = format.Guid();
			var archiveGuid = typeof(IOutArchive).GUID;
			object outArchive;
			CreateObject(ref formatGuid, ref archiveGuid, out outArchive);
			if (outArchive == null)
				throw new Exception("Failed to open archive");

			try
			{
				return new SevenZipArchive(format, outArchive as IOutArchive, stream);
			}
			catch
			{
				Marshal.ReleaseComObject(outArchive);
				throw;
			}
		}

		public void Add(string basePath, List<string> fileNames)
		{
			using (var auc = new ArchiveUpdateCallback(format, basePath, fileNames))
				outArchive.UpdateItems(outStream, (uint)fileNames.Count, auc);
		}

		internal void Extract(uint index, Stream stream)
		{
			var callback = new ArchiveExtractFileCallback(index, stream);
			inArchive.Extract(new uint[] { index }, 1, 0, callback);
			if (!callback.Success)
				throw new Exception("Failed to extract file");
		}

		public void ExtractAll(string outPath)
		{
			var items = this.ToList();
			var extractCallback = new ArchiveExtractAllCallback(items, outPath);
			inArchive.Extract(null, uint.MaxValue, 0, extractCallback);
			if (!extractCallback.Success)
				throw new Exception("Failed to extract file");
		}

		public IEnumerator<SevenZipEntry> GetEnumerator()
		{
			var count = inArchive.GetNumberOfItems();
			for (uint ctr = 0; ctr < count; ++ctr)
				yield return new SevenZipEntry(this, ctr);
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		internal T GetProperty<T>(uint index, PropID propID)
		{
			var value = new PropVariant();
			inArchive.GetProperty(index, propID, ref value);
			if (value.VarType == VarEnum.VT_EMPTY)
				return default(T);
			var result = (T)Convert.ChangeType(value.GetObject(), typeof(T));
			value.Clear();
			return result;
		}

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern IntPtr LoadLibrary(string lpFileName);
		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
		static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
	}
}
