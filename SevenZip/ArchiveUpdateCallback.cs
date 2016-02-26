using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace NeoEdit.SevenZip
{
	class ArchiveUpdateCallback : IArchiveUpdateCallback, IDisposable
	{
		readonly Formats format;
		readonly string basePath;
		readonly List<string> fileNames;
		public bool Success { get; private set; }
		InStreamWrapper inStream;
		readonly List<InStreamWrapper> toClose = new List<InStreamWrapper>();

		public ArchiveUpdateCallback(Formats format, string basePath, List<string> fileNames)
		{
			this.format = format;
			this.basePath = basePath;
			this.fileNames = fileNames;
			Success = true;
		}

		public long EnumProperties(IntPtr enumerator) => 0x80004001L; // Not implemented

		public int GetProperty(uint index, PropID propId, ref PropVariant value)
		{
			var file = fileNames[(int)index];
			switch (propId)
			{
				case PropID.Attributes:
					value.VarType = VarEnum.VT_UI4;
					value.IntValue = (int)(Directory.Exists(file) ? FileAttributes.Directory : FileAttributes.Archive);
					break;
				case PropID.LastWriteTime:
					value.VarType = VarEnum.VT_FILETIME;
					value.LongValue = new FileInfo(file).LastWriteTime.ToFileTime();
					break;
				case PropID.Path:
					value.VarType = VarEnum.VT_BSTR;
					value.Value = Marshal.StringToBSTR(file.Substring(basePath.Length));
					break;
				case PropID.IsDirectory:
					value.VarType = VarEnum.VT_BOOL;
					value.LongValue = Convert.ToByte(Directory.Exists(file));
					break;
				case PropID.IsAnti:
					value.VarType = VarEnum.VT_BOOL;
					value.LongValue = 0;
					break;
				case PropID.Size:
					value.VarType = VarEnum.VT_UI8;
					value.LongValue = new FileInfo(file).Length;
					break;
			}

			return 0;
		}

		public int GetStream(uint index, [MarshalAs(UnmanagedType.Interface), Out] out ISequentialInStream inStream)
		{
			if (!Success)
			{
				inStream = null;
				return -1;
			}
			inStream = this.inStream = new InStreamWrapper(File.OpenRead(fileNames[(int)index]));
			return 0;
		}

		public int GetUpdateItemInfo(uint index, ref int newData, ref int newProperties, ref uint indexInArchive)
		{
			newData = 1;
			newProperties = 1;
			indexInArchive = uint.MaxValue;
			return 0;
		}

		public void SetCompleted([In] ref ulong completeValue) { }

		public void SetOperationResult(OperationResult operationResult)
		{
			if (operationResult != OperationResult.Ok)
				Success = false;
			if (format == Formats.Zip)
				toClose.Add(inStream);
			else
				inStream?.Dispose();
			inStream = null;
		}

		public void SetTotal(ulong total) { }

		public void Dispose() => toClose.ForEach(stream => stream.Dispose());
	}
}
