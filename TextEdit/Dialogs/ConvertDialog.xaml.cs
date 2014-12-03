using System.Linq;
using System.Windows;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class ConvertDialog
	{
		internal class Result
		{
			public Coder.CodePage InputType { get; set; }
			public bool InputBOM { get; set; }
			public Coder.CodePage OutputType { get; set; }
			public bool OutputBOM { get; set; }
		}

		[DepProp]
		public Coder.CodePage InputType { get { return UIHelper<ConvertDialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<ConvertDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool InputBOM { get { return UIHelper<ConvertDialog>.GetPropValue<bool>(this); } set { UIHelper<ConvertDialog>.SetPropValue(this, value); } }
		[DepProp]
		public Coder.CodePage OutputType { get { return UIHelper<ConvertDialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<ConvertDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool OutputBOM { get { return UIHelper<ConvertDialog>.GetPropValue<bool>(this); } set { UIHelper<ConvertDialog>.SetPropValue(this, value); } }

		static ConvertDialog() { UIHelper<ConvertDialog>.Register(); }

		ConvertDialog()
		{
			InitializeComponent();

			var codePages = Coder.GetCodePages(false).ToDictionary(page => page, page => Coder.GetDescription(page));

			inputType.ItemsSource = codePages;
			inputType.SelectedValuePath = "Key";
			inputType.DisplayMemberPath = "Value";

			outputType.ItemsSource = codePages;
			outputType.SelectedValuePath = "Key";
			outputType.DisplayMemberPath = "Value";

			InputType = Coder.CodePage.UTF8;
			OutputType = Coder.CodePage.Hex;
			InputBOM = OutputBOM = false;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { InputType = InputType, InputBOM = InputBOM, OutputType = OutputType, OutputBOM = OutputBOM };
			DialogResult = true;
		}

		static public Result Run()
		{
			var dialog = new ConvertDialog();
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
