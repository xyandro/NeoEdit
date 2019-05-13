using System.Linq;
using System.Windows;
using NeoEdit.Transform;
using NeoEdit.Controls;

namespace NeoEdit.Dialogs
{
	internal partial class NumericMinMaxValuesDialog
	{
		internal class Result
		{
			public Coder.CodePage CodePage { get; set; }
			public bool Min { get; set; }
			public bool Max { get; set; }
		}

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

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if ((!Min) && (!Max))
				return;
			result = new Result { CodePage = CodePage, Min = Min, Max = Max };
			DialogResult = true;
		}

		static public Result Run(Window parent)
		{
			var dialog = new NumericMinMaxValuesDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
