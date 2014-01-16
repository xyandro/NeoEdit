using NeoEdit.Records;
using System.Windows.Input;

namespace NeoEdit.UI
{
	public partial class Browser : UIWindow
	{
		[DepProp]
		public string DirectoryName { get { return GetProp<string>(); } set { SetProp(value); } }

		static Browser()
		{
			Register<Browser>();
		}

		IRecordList directory;
		public Browser(string recordUri)
		{
			InitializeComponent();
			SetDirectory(RecordListProvider.GetRecordList(recordUri));
		}

		void SetDirectory(IRecordList _directory)
		{
			directory = _directory;
			DirectoryName = directory.FullName;
			files.ItemsSource = directory.Records;
		}

		private void files_KeyDown(object sender, KeyEventArgs e)
		{
			
			switch (e.Key)
			{
				case Key.Enter:
					{
						var item = files.SelectedItem as IRecordList;
						if (item != null)
							SetDirectory(item);
					}
					break;
				case Key.Back:
					SetDirectory(directory.Parent);
					break;
			}
		}
	}
}
