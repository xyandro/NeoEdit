using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.Processes
{
	public class ProcessItem : DependencyObject
	{
		[DepProp]
		public int PID { get { return UIHelper<ProcessItem>.GetPropValue(() => this.PID); } set { UIHelper<ProcessItem>.SetPropValue(() => this.PID, value); } }
		[DepProp]
		public string Name { get { return UIHelper<ProcessItem>.GetPropValue(() => this.Name); } set { UIHelper<ProcessItem>.SetPropValue(() => this.Name, value); } }
		[DepProp]
		public string Title { get { return UIHelper<ProcessItem>.GetPropValue(() => this.Title); } set { UIHelper<ProcessItem>.SetPropValue(() => this.Title, value); } }
		[DepProp]
		public long Size { get { return UIHelper<ProcessItem>.GetPropValue(() => this.Size); } set { UIHelper<ProcessItem>.SetPropValue(() => this.Size, value); } }
		[DepProp]
		public double CPU { get { return UIHelper<ProcessItem>.GetPropValue(() => this.CPU); } set { UIHelper<ProcessItem>.SetPropValue(() => this.CPU, value); } }
		[DepProp]
		public int ParentPID { get { return UIHelper<ProcessItem>.GetPropValue(() => this.ParentPID); } set { UIHelper<ProcessItem>.SetPropValue(() => this.ParentPID, value); } }

		static ProcessItem() { UIHelper<ProcessItem>.Register(); }
	}
}
