using System;

namespace NeoEdit.TaskRunning
{
	class TaskRunnerProgress : ITaskRunnerProgress
	{
		long lastCurrent = 0;
		public long Current
		{
			get => lastCurrent;
			set
			{
				if (value > Total)
					value = Total;
				if (value < 0)
					value = 0;
				addCurrent(value - lastCurrent);
				lastCurrent = value;
			}
		}
		long lastTotal = 0;
		public long Total
		{
			get => lastTotal;
			set
			{
				addTotal(value - lastTotal);
				lastTotal = value;
			}
		}

		readonly Action<long> addCurrent;
		readonly Action<long> addTotal;
		internal TaskRunnerProgress(Action<long> addCurrent, Action<long> addTotal)
		{
			this.addCurrent = addCurrent;
			this.addTotal = addTotal;
		}

		internal void Reset(long total)
		{
			lastCurrent = 0;
			lastTotal = total;
		}
	}
}
