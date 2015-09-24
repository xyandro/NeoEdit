using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Data;
using Microsoft.Win32;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Converters;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.Tables
{
	partial class TableEditor
	{
		[DepProp]
		public Table Table { get { return UIHelper<TableEditor>.GetPropValue<Table>(this); } set { UIHelper<TableEditor>.SetPropValue(this, value); } }
		[DepProp]
		public string FileName { get { return UIHelper<TableEditor>.GetPropValue<string>(this); } set { UIHelper<TableEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsModified { get { return UIHelper<TableEditor>.GetPropValue<bool>(this); } set { UIHelper<TableEditor>.SetPropValue(this, value); } }
		[DepProp]
		public string TabLabel { get { return UIHelper<TableEditor>.GetPropValue<string>(this); } set { UIHelper<TableEditor>.SetPropValue(this, value); } }

		TablesTabs TabsParent { get { return UIHelper.FindParent<TablesTabs>(GetValue(Tabs.TabParentProperty) as Tabs); } }

		static TableEditor() { UIHelper<TableEditor>.Register(); }

		public TableEditor(string fileName)
		{
			InitializeComponent();
			FileName = fileName;

			if (fileName != null)
			{
				var text = File.ReadAllText(fileName);
				Table = new Table(text);
			}

			Table = Table ?? new Table();

			SetupTabLabel();
		}

		void SetupTabLabel()
		{
			var multiBinding = new MultiBinding { Converter = new NEExpressionConverter(), ConverterParameter = @"([0] == null?""[Untitled]"":FileName([0]))+([1]?""*"":"""")" };
			multiBinding.Bindings.Add(new Binding(UIHelper<TableEditor>.GetProperty(a => a.FileName).Name) { Source = this });
			multiBinding.Bindings.Add(new Binding(UIHelper<TableEditor>.GetProperty(a => a.IsModified).Name) { Source = this });
			SetBinding(UIHelper<TableEditor>.GetProperty(a => a.TabLabel), multiBinding);
		}

		bool CanClose()
		{
			Message.OptionsEnum answer = Message.OptionsEnum.None;
			return CanClose(ref answer);
		}

		internal bool CanClose(ref Message.OptionsEnum answer)
		{
			if (!IsModified)
				return true;

			if ((answer != Message.OptionsEnum.YesToAll) && (answer != Message.OptionsEnum.NoToAll))
				answer = new Message
				{
					Title = "Confirm",
					Text = "Do you want to save changes?",
					Options = Message.OptionsEnum.YesNoYesAllNoAllCancel,
					DefaultCancel = Message.OptionsEnum.Cancel,
				}.Show();

			switch (answer)
			{
				case Message.OptionsEnum.Cancel:
					return false;
				case Message.OptionsEnum.No:
				case Message.OptionsEnum.NoToAll:
					return true;
				case Message.OptionsEnum.Yes:
				case Message.OptionsEnum.YesToAll:
					Command_File_Save();
					return !IsModified;
			}
			return false;
		}

		Table.TableTypeEnum GetFileTableType(string fileName)
		{
			switch (Path.GetExtension(fileName).ToLowerInvariant())
			{
				case ".tsv": return Table.TableTypeEnum.TSV;
				case ".csv": return Table.TableTypeEnum.CSV;
				case ".txt": return Table.TableTypeEnum.Columns;
				default: return Table.TableTypeEnum.TSV;
			}
		}

		string GetSaveFileName()
		{
			var dialog = new SaveFileDialog
			{
				Filter = "TSV files|*.tsv|CSV files|*.csv|Text files|*.txt",
				FileName = Path.GetFileName(FileName),
				InitialDirectory = Path.GetDirectoryName(FileName),
				FilterIndex = (int)GetFileTableType(FileName),
			};
			if (dialog.ShowDialog() != true)
				return null;

			if (Directory.Exists(dialog.FileName))
				throw new Exception("A directory by that name already exists");
			if (!Directory.Exists(Path.GetDirectoryName(dialog.FileName)))
				throw new Exception("Directory doesn't exist");
			return dialog.FileName;
		}

		void Save(string fileName)
		{
			var data = Table.ConvertToString("\r\n", GetFileTableType(fileName));
			File.WriteAllText(fileName, data, Encoding.UTF8);
			FileName = fileName;
		}

		void Command_File_Save()
		{
			if (FileName == null)
				Command_File_SaveAs();
			else
				Save(FileName);
		}

		void Command_File_SaveAs()
		{
			var fileName = GetSaveFileName();
			if (fileName != null)
				Save(fileName);
		}

		internal bool GetDialogResult(TablesCommand command, out object dialogResult)
		{
			dialogResult = null;

			switch (command)
			{
				default: return true;
			}

			return dialogResult != null;
		}

		internal void HandleCommand(TablesCommand command, bool shiftDown, object dialogResult)
		{
			switch (command)
			{
				case TablesCommand.File_Save: Command_File_Save(); break;
				case TablesCommand.File_SaveAs: Command_File_SaveAs(); break;
				case TablesCommand.File_Close: if (CanClose()) { TabsParent.Remove(this); } break;
			}
		}

		internal bool Empty()
		{
			return (FileName == null) && (!IsModified) && (!Table.Headers.Any());
		}

		internal void Closed()
		{
		}
	}
}
