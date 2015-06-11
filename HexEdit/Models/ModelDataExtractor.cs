using System;
using System.Collections.Generic;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Transform;
using NeoEdit.HexEdit.Data;

namespace NeoEdit.HexEdit.Models
{
	class ModelDataExtractor
	{
		readonly BinaryData data;
		readonly ModelData modelData;
		Dictionary<string, object> values = new Dictionary<string, object>();
		List<ModelResult> results = new List<ModelResult>();

		string current
		{
			get { return (string)values["Current"]; }
			set
			{
				if (value == null)
				{
					value = currentByte.ToString();
					if (currentBit != 0)
						value += "." + currentBit.ToString();
					values["Current"] = value;
					return;
				}

				if (current == value)
					return;

				values["Current"] = value;
				var idx = current.IndexOf('.');
				if (idx == -1)
					currentByte = long.Parse(current);
				else
				{
					currentByte = long.Parse(current.Substring(0, idx));
					currentBit = int.Parse(current.Substring(idx + 1));
				}
			}
		}

		long currentByte
		{
			get { return (long)values["CurrentByte"]; }
			set { values["CurrentByte"] = value; currentBit = 0; current = null; }
		}

		int currentBit
		{
			get { return (int)values["CurrentBit"]; }
			set
			{
				while (value >= 8)
				{
					value -= 8;
					++currentByte;
				}
				values["CurrentBit"] = value;
				current = null;
			}
		}

		long start
		{
			get { return (long)values["Start"]; }
			set { values["Start"] = value; }
		}

		long end
		{
			get { return (long)values["End"]; }
			set { values["End"] = value; }
		}

		void AlignCurrent(ModelAction action)
		{
			if (action.AlignmentBits < 8)
				currentBit += action.AlignmentBits - 1 - (currentBit - 1) % action.AlignmentBits;
			else
			{
				if (currentBit != 0)
					++currentByte;

				var alignmentBytes = action.AlignmentBits / 8;
				currentByte += alignmentBytes - 1 - (currentByte - 1) % alignmentBytes;
			}

			if ((action.Type != ModelAction.ActionType.Bit) && (currentBit != 0))
				throw new Exception("All items (except for bits) must be byte-aligned");
		}

		byte[] GetBytes(long position, long count, long start, long end)
		{
			if ((position < 0) || (position < start) || (position + count > end) || (position + count > data.Length))
				return null;
			return data.GetSubset(position, count);
		}

		Dictionary<string, NEExpression> expressions = new Dictionary<string, NEExpression>();
		string EvalExpression(string expression)
		{
			if (!expressions.ContainsKey(expression))
				expressions[expression] = new NEExpression(expression);
			return expressions[expression].Evaluate(values).ToString();
		}

		Dictionary<string, int> nameCounts = new Dictionary<string, int>();
		void SaveResult(ModelAction action, string value, long startByte, int startBit, long endByte, int endBit)
		{
			if (String.IsNullOrEmpty(action.SaveName))
				return;
			values[action.SaveName] = value;
			if (!nameCounts.ContainsKey(action.SaveName))
				nameCounts[action.SaveName] = 0;
			var name = String.Format("{0} #{1}", action.SaveName, ++nameCounts[action.SaveName]);
			results.Add(new ModelResult(action, results.Count + 1, name, value, startByte, startBit, endByte, endBit));
		}

		bool HandleBit(ModelAction action)
		{
			var startByte = currentByte;
			var startBit = currentBit;
			var bit = currentBit;
			var bytes = GetBytes(currentByte, 1, start, end);
			++currentBit;
			if (bytes == null)
				return false;
			SaveResult(action, (bytes[0] & (1 << bit)) == 0 ? "0" : "1", startByte, startBit, currentByte, currentBit);
			return true;
		}

		bool HandleBasicType(ModelAction action)
		{
			var startByte = currentByte;
			var startBit = currentBit;
			var codePage = action.CodePage;
			var count = Coder.BytesRequired(codePage);
			var bytes = GetBytes(currentByte, count, start, end);
			currentByte += count;
			if (bytes == null)
				return false;
			SaveResult(action, Coder.BytesToString(bytes, codePage), startByte, startBit, currentByte, currentBit);
			return true;
		}

