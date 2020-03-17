using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program.Controls
{
	partial class ViewValue
	{
		public Coder.CodePage CodePage { get; set; } = Coder.CodePage.None;
		public IList<byte> Data { get; set; }
		public bool HasSel { get; set; }
		public string Text { get => text.Text; set => text.Text = value; }

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

		public void SetData(IList<byte> data, bool hasSel)
		{
			if (CodePage == Coder.CodePage.None)
				return;

			if (Visibility != Visibility.Visible)
			{
				text.Text = null;
				return;
			}

			Data = data;
			HasSel = hasSel;
			text.Text = Font.RemoveSpecialChars(GetValue() ?? "");
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
					value = ViewValuesEditValueDialog.Run(UIHelper.FindParent<Window>(this), value);
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
