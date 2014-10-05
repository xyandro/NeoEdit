using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.GUI.Dialogs
{
	internal partial class GotoIndexDialog
	{
		[DepProp]
		public int Index { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int MinIndex { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int MaxIndex { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool Relative { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }

		static GotoIndexDialog()
		{
			UIHelper<GotoIndexDialog>.Register();
			UIHelper<GotoIndexDialog>.AddCallback(a => a.Relative, (obj, o, n) => obj.SetRelative());
		}

		readonly UIHelper<GotoIndexDialog> uiHelper;
		readonly int numIndexes, startIndex;
		GotoIndexDialog(int _numIndexes, int _startIndex)
		{
			numIndexes = _numIndexes;
			startIndex = _startIndex + 1;

			uiHelper = new UIHelper<GotoIndexDialog>(this);
			InitializeComponent();

			SetRelative();
			Index = startIndex;
		}

		void SetRelative()
		{
			var index = Index - MinIndex;
			if (Relative)
			{
				MinIndex = -startIndex + 1;
				MaxIndex = numIndexes - startIndex;
			}
			else
			{
				MinIndex = 1;
				MaxIndex = numIndexes;
			}
			Index = index + MinIndex;
		}

		int result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = Index - 1;
			if (Relative)
				result += startIndex;
			DialogResult = true;
		}

		public static int? Run(int numIndexes, int index)
		{
			var dialog = new GotoIndexDialog(numIndexes, index);
			return dialog.ShowDialog() == true ? (int?)dialog.result : null;
		}
	}
}