		bool HandleString(ModelAction action)
		{
			var startByte = currentByte;
			var startBit = currentBit;
			switch (action.StringType)
			{
				case ModelAction.ActionStringType.StringWithLength:
					{
						var count = Coder.BytesRequired(action.CodePage);
						var bytes = GetBytes(currentByte, count, start, end);
						currentByte += count;
						if (bytes == null)
							return false;
						var strLen = long.Parse(Coder.BytesToString(bytes, action.CodePage));
						bytes = GetBytes(currentByte, strLen, start, end);
						currentByte += strLen;
						if (bytes == null)
							return false;
						SaveResult(action, Coder.BytesToString(bytes, action.Encoding), startByte, startBit, currentByte, currentBit);
					}
					break;
				case ModelAction.ActionStringType.StringNullTerminated:
					{
						var nullBytes = Coder.StringToBytes("\0", action.Encoding);
						var resultBytes = new byte[0];
						while (true)
						{
							var bytes = GetBytes(currentByte, nullBytes.Length, start, end);
							currentByte += nullBytes.Length;
							if (bytes == null)
								return false;
							var found = true;
							for (var ctr = 0; ctr < nullBytes.Length; ++ctr)
								if (bytes[ctr] != nullBytes[ctr])
								{
									found = false;
									break;
								}
							if (found)
								break;
							Array.Resize(ref resultBytes, resultBytes.Length + bytes.Length);
							Array.Copy(bytes, 0, resultBytes, resultBytes.Length - bytes.Length, bytes.Length);
						}
						SaveResult(action, Coder.BytesToString(resultBytes, action.Encoding), startByte, startBit, currentByte, currentBit);
					}
					break;
				case ModelAction.ActionStringType.StringFixedWidth:
					{
						var fixedLength = Convert.ToInt64(EvalExpression(action.FixedWidth));

						var bytes = GetBytes(currentByte, fixedLength, start, end);
						currentByte += fixedLength;
						if (bytes == null)
							return false;
						SaveResult(action, Coder.BytesToString(bytes, action.Encoding), startByte, startBit, currentByte, currentBit);
					}
					break;
				default: throw new Exception("Invalid string type");
			}
			return true;
		}

		bool HandleUnused(ModelAction action)
		{
			var startByte = currentByte;
			var startBit = currentBit;
			var fixedLength = Convert.ToInt64(EvalExpression(action.FixedWidth));
			currentByte += fixedLength;
			return true;
		}

		bool SaveResults(ModelAction action)
		{
			switch (action.Type)
			{
				case ModelAction.ActionType.Bit: if (!HandleBit(action)) return false; break;
				case ModelAction.ActionType.BasicType: if (!HandleBasicType(action)) return false; break;
				case ModelAction.ActionType.String: if (!HandleString(action)) return false; break;
				case ModelAction.ActionType.Unused: if (!HandleUnused(action)) return false; break;
				case ModelAction.ActionType.Model: if (!HandleModel(action.Model)) return false; break;
			}
			return true;
		}

		bool HandleAction(ModelAction action)
		{
			current = EvalExpression(action.Location);
			var repeat = Convert.ToInt64(EvalExpression(action.Repeat));

			var end = long.MaxValue;
			if (action.RepeatType == ModelAction.ActionRepeatType.DataPosition)
			{
				end = repeat;
				repeat = long.MaxValue;
			}

			while (repeat > 0)
			{
				if (currentByte >= end)
					break;

				--repeat;

				AlignCurrent(action);

				if (!SaveResults(action))
					return false;
			}

			return true;
		}

		bool HandleModel(string guid)
		{
			var model = modelData.GetModel(guid);
			foreach (var action in model.Actions)
				if (!HandleAction(action))
					return false;

			return true;
		}

		ModelDataExtractor(BinaryData data, long start, long current, long end, ModelData modelData)
		{
			this.data = data;
			this.start = start;
			this.end = end;
			currentByte = current;
			this.modelData = modelData;
		}

		static public List<ModelResult> ExtractData(BinaryData data, long start, long current, long end, ModelData modelData, string model)
		{
			var extractor = new ModelDataExtractor(data, start, current, end, modelData);
			extractor.HandleModel(model);
			return extractor.results;
		}
	}
}
