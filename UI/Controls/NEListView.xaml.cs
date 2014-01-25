using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using NeoEdit.Records;
using NeoEdit.UI.Converters;

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
		public RecordProperty.PropertyName SortProperty { get { return uiHelper.GetPropValue<RecordProperty.PropertyName>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool SortAscending { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public ObservableCollection<Record> Records { get { return uiHelper.GetPropValue<ObservableCollection<Record>>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public ObservableCollection<RecordProperty.PropertyName> Properties { get { return uiHelper.GetPropValue<ObservableCollection<RecordProperty.PropertyName>>(); } set { uiHelper.SetPropValue(value); } }

		readonly UIHelper<NEListView> uiHelper;
		readonly CollectionViewSource collectionView;
		public NEListView()
		{
			uiHelper = new UIHelper<NEListView>(this);
			uiHelper.AddObservableCallback(a => a.Properties, SetupColumns);
			InitializeComponent();

			collectionView = FindResource("collectionView") as CollectionViewSource;
			uiHelper.AddCallback(CollectionViewSource.ViewProperty, collectionView, Resort);
			uiHelper.AddCallback(a => a.SortProperty, (o, n) => { SortAscending = RecordProperty.Get(SortProperty).DefaultAscending; Resort(); });
			uiHelper.AddCallback(a => a.SortAscending, (o, n) => Resort());
		}

		void SetupColumns()
		{
			gridView.Columns.Clear();
			foreach (var property in Properties)
			{
				var headerBinding = new MultiBinding { Converter = new PropertyToSortIndicatorHeader(property) };
				headerBinding.Bindings.Add(new Binding("SortProperty") { Source = this });
				headerBinding.Bindings.Add(new Binding("SortAscending") { Source = this });

				var col = new GridViewColumn { DisplayMemberBinding = new Binding(property.ToString()) { Converter = new PropertyFormatter() } };
				BindingOperations.SetBinding(col, GridViewColumn.HeaderProperty, headerBinding);
				gridView.Columns.Add(col);
			}
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

		public void Resort()
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
