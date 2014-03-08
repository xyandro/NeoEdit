using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Records;

namespace NeoEdit.Browser
{
	class GUIRecord : DependencyObject
	{
		static Dictionary<RecordProperty.PropertyName, DependencyProperty> dependencyProperty;
		static GUIRecord()
		{
			var properties = Enum.GetValues(typeof(RecordProperty.PropertyName)).Cast<RecordProperty.PropertyName>().ToList();
			dependencyProperty = properties.ToDictionary(a => a, a => DependencyProperty.Register(a.ToString(), RecordProperty.Get(a).Type, typeof(GUIRecord)));
		}

		public Record record { get; private set; }

		public GUIRecord(Record _record)
		{
			record = _record;
			dependencyProperty.Keys.ToList().ForEach(prop => SetProperty(prop, record[prop]));
			record.PropertyChanged += SetProperty;
		}

		void SetProperty(RecordProperty.PropertyName property, object value)
		{
			SetValue(dependencyProperty[property], value);
		}
	}
}
