using System;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Numeric_CombinationsPermutations_Dialog
	{
		[DepProp]
		public int ItemCount { get { return UIHelper<Numeric_CombinationsPermutations_Dialog>.GetPropValue<int>(this); } set { UIHelper<Numeric_CombinationsPermutations_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public int UseCount { get { return UIHelper<Numeric_CombinationsPermutations_Dialog>.GetPropValue<int>(this); } set { UIHelper<Numeric_CombinationsPermutations_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public Configuration_Numeric_CombinationsPermutations.CombinationsPermutationsType Type { get { return UIHelper<Numeric_CombinationsPermutations_Dialog>.GetPropValue<Configuration_Numeric_CombinationsPermutations.CombinationsPermutationsType>(this); } set { UIHelper<Numeric_CombinationsPermutations_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Repeat { get { return UIHelper<Numeric_CombinationsPermutations_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Numeric_CombinationsPermutations_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public BigInteger? NumResults { get { return UIHelper<Numeric_CombinationsPermutations_Dialog>.GetPropValue<BigInteger?>(this); } set { UIHelper<Numeric_CombinationsPermutations_Dialog>.SetPropValue(this, value); } }

		static Numeric_CombinationsPermutations_Dialog()
		{
			UIHelper<Numeric_CombinationsPermutations_Dialog>.Register();
			UIHelper<Numeric_CombinationsPermutations_Dialog>.AddCallback(a => a.ItemCount, (obj, o, n) => obj.SetNumResults());
			UIHelper<Numeric_CombinationsPermutations_Dialog>.AddCallback(a => a.UseCount, (obj, o, n) => obj.SetNumResults());
			UIHelper<Numeric_CombinationsPermutations_Dialog>.AddCallback(a => a.Type, (obj, o, n) => { obj.SetFormula(); obj.SetNumResults(); });
			UIHelper<Numeric_CombinationsPermutations_Dialog>.AddCallback(a => a.Repeat, (obj, o, n) => { obj.SetFormula(); obj.SetNumResults(); });
		}

		Numeric_CombinationsPermutations_Dialog()
		{
			InitializeComponent();
			SetFormula();
		}

		Configuration_Numeric_CombinationsPermutations result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (!(NumResults > 0))
			{
				MessageBox.Show("No results.");
				return;
			}
			result = new Configuration_Numeric_CombinationsPermutations { ItemCount = ItemCount, UseCount = UseCount, Type = Type, Repeat = Repeat };
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
				case Configuration_Numeric_CombinationsPermutations.CombinationsPermutationsType.Combinations:
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
				case Configuration_Numeric_CombinationsPermutations.CombinationsPermutationsType.Permutations:
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
				case Configuration_Numeric_CombinationsPermutations.CombinationsPermutationsType.Combinations:
					switch (Repeat)
					{
						case true: NumResults = Factorial(ItemCount + UseCount - 1) / Factorial(UseCount) / Factorial(ItemCount - 1); break;
						case false: NumResults = Factorial(ItemCount) / Factorial(ItemCount - UseCount) / Factorial(UseCount); break;
					}
					break;
				case Configuration_Numeric_CombinationsPermutations.CombinationsPermutationsType.Permutations:
					switch (Repeat)
					{
						case true: NumResults = BigInteger.Pow(ItemCount, UseCount); break;
						case false: NumResults = Factorial(ItemCount) / Factorial(ItemCount - UseCount); break;
					}
					break;
			}
		}

		public static Configuration_Numeric_CombinationsPermutations Run(Window parent)
		{
			var dialog = new Numeric_CombinationsPermutations_Dialog { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
