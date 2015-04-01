using System;
using System.Collections.Generic;
using NeoEdit.Common;
using NeoEdit.Common.Transform;
using NeoEdit.HexEdit.Data;

namespace NeoEdit.HexEdit.Models
{
	class ModelDataExtractor
	{
		readonly BinaryData data;
		readonly ModelData modelData;
		Dictionary<string, object> values;
		List<object> results;

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

		void AlignCurrent(int alignmentBits)
		{
			if (alignmentBits < 8)
				currentBit += alignmentBits - 1 - (currentBit - 1) % alignmentBits;
			else
			{
				if (currentBit != 0)
					++currentByte;

				var alignmentBytes = alignmentBits / 8;
				currentByte += alignmentBytes - 1 - (currentByte - 1) % alignmentBytes;
			}
		}

		byte[] GetBytes(long position, long count, long start, long end)
		{
			if ((position < 0) || (position < start) || (position + count > end) || (position + count > data.Length))
				return null;
			return data.GetSubset(position, count);
		}

		Dictionary<string, Expression> expressions = new Dictionary<string, Expression>();
		bool HandleModel(string guid)
		{
			var model = modelData.GetModel(guid);
			foreach (var action in model.Actions)
			{
				if (!expressions.ContainsKey(action.Location))
					expressions[action.Location] = new Expression(action.Location, values.Keys);
				current = expressions[action.Location].EvaluateDict(values, 0).ToString();

				var end = long.MaxValue;
				if (!expressions.ContainsKey(action.Repeat))
					expressions[action.Repeat] = new Expression(action.Repeat, values.Keys);
				var repeat = Convert.ToInt64(expressions[action.Repeat].EvaluateDict(values, 0));

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

					AlignCurrent(action.AlignmentBits);

					if ((action.Type != ModelAction.ActionType.Bit) && (currentBit != 0))
						throw new Exception("Can only handle byte-aligned data");

					string result = null;
					switch (action.Type)
					{
						case ModelAction.ActionType.Bit:
							{
								var bit = currentBit;
								var bytes = GetBytes(currentByte, 1, start, end);
								++currentBit;
								if (bytes == null)
									return false;
								result = (bytes[0] & (1 << (7 - bit))) == 0 ? "0" : "1";
							}
							break;
						case ModelAction.ActionType.BasicType:
							{
								var codePage = action.CodePage;
								var count = Coder.BytesRequired(codePage);
								var bytes = GetBytes(currentByte, count, start, end);
								currentByte += count;
								if (bytes == null)
									return false;
								result = Coder.BytesToString(bytes, codePage);
							}
							break;
						case ModelAction.ActionType.String:
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
										result = Coder.BytesToString(bytes, action.Encoding);
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
										result = Coder.BytesToString(resultBytes, action.Encoding);
									}
									break;
								case ModelAction.ActionStringType.StringFixedWidth:
									{
										if (!expressions.ContainsKey(action.FixedWidth))
											expressions[action.FixedWidth] = new Expression(action.FixedWidth, values.Keys);
										var fixedLength = Convert.ToInt64(expressions[action.FixedWidth].EvaluateDict(values, 0));

										var bytes = GetBytes(currentByte, fixedLength, start, end);
										currentByte += fixedLength;
										if (bytes == null)
											return false;
										result = Coder.BytesToString(bytes, action.Encoding);
									}
									break;
							}
							break;
						case ModelAction.ActionType.Unused:
							{
								if (!expressions.ContainsKey(action.FixedWidth))
									expressions[action.FixedWidth] = new Expression(action.FixedWidth, values.Keys);
								var fixedLength = Convert.ToInt64(expressions[action.FixedWidth].EvaluateDict(values, 0));
								currentByte += fixedLength;
							}
							break;
						case ModelAction.ActionType.Model:
							if (!HandleModel(action.Model))
								return false;
							break;
					}

					if (result != null)
					{
						if (action.SaveName != null)
							values[action.SaveName] = result;
						results.Add(result);
					}
				}
			}

			return true;
		}

		public ModelDataExtractor(BinaryData data, long start, long current, long end, ModelData modelData, string model)
		{
			this.data = data;
			values = new Dictionary<string, object>();
			this.start = start;
			this.end = end;
			currentByte = current;
			results = new List<object>();

			this.modelData = modelData;
			HandleModel(model);
		}
	}
}
