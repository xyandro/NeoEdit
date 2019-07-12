using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class NumericCombinationsPermutationsDialog
	{
		public enum CombinationsPermutationsType
		{
			Combinations,
			Permutations,
		}

		public class Result
		{
			public int ItemCount { get; set; }
			public int UseCount { get; set; }
			public CombinationsPermutationsType Type { get; set; }
			public bool Repeat { get; set; }
		}

		[DepProp]
		public int ItemCount { get { return UIHelper<NumericCombinationsPermutationsDialog>.GetPropValue<int>(this); } set { UIHelper<NumericCombinationsPermutationsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int UseCount { get { return UIHelper<NumericCombinationsPermutationsDialog>.GetPropValue<int>(this); } set { UIHelper<NumericCombinationsPermutationsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public CombinationsPermutationsType Type { get { return UIHelper<NumericCombinationsPermutationsDialog>.GetPropValue<CombinationsPermutationsType>(this); } set { UIHelper<NumericCombinationsPermutationsDialog>.SetPropValue(this, value); } }
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

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (!(NumResults > 0))
			{
				MessageBox.Show("No results.");
				return;
			}
			result = new Result { ItemCount = ItemCount, UseCount = UseCount, Type = Type, Repeat = Repeat };
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
				case CombinationsPermutationsType.Combinations:
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
				case CombinationsPermutationsType.Permutations:
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
				case CombinationsPermutationsType.Combinations:
					switch (Repeat)
					{
						case true: NumResults = Factorial(ItemCount + UseCount - 1) / Factorial(UseCount) / Factorial(ItemCount - 1); break;
						case false: NumResults = Factorial(ItemCount) / Factorial(ItemCount - UseCount) / Factorial(UseCount); break;
					}
					break;
				case CombinationsPermutationsType.Permutations:
					switch (Repeat)
					{
						case true: NumResults = BigInteger.Pow(ItemCount, UseCount); break;
						case false: NumResults = Factorial(ItemCount) / Factorial(ItemCount - UseCount); break;
					}
					break;
			}
		}

		public static Result Run(Window parent)
		{
			var dialog = new NumericCombinationsPermutationsDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
