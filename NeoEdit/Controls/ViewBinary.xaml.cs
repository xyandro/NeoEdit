using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.Common;
using NeoEdit.Common.Transform;

namespace NeoEdit.Program.Controls
{
	partial class ViewBinary
	{
		static ViewBinary() => UIHelper<ViewBinary>.Register();

		public ViewBinary() => InitializeComponent();

		public void SetData(IList<byte> data, bool hasSel, HashSet<Coder.CodePage> codePages, IReadOnlyList<HashSet<string>> searches)
		{
			SetupNumeric(data, hasSel, codePages, searches);
			SetupStrings(data, hasSel, codePages, searches);
		}

		void SetupNumeric(IList<byte> data, bool hasSel, HashSet<Coder.CodePage> codePages, IReadOnlyList<HashSet<string>> searches)
		{
			var controls = new List<FrameworkElement> { LE, Int08LEHeader, Int16LEHeader, Int32LEHeader, Int64LEHeader, Int08LESizing, Int16LESizing, Int32LESizing, Int64LESizing, SIntLEHeader, UIntLEHeader, SInt08LE, SInt16LE, SInt32LE, SInt64LE, UInt08LE, UInt16LE, UInt32LE, UInt64LE, BE, Int08BEHeader, Int16BEHeader, Int32BEHeader, Int64BEHeader, Int08BESizing, Int16BESizing, Int32BESizing, Int64BESizing, SIntBEHeader, UIntBEHeader, SInt08BE, SInt16BE, SInt32BE, SInt64BE, UInt08BE, UInt16BE, UInt32BE, UInt64BE, Float, SingleHeader, DoubleHeader, Single, Double };
			controls.ForEach(control => control.Visibility = GetVisibility(codePages, control.Tag as string));
			controls.OfType<ViewValue>().ForEach(viewValue => viewValue.SetData(data, hasSel, searches));
		}

		bool HasCodePage(HashSet<Coder.CodePage> codePages, string find)
		{
			if ((find.Length == 8) && (find.Substring(1, 5) == "Int08"))
			{
				var isLE = find.EndsWith("LE");
				find = find.Substring(0, 4) + "8";
				var list = codePages.Intersect(Coder.GetNumericCodePages()).Select(x => x.ToString()).ToList();
				var hasLE = list.Any(x => x.EndsWith("LE"));
				var hasBE = list.Any(x => x.EndsWith("BE"));
				if ((isLE) && (!hasLE) && (hasBE))
					return false;
				if ((!isLE) && (!hasBE))
					return false;
			}
			if (find.StartsWith("SInt"))
				find = find.Substring(1);
			return codePages.Any(x => x.ToString() == find);
		}

		Visibility GetVisibility(HashSet<Coder.CodePage> codePages, string find)
		{
			if (find.Split('|').Any(str => HasCodePage(codePages, str)))
				return Visibility.Visible;
			return Visibility.Collapsed;
		}

		void SetupStrings(IList<byte> data, bool hasSel, HashSet<Coder.CodePage> codePages, IReadOnlyList<HashSet<string>> searches)
		{
			stringsGrid.RowDefinitions.Clear();
			stringsGrid.Children.Clear();

			var strCodePages = codePages.Where(codePage => Coder.IsStr(codePage)).ToList();
			if (!strCodePages.Any())
			{
				strings.Visibility = Visibility.Collapsed;
				return;
			}

			strings.Visibility = Visibility.Visible;
			for (var ctr = 0; ctr < strCodePages.Count; ++ctr)
			{
				stringsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

				var textBlock = new TextBlock
				{
					Text = Coder.GetDescription(strCodePages[ctr], true),
					VerticalAlignment = VerticalAlignment.Center,
				};
				Grid.SetColumn(textBlock, 0);
				Grid.SetRow(textBlock, ctr);
				stringsGrid.Children.Add(textBlock);

				var viewValue = new ViewValue
				{
					CodePage = strCodePages[ctr],
					VerticalAlignment = VerticalAlignment.Center,
					TextAlignment = HorizontalAlignment.Left,
					TextMargin = new Thickness(4, 0, 4, 0),
				};
				viewValue.SetData(data, hasSel, searches);
				Grid.SetColumn(viewValue, 1);
				Grid.SetRow(viewValue, ctr);
				stringsGrid.Children.Add(viewValue);
			}
		}

		void OnStringsListSizeChanged(object sender, SizeChangedEventArgs e)
		{
			var listView = sender as ListView;
			var gridView = listView.View as GridView;
			var width = listView.ActualWidth - SystemParameters.VerticalScrollBarWidth;
			for (var ctr = 0; ctr < gridView.Columns.Count - 1; ++ctr)
				width -= gridView.Columns[ctr].ActualWidth;
			gridView.Columns[gridView.Columns.Count - 1].Width = width;
		}
	}
}
