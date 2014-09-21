using NeoEdit.GUI.Common;
using NeoEdit.GUI.ItemGridControl;

namespace NeoEdit.Processes
{
	public class ProcessItem : ItemGridItem<ProcessItem>
	{
		[DepProp]
		public int PID { get { return GetValue<int>(); } set { SetValue(value); } }
		[DepProp]
		public string Name { get { return GetValue<string>(); } set { SetValue(value); } }
		[DepProp]
		public string Title { get { return GetValue<string>(); } set { SetValue(value); } }
		[DepProp]
		public long Size { get { return GetValue<long>(); } set { SetValue(value); } }
		[DepProp]
		public double CPU { get { return GetValue<double>(); } set { SetValue(value); } }
		[DepProp]
		public int ParentPID { get { return GetValue<int>(); } set { SetValue(value); } }
	}
}
