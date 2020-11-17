using System;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class FilesList
	{
		NEFilesHandler neFiles;
		OrderedHashSet<NEFileHandler> allFiles, activeFiles;
		bool activeFilesSorted;
		NEFileHandler focused;

		public FilesList(NEFilesHandler neFiles)
		{
			this.neFiles = neFiles;
			allFiles = new OrderedHashSet<NEFileHandler>();
			activeFiles = new OrderedHashSet<NEFileHandler>();
			activeFilesSorted = true;
			focused = null;
		}

		public FilesList(FilesList old)
		{
			neFiles = old.neFiles;
			allFiles = new OrderedHashSet<NEFileHandler>(old.allFiles);
			activeFiles = new OrderedHashSet<NEFileHandler>(old.activeFiles);
			activeFilesSorted = old.activeFilesSorted;
			focused = old.focused;
		}

		public IReadOnlyOrderedHashSet<NEFileHandler> AllFiles => allFiles;

		public void InsertFile(FilesList old, NEFileHandler neFile, int? index = null)
		{
			if (neFile == null)
				throw new ArgumentNullException();
			if (neFile.NEFiles != null)
				throw new Exception("File already assigned");
			if (allFiles.Contains(neFile))
				throw new Exception("File already in list");

			neFiles.AddToTransaction(neFile);

			allFiles.Insert(index ?? allFiles.Count, neFile);
			neFile.NEFiles = neFiles;

			if (!activeFiles.Where(x => !old.Contains(x)).Any())
				ClearActive();

			activeFiles.Add(neFile);
			activeFilesSorted = false;
			if (focused == null)
				focused = neFile;
		}

		public void RemoveFile(NEFileHandler neFile)
		{
			if (neFile == null)
				throw new ArgumentNullException();
			if (neFile.NEFiles == null)
				throw new Exception("File not assigned");
			if (!allFiles.Contains(neFile))
				throw new Exception("File not in list");

			neFiles.AddToTransaction(neFile);

			allFiles.Remove(neFile);
			neFile.NEFiles = null;

			activeFiles.Remove(neFile);
			if (focused == neFile)
				focused = activeFiles.LastOrDefault();

			if (focused == null)
			{
				focused = allFiles.OrderByDescending(x => x.LastActive).FirstOrDefault();
				if (focused != null)
				{
					activeFiles.Add(focused);
					activeFilesSorted = false;
				}
			}

			if (focused != null)
				neFiles.AddToTransaction(focused);
		}

		public void MoveFile(NEFileHandler neFile, int index)
		{
			var oldIndex = allFiles.IndexOf(neFile);
			if (oldIndex == -1)
				throw new Exception("File not found");
			allFiles.RemoveAt(oldIndex);
			allFiles.Insert(index, neFile);
			activeFilesSorted = false;
		}

		public IReadOnlyOrderedHashSet<NEFileHandler> ActiveFiles
		{
			get
			{
				if (!activeFilesSorted)
				{
					activeFiles = new OrderedHashSet<NEFileHandler>(allFiles.Where(neFile => activeFiles.Contains(neFile)));
					activeFilesSorted = true;
				}
				return activeFiles;
			}
		}

		public void ClearActive()
		{
			activeFiles = new OrderedHashSet<NEFileHandler>();
			activeFilesSorted = true;
			focused = null;
		}

		public bool Contains(NEFileHandler neFile) => allFiles.Contains(neFile);

		public void SetActive(NEFileHandler neFile, bool active = true)
		{
			if (neFile == null)
				throw new ArgumentNullException();
			if (activeFiles.Contains(neFile) == active)
				return;

			if (active)
			{
				neFiles.AddToTransaction(neFile);

				activeFiles.Add(neFile);
				activeFilesSorted = false;
				if (focused == null)
					focused = neFile;
			}
			else
			{
				activeFiles.Remove(neFile);
				if (focused == neFile)
					focused = activeFiles.OrderByDescending(x => x.LastActive).FirstOrDefault();
			}
		}

		public bool IsActive(NEFileHandler neFile) => activeFiles.Contains(neFile);

		public NEFileHandler Focused
		{
			get => focused;
			set
			{
				if ((value != null) && (!activeFiles.Contains(value)))
					throw new Exception("Value not in active set");
				focused = value;
			}
		}
	}
}
