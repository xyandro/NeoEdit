using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.TaskRunning;

namespace NeoEdit.Editor
{
	partial class NEFile
	{
		class GotoRange
		{
			public class GotoLocation
			{
				public int? Line { get; set; }
				public int? Index { get; set; }
				public int? Column { get; set; }
				public int? Position { get; set; }

				public GotoLocation(int? line = null, int? index = null, int? column = null, int? position = null)
				{
					Line = line;
					Index = index;
					Column = column;
					Position = position;
				}

				public override string ToString() => $"Line: {Line}, Index: {Index}, Column: {Column}, Position: {Position}";

				public int? GetPosition(NEFile neFile, int position, GotoLocation lastPosition = null)
				{
					if ((Line == null) && (Index == null) && (Column == null) && (Position == null))
						return null;

					if (Position.HasValue)
						return Math.Max(0, Math.Min(Position.Value, neFile.Text.Length));

					var line = Math.Max(0, Math.Min(neFile.Text.GetNonDiffLine(Line ?? lastPosition?.Line ?? neFile.Text.GetPositionLine(position)), neFile.Text.NumLines - 1));
					var index = Index ?? lastPosition?.Index;
					if (index.HasValue)
						index = Math.Max(0, Math.Min(index.Value, neFile.Text.GetLineLength(line)));
					else
					{
						var column = Column ?? lastPosition?.Column;
						if (column.HasValue)
							index = neFile.Text.GetIndexFromColumn(line, Math.Max(0, column.Value), true);
						else
							index = 0;
					}

					return Math.Max(0, Math.Min(neFile.Text.GetPosition(line, index.Value), neFile.Text.Length));
				}
			}

			public string File { get; set; }
			public GotoLocation Start { get; } = new GotoLocation();
			public GotoLocation End { get; } = new GotoLocation();

			GotoRange() { }

			public override string ToString() => $"{Start} - {End}";

			public static List<GotoRange> GetPositionsData(IEnumerable<string> strs, GotoType gotoType)
			{
				void setFile(GotoRange pd, string value) => pd.File = value;
				void setStartLine(GotoRange pd, string value) => pd.Start.Line = int.Parse(value) - 1;
				void setEndLine(GotoRange pd, string value) => pd.End.Line = int.Parse(value) - 1;
				void setStartIndex(GotoRange pd, string value) => pd.Start.Index = int.Parse(value) - 1;
				void setEndIndex(GotoRange pd, string value) => pd.End.Index = int.Parse(value) - 1;
				void setStartColumn(GotoRange pd, string value) => pd.Start.Column = int.Parse(value) - 1;
				void setEndColumn(GotoRange pd, string value) => pd.End.Column = int.Parse(value) - 1;
				void setStartPosition(GotoRange pd, string value) => pd.Start.Position = int.Parse(value);
				void setEndPosition(GotoRange pd, string value) => pd.End.Position = int.Parse(value);

				string regexPattern;
				List<Action<GotoRange, string>> actions;
				switch (gotoType)
				{
					case GotoType.Line:
						regexPattern = @"^(?:((?:[a-z]:)?[\w\s,\\.-]+):)?(\d+)(?:-(\d+))?|((?:[a-z]:)?[\w\s,\\.-]+)\((\d+)\)$";
						actions = new List<Action<GotoRange, string>> { setFile, setStartLine, setEndLine, setFile, setStartLine };
						break;
					case GotoType.Column:
						regexPattern = @"^(?:(?:((?:[a-z]:)?[\w\s,\\.-]+):)?(\d+):)?(\d+)(?:-(?:(\d+):)?(\d+))?$";
						actions = new List<Action<GotoRange, string>> { setFile, setStartLine, setStartColumn, setEndLine, setEndColumn };
						break;
					case GotoType.Index:
						regexPattern = @"^(?:(?:((?:[a-z]:)?[\w\s,\\.-]+):)?(\d+):)?(\d+)(?:-(?:(\d+):)?(\d+))?|((?:[a-z]:)?[\w\s,\\.-]+)\((\d+),(\d+),(\d+),(\d+)\)$";
						actions = new List<Action<GotoRange, string>> { setFile, setStartLine, setStartIndex, setEndLine, setEndIndex, setFile, setStartLine, setStartIndex, setEndLine, setEndIndex };
						break;
					case GotoType.Position:
						regexPattern = @"^(?:((?:[a-z]:)?[\w\s,\\.-]+):)?(\d+)(?:-(\d+))?$";
						actions = new List<Action<GotoRange, string>> { setFile, setStartPosition, setEndPosition };
						break;
					default: throw new Exception("Invalid gototype");
				}
				var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

				var result = new List<GotoRange>();
				foreach (var str in strs)
				{
					var pd = new GotoRange();
					var match = regex.Match(str);
					if (!match.Success)
						throw new Exception($"Invalid location: {str} ({gotoType})");
					for (var ctr = 0; ctr < actions.Count; ++ctr)
						if (match.Groups[ctr + 1].Success)
							actions[ctr](pd, match.Groups[ctr + 1].Value);
					result.Add(pd);
				}

				return result;
			}

			public Range GetRange(NEFile te, Range range, bool selecting)
			{
				var start = Start.GetPosition(te, range.Cursor);
				var end = End.GetPosition(te, range.Cursor, Start);
				if (end.HasValue)
					return new Range(end.Value, start.Value);
				if (selecting)
					return new Range(start.Value, range.Anchor);
				return Range.FromIndex(start.Value, 0);
			}
		}

