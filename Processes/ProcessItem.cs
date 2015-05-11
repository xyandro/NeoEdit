using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.Processes
{
	public class ProcessItem : DependencyObject
	{
		[DepProp]
		public int PID { get { return UIHelper<ProcessItem>.GetPropValue<int>(this); } set { UIHelper<ProcessItem>.SetPropValue(this, value); } }
		[DepProp]
		public string Name { get { return UIHelper<ProcessItem>.GetPropValue<string>(this); } set { UIHelper<ProcessItem>.SetPropValue(this, value); } }
		[DepProp]
		public string Title { get { return UIHelper<ProcessItem>.GetPropValue<string>(this); } set { UIHelper<ProcessItem>.SetPropValue(this, value); } }
		[DepProp]
		public long Size { get { return UIHelper<ProcessItem>.GetPropValue<long>(this); } set { UIHelper<ProcessItem>.SetPropValue(this, value); } }
		[DepProp]
		public double CPU { get { return UIHelper<ProcessItem>.GetPropValue<double>(this); } set { UIHelper<ProcessItem>.SetPropValue(this, value); } }
		[DepProp]
		public int ParentPID { get { return UIHelper<ProcessItem>.GetPropValue<int>(this); } set { UIHelper<ProcessItem>.SetPropValue(this, value); } }

		static ProcessItem() { UIHelper<ProcessItem>.Register(); }
	}
}
