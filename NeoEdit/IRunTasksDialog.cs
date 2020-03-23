using System;
using System.Collections.Generic;
using NeoEdit.Common;

namespace NeoEdit.Program
{
	public interface IRunTasksDialog
	{
		void AddTasks<TSource>(IEnumerable<TSource> items, Action<TSource> taskFunc);
		void AddTasks<TSource>(IEnumerable<TSource> items, Action<TSource, int> taskFunc);
		void AddTasks<TSource>(IEnumerable<TSource> items, Action<TSource, ProgressData> taskFunc);
		void AddTasks<TSource>(IEnumerable<TSource> items, Action<TSource, int, ProgressData> taskFunc);
		void AddTasks<TSource, TResult>(IEnumerable<TSource> items, Func<TSource, TResult> taskFunc, Action<List<TResult>> finished);
		void AddTasks<TSource, TResult>(IEnumerable<TSource> items, Func<TSource, int, TResult> taskFunc, Action<List<TResult>> finished);
		void AddTasks<TSource, TResult>(IEnumerable<TSource> items, Func<TSource, ProgressData, TResult> taskFunc, Action<List<TResult>> finished);
		void AddTasks<TSource, TResult>(IEnumerable<TSource> items, Func<TSource, int, ProgressData, TResult> taskFunc, Action<List<TResult>> finished);
		void Run();
	}
}
