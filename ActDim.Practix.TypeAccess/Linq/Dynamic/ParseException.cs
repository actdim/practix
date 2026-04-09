using System;

namespace ActDim.Practix.TypeAccess.Linq.Dynamic
{
	public sealed class ParseException : Exception
	{
		readonly int _position;

		public ParseException(string message, int position)
			: base(message)
		{
			this._position = position;
		}

		public int Position
		{
			get { return _position; }
		}

		public override string ToString()
		{
			return string.Format(Res.ParseExceptionFormat, Message, _position);
		}
	}
}
