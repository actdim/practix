using System;

namespace ActDim.Practix.Abstractions.Logging
{
	[AttributeUsage(AttributeTargets.Method)]
	public class ArgumentListFormattingAttribute: Attribute
	{
		private readonly string _template;

		public ArgumentListFormattingAttribute(string template)
		{
			_template = template;
		}

		public string Template
		{
			get
			{
				return _template;
			}
		}
	}


}
