namespace NeoEdit.Editor
{
	public class CacheValue
	{
		NEText value;
		bool invalid = true;

		public bool Match(NEText value)
		{
			if (invalid)
				return false;

			if (ReferenceEquals(this.value, value))
				return true;
			if (!this.value.Equals(value))
				return false;
			this.value = value;
			return true;
		}

		public void SetValue(NEText value)
		{
			this.value = value;
			invalid = false;
		}

		public void Invalidate()
		{
			value = null;
			invalid = true;
		}
	}
}
