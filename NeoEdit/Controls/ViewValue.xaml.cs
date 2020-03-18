using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program.Controls
{
	partial class ViewValue
	{
		static readonly Brush NotFoundBorderBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64));
		static readonly Brush NotFoundBackgroundBrush = Brushes.Transparent;
		static readonly Brush FoundBorderBrush = new SolidColorBrush(Color.FromArgb(192, 38, 132, 255));
		static readonly Brush FoundBackgroundBrush = new SolidColorBrush(Color.FromArgb(32, 38, 132, 255));

		static ViewValue()
		{
			NotFoundBorderBrush.Freeze();
			NotFoundBackgroundBrush.Freeze();
			FoundBorderBrush.Freeze();
			FoundBackgroundBrush.Freeze();
		}

		public Coder.CodePage CodePage { get; set; } = Coder.CodePage.None;
		public IList<byte> Data { get; set; }
		public bool HasSel { get; set; }
		public string Text { get => text.Text; set => text.Text = value; }
		public HorizontalAlignment TextAlignment { get => text.HorizontalAlignment; set => text.HorizontalAlignment = value; }
		public Thickness TextMargin { get => border.Margin; set => border.Margin = value; }

		public ViewValue() => InitializeComponent();

		public string GetValue()
		{
			if (Data == null)
				return null;

			var data = Data;
			if (!Coder.IsStr(CodePage))
			{
				var size = Coder.BytesRequired(CodePage);
				if ((!HasSel) && (data.Count > size))
				{
					var newData = new byte[size];
					Array.Copy(data as byte[], newData, size);
					data = newData;
				}
				if (data.Count != size)
					return null;
			}

			return Coder.TryBytesToString(data as byte[], CodePage);
		}

		public void SetData(IList<byte> data, bool hasSel, IReadOnlyList<HashSet<string>> searches)
		{
			border.BorderBrush = NotFoundBorderBrush;
			border.Background = NotFoundBackgroundBrush;

			if (CodePage == Coder.CodePage.None)
				return;

			if (Visibility != Visibility.Visible)
			{
				text.Text = null;
				return;
			}

			Data = data;
			HasSel = hasSel;
			var value = GetValue();
			text.Text = Font.RemoveSpecialChars(value ?? "");
			if ((value != null) && (searches != null) && (searches.Any(search => search.Contains(value))))
			{
				border.BorderBrush = FoundBorderBrush;
				border.Background = FoundBackgroundBrush;
			}
		}

		void OnClick(object sender, MouseButtonEventArgs e)
		{
			if ((Coder.IsStr(CodePage)) && (!HasSel))
				return;

			var value = GetValue();
			if (value == null)
				return;

			byte[] newBytes;
			try
			{
				while (true)
				{
					value = ViewBinaryEditValueDialog.Run(UIHelper.FindParent<Window>(this), value);
					newBytes = Coder.TryStringToBytes(value, CodePage);
					if (newBytes != null)
						break;
				}
			}
			catch { return; }

			int? size = null;
			if (!Coder.IsStr(CodePage))
				size = Coder.BytesRequired(CodePage);

			UIHelper.FindParent<TabsWindow>(this).HandleCommand(new ExecuteState(NECommand.Internal_SetViewValue) { Configuration = (newBytes, size) });
		}
	}
}
