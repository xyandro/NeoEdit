using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using NeoEdit.BinaryEditor.Data;
using NeoEdit.Common;
using NeoEdit.Common.Transform;
using NeoEdit.GUI;
using NeoEdit.GUI.Common;

namespace NeoEdit.BinaryEditor
{
	public partial class BinaryEditorWindow : Window
	{
		public static RoutedCommand Command_File_New = new RoutedCommand();
		public static RoutedCommand Command_File_Open = new RoutedCommand();
		public static RoutedCommand Command_File_OpenDump = new RoutedCommand();
		public static RoutedCommand Command_File_Save = new RoutedCommand();
		public static RoutedCommand Command_File_SaveAs = new RoutedCommand();
		public static RoutedCommand Command_File_TextEditor = new RoutedCommand();
		public static RoutedCommand Command_File_Exit = new RoutedCommand();
		public static RoutedCommand Command_Edit_Undo = new RoutedCommand();
		public static RoutedCommand Command_Edit_Redo = new RoutedCommand();
		public static RoutedCommand Command_Edit_Cut = new RoutedCommand();
		public static RoutedCommand Command_Edit_Copy = new RoutedCommand();
		public static RoutedCommand Command_Edit_Paste = new RoutedCommand();
		public static RoutedCommand Command_Edit_ShowClipboard = new RoutedCommand();
		public static RoutedCommand Command_Edit_Find = new RoutedCommand();
		public static RoutedCommand Command_Edit_FindNext = new RoutedCommand();
		public static RoutedCommand Command_Edit_FindPrev = new RoutedCommand();
		public static RoutedCommand Command_Edit_Goto = new RoutedCommand();
		public static RoutedCommand Command_Edit_Insert = new RoutedCommand();
		public static RoutedCommand Command_View_Values = new RoutedCommand();
		public static RoutedCommand Command_View_Refresh = new RoutedCommand();
		public static RoutedCommand Command_Checksum_MD5 = new RoutedCommand();
		public static RoutedCommand Command_Checksum_SHA1 = new RoutedCommand();
		public static RoutedCommand Command_Checksum_SHA256 = new RoutedCommand();
		public static RoutedCommand Command_Compress_GZip = new RoutedCommand();
		public static RoutedCommand Command_Decompress_GZip = new RoutedCommand();
		public static RoutedCommand Command_Compress_Deflate = new RoutedCommand();
		public static RoutedCommand Command_Decompress_Inflate = new RoutedCommand();
		public static RoutedCommand Command_Encrypt_AES = new RoutedCommand();
		public static RoutedCommand Command_Decrypt_AES = new RoutedCommand();
		public static RoutedCommand Command_Encrypt_DES = new RoutedCommand();
		public static RoutedCommand Command_Decrypt_DES = new RoutedCommand();
		public static RoutedCommand Command_Encrypt_DES3 = new RoutedCommand();
		public static RoutedCommand Command_Decrypt_DES3 = new RoutedCommand();
		public static RoutedCommand Command_Encrypt_RSA = new RoutedCommand();
		public static RoutedCommand Command_Decrypt_RSA = new RoutedCommand();
		public static RoutedCommand Command_Encrypt_RSAAES = new RoutedCommand();
		public static RoutedCommand Command_Decrypt_RSAAES = new RoutedCommand();
		public static RoutedCommand Command_Sign_RSA = new RoutedCommand();
		public static RoutedCommand Command_Verify_RSA = new RoutedCommand();
		public static RoutedCommand Command_Sign_DSA = new RoutedCommand();
		public static RoutedCommand Command_Verify_DSA = new RoutedCommand();

