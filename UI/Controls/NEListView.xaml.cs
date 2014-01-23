using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using NeoEdit.Records;

namespace NeoEdit.UI.Controls
{
	class LambdaComparer<T> : IComparer where T : class
	{
		readonly Func<T, T, int> lambda;
		public LambdaComparer(Func<T, T, int> _lambda)
		{
			lambda = _lambda;
		}

		public int Compare(object o1, object o2)
		{
			return lambda(o1 as T, o2 as T);
		}
	}

	public partial class NEListView : ListView
	{
		[DepProp]
		public Property.PropertyType SortProperty { get { return uiHelper.GetPropValue<Property.PropertyType>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool SortAscending { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public IEnumerable<Record> Records { get { return uiHelper.GetPropValue<IEnumerable<Record>>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public IEnumerable<Property.PropertyType> Properties { get { return uiHelper.GetPropValue<IEnumerable<Property.PropertyType>>(); } set { uiHelper.SetPropValue(value); } }

		readonly UIHelper<NEListView> uiHelper;
		readonly CollectionViewSource collectionView;
		public NEListView()
		{
			uiHelper = new UIHelper<NEListView>(this);
			uiHelper.AddCallback(a => a.Properties, PropertiesChanged);
			InitializeComponent();

			collectionView = FindResource("collectionView") as CollectionViewSource;
			uiHelper.AddCallback(CollectionViewSource.ViewProperty, collectionView, Resort);
			uiHelper.AddCallback(a => a.SortProperty, (o, n) => { SortAscending = Property.Get(SortProperty).DefaultAscending; Resort(); });
			uiHelper.AddCallback(a => a.SortAscending, (o, n) => Resort());
		}

		void PropertiesChanged(object oldValue, object newValue)
		{
			var observableCollection = oldValue as ObservableCollection<Property.PropertyType>;
			if (observableCollection != null)
				observableCollection.CollectionChanged -= PropertiesListChanged;

			observableCollection = newValue as ObservableCollection<Property.PropertyType>;
			if (observableCollection != null)
				observableCollection.CollectionChanged += PropertiesListChanged;

			SetupColumns();
		}

		void PropertiesListChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			SetupColumns();
		}

		void SetupColumns()
		{
			gridView.Columns.Clear();
			Properties.ToList().ForEach(a => gridView.Columns.Add(new NEColumn(this) { Property = a }));
			Resort();
		}

		public int Compare(Record record1, Record record2)
		{
			var propertyValue1 = record1[SortProperty];
			var propertyValue2 = record2[SortProperty];
			if (propertyValue1 == propertyValue2)
				return 0;
			if (propertyValue1 == null)
				return 1;
			if (propertyValue2 == null)
				return -1;
			return (propertyValue1 as IComparable).CompareTo(propertyValue2) * (SortAscending ? 1 : -1);
		}

		void Resort()
		{
			if ((Properties == null) || (Properties.Count() == 0))
				return;

			if (!Properties.Contains(SortProperty))
				SortProperty = Properties.First();

			var view = collectionView.View as ListCollectionView;
			if (view != null)
				view.CustomSort = new LambdaComparer<Record>((r1, r2) => Compare(r1, r2));
		}
	}
}
