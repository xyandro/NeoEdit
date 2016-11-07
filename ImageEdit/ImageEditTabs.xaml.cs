using System.IO;
using System.Linq;
using Microsoft.Win32;
using NeoEdit.Common.NEClipboards;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.ImageEdit
{
	public class Tabs : Tabs<ImageEditor, ImageEditCommand> { }
	public class TabsWindow : TabsWindow<ImageEditor, ImageEditCommand> { }

	partial class ImageEditTabs
	{
		static ImageEditTabs() { UIHelper<ImageEditTabs>.Register(); }

		public static void Create(string fileName = null, ImageEditTabs imageEditTabs = null, bool forceCreate = false)
		{
			foreach (var imageEditor in ImageEditor.OpenFile(fileName))
				CreateTab(imageEditor, imageEditTabs, forceCreate);
		}

		public void AddImageEditor(string fileName = null) => Create(fileName, this);

		ImageEditTabs()
		{
			ImageEditMenuItem.RegisterCommands(this, (command, multiStatus) => RunCommand(command, multiStatus));
			InitializeComponent();
			ItemTabs = tabs;
			UIHelper.AuditMenu(menu);
		}

		CustomGridDialog.Result Command_View_Type_Dialog() => CustomGridDialog.Run(this, ItemTabs.Columns, ItemTabs.Rows);

		void Command_File_Open_Open()
		{
			var dialog = new OpenFileDialog
			{
				Filter = FileTypeExtensions.GetOpenFilter(),
				Multiselect = true,
				InitialDirectory = ItemTabs.TopMost == null ? Directory.GetCurrentDirectory() : Path.GetDirectoryName(ItemTabs.TopMost.FileName),
			};
			if (dialog.ShowDialog() != true)
				return;

			foreach (var filename in dialog.FileNames)
				AddImageEditor(filename);
		}

		void Command_File_Open_CopiedCut()
		{
			var files = NEClipboard.Strings;

			if ((files.Count > 5) && (new Message
			{
				Title = "Confirm",
				Text = $"Are you sure you want to open these {files.Count} files?",
				Options = Message.OptionsEnum.YesNoCancel,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.Cancel,
			}.Show() != Message.OptionsEnum.Yes))
				return;

			foreach (var item in ItemTabs.Items)
				item.Active = false;
			foreach (var file in files)
				AddImageEditor(file);
		}

		void Command_Edit_Paste()
		{
			var image = NEClipboard.Image;
			if (image != null)
				CreateTab(new ImageEditor(null, image), this);
		}

		void Command_View_Type(TabsLayout layout, CustomGridDialog.Result result) => ItemTabs.SetLayout(layout, result?.Columns ?? 0, result?.Rows ?? 0);

		void Command_View_ActiveTabs() => tabs.ShowActiveTabsDialog();

		internal void RunCommand(ImageEditCommand command, bool? multiStatus)
		{
			var shiftDown = this.shiftDown;

			object dialogResult;
			if (!GetDialogResult(command, out dialogResult))
				return;

			HandleCommand(command, shiftDown, dialogResult, multiStatus);
		}

		internal bool GetDialogResult(ImageEditCommand command, out object dialogResult)
		{
			dialogResult = null;

			switch (command)
			{
				case ImageEditCommand.View_CustomGrid: dialogResult = Command_View_Type_Dialog(); break;
				default: return ItemTabs.TopMost == null ? true : ItemTabs.TopMost.GetDialogResult(command, out dialogResult);
			}

			return dialogResult != null;
		}

		public override bool HandleCommand(ImageEditCommand command, bool shiftDown, object dialogResult, bool? multiStatus)
		{
			switch (command)
			{
				case ImageEditCommand.File_New: Create(imageEditTabs: this, forceCreate: shiftDown); break;
				case ImageEditCommand.File_Open_Open: Command_File_Open_Open(); break;
				case ImageEditCommand.File_Open_CopiedCut: Command_File_Open_CopiedCut(); break;
				case ImageEditCommand.Edit_Paste: Command_Edit_Paste(); break;
				case ImageEditCommand.View_Full: Command_View_Type(TabsLayout.Full, null); break;
				case ImageEditCommand.View_Grid: Command_View_Type(TabsLayout.Grid, null); break;
				case ImageEditCommand.View_CustomGrid: Command_View_Type(TabsLayout.Grid, dialogResult as CustomGridDialog.Result); break;
				case ImageEditCommand.View_ActiveTabs: Command_View_ActiveTabs(); break;
			}

			foreach (var imageEditItem in ItemTabs.Items.Where(item => item.Active).ToList())
				imageEditItem.HandleCommand(command, shiftDown, dialogResult, multiStatus);

			return true;
		}
	}
}
