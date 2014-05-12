using System.Collections.Generic;

namespace NeoEdit.TextEditor
{
	class RangeList : List<Range>
	{
		public delegate void CollectionChangedDelegate();
		CollectionChangedDelegate collectionChanged = () => { };
		public event CollectionChangedDelegate CollectionChanged { add { collectionChanged += value; } remove { collectionChanged -= value; } }

		public RangeList() { }
		public RangeList(IEnumerable<Range> collection) : base(collection) { }

		public new void Clear()
		{
			base.Clear();
			collectionChanged();
		}

		public new void Add(Range item)
		{
			base.Add(item);
			collectionChanged();
		}

		public new void Remove(Range item)
		{
			base.Remove(item);
			collectionChanged();
		}

		public void Replace(RangeList items)
		{
			base.Clear();
			base.AddRange(items);
			collectionChanged();
		}

		public void Replace(Range item)
		{
			base.Clear();
			base.Add(item);
			collectionChanged();
		}

		public new void AddRange(IEnumerable<Range> items)
		{
			base.AddRange(items);
			collectionChanged();
		}
	}

	static class RangeListExtensions
	{
		public static RangeList ToList(this IEnumerable<Range> source)
		{
			return new RangeList(source);
		}
	}
}
