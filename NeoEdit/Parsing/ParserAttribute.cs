using System;

namespace NeoEdit.Program.Parsing
{
	public class ParserAttribute : ParserBase
	{
		public string Type { get; set; }
		public string Text { get; set; }

		ParserNode parent;
		public ParserNode Parent
		{
			get { return parent; }
			set
			{
				if (parent != null)
					throw new Exception("Parent already assigned");

				if (value == null)
					return;

				parent = value;
				parent.attributes.Add(this);
			}
		}
	}
}
