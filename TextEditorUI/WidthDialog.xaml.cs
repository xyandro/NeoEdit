﻿using System;
using System.Windows;
using System.Windows.Input;
using NeoEdit.Common;

namespace NeoEdit.TextEditorUI
{
	public partial class WidthDialog : Window
	{
		[DepProp]
		public int WidthNum { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int MinWidthNum { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public char PadChar { get { return uiHelper.GetPropValue<char>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool Before { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }

		static WidthDialog() { UIHelper<WidthDialog>.Register(); }

		readonly UIHelper<WidthDialog> uiHelper;
		WidthDialog()
		{
			uiHelper = new UIHelper<WidthDialog>(this);
			InitializeComponent();
			widthBox.SelectAll();

			Loaded += (s, e) => WidthNum = MinWidthNum;
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Handled)
				return;

			switch (e.Key)
			{
				case Key.Up: WidthNum++; break;
				case Key.Down: WidthNum = Math.Max(MinWidthNum, WidthNum - 1); break;
			}
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		public static bool Run(int min, bool numeric, out int width, out char padChar, out bool before)
		{
			width = min;
			padChar = numeric ? '0' : ' ';
			before = true;

			var widthDialog = new WidthDialog { MinWidthNum = min, PadChar = padChar, Before = numeric };
			if (widthDialog.ShowDialog() == false)
				return false;

			width = widthDialog.WidthNum;
			padChar = widthDialog.PadChar;
			before = widthDialog.Before;
			return true;
		}
	}
}
