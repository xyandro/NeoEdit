using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using NeoEdit.UI.Resources;

namespace NeoEdit.UI.BinaryEditorUI
{
	public partial class BinaryEditor : Window
	{
		[DepProp]
		public byte[] Data { get { return uiHelper.GetPropValue<byte[]>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool ShowValues { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long SelStart { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long SelEnd { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public string FoundText { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }

		bool shiftDown { get { return (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None; } }

		static BinaryEditor() { UIHelper<BinaryEditor>.Register(); }

		readonly UIHelper<BinaryEditor> uiHelper;
		public BinaryEditor(byte[] data)
		{
			uiHelper = new UIHelper<BinaryEditor>(this);
			InitializeComponent();
			uiHelper.InitializeCommands();

			Data = data;
			SelStart = SelEnd = 0;
			PreviewMouseWheel += (s, e) => uiHelper.RaiseEvent(yScroll, e);

			Show();
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.F3: FindNext(!shiftDown); break;
				default: uiHelper.RaiseEvent(canvas, e); break;
			}
		}

		FindResult currentFind;
		void FindNext(bool forward = true)
		{
			if (currentFind == null)
				return;

			var offset = forward ? 1 : -1;
			Func<byte[], byte, long, long> findFunc;
			if (forward)
			{
				findFunc = (_data, _find, _start) =>
				{
					var _pos = Array.IndexOf(_data, _find, (int)_start);
					if (_pos == -1)
						return long.MaxValue;
					return _pos;
				};
			}
			else
			{
				findFunc = (_data, _find, _start) => Array.LastIndexOf(_data, _find, (int)_start);
			}
			var selectFunc = forward ? (Func<long, long, long>)Math.Min : Math.Max;
			var invalid = forward ? long.MaxValue : -1;

			var pos = SelStart;
			while (true)
			{
				pos += offset;

				var usePos = invalid;
				for (var findPos = 0; findPos < currentFind.FindData.Count; findPos++)
				{
					var caseSensitive = currentFind.CaseSensitive[findPos];
					var findData = currentFind.FindData[findPos];

					usePos = selectFunc(usePos, findFunc(Data, findData[0], pos));
					if (!caseSensitive)
					{
						if ((findData[0] >= 'a') && (findData[0] <= 'z'))
							usePos = selectFunc(usePos, findFunc(Data, (byte)(findData[0] - 'a' + 'A'), pos));
						else if ((findData[0] >= 'A') && (findData[0] <= 'Z'))
							usePos = selectFunc(usePos, findFunc(Data, (byte)(findData[0] - 'A' + 'z'), pos));
					}
				}

				if ((usePos < 0) || (usePos >= Data.Length))
					break;

				pos = usePos;

				for (var findPos = 0; findPos < currentFind.FindData.Count; findPos++)
				{
					var caseSensitive = currentFind.CaseSensitive[findPos];
					var findData = currentFind.FindData[findPos];

					int findIdx;
					for (findIdx = 0; findIdx < findData.Length; ++findIdx)
					{
						if (pos + findIdx >= Data.Length)
							break;

						if (Data[pos + findIdx] == findData[findIdx])
							continue;

						if (caseSensitive)
							break;

						if ((Data[pos + findIdx] >= 'a') && (Data[pos + findIdx] <= 'z') && (findData[findIdx] >= 'A') && (findData[findIdx] <= 'Z'))
							if (Data[pos + findIdx] - 'a' + 'A' == findData[findIdx])
								continue;

						if ((Data[pos + findIdx] >= 'A') && (Data[pos + findIdx] <= 'Z') && (findData[findIdx] >= 'a') && (findData[findIdx] <= 'z'))
							if (Data[pos + findIdx] - 'A' + 'a' == findData[findIdx])
								continue;

						break;
					}

					if (findIdx == findData.Length)
					{
						SelStart = pos;
						SelEnd = pos + findData.Length - 1;
						return;
					}
				}
			}
		}

		void CommandCallback(object obj)
		{
			switch (obj as string)
			{
				case "Edit_Find":
					{
						var results = FindDialog.Run();
						if (results != null)
						{
							currentFind = results;
							FoundText = currentFind.FindText;
							FindNext();
						}
					}
					break;
				case "View_Values": ShowValues = !ShowValues; break;
			}
		}

		void ScrollBar_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			var scrollBar = sender as ScrollBar;
			scrollBar.Value -= e.Delta;
			e.Handled = true;
		}
	}
}
