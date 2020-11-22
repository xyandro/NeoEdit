using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class NEFileData
	{
		public readonly int NESerial = NESerialTracker.NESerial;
		public readonly NEFile neFile;

		public NEText text;
		public IReadOnlyList<Range> selections;
		public IReadOnlyList<Range>[] regions;
		public NEFile diffTarget;

		public NEFileData undo;
		public NEFileData redo;

		public NEFileData(NEFile neFile)
		{
			this.neFile = neFile;
			regions = new IReadOnlyList<Range>[9];
		}

		public NEFileData(NEFileData neFileData)
		{
			neFile = neFileData.neFile;

			text = neFileData.text;
			selections = neFileData.selections;
			regions = neFileData.regions.ToArray();
			diffTarget = neFileData.diffTarget;

			undo = neFileData;
			neFileData.redo = this;
		}

		public override string ToString() => NESerial.ToString();
	}
}
