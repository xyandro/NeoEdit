namespace NeoEdit.HexEdit
{
	class VirtualQueryInfo
	{
		public bool Committed;
		public bool Mapped;
		public bool NoAccess;
		public int Protect;
		public long StartAddress;
		public long EndAddress;
		public long RegionSize;
	}
}
