using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class NEFileData : INEFileData
	{
		public long NESerial { get; } = NESerialTracker.NESerial;
		public NEFile NEFile { get; }

		NETextPoint neTextPoint;
		public NETextPoint NETextPoint
		{
			get => neTextPoint;
			set
			{
				neTextPoint = value;
				NEFile.NEWindow?.SetTextChanged();
			}
		}
		public IReadOnlyList<NERange> Selections { get; set; }
		public IReadOnlyList<NERange>[] Regions { get; set; }
		public bool AllowOverlappingSelections { get; set; }

		NEFileData() { }

		public NEFileData(NEFile neFile)
		{
			NEFile = neFile;
			Regions = new IReadOnlyList<NERange>[9];
		}

		public INEFileData Next()
		{
			return new NEFileData(NEFile)
			{
				neTextPoint = neTextPoint,
				Selections = Selections,
				Regions = Regions.ToArray(),
				AllowOverlappingSelections = AllowOverlappingSelections,
			};
		}

		public override string ToString() => NESerial.ToString();
	}
}
