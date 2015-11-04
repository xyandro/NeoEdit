using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Converters;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class EditDatabaseConnectDialog
	{
		public class DBParam
		{
			public string Name { get; }
			public object Value { get; }
			public object Default { get; }
			public Type Type { get; }

			public DBParam(string name, object value, object _default, Type type)
			{
				Name = name;
				Value = value;
				Default = _default;
				Type = type;
			}

			public override string ToString() => $"{Name} = {Value}";
		}

		[DepProp]
		public string ConnectionName { get { return UIHelper<EditDatabaseConnectDialog>.GetPropValue<string>(this); } set { UIHelper<EditDatabaseConnectDialog>.SetPropValue(this, value); } }
		[DepProp]
		public DBConnectInfo.DBType Database { get { return UIHelper<EditDatabaseConnectDialog>.GetPropValue<DBConnectInfo.DBType>(this); } set { UIHelper<EditDatabaseConnectDialog>.SetPropValue(this, value); } }
		[DepProp]
		public ObservableCollection<DBParam> Params { get { return UIHelper<EditDatabaseConnectDialog>.GetPropValue<ObservableCollection<DBParam>>(this); } set { UIHelper<EditDatabaseConnectDialog>.SetPropValue(this, value); } }

		static EditDatabaseConnectDialog()
		{
			UIHelper<EditDatabaseConnectDialog>.Register();
			UIHelper<EditDatabaseConnectDialog>.AddCallback(a => a.Database, (obj, o, n) => obj.SetParams(new DBConnectInfo { Type = obj.Database }));
		}

		EditDatabaseConnectDialog(DBConnectInfo dbConnectInfo)
		{
			InitializeComponent();
			type.ItemsSource = Enum.GetValues(typeof(DBConnectInfo.DBType)).Cast<DBConnectInfo.DBType>().Where(item => item != DBConnectInfo.DBType.None).ToList();
			ConnectionName = dbConnectInfo.Name;
			Database = dbConnectInfo.Type;
			SetParams(dbConnectInfo);
		}

		void SetParams(DBConnectInfo dbConnectInfo)
		{
			if (dbConnectInfo.Type == DBConnectInfo.DBType.None)
			{
				Params = new ObservableCollection<DBParam>();
				return;
			}
			var itemBuilder = dbConnectInfo.ConnectionStringBuilder;
			var defaultBuilder = new DBConnectInfo { Type = Database }.ConnectionStringBuilder;
			var baseProps = new HashSet<string>(typeof(DbConnectionStringBuilder).GetProperties().Select(prop => prop.Name));
			var props = itemBuilder.GetType().GetProperties().Where(prop => !baseProps.Contains(prop.Name)).Where(prop => prop.CanWrite).Where(prop => prop.GetIndexParameters().Length == 0).ToDictionary(prop => prop.Name);
			Params = new ObservableCollection<DBParam>(props.Values.Select(prop =>
			{
				object itemValue = null, defaultValue = null;
				try { itemValue = prop.GetValue(itemBuilder); } catch { }
				try { defaultValue = prop.GetValue(defaultBuilder); } catch { }
				return new DBParam(prop.Name, itemValue, defaultValue, prop.PropertyType);
			}).OrderBy(dbParam => (dbParam.Value ?? "").Equals(dbParam.Default ?? "")));
		}

		DBConnectInfo Result
		{
			get
			{
				var result = new DBConnectInfo { Name = ConnectionName, Type = Database };
				var builder = result.ConnectionStringBuilder;
				var props = builder.GetType().GetProperties().ToDictionary(prop => prop.Name);
				foreach (var dbParam in Params)
					if (!(dbParam.Value ?? "").Equals(dbParam.Default ?? ""))
						props[dbParam.Name].SetValue(builder, dbParam.Value);
				result.ConnectionStringBuilder = builder;
				return result;
			}
		}

		void CreateClick(object sender, RoutedEventArgs e)
		{
			Result.CreateDatabase();

			new Message
			{
				Title = "Information",
				Text = Result.Test() ?? "Database created.",
				Options = Message.OptionsEnum.Ok,
				DefaultAccept = Message.OptionsEnum.Ok,
				DefaultCancel = Message.OptionsEnum.Ok,
			}.Show();
		}

		void TestClick(object sender, RoutedEventArgs e)
		{
			new Message
			{
				Title = "Information",
				Text = Result.Test() ?? "Connection successful.",
				Options = Message.OptionsEnum.Ok,
				DefaultAccept = Message.OptionsEnum.Ok,
				DefaultCancel = Message.OptionsEnum.Ok,
			}.Show();
		}

		void OkClick(object sender, RoutedEventArgs e) => DialogResult = true;

		public static DBConnectInfo Run(Window parent, DBConnectInfo dbConnectInfo)
		{
			var dialog = new EditDatabaseConnectDialog(dbConnectInfo) { Owner = parent };
			return dialog.ShowDialog() ? dialog.Result : null;
		}
	}

	class DBParamTemplateSelector : DataTemplateSelector
	{
		public bool Default { get; set; }

		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			var dbParam = item as EditDatabaseConnectDialog.DBParam;
			if (dbParam == null)
				return base.SelectTemplate(item, container);

			var binding = new Binding(Default ? "Default" : "Value") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };

			if (dbParam.Type == typeof(bool))
			{
				var checkbox = new FrameworkElementFactory(typeof(CheckBox));
				checkbox.SetValue(CheckBox.IsEnabledProperty, !Default);
				checkbox.SetBinding(CheckBox.IsCheckedProperty, binding);
				return new DataTemplate { VisualTree = checkbox };
			}
			if ((dbParam.Type == typeof(string)) || (dbParam.Type == typeof(int)) || (dbParam.Type == typeof(int?)) || (dbParam.Type == typeof(uint)) || (dbParam.Type == typeof(byte[])))
			{
				var textbox = new FrameworkElementFactory(typeof(TextBox));
				textbox.SetValue(TextBox.IsReadOnlyProperty, Default);
				textbox.SetValue(TextBox.BorderThicknessProperty, new Thickness(0));
				if (dbParam.Type == typeof(byte[]))
					binding.Converter = new HexConverter();
				textbox.SetBinding(TextBox.TextProperty, binding);
				return new DataTemplate { VisualTree = textbox };
			}
			if (dbParam.Type.IsEnum)
			{
				var combobox = new FrameworkElementFactory(typeof(ComboBox));
				combobox.SetValue(ComboBox.IsEnabledProperty, !Default);
				combobox.SetBinding(ComboBox.SelectedValueProperty, binding);
				var values = Enum.GetValues(dbParam.Type).Cast<object>().Distinct().ToDictionary(value => value.ToString(), value => value);
				if (!values.ContainsValue(dbParam.Default))
					values["<NONE>"] = dbParam.Default;
				combobox.SetValue(ComboBox.ItemsSourceProperty, values);
				combobox.SetValue(ComboBox.DisplayMemberPathProperty, "Key");
				combobox.SetValue(ComboBox.SelectedValuePathProperty, "Value");
				return new DataTemplate { VisualTree = combobox };
			}
			if (dbParam.Type == typeof(object))
			{
				var label = new FrameworkElementFactory(typeof(Label));
				label.SetValue(Label.IsEnabledProperty, !Default);
				label.SetBinding(Label.ContentProperty, binding);
				return new DataTemplate { VisualTree = label };
			}

			return base.SelectTemplate(item, container);
		}
	}
}
