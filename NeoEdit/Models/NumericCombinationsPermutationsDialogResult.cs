using System;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Models
{
	public class NumericCombinationsPermutationsDialogResult
	{
		public enum CombinationsPermutationsType
		{
			Combinations,
			Permutations,
		}

		public int ItemCount { get; set; }
		public int UseCount { get; set; }
		public CombinationsPermutationsType Type { get; set; }
		public bool Repeat { get; set; }
	}
}
