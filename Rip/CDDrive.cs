using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using NeoEdit.Common;
using NeoEdit.Common.Transform;

namespace NeoEdit.Rip
{
	class CDDrive : IDisposable
	{
		public static List<CDDrive> GetDrives() => DriveInfo.GetDrives().Where(d => (d.DriveType == DriveType.CDRom) && (d.IsReady)).Select(d => new CDDrive(d.Name[0], d.VolumeLabel)).ToList();

		public char Drive { get; }
		public string VolumeLabel { get; }

		public override string ToString() => $"{Drive}: ({VolumeLabel})";

		IntPtr handle = IntPtr.Zero;

		public CDDrive(char drive, string volumeLabel)
		{
			Drive = drive;
			VolumeLabel = volumeLabel;
		}

		public void Open()
		{
			if (handle != IntPtr.Zero)
				return;

			handle = CDWin32.CreateFile($@"\\.\{Drive}:", CDWin32.DesiredAccess.GENERIC_READ, CDWin32.ShareMode.FILE_SHARE_READ, IntPtr.Zero, CDWin32.CreationDisposition.OPEN_EXISTING, 0, IntPtr.Zero);
			if (handle.ToInt64() <= 0)
				throw new Exception($"Unable to open drive {Drive}:", new Win32Exception());

			var bytesRead = 0;
			if (!CDWin32.DeviceIoControl(handle, CDWin32.IoControlCode.IOCTL_STORAGE_CHECK_VERIFY, IntPtr.Zero, 0, IntPtr.Zero, 0, ref bytesRead, IntPtr.Zero))
				throw new Exception("Drive not ready", new Win32Exception());

			bytesRead = 0;
			var pmr = new CDWin32.PREVENT_MEDIA_REMOVAL { PreventMediaRemoval = 1 };
			if (!CDWin32.DeviceIoControl(handle, CDWin32.IoControlCode.IOCTL_STORAGE_MEDIA_REMOVAL, pmr, Marshal.SizeOf(pmr), IntPtr.Zero, 0, ref bytesRead, IntPtr.Zero))
				throw new Exception("Failed to lock drive", new Win32Exception());
		}

		public void Close()
		{
			if (handle == IntPtr.Zero)
				return;

			var bytesRead = 0;
			var pmr = new CDWin32.PREVENT_MEDIA_REMOVAL { PreventMediaRemoval = 0 };
			CDWin32.DeviceIoControl(handle, CDWin32.IoControlCode.IOCTL_STORAGE_MEDIA_REMOVAL, pmr, Marshal.SizeOf(pmr), IntPtr.Zero, 0, ref bytesRead, IntPtr.Zero);

			CDWin32.CloseHandle(handle);
			handle = IntPtr.Zero;
		}

		public void Dispose() => Close();

		~CDDrive() { Dispose(); }

		public List<CDTrack> GetTracks()
		{
			Open();

			var bytesRead = 0;
			var contents = new CDWin32.CDROM_TOC();
			if (!CDWin32.DeviceIoControl(handle, CDWin32.IoControlCode.IOCTL_CDROM_READ_TOC, IntPtr.Zero, 0, contents, Marshal.SizeOf(contents), ref bytesRead, IntPtr.Zero))
				throw new Exception("Failed to read table of contents", new Win32Exception());

			var result = new List<CDTrack>();
			for (var track = contents.FirstTrack; track <= contents.LastTrack; ++track)
				result.Add(new CDTrack(this, track, contents.TrackData[track - contents.FirstTrack].Addr, contents.TrackData[track - contents.FirstTrack + 1].Addr - 1, (contents.TrackData[track - contents.FirstTrack].BitMapped & 4) == 0));
			return result;
		}

		static object cdLock = new object();
		public void WriteTrack(string fileName, CDTrack track, IProgress<ProgressReport> progress, CancellationToken token)
		{
			lock (cdLock)
			{
				Open();

				using (var output = File.Create(fileName))
				{
					var bytes = Coder.StringToBytes("UklGRgAAAABXQVZFZm10IBIAAAABAAIARKwAABCxAgAEABAAAABkYXRhAAAAAA", Coder.CodePage.Base64);
					Array.Copy(BitConverter.GetBytes(track.Size + 38), 0, bytes, 4, 4);
					Array.Copy(BitConverter.GetBytes(track.Size), 0, bytes, 42, 4);
					output.Write(bytes, 0, bytes.Length);

					var sector = track.StartSector;
					while (sector < track.EndSector)
					{
						if (token.IsCancellationRequested)
							throw new Exception("Cancelled");

						progress.Report(new ProgressReport(sector - track.StartSector, track.EndSector - track.StartSector));

						const int NumSectors = 13;
						int numSectors = Math.Min(track.EndSector - sector, NumSectors);

						var rri = new CDWin32.RAW_READ_INFO
						{
							TrackMode = CDWin32.TRACK_MODE_TYPE.CDDA,
							SectorCount = numSectors,
							DiskOffset = sector * 2048,
						};

						var bytesRead = 0;
						var buffer = new byte[CDWin32.SectorBytes * numSectors];
						if (!CDWin32.DeviceIoControl(handle, CDWin32.IoControlCode.IOCTL_CDROM_RAW_READ, rri, Marshal.SizeOf(rri), buffer, buffer.Length, ref bytesRead, IntPtr.Zero))
							throw new Exception("Failed to read CD", new Win32Exception());

						output.Write(buffer, 0, CDWin32.SectorBytes * numSectors);
						sector += numSectors;
					}
				}
			}
		}
	}
}
