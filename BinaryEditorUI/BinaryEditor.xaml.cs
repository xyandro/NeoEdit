using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using NeoEdit.Common;

namespace NeoEdit.BinaryEditorUI
{
	public partial class BinaryEditor : Window
	{
		[DepProp]
		public BinaryData Data { get { return uiHelper.GetPropValue<BinaryData>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool ShowValues { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long SelStart { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long SelEnd { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public string FoundText { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool Insert { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public BinaryData.EncodingName TypeEncoding { get { return uiHelper.GetPropValue<BinaryData.EncodingName>(); } set { uiHelper.SetPropValue(value); } }

		static BinaryEditor() { UIHelper<BinaryEditor>.Register(); }

		readonly UIHelper<BinaryEditor> uiHelper;
		public BinaryEditor(BinaryData data)
		{
			uiHelper = new UIHelper<BinaryEditor>(this);
			InitializeComponent();
			uiHelper.InitializeCommands();

			Data = data;
			MouseWheel += (s, e) => uiHelper.RaiseEvent(yScroll, e);
			TextInput += (s, e) => uiHelper.RaiseEvent(canvas, e);

			yScroll.MouseWheel += (s, e) => (s as ScrollBar).Value -= e.Delta;

			Show();
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Handled)
				return;

			switch (e.Key)
			{
				case Key.Escape: canvas.Focus(); break;
				default: uiHelper.RaiseEvent(canvas, e); break;
			}
		}

		void CommandRun(string name, object parameter)
		{
			canvas.HandleCommand(name);

			switch (name)
			{
				case "View_Values": ShowValues = !ShowValues; break;
			}
		}

		void TypeEncodingClick(object sender, RoutedEventArgs e)
		{
			canvas.TypeEncoding = Helpers.ParseEnum<BinaryData.EncodingName>(((MenuItem)e.Source).Header as string);
		}
	}
}
