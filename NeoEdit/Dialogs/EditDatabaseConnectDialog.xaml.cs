using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit;
using NeoEdit.Controls;
using NeoEdit.Converters;
using NeoEdit.Dialogs;

namespace NeoEdit.Dialogs
{
	partial class EditDatabaseConnectDialog
	{
		public class DBParam : DependencyObject
		{
			public DBConnectInfo.DBType DBType { get; }
			public string Name { get; }
			[DepProp]
			public object Value { get { return UIHelper<DBParam>.GetPropValue<object>(this); } private set { UIHelper<DBParam>.SetPropValue(this, value); } }
			public object Original { get; }
			public object Default { get; }
			public Type Type { get; }
			public bool IsDefault => (Value == Default) || ((Value != null) && (Default != null) && (Value.Equals(Default)));

			static DBParam() { UIHelper<DBParam>.Register(); }

			public DBParam(DBConnectInfo.DBType dbType, PropertyInfo prop, DbConnectionStringBuilder values, DbConnectionStringBuilder defaults)
			{
				DBType = dbType;
				Name = prop.Name;
				try { Original = prop.GetValue(values); } catch { }
				try { Default = prop.GetValue(defaults); } catch { }
				Value = Original;
				Type = prop.PropertyType;
			}

			public void ResetOriginal() => Value = Original;
			public void ResetDefault() => Value = Default;

			public override string ToString() => $"{Name} = {Value}";
		}

		[DepProp]
		public string ConnectionName { get { return UIHelper<EditDatabaseConnectDialog>.GetPropValue<string>(this); } set { UIHelper<EditDatabaseConnectDialog>.SetPropValue(this, value); } }
		[DepProp]
		public DBConnectInfo.DBType DBType { get { return UIHelper<EditDatabaseConnectDialog>.GetPropValue<DBConnectInfo.DBType>(this); } set { UIHelper<EditDatabaseConnectDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string ConnStr { get { return UIHelper<EditDatabaseConnectDialog>.GetPropValue<string>(this); } set { UIHelper<EditDatabaseConnectDialog>.SetPropValue(this, value); } }

		static EditDatabaseConnectDialog()
		{
			UIHelper<EditDatabaseConnectDialog>.Register();
			UIHelper<EditDatabaseConnectDialog>.AddCallback(a => a.DBType, (obj, o, n) => obj.ConnStr = "");
		}

		EditDatabaseConnectDialog(DBConnectInfo dbConnectInfo)
		{
			InitializeComponent();
			type.ItemsSource = Enum.GetValues(typeof(DBConnectInfo.DBType)).Cast<DBConnectInfo.DBType>().Where(item => item != DBConnectInfo.DBType.None).ToList();
			ConnectionName = dbConnectInfo.Name;
			DBType = dbConnectInfo.Type;
			ConnStr = dbConnectInfo.ConnectionString;
		}

		DBConnectInfo GetResult() => new DBConnectInfo { Name = ConnectionName, Type = DBType, ConnectionString = ConnStr };

		void CreateClick(object sender, RoutedEventArgs e)
		{
			GetResult().CreateDatabase();

			new Message(this)
			{
				Title = "Information",
				Text = GetResult().Test() ?? "Database created.",
				Options = MessageOptions.Ok,
				DefaultAccept = MessageOptions.Ok,
				DefaultCancel = MessageOptions.Ok,
			}.Show();
		}

		void TestClick(object sender, RoutedEventArgs e)
		{
			new Message(this)
			{
				Title = "Information",
				Text = GetResult().Test() ?? "Connection successful.",
				Options = MessageOptions.Ok,
				DefaultAccept = MessageOptions.Ok,
				DefaultCancel = MessageOptions.Ok,
			}.Show();
		}

		void ResetOriginalClick(object sender, RoutedEventArgs e)
		{
			var tag = (sender as Button).Tag;
			var dbParams = new List<DBParam>();
			if (tag is DBParam)
				dbParams.Add(tag as DBParam);
			else
				dbParams.AddRange(parameters.Items.OfType<DBParam>());
			dbParams.ForEach(dbParam => dbParam.ResetOriginal());
			BindingOperations.GetMultiBindingExpression(parameters, DataGrid.ItemsSourceProperty).UpdateSource();
		}

		void ResetDefaultClick(object sender, RoutedEventArgs e)
		{
			var tag = (sender as Button).Tag;
			var dbParams = new List<DBParam>();
			if (tag is DBParam)
				dbParams.Add(tag as DBParam);
			else
				dbParams.AddRange(parameters.Items.OfType<DBParam>());
			dbParams.ForEach(dbParam => dbParam.ResetDefault());
			BindingOperations.GetMultiBindingExpression(parameters, DataGrid.ItemsSourceProperty).UpdateSource();
		}

		void OkClick(object sender, RoutedEventArgs e) => DialogResult = true;

		public static DBConnectInfo Run(Window parent, DBConnectInfo dbConnectInfo)
		{
			var dialog = new EditDatabaseConnectDialog(dbConnectInfo) { Owner = parent };
			return dialog.ShowDialog() ? dialog.GetResult() : null;
		}
	}

	class DBParamTemplateSelector : DataTemplateSelector
	{
		public string Path { get; set; }
		public bool ReadOnly { get; set; }

		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			var dbParam = item as EditDatabaseConnectDialog.DBParam;
			if (dbParam == null)
				return base.SelectTemplate(item, container);

			Binding binding;

			if (ReadOnly)
				binding = new Binding(Path) { Mode = BindingMode.OneWay };
			else
				binding = new Binding(Path) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, NotifyOnSourceUpdated = true };

			FrameworkElementFactory factory;
			if (dbParam.Type == typeof(bool))
			{
				factory = new FrameworkElementFactory(typeof(CheckBox));
				factory.SetBinding(CheckBox.IsCheckedProperty, binding);
			}
			else if ((dbParam.Type == typeof(string)) || (dbParam.Type == typeof(int)) || (dbParam.Type == typeof(int?)) || (dbParam.Type == typeof(uint)) || (dbParam.Type == typeof(byte[])))
			{
				factory = new FrameworkElementFactory(typeof(TextBox));
				factory.SetValue(TextBox.IsReadOnlyProperty, ReadOnly);
				factory.SetValue(TextBox.BorderThicknessProperty, new Thickness(0));
				if (dbParam.Type == typeof(byte[]))
					binding.Converter = new HexConverter();
				factory.SetBinding(TextBox.TextProperty, binding);
			}
			else if (dbParam.Type.IsEnum)
			{
				factory = new FrameworkElementFactory(typeof(ComboBox));
				factory.SetBinding(ComboBox.SelectedValueProperty, binding);
				var values = Enum.GetValues(dbParam.Type).Cast<object>().Distinct().ToDictionary(value => value.ToString(), value => value);
				if (!values.ContainsValue(dbParam.Default))
					values["<NONE>"] = dbParam.Default;
				factory.SetValue(ComboBox.ItemsSourceProperty, values);
				factory.SetValue(ComboBox.DisplayMemberPathProperty, "Key");
				factory.SetValue(ComboBox.SelectedValuePathProperty, "Value");
			}
			else if (dbParam.Type == typeof(object))
			{
				factory = new FrameworkElementFactory(typeof(Label));
				factory.SetBinding(Label.ContentProperty, binding);
			}
			else
				return base.SelectTemplate(item, container);

			factory.AddHandler(Binding.SourceUpdatedEvent, new EventHandler<DataTransferEventArgs>(ValueChanged));
			factory.SetValue(UIElement.IsEnabledProperty, !ReadOnly);
			return new DataTemplate { VisualTree = factory };
		}

