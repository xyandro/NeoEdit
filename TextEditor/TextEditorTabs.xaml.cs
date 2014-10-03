using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor
{
	public partial class TextEditorTabs
	{
		public enum ViewType
		{
			Tabs,
			Tiles,
		}

		[DepProp]
		public ObservableCollection<TextEditor> TextEditors { get { return uiHelper.GetPropValue<ObservableCollection<TextEditor>>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public TextEditor Active { get { return uiHelper.GetPropValue<TextEditor>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public ViewType View { get { return uiHelper.GetPropValue<ViewType>(); } set { uiHelper.SetPropValue(value); } }

		static TextEditorTabs()
		{
			UIHelper<TextEditorTabs>.Register();
			UIHelper<TextEditorTabs>.AddObservableCallback(a => a.TextEditors, (obj, s, e) => obj.SetActive(e));
			UIHelper<TextEditorTabs>.AddCoerce(a => a.Active, (obj, value) => (value == null) || ((obj.TextEditors != null) && (obj.TextEditors.Contains(value))) ? value : null);
		}

		readonly UIHelper<TextEditorTabs> uiHelper;
		public TextEditorTabs()
		{
			InitializeComponent();
			uiHelper = new UIHelper<TextEditorTabs>(this);
		}

		public void MovePrev()
		{
			var index = TextEditors.IndexOf(Active) - 1;
			if (index < 0)
				index = TextEditors.Count - 1;
			if (index >= 0)
				Active = TextEditors[index];
		}

		public void MoveNext()
		{
			var index = TextEditors.IndexOf(Active) + 1;
			if (index >= TextEditors.Count)
				index = 0;
			if (index < TextEditors.Count)
				Active = TextEditors[index];
		}

		void SetActive(NotifyCollectionChangedEventArgs e)
		{
			if (e == null)
			{
				Active = TextEditors.FirstOrDefault();
				return;
			}

			if (Active == null)
			{
				Active = TextEditors.FirstOrDefault();
				return;
			}

			if (e.Action == NotifyCollectionChangedAction.Move)
				return;
			if (e.Action == NotifyCollectionChangedAction.Reset)
			{
				Active = null;
				return;
			}

			if (e.OldItems == null)
				return;
			int index = e.OldItems.IndexOf(Active);
			if (index == -1)
				return;

			index += e.OldStartingIndex;
			index = Math.Min(index, TextEditors.Count - 1);
			if (index < 0)
				Active = null;
			else
				Active = TextEditors[index];
		}

		protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			foreach (var editor in TextEditors)
				if (editor.IsMouseOver)
					Active = editor;

			base.OnPreviewMouseLeftButtonDown(e);
		}
	}

	public class NoFocusTabControl : TabControl
	{
		protected override void OnKeyDown(KeyEventArgs e) { }
	}

	public class TabsPanel : ItemsControl
	{
		public TabsPanel()
		{
			HorizontalAlignment = HorizontalAlignment.Left;
			VerticalAlignment = VerticalAlignment.Top;
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			var infiniteSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
			base.MeasureOverride(infiniteSize);
			return availableSize;
		}

		protected override Size ArrangeOverride(Size arrangeBounds)
		{
			base.ArrangeOverride(arrangeBounds);

			const double border = 2;
			if (Items.Count == 0)
				return arrangeBounds;

			var columns = (int)Math.Ceiling(Math.Sqrt(Items.Count));
			var rows = (Items.Count + columns - 1) / columns;

			var xPosMult = (arrangeBounds.Width + border) / columns;
			var yPosMult = (arrangeBounds.Height + border) / rows;
			var xSize = xPosMult - border;
			var ySize = yPosMult - border;

			for (var count = 0; count < Items.Count; ++count)
				(Items[count] as UIElement).Arrange(new Rect(xPosMult * (count % columns), yPosMult * (count / columns), xSize, ySize));

			return arrangeBounds;
		}
	}
}
