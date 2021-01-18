using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class NEFileData : INEFileData
	{
		public int NESerial { get; } = NESerialTracker.NESerial;
		public NEFile NEFile { get; }

		NETextPoint neTextPoint;
		public NETextPoint NETextPoint
		{
			get => neTextPoint;
			set
			{
				if (neTextPoint == value)
					return;

				var data = Undo;
				while ((data != null) && (data.NETextPoint == NETextPoint))
				{
					(data as NEFileData).RedoText = this;
					data = data.Undo;
				}
				neTextPoint = value;
				RedoText = null;
			}
		}
		public IReadOnlyList<NERange> Selections { get; set; }
		public IReadOnlyList<NERange>[] Regions { get; set; }
		public bool AllowOverlappingSelections { get; set; }

		public INEFileData Undo { get; set; }
		public INEFileData Redo { get; set; }
		public INEFileData RedoText { get; set; }

		NEFileData() { }

		public NEFileData(NEFile neFile)
		{
			NEFile = neFile;
			Regions = new IReadOnlyList<NERange>[9];
		}

		public INEFileData Next()
		{
			var next = new NEFileData(NEFile)
			{
				neTextPoint = neTextPoint,
				Selections = Selections,
				Regions = Regions.ToArray(),
				AllowOverlappingSelections = AllowOverlappingSelections,
				RedoText = RedoText,
				Undo = this,
			};
			Redo = next;
			return next;
		}

		public override string ToString() => NESerial.ToString();
	}
}
