using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Data;
using NeoEdit.GUI.Records;

namespace NeoEdit.GUI.BinaryEditorUI
{
	public partial class BinaryEditor : Window
	{
		public enum Commands
		{
			File_New,
			File_Open,
			File_Save,
			File_SaveAs,
			File_Exit,
			Edit_Undo,
			Edit_Cut,
			Edit_Copy,
			Edit_Paste,
			Edit_ShowClipboard,
			Edit_Find,
			Edit_FindNext,
			Edit_FindPrev,
			Edit_Goto,
			Edit_Insert,
			Checksum_MD5,
			Checksum_SHA1,
			Checksum_SHA256,
			Compress_GZip,
			Decompress_GZip,
			Compress_Deflate,
			Decompress_Inflate,
			Encrypt_AES,
			Decrypt_AES,
			Encrypt_DES,
			Decrypt_DES,
			Encrypt_DES3,
			Decrypt_DES3,
			Encrypt_RSA,
			Decrypt_RSA,
			Encrypt_RSAAES,
			Decrypt_RSAAES,
			Sign_RSA,
			Verify_RSA,
			Sign_DSA,
			Verify_DSA,
			View_Values,
			View_Refresh,
		}

		[DepProp]
		public BinaryData Data { get { return uiHelper.GetPropValue<BinaryData>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long ChangeCount { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }
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
		public Coder.Type InputCoderType { get { return uiHelper.GetPropValue<Coder.Type>(); } set { uiHelper.SetPropValue(value); } }

		static BinaryEditor() { UIHelper<BinaryEditor>.Register(); }

		readonly UIHelper<BinaryEditor> uiHelper;
		Record record;
		public BinaryEditor(Record _record = null, BinaryData data = null)
		{
			uiHelper = new UIHelper<BinaryEditor>(this);
			InitializeComponent();
			uiHelper.InitializeCommands();

			record = _record;
			if (data == null)
			{
				if (record == null)
					data = new MemoryBinaryData();
				else
					data = record.Read();
			}
			Data = data;
			BinaryData.BinaryDataChangedDelegate changed = () => ++ChangeCount;
			Data.Changed += changed;
			Closed += (s, e) => Data.Changed -= changed;

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

			switch ((Commands)command.Enum)
			{
				case Commands.File_New:
					record = null;
					Data = new MemoryBinaryData();
					break;
				case Commands.File_Open: break;
				case Commands.File_Save:
					if (record != null)
						record.Write(Data);
					break;
				case Commands.File_SaveAs: break;
				case Commands.File_Exit: Close(); break;
				case Commands.View_Values: ShowValues = !ShowValues; break;
				case Commands.Edit_ShowClipboard: Clipboard.Show(); break;
			}
		}

		bool CommandCanRun(UICommand command, object parameter)
		{
			return canvas.CommandCanRun(command, parameter);
		}

		void InputEncodingClick(object sender, RoutedEventArgs e)
		{
			canvas.InputCoderType = Helpers.ParseEnum<Coder.Type>(((MenuItem)e.Source).Header as string);
		}

		void EncodeClick(object sender, RoutedEventArgs e)
		{
			var header = (e.OriginalSource as MenuItem).Header as string;
			var encoding = Coder.Type.None;
			if (header != "Auto")
				encoding = Helpers.ParseEnum<Coder.Type>(header);
			var data = new TextData(Data.GetAllBytes(), encoding);
			new TextEditorUI.TextEditor(record, data);
			this.Close();
		}
	}
}
