using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor
{
	public class TextEditorTabs : Canvas
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
			UIHelper<TextEditorTabs>.AddObservableCallback(a => a.TextEditors, (obj, s, e) => obj.Layout());
			UIHelper<TextEditorTabs>.AddCoerce(a => a.Active, (obj, value) => (value == null) || ((obj.TextEditors != null) && (obj.TextEditors.Contains(value))) ? value : null);
		}

		readonly UIHelper<TextEditorTabs> uiHelper;
		public TextEditorTabs()
		{
			uiHelper = new UIHelper<TextEditorTabs>(this);
			Background = Brushes.Gray;
			UIHelper<TextEditorTabs>.AddCallback(this, ActualWidthProperty, () => Layout());
			UIHelper<TextEditorTabs>.AddCallback(this, ActualHeightProperty, () => Layout());
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

		void Layout()
		{
			Children.Clear();

			if ((TextEditors.Count == 0) || (ActualWidth <= 0) || (ActualHeight <= 0))
				return;

			const double border = 2;
			var columns = (int)Math.Ceiling(Math.Sqrt(TextEditors.Count));
			var rows = (TextEditors.Count + columns - 1) / columns;

			var xPosMult = (ActualWidth + border) / columns;
			var yPosMult = (ActualHeight + border) / rows;
			var xSize = Math.Max(0, xPosMult - border);
			var ySize = Math.Max(0, yPosMult - border);

			for (var count = 0; count < TextEditors.Count; ++count)
			{
				var textEditor = TextEditors[count];
				textEditor.Width = xSize;
				textEditor.Height = ySize;

				Canvas.SetLeft(textEditor, xPosMult * (count % columns));
				Canvas.SetTop(textEditor, yPosMult * (count / columns));
				Children.Add(textEditor);
			}
		}
	}
}
