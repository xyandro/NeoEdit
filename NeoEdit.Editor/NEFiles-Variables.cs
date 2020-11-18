using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	partial class NEFiles
	{
		void EnsureInTransaction()
		{
			if (!inTransaction)
				throw new Exception("Must start transaction before editing data");
		}

		NEFilesData saveFilesData, filesData;

		public IReadOnlyOrderedHashSet<NEFile> AllFiles
		{
			get => filesData.allFiles;
			set
			{
				EnsureInTransaction();
				filesData.allFiles = value;
			}
		}

		public IReadOnlyOrderedHashSet<NEFile> ActiveFiles
		{
			get => filesData.activeFiles;
			set
			{
				EnsureInTransaction();
				filesData.activeFiles = value;
				if (!filesData.activeFiles.Contains(Focused))
					Focused = filesData.activeFiles.FirstOrDefault();
			}
		}

		public void ClearActiveFiles() => ActiveFiles = new OrderedHashSet<NEFile>();

		public void SetActiveFile(NEFile file) => ActiveFiles = new OrderedHashSet<NEFile> { file };

		public void SetActiveFiles(IEnumerable<NEFile> files) => ActiveFiles = new OrderedHashSet<NEFile>(files);

		public NEFile Focused
		{
			get => filesData.focused;
			set
			{
				EnsureInTransaction();
				filesData.focused = value;
			}
		}

		HashSet<NEFile> transactionFiles;
		public void AddToTransaction(NEFile neFile)
		{
			if (transactionFiles.Contains(neFile))
				return;
			transactionFiles.Add(neFile);
			neFile.BeginTransaction();
		}

		public WindowLayout WindowLayout
		{
			get => filesData.windowLayout;
			set
			{
				EnsureInTransaction();
				filesData.windowLayout = value;
			}
		}

		public bool ActiveOnly
		{
			get => filesData.activeOnly;
			set
			{
				EnsureInTransaction();
				filesData.activeOnly = value;
			}
		}

		public bool MacroVisualize
		{
			get => filesData.macroVisualize;
			set
			{
				EnsureInTransaction();
				filesData.macroVisualize = value;
			}
		}

		public NEFilesResult result { get; private set; }
		NEFilesResult CreateResult()
		{
			if (result == null)
				result = new NEFilesResult();
			return result;
		}

		public void ClearResult() => result = null;

		public void AddNewFile(NEFile neFile) => CreateResult().AddNewFile(neFile);

		public void BeginTransaction()
		{
			if (inTransaction)
				throw new Exception("Already in a transaction");
			inTransaction = true;
			saveFilesData = filesData;
			filesData = filesData.Clone();
			transactionFiles = new HashSet<NEFile>();
			ActiveFiles.ForEach(AddToTransaction);
		}

		public void Rollback()
		{
			EnsureInTransaction();

			filesData = saveFilesData;

			transactionFiles.ForEach(neFile => neFile.Rollback());
			transactionFiles = null;

			inTransaction = false;
			result = null;
		}

		public void Commit()
		{
			EnsureInTransaction();

			transactionFiles.ForEach(neFile => neFile.Commit());
			transactionFiles = null;

			NEClipboard setClipboard = null;
			List<KeysAndValues>[] setKeysAndValues = null;
			List<string> dragFiles = null;

			var nextAllFiles = new OrderedHashSet<NEFile>();
			var newFiles = new List<NEFile>();
			var filesChanged = false;
			foreach (var neFile in AllFiles)
			{
				if (neFile.result == null)
				{
					nextAllFiles.Add(neFile);
					continue;
				}

				if (neFile.result.Files == null)
					nextAllFiles.Add(neFile);
				else
				{
					neFile.result.Files.ForEach(nextAllFiles.Add);
					filesChanged = true;
				}

				if (neFile.result.NewFiles != null)
				{
					newFiles.AddRange(neFile.result.NewFiles);
					filesChanged = true;
				}

				if (neFile.result.Clipboard != null)
				{
					if (setClipboard == null)
						setClipboard = new NEClipboard();
					setClipboard.Add(neFile.result.Clipboard.Item1);
					setClipboard.IsCut = neFile.result.Clipboard.Item2;
				}

				if (neFile.result.KeysAndValues != null)
					for (var kvIndex = 0; kvIndex < KeysAndValuesCount; ++kvIndex)
						if (neFile.result.KeysAndValues[kvIndex] != null)
						{
							if (setKeysAndValues == null)
								setKeysAndValues = new List<KeysAndValues>[KeysAndValuesCount];
							if (setKeysAndValues[kvIndex] == null)
								setKeysAndValues[kvIndex] = new List<KeysAndValues>();
							setKeysAndValues[kvIndex].Add(neFile.result.KeysAndValues[kvIndex]);
						}

				if (neFile.result.DragFiles != null)
				{
					if (dragFiles == null)
						dragFiles = new List<string>();
					dragFiles.AddRange(neFile.result.DragFiles);
				}

				neFile.ClearResult();
			}

			newFiles.ForEach(nextAllFiles.Add);

			if (result != null)
			{
				if (result.NewFiles != null)
				{
					result.NewFiles.ForEach(nextAllFiles.Add);
					filesChanged = true;
				}
			}

			if (filesChanged)
			{
				var newlyAdded = nextAllFiles.Except(AllFiles).ToList();

				AllFiles = nextAllFiles;

				if (newlyAdded.Any())
					SetActiveFiles(newlyAdded);
				else
				{
					SetActiveFiles(AllFiles.Intersect(ActiveFiles));
					if (!ActiveFiles.Any())
					{
						var newActive = AllFiles.OrderByDescending(file => file.LastActive).FirstOrDefault();
						if (newActive != null)
							SetActiveFile(newActive);
					}
				}

				var now = DateTime.Now;
				ActiveFiles.ForEach(neFile => neFile.LastActive = now);
			}

			if (setClipboard != null)
				CreateResult().SetClipboard(setClipboard);

			if (setKeysAndValues != null)
				CreateResult().SetKeysAndValues(setKeysAndValues);

			if (dragFiles != null)
				CreateResult().SetDragFiles(dragFiles);

			inTransaction = false;
		}
	}
}
