using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class NEWindowData : INEWindowData
	{
		public long NESerial { get; } = NESerialTracker.NESerial;
		public NEWindow NEWindow { get; }

		public IReadOnlyOrderedHashSet<INEFileData> NEFileDatas { get; set; }
		public IReadOnlyOrderedHashSet<NEFile> NEFiles { get; set; }
		public IReadOnlyOrderedHashSet<NEFile> ActiveFiles { get; set; }
		public NEFile Focused { get; set; }
		public WindowLayout WindowLayout { get; set; }
		public bool WorkMode { get; set; }

		public INEWindowData Undo { get; set; }
		public INEWindowData Redo { get; set; }
		public INEWindowData RedoText { get; set; }

		public bool TextChanged { get; private set; } = false;
		public void SetTextChanged()
		{
			if (TextChanged)
				return;

			TextChanged = true;
			RedoText = null;
			var data = this;
			while (true)
			{
				data = data.Undo as NEWindowData;
				if (data == null)
					break;
				data.RedoText = this;
				if (data.TextChanged)
					break;
			}
		}

		public NEWindowData(NEWindow neWindow)
		{
			NEWindow = neWindow;
			NEFileDatas = new OrderedHashSet<INEFileData>();
			NEFiles = ActiveFiles = new OrderedHashSet<NEFile>();
			WindowLayout = new WindowLayout(1, 1);
		}

		public INEWindowData Next()
		{
			var next = new NEWindowData(NEWindow)
			{
				NEFileDatas = NEFileDatas,
				NEFiles = NEFiles,
				ActiveFiles = ActiveFiles,
				Focused = Focused,
				WindowLayout = WindowLayout,
				WorkMode = WorkMode,
				RedoText = RedoText,
				Undo = this,
			};
			Redo = next;
			return next;
		}

		public override string ToString() => NESerial.ToString();
	}
}
