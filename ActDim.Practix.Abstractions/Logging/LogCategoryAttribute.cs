using System;

namespace ActDim.Practix.Abstractions.Logging
{
	// [AttributeUsage(AttributeTargets.Class)]
	public class LogCategoryAttribute : Attribute
	{
		private readonly string _name;

		public LogCategoryAttribute(string name)
		{
			_name = name;
		}

		public string Name
		{
			get
			{
				return _name;
			}
		}
	}


}
