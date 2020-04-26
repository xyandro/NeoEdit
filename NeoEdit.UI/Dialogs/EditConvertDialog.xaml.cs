using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Transform;
using NeoEdit.UI;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class EditConvertDialog
	{
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

		EditConvertDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new EditConvertDialogResult { InputType = InputType, InputBOM = InputBOM, OutputType = OutputType, OutputBOM = OutputBOM };
			DialogResult = true;
		}

		public static EditConvertDialogResult Run(Window parent)
		{
			var dialog = new EditConvertDialog { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
