﻿using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class NEFileData : INEFileData
	{
		public int NESerial { get; } = NESerialTracker.NESerial;
		public NEFile NEFile { get; }

		public NETextPoint NETextPoint { get; set; }
		public IReadOnlyList<Range> Selections { get; set; }
		public IReadOnlyList<Range>[] Regions { get; set; }
		public NEFile DiffTarget { get; set; }

		public INEFileData Undo { get; set; }
		public INEFileData Redo { get; set; }

		public NEFileData(NEFile neFile)
		{
			NEFile = neFile;
			Regions = new IReadOnlyList<Range>[9];
		}

		public NEFileData(INEFileData neFileData)
		{
			NEFile = neFileData.NEFile;

			NETextPoint = neFileData.NETextPoint;
			Selections = neFileData.Selections;
			Regions = neFileData.Regions.ToArray();
			DiffTarget = neFileData.DiffTarget;

			Undo = neFileData;
			(neFileData as NEFileData).Redo = this;
		}

		public override string ToString() => NESerial.ToString();
	}
}
