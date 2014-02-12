using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using NeoEdit.Common;

namespace NeoEdit.BinaryEditorUI
{
	public partial class BinaryEditor : Window
	{
		public const string Edit_Cut = "Edit_Cut";
		public const string Edit_Copy = "Edit_Copy";
		public const string Edit_Paste = "Edit_Paste";
		public const string Edit_Find = "Edit_Find";
		public const string Edit_FindNext = "Edit_FindNext";
		public const string Edit_FindPrev = "Edit_FindPrev";
		public const string Edit_Insert = "Edit_Insert";
		public const string Checksum_MD5 = "Checksum_MD5";
		public const string Checksum_SHA1 = "Checksum_SHA1";
		public const string Checksum_SHA256 = "Checksum_SHA256";
		public const string Compress_GZip = "Compress_GZip";
		public const string Decompress_GZip = "Decompress_GZip";
		public const string Compress_Deflate = "Compress_Deflate";
		public const string Decompress_Inflate = "Decompress_Inflate";
		public const string Encrypt_AES = "Encrypt_AES";
		public const string Decrypt_AES = "Decrypt_AES";
		public const string Encrypt_DES = "Encrypt_DES";
		public const string Decrypt_DES = "Decrypt_DES";
		public const string Encrypt_DES3 = "Encrypt_DES3";
		public const string Decrypt_DES3 = "Decrypt_DES3";
		public const string Encrypt_RSA = "Encrypt_RSA";
		public const string Decrypt_RSA = "Decrypt_RSA";
		public const string Sign_RSA = "Sign_RSA";
		public const string Verify_RSA = "Verify_RSA";
		public const string Sign_DSA = "Sign_DSA";
		public const string Verify_DSA = "Verify_DSA";
		public const string View_Values = "View_Values";

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
			yScroll.MouseWheel += (s, e) => (s as ScrollBar).Value -= e.Delta;

			Show();
		}

		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			base.OnTextInput(e);
			if (e.Handled)
				return;

			if (e.OriginalSource is MenuItem)
				return;

			uiHelper.RaiseEvent(canvas, e);
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

		void CommandRun(UICommand command, object parameter)
		{
			canvas.CommandRun(command, parameter);

			switch (command.Name)
			{
				case View_Values: ShowValues = !ShowValues; break;
			}
		}

		bool CommandCanRun(UICommand command, object parameter)
		{
			return canvas.CommandCanRun(command, parameter);
		}

		void TypeEncodingClick(object sender, RoutedEventArgs e)
		{
			canvas.TypeEncoding = Helpers.ParseEnum<BinaryData.EncodingName>(((MenuItem)e.Source).Header as string);
		}


		void EncodeClick(object sender, RoutedEventArgs e)
		{
			try
			{
				var header = (e.OriginalSource as MenuItem).Header as string;
				var encoding = BinaryData.EncodingName.None;
				if (header != "Auto")
					encoding = Helpers.ParseEnum<BinaryData.EncodingName>(header);
				var data = new TextData(Data, encoding);
				new TextEditorUI.TextEditor(data);
				this.Close();
			}
			catch (Exception ex) { MessageBox.Show(ex.Message, "Error"); }
		}
	}
}
