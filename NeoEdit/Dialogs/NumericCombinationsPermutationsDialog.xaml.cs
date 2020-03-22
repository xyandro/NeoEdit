using System;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Models;

namespace NeoEdit.Program.Dialogs
{
	partial class NumericCombinationsPermutationsDialog
	{
		[DepProp]
		public int ItemCount { get { return UIHelper<NumericCombinationsPermutationsDialog>.GetPropValue<int>(this); } set { UIHelper<NumericCombinationsPermutationsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int UseCount { get { return UIHelper<NumericCombinationsPermutationsDialog>.GetPropValue<int>(this); } set { UIHelper<NumericCombinationsPermutationsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public NumericCombinationsPermutationsDialogResult.CombinationsPermutationsType Type { get { return UIHelper<NumericCombinationsPermutationsDialog>.GetPropValue<NumericCombinationsPermutationsDialogResult.CombinationsPermutationsType>(this); } set { UIHelper<NumericCombinationsPermutationsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Repeat { get { return UIHelper<NumericCombinationsPermutationsDialog>.GetPropValue<bool>(this); } set { UIHelper<NumericCombinationsPermutationsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public BigInteger? NumResults { get { return UIHelper<NumericCombinationsPermutationsDialog>.GetPropValue<BigInteger?>(this); } set { UIHelper<NumericCombinationsPermutationsDialog>.SetPropValue(this, value); } }

		static NumericCombinationsPermutationsDialog()
		{
			UIHelper<NumericCombinationsPermutationsDialog>.Register();
			UIHelper<NumericCombinationsPermutationsDialog>.AddCallback(a => a.ItemCount, (obj, o, n) => obj.SetNumResults());
			UIHelper<NumericCombinationsPermutationsDialog>.AddCallback(a => a.UseCount, (obj, o, n) => obj.SetNumResults());
			UIHelper<NumericCombinationsPermutationsDialog>.AddCallback(a => a.Type, (obj, o, n) => { obj.SetFormula(); obj.SetNumResults(); });
			UIHelper<NumericCombinationsPermutationsDialog>.AddCallback(a => a.Repeat, (obj, o, n) => { obj.SetFormula(); obj.SetNumResults(); });
		}

		NumericCombinationsPermutationsDialog()
		{
			InitializeComponent();
			SetFormula();
		}

		NumericCombinationsPermutationsDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (!(NumResults > 0))
			{
				MessageBox.Show("No results.");
				return;
			}
			result = new NumericCombinationsPermutationsDialogResult { ItemCount = ItemCount, UseCount = UseCount, Type = Type, Repeat = Repeat };
			DialogResult = true;
		}

		static BigInteger Factorial(BigInteger number)
		{
			BigInteger result = 1;
			while (number > 1)
			{
				result *= number;
				--number;
			}
			return result;
		}

		void SetFormula()
		{
			if (formula == null)
				return;

			formula.Children.Clear();

			switch (Type)
			{
				case NumericCombinationsPermutationsDialogResult.CombinationsPermutationsType.Combinations:
					switch (Repeat)
					{
						case true:
							formula.Children.Add(new Label { Content = "(n+r-1)!", FontStyle = FontStyles.Italic, HorizontalAlignment = HorizontalAlignment.Center });
							formula.Children.Add(new Separator());
							formula.Children.Add(new Label { Content = "r!(n-1)!", FontStyle = FontStyles.Italic, HorizontalAlignment = HorizontalAlignment.Center });
							break;
						case false:
							formula.Children.Add(new Label { Content = "n!", FontStyle = FontStyles.Italic, HorizontalAlignment = HorizontalAlignment.Center });
							formula.Children.Add(new Separator());
							formula.Children.Add(new Label { Content = "(n-r)!r!", FontStyle = FontStyles.Italic, HorizontalAlignment = HorizontalAlignment.Center });
							break;
					}
					break;
				case NumericCombinationsPermutationsDialogResult.CombinationsPermutationsType.Permutations:
					switch (Repeat)
					{
						case true:
							var tb = new TextBlock();
							tb.Inlines.Add(new Run("n") { FontStyle = FontStyles.Italic });
							tb.Inlines.Add(new Run("r") { FontStyle = FontStyles.Italic, BaselineAlignment = BaselineAlignment.Superscript });
							formula.Children.Add(tb);
							break;
						case false:
							formula.Children.Add(new Label { Content = "n!", FontStyle = FontStyles.Italic, HorizontalAlignment = HorizontalAlignment.Center });
							formula.Children.Add(new Separator());
							formula.Children.Add(new Label { Content = "(n-r)!", FontStyle = FontStyles.Italic, HorizontalAlignment = HorizontalAlignment.Center });
							break;
					}
					break;
			}
		}

		void SetNumResults()
		{
			NumResults = null;
			if ((UseCount > ItemCount) && (!Repeat))
				return;

			switch (Type)
			{
				case NumericCombinationsPermutationsDialogResult.CombinationsPermutationsType.Combinations:
					switch (Repeat)
					{
						case true: NumResults = Factorial(ItemCount + UseCount - 1) / Factorial(UseCount) / Factorial(ItemCount - 1); break;
						case false: NumResults = Factorial(ItemCount) / Factorial(ItemCount - UseCount) / Factorial(UseCount); break;
					}
					break;
				case NumericCombinationsPermutationsDialogResult.CombinationsPermutationsType.Permutations:
					switch (Repeat)
					{
						case true: NumResults = BigInteger.Pow(ItemCount, UseCount); break;
						case false: NumResults = Factorial(ItemCount) / Factorial(ItemCount - UseCount); break;
					}
					break;
			}
		}

		public static NumericCombinationsPermutationsDialogResult Run(Window parent)
		{
			var dialog = new NumericCombinationsPermutationsDialog { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
