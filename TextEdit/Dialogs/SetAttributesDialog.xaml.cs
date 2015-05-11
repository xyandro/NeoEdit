using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class SetAttributesDialog
	{
		internal class Result
		{
			public Dictionary<FileAttributes, bool?> Attributes { get; set; }
		}

		[DepProp]
		public bool? ReadOnlyAttr { get { return UIHelper<SetAttributesDialog>.GetPropValue<bool?>(this); } set { UIHelper<SetAttributesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ReadOnlyThreeState { get { return UIHelper<SetAttributesDialog>.GetPropValue<bool>(this); } set { UIHelper<SetAttributesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool? ArchiveAttr { get { return UIHelper<SetAttributesDialog>.GetPropValue<bool?>(this); } set { UIHelper<SetAttributesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ArchiveThreeState { get { return UIHelper<SetAttributesDialog>.GetPropValue<bool>(this); } set { UIHelper<SetAttributesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool? SystemAttr { get { return UIHelper<SetAttributesDialog>.GetPropValue<bool?>(this); } set { UIHelper<SetAttributesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool SystemThreeState { get { return UIHelper<SetAttributesDialog>.GetPropValue<bool>(this); } set { UIHelper<SetAttributesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool? HiddenAttr { get { return UIHelper<SetAttributesDialog>.GetPropValue<bool?>(this); } set { UIHelper<SetAttributesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool HiddenThreeState { get { return UIHelper<SetAttributesDialog>.GetPropValue<bool>(this); } set { UIHelper<SetAttributesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool? NotContentIndexedAttr { get { return UIHelper<SetAttributesDialog>.GetPropValue<bool?>(this); } set { UIHelper<SetAttributesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool NotContentIndexedThreeState { get { return UIHelper<SetAttributesDialog>.GetPropValue<bool>(this); } set { UIHelper<SetAttributesDialog>.SetPropValue(this, value); } }

		static SetAttributesDialog() { UIHelper<SetAttributesDialog>.Register(); }

		readonly Dictionary<FileAttributes, bool?> initial;
		SetAttributesDialog(Dictionary<FileAttributes, bool?> initial)
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

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result
			{
				Attributes = new Dictionary<FileAttributes, bool?>
				{
					{ FileAttributes.ReadOnly, ReadOnlyAttr },
					{ FileAttributes.Archive, ArchiveAttr },
					{ FileAttributes.System, SystemAttr },
					{ FileAttributes.Hidden, HiddenAttr },
					{ FileAttributes.NotContentIndexed, NotContentIndexedAttr },
				}
			};

			var removeAttrs = result.Attributes.Where(pair => initial[pair.Key] == pair.Value).Select(pair => pair.Key).ToList();
			foreach (var attr in removeAttrs)
				result.Attributes.Remove(attr);

			DialogResult = true;
		}

		public static Result Run(Window parent, Dictionary<FileAttributes, bool?> attributes)
		{
			var dialog = new SetAttributesDialog(attributes) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
