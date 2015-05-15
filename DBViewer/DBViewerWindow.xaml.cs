using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NeoEdit.DBViewer
{
	/// <summary>
	/// Interaction logic for DBViewerWindow.xaml
	/// </summary>
	partial class DBViewerWindow
	{
		public DBViewerWindow()
		{
			InitializeComponent();

			query.Text = "SELECT * FROM TestTable";

			RunQuery();
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnPreviewKeyDown(e);
			if (e.Handled)
				return;

			e.Handled = true;
			switch (e.Key)
			{
				case Key.F5: RunQuery(); break;
				default: e.Handled = false; break;
			}
		}

		void RunQuery()
		{
			results.Children.Clear();
			results.RowDefinitions.Clear();
			results.ColumnDefinitions.Clear();

			var sb = new SqlConnectionStringBuilder { IntegratedSecurity = true, InitialCatalog = "Test" };
			using (var conn = new SqlConnection(sb.ToString()))
			{
				conn.Open();
				using (var stmt = conn.CreateCommand())
				{
					stmt.CommandText = query.Text;
					using (var reader = stmt.ExecuteReader())
					{
						var fields = reader.FieldCount;

						results.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

						for (var field = 0; field < fields; ++field)
						{
							results.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
							var label = new Label { Content = reader.GetName(field) };
							Grid.SetColumn(label, field);
							Grid.SetRow(label, 0);
							results.Children.Add(label);
						}

						while (reader.Read())
						{
							var row = results.RowDefinitions.Count;
							results.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
							for (var field = 0; field < fields; ++field)
							{
								var label = new Label { Content = reader.GetValue(field) };
								Grid.SetColumn(label, field);
								Grid.SetRow(label, row);
								results.Children.Add(label);
							}
						}
					}
				}
			}
		}
	}
}
