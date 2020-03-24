using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class ExpressionHelpUnitsDialog
	{
		class UnitData
		{
			public string Unit { get; set; }
			public string Value1 { get; set; }
			public string Value2 { get; set; }
		}

		[DepProp]
		List<UnitData> Units { get { return UIHelper<ExpressionHelpUnitsDialog>.GetPropValue<List<UnitData>>(this); } set { UIHelper<ExpressionHelpUnitsDialog>.SetPropValue(this, value); } }

		static ExpressionHelpUnitsDialog() { UIHelper<ExpressionHelpUnitsDialog>.Register(); }

		ExpressionHelpUnitsDialog()
		{
			InitializeComponent();
			Units = ExpressionUnitsConversion.GetConversionConstants().Select(unit => new UnitData
			{
				Unit = unit.fromUnits.ToString(),
				Value1 = $"1 {unit.fromUnits} = {unit.mult} {unit.toUnits}",
				Value2 = $"1 {unit.toUnits} = {1 / unit.mult} {unit.fromUnits}",
			}).ToList();
		}

		void OkClick(object sender, RoutedEventArgs e) => DialogResult = true;

		public static void Run() => new ExpressionHelpUnitsDialog().ShowDialog();
	}
}
