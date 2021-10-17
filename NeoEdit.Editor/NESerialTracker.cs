namespace NeoEdit.Editor
{
	public static class NESerialTracker
	{
		public static long NESerial { get; private set; } = 1;
		public static void MoveNext() => ++NESerial;
	}
}
