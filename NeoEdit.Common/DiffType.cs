namespace NeoEdit.Common
{
	public enum DiffType
	{
		Match = 1,
		Mismatch = 2,
		MismatchGap = 4 | HasGap,
		GapMismatch = 8 | HasGap,
		HasGap = 16,
	}
}
