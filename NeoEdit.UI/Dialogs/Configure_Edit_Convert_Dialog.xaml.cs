using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Transform;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Edit_Convert_Dialog
	{
		[DepProp]
		public Coder.CodePage InputType { get { return UIHelper<Configure_Edit_Convert_Dialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<Configure_Edit_Convert_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool InputBOM { get { return UIHelper<Configure_Edit_Convert_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_Edit_Convert_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public Coder.CodePage OutputType { get { return UIHelper<Configure_Edit_Convert_Dialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<Configure_Edit_Convert_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool OutputBOM { get { return UIHelper<Configure_Edit_Convert_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_Edit_Convert_Dialog>.SetPropValue(this, value); } }

		static Configure_Edit_Convert_Dialog() { UIHelper<Configure_Edit_Convert_Dialog>.Register(); }

		Configure_Edit_Convert_Dialog()
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

		Configuration_Edit_Convert result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Edit_Convert { InputType = InputType, InputBOM = InputBOM, OutputType = OutputType, OutputBOM = OutputBOM };
			DialogResult = true;
		}

		public static Configuration_Edit_Convert Run(Window parent)
		{
			var dialog = new Configure_Edit_Convert_Dialog { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
