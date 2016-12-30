using System;
using System.Threading;
using System.Threading.Tasks;
using NeoEdit.Common;

namespace NeoEdit.Rip
{
	class CDTrack : RipItem
	{
		public override string FileName => $"Track {Track}.wav";

		public CDDrive Drive { get; }
		public int Track { get; }
		public int StartSector { get; }
		public int EndSector { get; }
		public int Size { get; }
		public TimeSpan Length { get; }
		public bool IsAudio { get; }

		public CDTrack(CDDrive drive, int track, int startSector, int endSector, bool isAudio)
		{
			Drive = drive;
			Track = track;
			StartSector = startSector;
			EndSector = endSector;
			IsAudio = isAudio;
			Size = (EndSector - StartSector) * CDWin32.SectorBytes;
			Length = TimeSpan.FromSeconds(Size / 12d);
		}

		public override string ToString() => $"{Drive} - Track {Track}";

		public override Task Run(IProgress<ProgressReport> progress, CancellationToken token, string directory) => Task.Run(() => Drive.WriteTrack(GetFileName(directory), this, progress, token));
	}
}
