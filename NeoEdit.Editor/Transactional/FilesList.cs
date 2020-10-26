using System;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Editor.Transactional
{
	public class FilesList
	{
		NEFiles neFiles;
		OrderedHashSet<NEFile> allFiles, activeFiles;
		bool activeFilesSorted;
		NEFile focused;

		public FilesList(NEFiles neFiles)
		{
			this.neFiles = neFiles;
			allFiles = new OrderedHashSet<NEFile>();
			activeFiles = new OrderedHashSet<NEFile>();
			activeFilesSorted = true;
			focused = null;
		}

		public FilesList(FilesList old)
		{
			neFiles = old.neFiles;
			allFiles = new OrderedHashSet<NEFile>(old.allFiles);
			activeFiles = new OrderedHashSet<NEFile>(old.activeFiles);
			activeFilesSorted = old.activeFilesSorted;
			focused = old.focused;
		}

		public IReadOnlyOrderedHashSet<NEFile> AllFiles => allFiles;

		public void InsertFile(FilesList old, NEFile neFile, int? index = null)
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

		public void RemoveFile(NEFile neFile)
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

		public void MoveFile(NEFile neFile, int index)
		{
			var oldIndex = allFiles.IndexOf(neFile);
			if (oldIndex == -1)
				throw new Exception("File not found");
			allFiles.RemoveAt(oldIndex);
			allFiles.Insert(index, neFile);
			activeFilesSorted = false;
		}

		public IReadOnlyOrderedHashSet<NEFile> ActiveFiles
		{
			get
			{
				if (!activeFilesSorted)
				{
					activeFiles = new OrderedHashSet<NEFile>(allFiles.Where(neFile => activeFiles.Contains(neFile)));
					activeFilesSorted = true;
				}
				return activeFiles;
			}
		}

		public void ClearActive()
		{
			activeFiles = new OrderedHashSet<NEFile>();
			activeFilesSorted = true;
			focused = null;
		}

		public bool Contains(NEFile neFile) => allFiles.Contains(neFile);

		public void SetActive(NEFile neFile, bool active = true)
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

		public bool IsActive(NEFile neFile) => activeFiles.Contains(neFile);

		public NEFile Focused
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