		void ValueChanged(object sender, DataTransferEventArgs e)
		{
			var dataGrid = UIHelper.FindParent<DataGrid>(sender as FrameworkElement);
			if (dataGrid == null)
				return;

			BindingOperations.GetMultiBindingExpression(dataGrid, DataGrid.ItemsSourceProperty).UpdateSource();
		}
	}

	class ConnStrToListConverter : MarkupExtension, IMultiValueConverter
	{
		ConnStrToListConverter converter;
		public override object ProvideValue(IServiceProvider serviceProvider) => converter = converter ?? new ConnStrToListConverter();

		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if ((values[0] == null) || (values[1] == null) || (!(values[0] is DBConnectInfo.DBType)) || ((DBConnectInfo.DBType)values[0] == DBConnectInfo.DBType.None) || (!(values[1] is string)))
				return DependencyProperty.UnsetValue;

			var dbType = (DBConnectInfo.DBType)values[0];
			var connStr = values[1] as string;

			try
			{
				var builder = new DBConnectInfo { Type = dbType, ConnectionString = connStr }.GetBuilder();
				var defaults = new DBConnectInfo { Type = dbType }.GetBuilder();
				var baseProps = new HashSet<string>(typeof(DbConnectionStringBuilder).GetProperties().Select(prop => prop.Name));
				var props = builder.GetType().GetProperties().Where(prop => !baseProps.Contains(prop.Name)).Where(prop => prop.CanWrite).Where(prop => prop.GetIndexParameters().Length == 0).ToList();
				return props.Select(prop => new EditDatabaseConnectDialog.DBParam(dbType, prop, builder, defaults)).OrderBy(dbParam => dbParam.IsDefault).ToList();
			}
			catch { return DependencyProperty.UnsetValue; }
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			var list = value as IEnumerable<EditDatabaseConnectDialog.DBParam>;
			var type = list.Select(dbParam => dbParam.DBType).FirstOrDefault();
			var builder = new DBConnectInfo { Type = type }.GetBuilder();
			var props = builder.GetType().GetProperties().ToDictionary(prop => prop.Name);
			list.Where(dbParam => !dbParam.IsDefault).ForEach(dbParam => props[dbParam.Name].SetValue(builder, System.Convert.ChangeType(dbParam.Value, props[dbParam.Name].PropertyType)));
			return new object[] { type, builder.ToString() };
		}
	}
}
