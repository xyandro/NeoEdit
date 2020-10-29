using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Numeric_RandomNumber_Dialog
	{
		[DepProp]
		public string MinValue { get { return UIHelper<Numeric_RandomNumber_Dialog>.GetPropValue<string>(this); } set { UIHelper<Numeric_RandomNumber_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public string MaxValue { get { return UIHelper<Numeric_RandomNumber_Dialog>.GetPropValue<string>(this); } set { UIHelper<Numeric_RandomNumber_Dialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static Numeric_RandomNumber_Dialog() { UIHelper<Numeric_RandomNumber_Dialog>.Register(); }

		Numeric_RandomNumber_Dialog(NEVariables variables)
		{
			Variables = variables;
			InitializeComponent();

			MinValue = "1";
			MaxValue = "1000";
		}

		Configuration_Numeric_RandomNumber result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Numeric_RandomNumber { MinValue = MinValue, MaxValue = MaxValue };
			DialogResult = true;
		}

		public static Configuration_Numeric_RandomNumber Run(Window parent, NEVariables variables)
		{
			var dialog = new Numeric_RandomNumber_Dialog(variables) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
