using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor
{
	public class TextEditorTabs : Grid
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
			UIHelper<TextEditorTabs>.AddCallback(a => a.View, (obj, o, n) => obj.Layout());
			UIHelper<TextEditorTabs>.AddCallback(a => a.Active, (obj, o, n) => obj.Layout());
			UIHelper<TextEditorTabs>.AddObservableCallback(a => a.TextEditors, (obj, s, e) => obj.SetActive(e));
			UIHelper<TextEditorTabs>.AddObservableCallback(a => a.TextEditors, (obj, s, e) => obj.Layout());
			UIHelper<TextEditorTabs>.AddCoerce(a => a.Active, (obj, value) => (value == null) || ((obj.TextEditors != null) && (obj.TextEditors.Contains(value))) ? value : null);
		}

		readonly UIHelper<TextEditorTabs> uiHelper;
		public TextEditorTabs()
		{
			uiHelper = new UIHelper<TextEditorTabs>(this);
			Background = Brushes.Gray;
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
			RowDefinitions.Clear();
			ColumnDefinitions.Clear();

			if (TextEditors.Count == 0)
				return;

			if (View == ViewType.Tiles)
				LayoutTiles();
			else
				LayoutTabs();
		}

		Label GetLabel(TextEditor textEditor, bool tile)
		{
			var label = new Label
			{
				Background = textEditor == Active ? Brushes.LightBlue : Brushes.LightGray,
				Padding = new Thickness(10, 2, 10, 2),
				Margin = new Thickness(0, 0, tile ? 0 : 2, 1),
				Target = textEditor,
				AllowDrop = true,
			};
			label.MouseLeftButtonDown += (s, e) => Active = label.Target as TextEditor;
			var multiBinding = new MultiBinding { Converter = new NeoEdit.GUI.Common.ExpressionConverter(), ConverterParameter = @"([0]==''?'[Untitled]':FileName:[0])t+([1]!=0?'*':'')" };
			multiBinding.Bindings.Add(new Binding("FileName") { Source = textEditor });
			multiBinding.Bindings.Add(new Binding("ModifiedSteps") { Source = textEditor });
			label.SetBinding(Label.ContentProperty, multiBinding);

			label.MouseMove += (s, e) =>
			{
				if (e.LeftButton == MouseButtonState.Pressed)
				{
					DragDrop.DoDragDrop(this, new DataObject(typeof(TextEditorTabs), label), DragDropEffects.Move);
				}
			};

			label.Drop += (s, e) =>
			{
				var editor = (e.Data.GetData(typeof(TextEditorTabs)) as Label).Target as TextEditor;
				var fromIndex = TextEditors.IndexOf(editor);
				var toIndex = TextEditors.IndexOf((s as Label).Target as TextEditor);
				TextEditors.RemoveAt(fromIndex);
				TextEditors.Insert(toIndex, editor);
				Active = editor;
			};

			return label;
		}

		void LayoutTiles()
		{
			const double border = 2;

			var columns = (int)Math.Ceiling(Math.Sqrt(TextEditors.Count));
			var rows = (TextEditors.Count + columns - 1) / columns;

			for (var ctr = 0; ctr < columns; ++ctr)
			{
				if (ctr != 0)
					ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(border) });
				ColumnDefinitions.Add(new ColumnDefinition());
			}

			for (var ctr = 0; ctr < rows; ++ctr)
			{
				if (ctr != 0)
					RowDefinitions.Add(new RowDefinition { Height = new GridLength(border) });
				RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
				RowDefinitions.Add(new RowDefinition());
			}

			int count = 0;
			foreach (var textEditor in TextEditors)
			{
				var column = count % columns * 2;
				var row = count / columns * 3;

				var label = GetLabel(textEditor, true);
				Grid.SetColumn(label, column);
				Grid.SetRow(label, row);
				Children.Add(label);

				Grid.SetColumn(textEditor, column);
				Grid.SetRow(textEditor, row + 1);
				Children.Add(textEditor);

				++count;
			}
		}

		void LayoutTabs()
		{
			ColumnDefinitions.Add(new ColumnDefinition());
			RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
			RowDefinitions.Add(new RowDefinition());

			var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
			foreach (var textEditor in TextEditors)
				stackPanel.Children.Add(GetLabel(textEditor, false));
			Children.Add(stackPanel);

			Grid.SetRow(Active, 1);
			Grid.SetColumn(Active, 0);
			Children.Add(Active);
		}
	}
}
