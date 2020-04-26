using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Files_Set_Attributes_Dialog
	{
		[DepProp]
		public bool? ReadOnlyAttr { get { return UIHelper<Configure_Files_Set_Attributes_Dialog>.GetPropValue<bool?>(this); } set { UIHelper<Configure_Files_Set_Attributes_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ReadOnlyThreeState { get { return UIHelper<Configure_Files_Set_Attributes_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_Files_Set_Attributes_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool? ArchiveAttr { get { return UIHelper<Configure_Files_Set_Attributes_Dialog>.GetPropValue<bool?>(this); } set { UIHelper<Configure_Files_Set_Attributes_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ArchiveThreeState { get { return UIHelper<Configure_Files_Set_Attributes_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_Files_Set_Attributes_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool? SystemAttr { get { return UIHelper<Configure_Files_Set_Attributes_Dialog>.GetPropValue<bool?>(this); } set { UIHelper<Configure_Files_Set_Attributes_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool SystemThreeState { get { return UIHelper<Configure_Files_Set_Attributes_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_Files_Set_Attributes_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool? HiddenAttr { get { return UIHelper<Configure_Files_Set_Attributes_Dialog>.GetPropValue<bool?>(this); } set { UIHelper<Configure_Files_Set_Attributes_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool HiddenThreeState { get { return UIHelper<Configure_Files_Set_Attributes_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_Files_Set_Attributes_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool? NotContentIndexedAttr { get { return UIHelper<Configure_Files_Set_Attributes_Dialog>.GetPropValue<bool?>(this); } set { UIHelper<Configure_Files_Set_Attributes_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool NotContentIndexedThreeState { get { return UIHelper<Configure_Files_Set_Attributes_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_Files_Set_Attributes_Dialog>.SetPropValue(this, value); } }

		static Configure_Files_Set_Attributes_Dialog() { UIHelper<Configure_Files_Set_Attributes_Dialog>.Register(); }

		readonly Dictionary<FileAttributes, bool?> initial;
		Configure_Files_Set_Attributes_Dialog(Dictionary<FileAttributes, bool?> initial)
		{
			InitializeComponent();

			this.initial = initial;

			ReadOnlyAttr = initial[FileAttributes.ReadOnly];
			ArchiveAttr = initial[FileAttributes.Archive];
			SystemAttr = initial[FileAttributes.System];
			HiddenAttr = initial[FileAttributes.Hidden];
			NotContentIndexedAttr = initial[FileAttributes.NotContentIndexed];

			ReadOnlyThreeState = initial[FileAttributes.ReadOnly] == null;
			ArchiveThreeState = initial[FileAttributes.Archive] == null;
			SystemThreeState = initial[FileAttributes.System] == null;
			HiddenThreeState = initial[FileAttributes.Hidden] == null;
			NotContentIndexedThreeState = initial[FileAttributes.NotContentIndexed] == null;
		}

		Configuration_Files_Set_Attributes result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Files_Set_Attributes
			{
				Attributes = new Dictionary<FileAttributes, bool?>
				{
					[FileAttributes.ReadOnly] = ReadOnlyAttr,
					[FileAttributes.Archive] = ArchiveAttr,
					[FileAttributes.System] = SystemAttr,
					[FileAttributes.Hidden] = HiddenAttr,
					[FileAttributes.NotContentIndexed] = NotContentIndexedAttr,
				}
			};

			var removeAttrs = result.Attributes.Where(pair => initial[pair.Key] == pair.Value).Select(pair => pair.Key).ToList();
			foreach (var attr in removeAttrs)
				result.Attributes.Remove(attr);

			DialogResult = true;
		}

		public static Configuration_Files_Set_Attributes Run(Window parent, Dictionary<FileAttributes, bool?> attributes)
		{
			var dialog = new Configure_Files_Set_Attributes_Dialog(attributes) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
