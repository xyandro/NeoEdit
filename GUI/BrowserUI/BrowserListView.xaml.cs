using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using NeoEdit.GUI.Common;
using NeoEdit.Records;

namespace NeoEdit.GUI.BrowserUI
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

	public partial class BrowserListView : ListView
	{
		[DepProp]
		public RecordProperty.PropertyName SortProperty { get { return uiHelper.GetPropValue<RecordProperty.PropertyName>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool SortAscending { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public List<GUIRecord> Records { get { return uiHelper.GetPropValue<List<GUIRecord>>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public ObservableCollection<RecordProperty.PropertyName> Properties { get { return uiHelper.GetPropValue<ObservableCollection<RecordProperty.PropertyName>>(); } set { uiHelper.SetPropValue(value); } }

		readonly CollectionViewSource collectionView;

		static BrowserListView() { UIHelper<BrowserListView>.Register(); }

		readonly UIHelper<BrowserListView> uiHelper;
		public BrowserListView()
		{
			uiHelper = new UIHelper<BrowserListView>(this);
			uiHelper.AddObservableCallback(a => a.Properties, SetupColumns);
			InitializeComponent();

			collectionView = FindResource("collectionView") as CollectionViewSource;
			uiHelper.AddCallback(CollectionViewSource.ViewProperty, collectionView, Resort);
			uiHelper.AddCallback(a => a.SortProperty, (o, n) => Resort());
			uiHelper.AddCallback(a => a.SortAscending, (o, n) => Resort());
		}

		void SetupColumns()
		{
			gridView.Columns.Clear();
			Properties.ToList().ForEach(a => gridView.Columns.Add(new BrowserColumn(this) { Property = a }));
			Resort();
		}

		public int Compare(Record record1, Record record2)
		{
			var propertyValue1 = record1[SortProperty];
			var propertyValue2 = record2[SortProperty];
			if ((propertyValue1 == null) && (propertyValue2 == null))
			{
				propertyValue1 = record1.FullName;
				propertyValue2 = record2.FullName;
			}
			if (propertyValue1 == null)
				return 1;
			if (propertyValue2 == null)
				return -1;
			if ((propertyValue1 as IComparable).CompareTo(propertyValue2) == 0)
			{
				propertyValue1 = record1.FullName;
				propertyValue2 = record2.FullName;
			}
			return (propertyValue1 as IComparable).CompareTo(propertyValue2) * (SortAscending ? 1 : -1);
		}

		public void Resort()
		{
			if ((Properties == null) || (Properties.Count() == 0))
				return;

			if (!Properties.Contains(SortProperty))
				SortProperty = Properties.First();

			var view = collectionView.View as ListCollectionView;
			if (view != null)
				view.CustomSort = new LambdaComparer<GUIRecord>((r1, r2) => Compare(r1.record, r2.record));
		}
	}
}
