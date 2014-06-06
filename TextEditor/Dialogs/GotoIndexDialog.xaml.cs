using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.GUI.Common;

namespace NeoEdit.GUI.Dialogs
{
	public partial class GotoIndexDialog : Window
	{
		[DepProp]
		public int Index { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int MinIndex { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int MaxIndex { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool Relative { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }

		static GotoIndexDialog() { UIHelper<GotoIndexDialog>.Register(); }

		readonly UIHelper<GotoIndexDialog> uiHelper;
		readonly int numIndexes, startIndex;
		GotoIndexDialog(int _numIndexes, int _startIndex)
		{
			numIndexes = _numIndexes;
			startIndex = _startIndex + 1;

			uiHelper = new UIHelper<GotoIndexDialog>(this);
			InitializeComponent();

			Loaded += (s, e) => index.SelectAll();

			uiHelper.AddCallback(a => a.Index, (o, n) => Index = Math.Max(MinIndex, Math.Min(Index, MaxIndex)));
			uiHelper.AddCallback(a => a.Relative, (o, n) => SetRelative());

			okClick.Click += (s, e) =>
			{
				if (Validation.GetHasError(index))
					return;
				DialogResult = true;
			};

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

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Handled)
				return;

			e.Handled = true;
			switch (e.Key)
			{
				case Key.Up: ++Index; break;
				case Key.Down: --Index; break;
				default: e.Handled = false; break;
			}
		}

		public static int? Run(int numIndexes, int index)
		{
			var d = new GotoIndexDialog(numIndexes, index);
			if (d.ShowDialog() != true)
				return null;
			index = d.Index - 1;
			if (d.Relative)
				index += d.startIndex;
			return index;
		}
	}
}
