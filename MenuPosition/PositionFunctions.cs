using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NeoEdit.Common;
using NeoEdit.MenuPosition.Dialogs;

namespace NeoEdit.MenuPosition
{
	public static class PositionFunctions
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

				public int? GetPosition(ITextEditor te, int offset, GotoLocation lastPosition = null)
				{
					if ((Line == null) && (Index == null) && (Column == null) && (Position == null))
						return null;

					if (Position.HasValue)
						return Math.Max(te.BeginOffset, Math.Min(Position.Value, te.EndOffset));

					var line = Math.Max(0, Math.Min(te.Data.GetNonDiffLine(Line ?? lastPosition?.Line ?? te.Data.GetOffsetLine(offset)), te.Data.NumLines - 1));
					var index = Index ?? lastPosition?.Index;
					if (index.HasValue)
						index = Math.Max(0, Math.Min(index.Value, te.Data.GetLineLength(line)));
					else
					{
						var column = Column ?? lastPosition?.Column;
						if (column.HasValue)
							index = te.Data.GetIndexFromColumn(line, Math.Max(0, column.Value), true);
						else
							index = 0;
					}

					return Math.Max(te.BeginOffset, Math.Min(te.Data.GetOffset(line, index.Value), te.EndOffset));
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

			public Range GetRange(ITextEditor te, Range range, bool selecting)
			{
				var start = Start.GetPosition(te, range.Cursor);
				var end = End.GetPosition(te, range.Cursor, Start);
				if (end.HasValue)
					return new Range(start.Value, end.Value);
				if (selecting)
					return new Range(start.Value, range.Anchor);
				return Range.FromIndex(start.Value, 0);
			}
		}

		static public void Load() { } // Doesn't do anything except load the assembly

		static public PositionGotoDialog.Result Command_Position_Goto_Dialog(ITextEditor te, GotoType gotoType)
		{
			int line = 1, column = 1, index = 1, position = 0;
			var range = te.Selections.FirstOrDefault();
			if (range != null)
			{
				line = te.Data.GetOffsetLine(range.Start) + 1;
				index = te.Data.GetOffsetIndex(range.Start, line - 1) + 1;
				column = te.Data.GetColumnFromIndex(line - 1, index - 1) + 1;
				position = range.Start;
			}
			int startValue;
			switch (gotoType)
			{
				case GotoType.Line: startValue = te.Data.GetDiffLine(line - 1) + 1; break;
				case GotoType.Column: startValue = column; break;
				case GotoType.Index: startValue = index; break;
				case GotoType.Position: startValue = position; break;
				default: throw new ArgumentException("GotoType invalid");
			}
			return PositionGotoDialog.Run(te.WindowParent, gotoType, startValue, te.GetVariables());
		}

		static public void Command_Position_Goto(ITextEditor te, GotoType gotoType, bool selecting, PositionGotoDialog.Result result)
		{
			var values = GotoRange.GetPositionsData(te.GetVariableExpressionResults<string>(result.Expression), gotoType);
			if (!values.Any())
				return;

			var hasFiles = values.First().File != null;
			if (values.Any(x => (x.File != null) != hasFiles))
				throw new Exception("Either all locations must have files or none");

			var valuesByFile = values.GroupBy(value => value.File, StringComparer.OrdinalIgnoreCase).Select(g => Tuple.Create(g.Key, g.ToList())).ToList();

			if (hasFiles)
			{
				var invalidFiles = valuesByFile.Select(tuple => tuple.Item1).NonNull().Where(file => !File.Exists(file)).ToList();
				if (invalidFiles.Any())
					throw new Exception($"The following files could not be found: {string.Join("\n", invalidFiles)}");
			}

			var active = new HashSet<ITextEditor>();
			foreach (var tuple in valuesByFile)
			{
				var useTE = tuple.Item1 == null ? te : te.TabsParent.Add(tuple.Item1);
				active.Add(useTE);

				var sels = useTE.Selections.ToList();
				var positions = tuple.Item2;

				if ((sels.Count == 0) && ((gotoType == GotoType.Line) || (gotoType == GotoType.Position)))
					sels.Add(useTE.BeginRange);
				if (sels.Count == 1)
					sels = sels.Resize(positions.Count, sels[0]).ToList();
				if (positions.Count == 1)
					positions = positions.Expand(sels.Count, positions[0]).ToList();
				if (positions.Count != sels.Count)
					throw new Exception("Expression count doesn't match selection count");

				useTE.SetSelections(sels.AsParallel().AsOrdered().Select((range, ctr) => positions[ctr].GetRange(useTE, range, selecting)).ToList());
			}

			if (hasFiles)
			{
				foreach (var item in te.TabsParent.Items)
					item.Active = active.Contains(item);
				te.TabsParent.TopMost = active.First();
			}
		}

		static public void Command_Position_Copy(ITextEditor te, GotoType gotoType, bool withLine)
		{
			var starts = new Dictionary<GotoType, List<int>>();
			var ends = new Dictionary<GotoType, List<int>>();

			var count = te.Selections.Count;
			starts[GotoType.Position] = te.Selections.Select(range => range.Start).ToList();
			ends[GotoType.Position] = te.Selections.Select(range => range.End).ToList();

			if ((gotoType == GotoType.Line) || (gotoType == GotoType.Column) || (gotoType == GotoType.Index))
			{
				starts[GotoType.Line] = starts[GotoType.Position].Select(pos => te.Data.GetOffsetLine(pos)).ToList();
				ends[GotoType.Line] = ends[GotoType.Position].Select(pos => te.Data.GetOffsetLine(pos)).ToList();
			}

			if ((gotoType == GotoType.Column) || (gotoType == GotoType.Index))
			{
				starts[GotoType.Index] = Enumerable.Range(0, count).Select(x => te.Data.GetOffsetIndex(starts[GotoType.Position][x], starts[GotoType.Line][x])).ToList();
				ends[GotoType.Index] = Enumerable.Range(0, count).Select(x => te.Data.GetOffsetIndex(ends[GotoType.Position][x], ends[GotoType.Line][x])).ToList();

				if (gotoType == GotoType.Column)
				{
					starts[GotoType.Column] = Enumerable.Range(0, count).Select(x => te.Data.GetColumnFromIndex(starts[GotoType.Line][x], starts[GotoType.Index][x])).ToList();
					ends[GotoType.Column] = Enumerable.Range(0, count).Select(x => te.Data.GetColumnFromIndex(ends[GotoType.Line][x], ends[GotoType.Index][x])).ToList();
				}
			}

			var delta = gotoType == GotoType.Position ? 0 : 1;
			te.SetClipboardStrings(Enumerable.Range(0, count).Select(index => $"{(withLine ? $"{starts[GotoType.Line][index] + 1}:" : "")}{starts[gotoType][index] + delta}{(starts[GotoType.Position][index] != ends[GotoType.Position][index] ? $"-{((withLine) && (starts[GotoType.Line][index] != ends[GotoType.Line][index]) ? $"{ends[GotoType.Line][index] + 1}:" : "")}{ends[gotoType][index] + delta}" : "")}"));
		}
	}
}
