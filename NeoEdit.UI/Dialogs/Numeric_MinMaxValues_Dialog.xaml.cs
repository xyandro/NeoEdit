﻿using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Transform;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Numeric_MinMaxValues_Dialog
	{
		[DepProp]
		public Coder.CodePage CodePage { get { return UIHelper<Numeric_MinMaxValues_Dialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<Numeric_MinMaxValues_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Min { get { return UIHelper<Numeric_MinMaxValues_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Numeric_MinMaxValues_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Max { get { return UIHelper<Numeric_MinMaxValues_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Numeric_MinMaxValues_Dialog>.SetPropValue(this, value); } }

		static Numeric_MinMaxValues_Dialog() { UIHelper<Numeric_MinMaxValues_Dialog>.Register(); }

		Numeric_MinMaxValues_Dialog()
		{
			InitializeComponent();

			var codePages = Coder.GetNumericCodePages().ToDictionary(page => page, page => Coder.GetDescription(page));
			codePage.ItemsSource = codePages;
			codePage.SelectedValuePath = "Key";
			codePage.DisplayMemberPath = "Value";

			CodePage = Coder.CodePage.Int32LE;
			Min = false;
			Max = true;
		}

		Configuration_Numeric_MinMaxValues result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if ((!Min) && (!Max))
				return;
			result = new Configuration_Numeric_MinMaxValues { CodePage = CodePage, Min = Min, Max = Max };
			DialogResult = true;
		}

		public static Configuration_Numeric_MinMaxValues Run(Window parent)
		{
			var dialog = new Numeric_MinMaxValues_Dialog { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
