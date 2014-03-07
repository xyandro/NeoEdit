using System.Reflection;
using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.GUI.AboutUI
{
	public partial class About : Window
	{
		[DepProp]
		string Product { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		string Version { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		string Copyright { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }

		static About() { UIHelper<About>.Register(); }

		readonly UIHelper<About> uiHelper;
		public About()
		{
			uiHelper = new UIHelper<About>(this);
			InitializeComponent();

			Product = ((AssemblyProductAttribute)Assembly.GetEntryAssembly().GetCustomAttribute(typeof(AssemblyProductAttribute))).Product;
			Version = ((AssemblyFileVersionAttribute)Assembly.GetEntryAssembly().GetCustomAttribute(typeof(AssemblyFileVersionAttribute))).Version;
			Copyright = ((AssemblyCopyrightAttribute)Assembly.GetEntryAssembly().GetCustomAttribute(typeof(AssemblyCopyrightAttribute))).Copyright;
		}
	}
}
