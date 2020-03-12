using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NeoEdit.Program.Dialogs;

namespace NeoEdit.Program
{
	partial class TextEditor
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

				public int? GetPosition(TextEditorWindow te, int position, GotoLocation lastPosition = null)
				{
					// TODO
					return default;

					//if ((Line == null) && (Index == null) && (Column == null) && (Position == null))
					//	return null;

					//if (Position.HasValue)
					//	return Math.Max(0, Math.Min(Position.Value, te.TextView.MaxPosition));

					//var line = Math.Max(0, Math.Min(te.Data.GetNonDiffLine(Line ?? lastPosition?.Line ?? te.TextView.GetPositionLine(position)), te.TextView.NumLines - 1));
					//var index = Index ?? lastPosition?.Index;
					//if (index.HasValue)
					//	index = Math.Max(0, Math.Min(index.Value, te.TextView.GetLineLength(line)));
					//else
					//{
					//	var column = Column ?? lastPosition?.Column;
					//	if (column.HasValue)
					//		index = te.Data.GetIndexFromColumn(line, Math.Max(0, column.Value), true);
					//	else
					//		index = 0;
					//}

					//return Math.Max(0, Math.Min(te.TextView.GetPosition(line, index.Value), te.TextView.MaxPosition));
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

			public Range GetRange(TextEditorWindow te, Range range, bool selecting)
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

		PositionGotoDialog.Result Command_Position_Goto_Dialog(GotoType gotoType)
		{
			int line = 1, column = 1, index = 1, position = 0;
			var range = Selections.FirstOrDefault();
			if (range != null)
			{
				line = TextView.GetPositionLine(range.Start) + 1;
				index = TextView.GetPositionIndex(range.Start, line - 1) + 1;
				column = GetColumnFromIndex(line - 1, index - 1) + 1;
				position = range.Start;
			}
			int startValue;
			switch (gotoType)
			{
				case GotoType.Line: startValue = line; break;
				case GotoType.Column: startValue = column; break;
				case GotoType.Index: startValue = index; break;
				case GotoType.Position: startValue = position; break;
				default: throw new ArgumentException("GotoType invalid");
			}
			return PositionGotoDialog.Run(state.TabsWindow, gotoType, startValue, GetVariables());
		}

		void Command_Position_Goto(GotoType gotoType, bool selecting, PositionGotoDialog.Result result)
		{
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

			// TODO
			//foreach (var list in valuesByFile)
			//{
			//	var useTE = this;
			//	var useFile = list.First().File;
			//	if (useFile != null)
			//	{
			//		useTE = new TextEditor(useFile);
			//		TabsParent.AddTextEditor(useTE);
			//	}

			//	var sels = useTE.Selections.ToList();
			//	var positions = list;

			//	if ((sels.Count == 0) && ((gotoType == GotoType.Line) || (gotoType == GotoType.Position)))
			//		sels.Add(new Range());
			//	if (sels.Count == 1)
			//		sels = sels.Resize(positions.Count, sels[0]).ToList();
			//	if (positions.Count == 1)
			//		positions = positions.Expand(sels.Count, positions[0]).ToList();
			//	if (positions.Count != sels.Count)
			//		throw new Exception("Expression count doesn't match selection count");

			//	useTE.Selections = sels.AsParallel().AsOrdered().Select((range, ctr) => positions[ctr].GetRange(useTE, range, selecting)).ToList();
			//}
		}

		void Command_Position_Copy(GotoType gotoType, bool withLine)
		{
			var starts = new Dictionary<GotoType, List<int>>();
			var ends = new Dictionary<GotoType, List<int>>();

			var count = Selections.Count;
			starts[GotoType.Position] = Selections.Select(range => range.Start).ToList();
			ends[GotoType.Position] = Selections.Select(range => range.End).ToList();

			if ((gotoType == GotoType.Line) || (gotoType == GotoType.Column) || (gotoType == GotoType.Index))
			{
				starts[GotoType.Line] = starts[GotoType.Position].Select(pos => TextView.GetPositionLine(pos)).ToList();
				ends[GotoType.Line] = ends[GotoType.Position].Select(pos => TextView.GetPositionLine(pos)).ToList();
			}

			if ((gotoType == GotoType.Column) || (gotoType == GotoType.Index))
			{
				starts[GotoType.Index] = Enumerable.Range(0, count).Select(x => TextView.GetPositionIndex(starts[GotoType.Position][x], starts[GotoType.Line][x])).ToList();
				ends[GotoType.Index] = Enumerable.Range(0, count).Select(x => TextView.GetPositionIndex(ends[GotoType.Position][x], ends[GotoType.Line][x])).ToList();

				if (gotoType == GotoType.Column)
				{
					starts[GotoType.Column] = Enumerable.Range(0, count).Select(x => GetColumnFromIndex(starts[GotoType.Line][x], starts[GotoType.Index][x])).ToList();
					ends[GotoType.Column] = Enumerable.Range(0, count).Select(x => GetColumnFromIndex(ends[GotoType.Line][x], ends[GotoType.Index][x])).ToList();
				}
			}

			var delta = gotoType == GotoType.Position ? 0 : 1;
			Clipboard = Enumerable.Range(0, count).Select(index => $"{(withLine ? $"{starts[GotoType.Line][index] + 1}:" : "")}{starts[gotoType][index] + delta}{(starts[GotoType.Position][index] != ends[GotoType.Position][index] ? $"-{((withLine) && (starts[GotoType.Line][index] != ends[GotoType.Line][index]) ? $"{ends[GotoType.Line][index] + 1}:" : "")}{ends[gotoType][index] + delta}" : "")}").ToList();
		}
	}
}
