using System;

namespace ActDim.Practix.DataAccess.Attributes
{
	[AttributeUsage(AttributeTargets.Property)]
	public class SequenceNameAttribute : Attribute
	{
		public string Name { get; }

		public SequenceNameAttribute(string name)
		{
			Name = name;
		}
	}
}