		static Configuration_Position_Goto_Various Configure_Position_Goto_Various(EditorExecuteState state, GotoType gotoType)
		{
			int line = 1, column = 1, index = 1, position = 0;
			var range = state.NEFiles.Focused.Selections.FirstOrDefault();
			if (range != null)
			{
				line = state.NEFiles.Focused.Text.GetPositionLine(range.Start) + 1;
				index = state.NEFiles.Focused.Text.GetPositionIndex(range.Start, line - 1) + 1;
				column = state.NEFiles.Focused.Text.GetColumnFromIndex(line - 1, index - 1) + 1;
				position = range.Start;
			}
			int startValue;
			switch (gotoType)
			{
				case GotoType.Line: startValue = state.NEFiles.Focused.Text.GetDiffLine(line); break;
				case GotoType.Column: startValue = column; break;
				case GotoType.Index: startValue = index; break;
				case GotoType.Position: startValue = position; break;
				default: throw new ArgumentException("GotoType invalid");
			}
			return state.NEFiles.FilesWindow.RunDialog_Configure_Position_Goto_Various(gotoType, startValue, state.NEFiles.Focused.GetVariables());
		}

		void Execute_Position_Goto_Various(GotoType gotoType, bool selecting)
		{
			var result = state.Configuration as Configuration_Position_Goto_Various;
			var values = GotoRange.GetPositionsData(GetExpressionResults<string>(result.Expression), gotoType);
			if (!values.Any())
				return;

			var hasFiles = values.First().File != null;
			if (values.Any(x => (x.File != null) != hasFiles))
				throw new Exception("Either all locations must have files or none");

			var valuesByFile = new List<List<GotoRange>>();
			var fileMap = new Dictionary<string, List<GotoRange>>(StringComparer.OrdinalIgnoreCase);
			foreach (var value in values)
			{
				List<GotoRange> list;
				if (result.OpenFilesOnce)
				{
					var key = value.File ?? "";
					if (!fileMap.ContainsKey(key))
					{
						fileMap[key] = list = new List<GotoRange>();
						valuesByFile.Add(list);
					}
					else
						list = fileMap[key];
				}
				else
				{
					list = new List<GotoRange>();
					valuesByFile.Add(list);
				}
				list.Add(value);
			}

			if (hasFiles)
			{
				var invalidFiles = valuesByFile.Select(list => list.First().File).NonNull().Where(file => !File.Exists(file)).ToList();
				if (invalidFiles.Any())
					throw new Exception($"The following files could not be found: {string.Join("\n", invalidFiles)}");
			}

			foreach (var list in valuesByFile)
			{
				var useTE = this;
				var useFile = list.First().File;
				if (useFile != null)
				{
					useTE = new NEFile(useFile);
					NEFiles.AddFile(useTE);
				}

				var sels = useTE.Selections.ToList();
				var positions = list;

				if ((sels.Count == 0) && ((gotoType == GotoType.Line) || (gotoType == GotoType.Position)))
					sels.Add(new Range());
				if (sels.Count == 1)
					sels = Enumerable.Repeat(sels[0], positions.Count).ToList();
				if (positions.Count == 1)
					positions = Enumerable.Repeat(positions[0], sels.Count).ToList();
				if (positions.Count != sels.Count)
					throw new Exception("Expression count doesn't match selection count");

				useTE.Selections = sels.AsTaskRunner().Select((range, ctr) => positions[ctr].GetRange(useTE, range, selecting)).ToList();
			}
		}

		void Execute_Position_Copy_Various(GotoType gotoType, bool withLine)
		{
			var starts = new Dictionary<GotoType, List<int>>();
			var ends = new Dictionary<GotoType, List<int>>();

			var count = Selections.Count;
			starts[GotoType.Position] = Selections.Select(range => range.Start).ToList();
			ends[GotoType.Position] = Selections.Select(range => range.End).ToList();

			if ((gotoType == GotoType.Line) || (gotoType == GotoType.Column) || (gotoType == GotoType.Index))
			{
				starts[GotoType.Line] = starts[GotoType.Position].Select(pos => Text.GetPositionLine(pos)).ToList();
				ends[GotoType.Line] = ends[GotoType.Position].Select(pos => Text.GetPositionLine(pos)).ToList();
			}

			if ((gotoType == GotoType.Column) || (gotoType == GotoType.Index))
			{
				starts[GotoType.Index] = Enumerable.Range(0, count).Select(x => Text.GetPositionIndex(starts[GotoType.Position][x], starts[GotoType.Line][x])).ToList();
				ends[GotoType.Index] = Enumerable.Range(0, count).Select(x => Text.GetPositionIndex(ends[GotoType.Position][x], ends[GotoType.Line][x])).ToList();

				if (gotoType == GotoType.Column)
				{
					starts[GotoType.Column] = Enumerable.Range(0, count).Select(x => Text.GetColumnFromIndex(starts[GotoType.Line][x], starts[GotoType.Index][x])).ToList();
					ends[GotoType.Column] = Enumerable.Range(0, count).Select(x => Text.GetColumnFromIndex(ends[GotoType.Line][x], ends[GotoType.Index][x])).ToList();
				}
			}

			var delta = gotoType == GotoType.Position ? 0 : 1;
			Clipboard = Enumerable.Range(0, count).Select(index => $"{(withLine ? $"{starts[GotoType.Line][index] + 1}:" : "")}{starts[gotoType][index] + delta}{(starts[GotoType.Position][index] != ends[GotoType.Position][index] ? $"-{((withLine) && (starts[GotoType.Line][index] != ends[GotoType.Line][index]) ? $"{ends[GotoType.Line][index] + 1}:" : "")}{ends[gotoType][index] + delta}" : "")}").ToList();
		}
	}
}
