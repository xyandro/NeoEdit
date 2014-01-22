using NeoEdit.Records;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Controls;

namespace NeoEdit.UI.Controls
{
	/// <summary>
	/// Interaction logic for NEListView.xaml
	/// </summary>
	public partial class NEListView : ListView
	{
		[DepProp]
		public IEnumerable<Record.Property> Properties { get { return uiHelper.GetPropValue<IEnumerable<Record.Property>>(); } set { uiHelper.SetPropValue(value); } }

		readonly UIHelper<NEListView> uiHelper;
		public NEListView()
		{
			uiHelper = new UIHelper<NEListView>(this);
			uiHelper.AddCallback(a => a.Properties, PropertiesChanged);
			InitializeComponent();
		}

		void PropertiesChanged(object oldValue, object newValue)
		{
			var observableCollection = oldValue as ObservableCollection<Record.Property>;
			if (observableCollection != null)
				observableCollection.CollectionChanged -= PropertiesListChanged;

			observableCollection = newValue as ObservableCollection<Record.Property>;
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
			Properties.ToList().ForEach(a => gridView.Columns.Add(new NEColumn { Property = a }));
		}
	}
}
