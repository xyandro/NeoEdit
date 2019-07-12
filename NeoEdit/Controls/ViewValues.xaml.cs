using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program.Controls
{
	partial class ViewValues
	{
		[DepProp]
		public IList<byte> Data { get { return UIHelper<ViewValues>.GetPropValue<IList<byte>>(this); } set { UIHelper<ViewValues>.SetPropValue(this, value); } }
		[DepProp]
		public bool HasSel { get { return UIHelper<ViewValues>.GetPropValue<bool>(this); } set { UIHelper<ViewValues>.SetPropValue(this, value); } }
		[DepProp]
		public string FindValue { get { return UIHelper<ViewValues>.GetPropValue<string>(this); } set { UIHelper<ViewValues>.SetPropValue(this, value); } }
		[DepProp]
		public bool LittleEndian { get { return UIHelper<ViewValues>.GetPropValue<bool>(this); } set { UIHelper<ViewValues>.SetPropValue(this, value); } }
		[DepProp]
		public bool BigEndian { get { return UIHelper<ViewValues>.GetPropValue<bool>(this); } set { UIHelper<ViewValues>.SetPropValue(this, value); } }
		[DepProp]
		public bool Floats { get { return UIHelper<ViewValues>.GetPropValue<bool>(this); } set { UIHelper<ViewValues>.SetPropValue(this, value); } }
		[DepProp]
		public ObservableCollection<Coder.CodePage> Strings { get { return UIHelper<ViewValues>.GetPropValue<ObservableCollection<Coder.CodePage>>(this); } set { UIHelper<ViewValues>.SetPropValue(this, value); } }

		static ViewValues() => UIHelper<ViewValues>.Register();

		public ViewValues()
		{
			InitializeComponent();
			OnReset();
		}

		void OnStrings(object sender, RoutedEventArgs e)
		{
			var window = UIHelper.FindParent<Window>(this);
			var strings = ViewValuesStringsDialog.Run(window, Strings.ToList());
			if (strings != null)
				Strings = new ObservableCollection<Coder.CodePage>(strings);
		}

		void OnReset(object sender = null, RoutedEventArgs e = null)
		{
			LittleEndian = true;
			BigEndian = Floats = false;
			Strings = new ObservableCollection<Coder.CodePage> { Coder.CodePage.UTF8, Coder.CodePage.UTF16LE };
		}
	}
}
