using System.Windows.Controls;
using NeoEdit.Common.Transform;

namespace NeoEdit.GUI.Controls
{
	public class CodePageCheckBox : CheckBox
	{
		[DepProp]
		public Coder.CodePage CodePage { get { return UIHelper<CodePageCheckBox>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<CodePageCheckBox>.SetPropValue(this, value); } }

		static CodePageCheckBox()
		{
			UIHelper<CodePageCheckBox>.Register();
			UIHelper<CodePageCheckBox>.AddCallback(a => a.CodePage, (obj, o, n) => obj.Content = Coder.IsStr(obj.CodePage) ? Coder.GetDescription(obj.CodePage) : obj.CodePage.ToString());
		}

		public CodePageCheckBox() { CodePage = Coder.CodePage.None; }
	}
}
