using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Parsing;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class DatabaseExamineDialog
	{
		[DepProp]
		public ObservableCollection<string> Collections { get { return UIHelper<DatabaseExamineDialog>.GetPropValue<ObservableCollection<string>>(this); } set { UIHelper<DatabaseExamineDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string Collection { get { return UIHelper<DatabaseExamineDialog>.GetPropValue<string>(this); } set { UIHelper<DatabaseExamineDialog>.SetPropValue(this, value); } }
		[DepProp]
		public DataTable Data { get { return UIHelper<DatabaseExamineDialog>.GetPropValue<DataTable>(this); } set { UIHelper<DatabaseExamineDialog>.SetPropValue(this, value); } }

		static DatabaseExamineDialog()
		{
			UIHelper<DatabaseExamineDialog>.Register();
			UIHelper<DatabaseExamineDialog>.AddCallback(a => a.Collection, (obj, o, n) => obj.UpdateData());
		}

		readonly DbConnection dbConnection;
		DatabaseExamineDialog(DbConnection dbConnection)
		{
			this.dbConnection = dbConnection;
			InitializeComponent();

			Collections = new ObservableCollection<string>(dbConnection.GetSchema().AsEnumerable().Select(row => row["CollectionName"]).Cast<string>());
		}

		void UpdateData() => Data = dbConnection.GetSchema(Collection);

		void OkClick(object sender, RoutedEventArgs e) => DialogResult = true;

		static public void Run(Window parent, DbConnection dbConnection)
		{
			var dialog = new DatabaseExamineDialog(dbConnection) { Owner = parent };
			dialog.ShowDialog();
		}
	}
}
