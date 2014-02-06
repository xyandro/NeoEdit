using System.Windows;
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

		bool shiftDown { get { return (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None; } }

		static BinaryEditor() { UIHelper<BinaryEditor>.Register(); }

		readonly UIHelper<BinaryEditor> uiHelper;
		public BinaryEditor(BinaryData data)
		{
			uiHelper = new UIHelper<BinaryEditor>(this);
			InitializeComponent();
			uiHelper.InitializeCommands();

			Data = data;
			SelStart = SelEnd = 0;
			PreviewMouseWheel += (s, e) => uiHelper.RaiseEvent(yScroll, e);

			Show();
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.F3: FindNext(!shiftDown); break;
				default: uiHelper.RaiseEvent(canvas, e); break;
			}
		}

		FindData currentFind;
		void FindNext(bool forward = true)
		{
			if (currentFind == null)
				return;

			long start = SelStart;
			long end = SelEnd;
			if (Data.Find(currentFind, ref start, ref end, forward))
			{
				SelStart = start;
				SelEnd = end;
			}
		}

		void CommandCallback(object obj)
		{
			switch (obj as string)
			{
				case "Edit_Find":
					{
						var results = FindDialog.Run();
						if (results != null)
						{
							currentFind = results;
							FoundText = currentFind.FindText;
							FindNext();
						}
					}
					break;
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
