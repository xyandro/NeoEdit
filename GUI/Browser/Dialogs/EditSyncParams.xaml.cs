using System.Linq;
using System.Windows;
using NeoEdit.Common;

namespace NeoEdit.GUI.Browser.Dialogs
{
	public partial class EditSyncParams : Window
	{
		readonly SyncParams syncParams;
		public EditSyncParams(SyncParams _syncParams)
		{
			InitializeComponent();
			Helpers.GetValues<SyncParams.SyncType>().ToList().ForEach(typeEnum => type.Items.Add(typeEnum));

			syncParams = _syncParams;
			type.SelectedItem = syncParams.Type;
			eraseExtra.IsChecked = syncParams.EraseExtra;
			stopOnError.IsChecked = syncParams.StopOnError;
			logOnly.IsChecked = syncParams.LogOnly;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			syncParams.Type = (SyncParams.SyncType)type.SelectedItem;
			syncParams.EraseExtra = eraseExtra.IsChecked == true;
			syncParams.StopOnError = stopOnError.IsChecked == true;
			syncParams.LogOnly = logOnly.IsChecked == true;
			DialogResult = true;
		}
	}
}
