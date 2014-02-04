using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using NeoEdit.UI.Resources;

namespace NeoEdit.UI.BinaryEditorUI
{
	public partial class BinaryEditor : Window
	{
		[DepProp]
		public byte[] Data { get { return uiHelper.GetPropValue<byte[]>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool ShowValues { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long SelStart { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long SelEnd { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }

		static BinaryEditor() { UIHelper<BinaryEditor>.Register(); }

		readonly UIHelper<BinaryEditor> uiHelper;
		public BinaryEditor(byte[] data)
		{
			uiHelper = new UIHelper<BinaryEditor>(this);
			InitializeComponent();
			uiHelper.InitializeCommands();

			Data = data;
			SelStart = SelEnd = 0;
			PreviewKeyDown += (s, e) => uiHelper.RaiseEvent(canvas, e);
			PreviewMouseWheel += (s, e) => uiHelper.RaiseEvent(yScroll, e);

			Show();
		}

		void CommandCallback(object obj)
		{
			switch (obj as string)
			{
				case "View_Values": ShowValues = !ShowValues; break;
			}
		}

		void ScrollBar_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			var scrollBar = sender as ScrollBar;
			scrollBar.Value -= e.Delta;
			e.Handled = true;
		}
	}
}
