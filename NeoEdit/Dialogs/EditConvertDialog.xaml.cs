using System.Linq;
using System.Windows;
using NeoEdit;
using NeoEdit.Controls;
using NeoEdit.Transform;

namespace NeoEdit.Dialogs
{
	partial class EditConvertDialog
	{
		public class Result
		{
			public Coder.CodePage InputType { get; set; }
			public bool InputBOM { get; set; }
			public Coder.CodePage OutputType { get; set; }
			public bool OutputBOM { get; set; }
		}

		[DepProp]
		public Coder.CodePage InputType { get { return UIHelper<EditConvertDialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<EditConvertDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool InputBOM { get { return UIHelper<EditConvertDialog>.GetPropValue<bool>(this); } set { UIHelper<EditConvertDialog>.SetPropValue(this, value); } }
		[DepProp]
		public Coder.CodePage OutputType { get { return UIHelper<EditConvertDialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<EditConvertDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool OutputBOM { get { return UIHelper<EditConvertDialog>.GetPropValue<bool>(this); } set { UIHelper<EditConvertDialog>.SetPropValue(this, value); } }

		static EditConvertDialog() { UIHelper<EditConvertDialog>.Register(); }

		EditConvertDialog()
		{
			InitializeComponent();

			var codePages = Coder.GetAllCodePages().ToDictionary(page => page, page => Coder.GetDescription(page));

			inputType.ItemsSource = codePages;
			inputType.SelectedValuePath = "Key";
			inputType.DisplayMemberPath = "Value";

			outputType.ItemsSource = codePages;
			outputType.SelectedValuePath = "Key";
			outputType.DisplayMemberPath = "Value";

			InputType = Coder.CodePage.UTF8;
			OutputType = Coder.CodePage.Hex;
			InputBOM = false;
			OutputBOM = true;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { InputType = InputType, InputBOM = InputBOM, OutputType = OutputType, OutputBOM = OutputBOM };
			DialogResult = true;
		}

		static public Result Run(Window parent)
		{
			var dialog = new EditConvertDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
