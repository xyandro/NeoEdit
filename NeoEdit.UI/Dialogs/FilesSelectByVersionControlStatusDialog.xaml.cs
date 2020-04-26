using System;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Transform;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class FilesSelectByVersionControlStatusDialog
	{
		[DepProp]
		public bool Normal { get { return UIHelper<FilesSelectByVersionControlStatusDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesSelectByVersionControlStatusDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Modified { get { return UIHelper<FilesSelectByVersionControlStatusDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesSelectByVersionControlStatusDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Ignored { get { return UIHelper<FilesSelectByVersionControlStatusDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesSelectByVersionControlStatusDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Unknown { get { return UIHelper<FilesSelectByVersionControlStatusDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesSelectByVersionControlStatusDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool VersionControl { get { return UIHelper<FilesSelectByVersionControlStatusDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesSelectByVersionControlStatusDialog>.SetPropValue(this, value); } }

		static FilesSelectByVersionControlStatusDialog() { UIHelper<FilesSelectByVersionControlStatusDialog>.Register(); }

		FilesSelectByVersionControlStatusDialog()
		{
			InitializeComponent();
			Normal = Modified = true;
			Ignored = Unknown = VersionControl = false;
		}

		void OnSelectionClick(object sender, RoutedEventArgs e)
		{
			Normal = Modified = Ignored = Unknown = VersionControl = false;
			switch ((sender as Button).Tag as string)
			{
				case "Controlled": Normal = Modified = true; break;
				case "Modified": Modified = true; break;
				case "Ignored": Ignored = true; break;
				case "Unknown": Unknown = true; break;
				case "VersionControl": VersionControl = true; break;
			}
		}

		Configuration_Files_Select_ByVersionControlStatus result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Files_Select_ByVersionControlStatus();
			if (Normal)
				result.Statuses |= Versioner.Status.Normal;
			if (Modified)
				result.Statuses |= Versioner.Status.Modified;
			if (Ignored)
				result.Statuses |= Versioner.Status.Ignored;
			if (Unknown)
				result.Statuses |= Versioner.Status.Unknown;
			if (VersionControl)
				result.Statuses |= Versioner.Status.VersionControl;
			DialogResult = true;
		}

		public static Configuration_Files_Select_ByVersionControlStatus Run(Window parent)
		{
			var dialog = new FilesSelectByVersionControlStatusDialog() { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
