﻿using System;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class CombinationsPermutationsDialog
	{
		internal enum CombinationsPermutationsType
		{
			Combinations,
			Permutations,
		}

		internal class Result
		{
			public string Items { get; set; }
			public int UseCount { get; set; }
			public CombinationsPermutationsType Type { get; set; }
			public bool Repeat { get; set; }
		}

		[DepProp]
		public string ItemsRE { get { return UIHelper<CombinationsPermutationsDialog>.GetPropValue<string>(this); } set { UIHelper<CombinationsPermutationsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Items { get { return UIHelper<CombinationsPermutationsDialog>.GetPropValue<string>(this); } set { UIHelper<CombinationsPermutationsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int UseCount { get { return UIHelper<CombinationsPermutationsDialog>.GetPropValue<int>(this); } set { UIHelper<CombinationsPermutationsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public CombinationsPermutationsType Type { get { return UIHelper<CombinationsPermutationsDialog>.GetPropValue<CombinationsPermutationsType>(this); } set { UIHelper<CombinationsPermutationsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Repeat { get { return UIHelper<CombinationsPermutationsDialog>.GetPropValue<bool>(this); } set { UIHelper<CombinationsPermutationsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public double NumResults { get { return UIHelper<CombinationsPermutationsDialog>.GetPropValue<double>(this); } set { UIHelper<CombinationsPermutationsDialog>.SetPropValue(this, value); } }

		static CombinationsPermutationsDialog()
		{
			UIHelper<CombinationsPermutationsDialog>.Register();
			UIHelper<CombinationsPermutationsDialog>.AddCallback(a => a.ItemsRE, (obj, o, n) => obj.CalculateItems());
			UIHelper<CombinationsPermutationsDialog>.AddCallback(a => a.UseCount, (obj, o, n) => obj.CalculateItems());
			UIHelper<CombinationsPermutationsDialog>.AddCallback(a => a.Type, (obj, o, n) => obj.CalculateItems());
			UIHelper<CombinationsPermutationsDialog>.AddCallback(a => a.Repeat, (obj, o, n) => obj.CalculateItems());
		}

		CombinationsPermutationsDialog()
		{
			InitializeComponent();
			ItemsRE = "";
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (!(NumResults > 0))
			{
				MessageBox.Show("No results.");
				return;
			}
			result = new Result { Items = Items, UseCount = UseCount, Type = Type, Repeat = Repeat };
			DialogResult = true;
		}

		int Factorial(int val)
		{
			int result = 1;
			for (var ctr = 2; ctr <= val; ++ctr)
				result *= ctr;
			return result;
		}

		static double CalculateFactorial(int dividend, int divisor1, int divisor2 = 1)
		{
			if (divisor1 < divisor2)
			{
				var tmp = divisor1;
				divisor1 = divisor2;
				divisor2 = tmp;
			}

			double result = 1;
			for (var ctr = divisor1 + 1; ctr <= dividend; ++ctr)
				result *= ctr;

			for (var ctr = 2; ctr <= divisor2; ++ctr)
				result /= ctr;

			return result;
		}

		double GetNumResults()
		{
			if ((UseCount > Items.Length) && (!Repeat))
				return Double.NaN;

			switch (Type)
			{
				case CombinationsPermutationsType.Combinations:
					switch (Repeat)
					{
						case true: return CalculateFactorial(Items.Length + UseCount - 1, UseCount, Items.Length - 1);
						case false: return CalculateFactorial(Items.Length, Items.Length - UseCount, UseCount);
					}
					break;
				case CombinationsPermutationsType.Permutations:
					switch (Repeat)
					{
						case true: return Math.Pow(Items.Length, UseCount);
						case false: return CalculateFactorial(Items.Length, Items.Length - UseCount);
					}
					break;
			}

			return Double.NaN;
		}

		void CalculateItems()
		{
			Items = Misc.GetCharsFromRegexString(ItemsRE);
			NumResults = GetNumResults();
		}

		public static Result Run(Window parent)
		{
			var dialog = new CombinationsPermutationsDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
