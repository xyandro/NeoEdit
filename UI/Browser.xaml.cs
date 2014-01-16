using NeoEdit.Records;
using System.Windows.Input;

namespace NeoEdit.UI
{
	public partial class Browser : UIWindow
	{
		[DepProp]
		public IRecordList Directory { get { return GetProp<IRecordList>(); } set { SetProp(value); } }

		static Browser()
		{
			Register<Browser>();
		}

		public Browser(string recordUri)
		{
			InitializeComponent();
			Directory = RecordListProvider.GetRecordList(recordUri);
		}

		private void Files_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Enter:
					{
						var item = Files.SelectedItem as IRecordList;
						if (item != null)
							Directory = item;
					}
					break;
				case Key.Back:
					Directory = Directory.Parent;
					break;
			}
		}

		private void DirectoryDisplay_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Enter:
					Directory = RecordListProvider.GetRecordList(DirectoryDisplay.Text, Directory);
					break;
			}
		}
	}
}
