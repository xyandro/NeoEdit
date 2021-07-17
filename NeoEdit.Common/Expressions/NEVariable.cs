using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common.Parsing;

namespace NeoEdit.Common.Expressions
{
	public class NEVariable
	{
		public string Name { get; }
		public string Description { get; }
		public Func<(Func<int, object>, int?)> DelayedGetValue { get; }

		int? count;
		public int? Count { get { Setup(); return count; } }

		Func<int, object> getValueFunc;

		internal NEVariable(string name, string description, Func<(Func<int, object>, int?)> delayedGetValue)
		{
			Name = name;
			Description = description;
			DelayedGetValue = delayedGetValue;
		}

		void Setup()
		{
			if (getValueFunc == null)
				lock (this)
					if (getValueFunc == null)
					{
						(getValueFunc, count) = DelayedGetValue();
						if (getValueFunc == null)
							throw new Exception($"Shouldn't ever have a null {nameof(getValueFunc)}");
					}
		}

		internal object GetValue(int index, NEVariableRepeat repeat, int rowCount)
		{
			Setup();

			switch (repeat)
			{
				case NEVariableRepeat.None:
					if (index >= count)
						throw new Exception("Invalid count");
					break;
				case NEVariableRepeat.Cycle:
					if (count.HasValue)
						index %= count.Value;
					break;
				case NEVariableRepeat.Repeat:
					if (count.HasValue)
						index = index * count.Value / rowCount;
					break;
			}

			return getValueFunc(index);
		}

		internal List<object> GetValues(NEVariableRepeat repeat, int count, int rowCount) => Enumerable.Range(0, count).Select(row => GetValue(row, repeat, rowCount)).ToList();

		internal List<object> GetValues(NEVariableRepeat repeat, int rowCount) => GetValues(repeat, rowCount, rowCount);

		public static NEVariable Constant<T>(string name, string description, Func<T> getValue)
		{
			return new NEVariable(name, description, () =>
			{
				var value = getValue();
				return (index => value, 1);
			});
		}

		public static NEVariable List<T>(string name, string description, Func<IEnumerable<T>> getList)
		{
			return new NEVariable(name, description, () =>
			{
				var list = getList().Cast<object>().ToList();
				return (index => list[index], list.Count);
			});
		}

		public static NEVariable Series<T>(string name, string description, Func<int, T> getSeries)
		{
			return new NEVariable(name, description, () => (index => getSeries(index), null));
		}

		public override string ToString() => Name;
	}
}
