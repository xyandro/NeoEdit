using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor
{
	public partial class TextEditorTabs
	{
		[DepProp]
		public ObservableCollection<TextEditor> TextEditors { get { return uiHelper.GetPropValue<ObservableCollection<TextEditor>>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public TextEditor Active { get { return uiHelper.GetPropValue<TextEditor>(); } set { uiHelper.SetPropValue(value); } }

		static TextEditorTabs() { UIHelper<TextEditorTabs>.Register(); }

		readonly UIHelper<TextEditorTabs> uiHelper;
		public TextEditorTabs()
		{
			InitializeComponent();
			uiHelper = new UIHelper<TextEditorTabs>(this);
		}
	}

	public class NoFocusTabControl : TabControl
	{
		protected override void OnKeyDown(KeyEventArgs e) { }
	}
}