		[DepProp]
		string FileTitle { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		string FileName { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		BinaryData Data { get { return uiHelper.GetPropValue<BinaryData>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool ShowValues { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		Coder.Type CoderUsed { get { return uiHelper.GetPropValue<Coder.Type>(); } set { uiHelper.SetPropValue(value); } }

		static BinaryEditorWindow() { UIHelper<BinaryEditorWindow>.Register(); }

		readonly UIHelper<BinaryEditorWindow> uiHelper;
		BinaryEditorWindow(BinaryData data, Coder.Type encoder = Coder.Type.None)
		{
			uiHelper = new UIHelper<BinaryEditorWindow>(this);
			InitializeComponent();

			Data = data;
			CoderUsed = encoder;
			if (CoderUsed == Coder.Type.None)
				CoderUsed = Data.GuessEncoding();

			MouseWheel += (s, e) => yScroll.Value -= e.Delta;
		}

		public static BinaryEditorWindow CreateFromFile(string filename = null, byte[] bytes = null, Coder.Type encoder = Coder.Type.None)
		{
			if (bytes == null)
			{
				if (filename == null)
					bytes = new byte[0];
				else
					bytes = File.ReadAllBytes(filename);
			}
			return new BinaryEditorWindow(new MemoryBinaryData(bytes), encoder) { FileName = filename };
		}

		public static BinaryEditorWindow CreateFromDump(string filename)
		{
			return new BinaryEditorWindow(new DumpBinaryData(filename)) { FileTitle = "Dump: ", FileName = filename };
		}

		public static BinaryEditorWindow CreateFromProcess(int pid)
		{
			var process = Process.GetProcessById(pid);
			if (process == null)
				throw new ArgumentException("Process doesn't exist.");
			if (process.Id == Process.GetCurrentProcess().Id)
				throw new ArgumentException("Can't open current process.");
			return new BinaryEditorWindow(new ProcessBinaryData(pid)) { FileTitle = String.Format("Process {0} ({1}) - ", pid, process.ProcessName) };
		}

		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			base.OnTextInput(e);
			if (e.Handled)
				return;

			if (e.OriginalSource is MenuItem)
				return;

			canvas.HandleText(e.Text);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Handled)
				return;

			switch (e.Key)
			{
				case Key.Escape: canvas.Focus(); e.Handled = true; break;
				default: e.Handled = canvas.HandleKey(e.Key); break;
			}
		}

		void Command_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			RunCommand(e.Command);
		}

		void RunCommand(ICommand command)
		{
			canvas.RunCommand(command);

			if (command == Command_File_New)
			{
				FileTitle = FileName = null;
				Data = new MemoryBinaryData();
			}
			else if (command == Command_File_Open)
			{
				{
					var dialog = new OpenFileDialog();
					if (dialog.ShowDialog() == true)
					{
						FileTitle = null;
						FileName = dialog.FileName;
						Data = new MemoryBinaryData(File.ReadAllBytes(FileName));
					}
				}
			}
			else if (command == Command_File_OpenDump)
			{
				{
					var dialog = new OpenFileDialog();
					if (dialog.ShowDialog() == true)
					{
						FileTitle = "Dump: ";
						FileName = dialog.FileName;
						Data = new DumpBinaryData(FileName);
					}
				}
			}
			else if (command == Command_File_Save)
			{
				if (FileName == null)
					RunCommand(Command_File_SaveAs);
				else
					Data.Save(FileName);
			}
			else if (command == Command_File_SaveAs)
			{
				{
					var dialog = new SaveFileDialog();
					if (dialog.ShowDialog() == true)
					{
						if (Directory.Exists(dialog.FileName))
							throw new Exception("A directory by that name already exists.");
						if (!Directory.Exists(Path.GetDirectoryName(dialog.FileName)))
							throw new Exception("Directory doesn't exist.");
						FileName = dialog.FileName;
						RunCommand(Command_File_Save);
					}
				}
			}
			else if (command == Command_File_TextEditor)
			{
				Launcher.Static.LaunchTextEditor(FileName, Data.GetAllBytes(), CoderUsed);
				Close();
			}
			else if (command == Command_File_Exit) { Close(); }
			else if (command == Command_View_Values) { ShowValues = !ShowValues; }
			else if (command == Command_Edit_ShowClipboard) { ClipboardWindow.Show(); }
		}

		void EncodingClick(object sender, RoutedEventArgs e)
		{
			var header = (e.OriginalSource as MenuItem).Header as string;
			CoderUsed = Coder.Type.None;
			if (header == "Auto")
				CoderUsed = Data.GuessEncoding();
			else
				CoderUsed = Helpers.ParseEnum<Coder.Type>(header);
		}
	}
}
