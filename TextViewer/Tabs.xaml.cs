using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using NeoEdit.GUI;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextView
{
	public class Tabs : Tabs<TextViewer> { }

	partial class TextViewerTabs
	{
		[DepProp]
		public ObservableCollection<TextViewer> TextViewers { get { return UIHelper<TextViewerTabs>.GetPropValue<ObservableCollection<TextViewer>>(this); } set { UIHelper<TextViewerTabs>.SetPropValue(this, value); } }
		[DepProp]
		public TextViewer Active { get { return UIHelper<TextViewerTabs>.GetPropValue<TextViewer>(this); } set { UIHelper<TextViewerTabs>.SetPropValue(this, value); } }
		[DepProp]
		public Tabs.ViewType View { get { return UIHelper<TextViewerTabs>.GetPropValue<Tabs.ViewType>(this); } set { UIHelper<TextViewerTabs>.SetPropValue(this, value); } }

		static TextViewerTabs() { UIHelper<TextViewerTabs>.Register(); }

		public static TextViewerTabs Create(string filename = null, bool createNew = false)
		{
			var textViewerTabs = (!createNew ? UIHelper<TextViewerTabs>.GetNewest() : null) ?? new TextViewerTabs();
			textViewerTabs.Add(filename);
			return textViewerTabs;
		}

		TextViewerTabs()
		{
			TextViewMenuItem.RegisterCommands(this, (s, e, command) => RunCommand(command));
			InitializeComponent();

			TextViewers = new ObservableCollection<TextViewer>();
		}

		void Command_File_Open()
		{
			var dir = Active != null ? Path.GetDirectoryName(Active.FileName) : null;
			var dialog = new OpenFileDialog
			{
				DefaultExt = "txt",
				Filter = "Text files|*.txt|All files|*.*",
				FilterIndex = 2,
				Multiselect = true,
				InitialDirectory = dir,
			};
			if (dialog.ShowDialog() != true)
				return;

			foreach (var filename in dialog.FileNames)
				Add(filename);
		}

		void Command_File_OpenCopiedCutFiles()
		{
			var files = ClipboardWindow.GetStrings();
			if ((files == null) || (files.Count < 0))
				return;

			if ((files.Count > 5) && (new Message
			{
				Title = "Confirm",
				Text = String.Format("Are you sure you want to open these {0} files?", files.Count),
				Options = Message.OptionsEnum.YesNoCancel,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.Cancel,
			}.Show() != Message.OptionsEnum.Yes))
				return;

			foreach (var file in files)
				Add(file);
		}

		bool shiftDown { get { return (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None; } }
		bool controlDown { get { return (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None; } }

		internal void RunCommand(TextViewCommand command)
		{
			var shiftDown = this.shiftDown;

			switch (command)
			{
				case TextViewCommand.File_Open: Command_File_Open(); break;
				case TextViewCommand.File_OpenCopiedCutFiles: Command_File_OpenCopiedCutFiles(); break;
				case TextViewCommand.File_Exit: Close(); break;
				case TextViewCommand.View_Tiles: View = View == Tabs.ViewType.Tiles ? Tabs.ViewType.Tabs : Tabs.ViewType.Tiles; break;
			}

			if (Active == null)
				return;

			switch (command)
			{
				case TextViewCommand.File_Close: TextViewers.Remove(Active); break;
				case TextViewCommand.File_CopyPath: Active.Command_File_CopyPath(); break;
			}
		}

		void Add(string filename)
		{
			if (filename == null)
				return;

			new TextData(filename, data => TextViewers.Add(Active = new TextViewer(data)));
		}

		Label GetLabel(TextViewer textViewer)
		{
			return textViewer.GetLabel();
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Handled)
				return;

			var shiftDown = this.shiftDown;
			var controlDown = this.controlDown;

			e.Handled = HandleKey(e.Key, shiftDown, controlDown);
		}

		internal bool HandleKey(Key key, bool shiftDown, bool controlDown)
		{
			if (Active == null)
				return false;
			return Active.HandleKey(key, shiftDown, controlDown);
		}

		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			base.OnTextInput(e);
			if (e.Handled)
				return;

			if (e.Source is MenuItem)
				return;

			e.Handled = HandleText(e.Text);
		}

		internal bool HandleText(string text)
		{
			if (Active == null)
				return false;
			return Active.HandleText(text);
		}
	}
}
