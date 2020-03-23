using System.Windows.Controls;
using System.Windows.Data;
using NeoEdit.Common.Transform;
using NeoEdit.Program.Converters;

namespace NeoEdit.Program.Controls
{
	public class CodePageCheckBox : CheckBox
	{
		[DepProp]
		public string FindText { get { return UIHelper<CodePageCheckBox>.GetPropValue<string>(this); } set { UIHelper<CodePageCheckBox>.SetPropValue(this, value); } }
		[DepProp]
		public Coder.CodePage CodePage { get { return UIHelper<CodePageCheckBox>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<CodePageCheckBox>.SetPropValue(this, value); } }

		static CodePageCheckBox()
		{
			UIHelper<CodePageCheckBox>.Register();
			UIHelper<CodePageCheckBox>.AddCallback(a => a.CodePage, (obj, o, n) => obj.Content = Coder.IsStr(obj.CodePage) ? Coder.GetDescription(obj.CodePage) : obj.CodePage.ToString());
		}

		public CodePageCheckBox()
		{
			CodePage = Coder.CodePage.None;
			var mb = new MultiBinding { Converter = new ValidValueConverter() };
			mb.Bindings.Add(new Binding(nameof(FindText)));
			mb.Bindings.Add(new Binding(nameof(CodePage)));
			SetBinding(IsEnabledProperty, mb);
		}
	}
}
