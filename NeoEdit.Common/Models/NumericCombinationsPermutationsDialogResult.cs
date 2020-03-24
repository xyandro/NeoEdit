namespace NeoEdit.Common.Models
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
