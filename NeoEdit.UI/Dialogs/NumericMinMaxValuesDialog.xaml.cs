using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Transform;
using NeoEdit.UI;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class NumericMinMaxValuesDialog
	{
		[DepProp]
		public Coder.CodePage CodePage { get { return UIHelper<NumericMinMaxValuesDialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<NumericMinMaxValuesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Min { get { return UIHelper<NumericMinMaxValuesDialog>.GetPropValue<bool>(this); } set { UIHelper<NumericMinMaxValuesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Max { get { return UIHelper<NumericMinMaxValuesDialog>.GetPropValue<bool>(this); } set { UIHelper<NumericMinMaxValuesDialog>.SetPropValue(this, value); } }

		static NumericMinMaxValuesDialog() { UIHelper<NumericMinMaxValuesDialog>.Register(); }

		NumericMinMaxValuesDialog()
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
			var dialog = new NumericMinMaxValuesDialog { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
