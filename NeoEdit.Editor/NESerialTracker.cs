namespace NeoEdit.Editor
{
	public static class NESerialTracker
	{
		public static int NESerial { get; private set; } = 1;
		public static void MoveNext() => ++NESerial;
	}
}
