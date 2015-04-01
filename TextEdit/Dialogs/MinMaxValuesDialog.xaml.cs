using System.Linq;
using System.Windows;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class MinMaxValuesDialog
	{
		internal class Result
		{
			public Coder.CodePage CodePage { get; set; }
			public bool Min { get; set; }
			public bool Max { get; set; }
		}

		[DepProp]
		public Coder.CodePage CodePage { get { return UIHelper<MinMaxValuesDialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<MinMaxValuesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Min { get { return UIHelper<MinMaxValuesDialog>.GetPropValue<bool>(this); } set { UIHelper<MinMaxValuesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Max { get { return UIHelper<MinMaxValuesDialog>.GetPropValue<bool>(this); } set { UIHelper<MinMaxValuesDialog>.SetPropValue(this, value); } }

		static MinMaxValuesDialog() { UIHelper<MinMaxValuesDialog>.Register(); }

		MinMaxValuesDialog()
		{
			InitializeComponent();

			codePage.ItemsSource = Coder.GetNumericCodePages().ToDictionary(page => page, page => Coder.GetDescription(page));
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

		static public Result Run()
		{
			var dialog = new MinMaxValuesDialog();
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
