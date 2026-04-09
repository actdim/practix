using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActDim.Practix.Abstractions.DataAccess
{
	public class Sorting
	{
		public string Column { set; get; }
		public SortingDirection? Direction { set; get; }
	}
}
