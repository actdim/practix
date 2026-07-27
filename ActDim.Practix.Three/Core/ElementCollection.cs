using System;
using System.Linq;
using System.Collections.Generic;

namespace THREE.Core
{
    public class ElementCollection : List<IElement> // Collection<IElement>
    {
        // public void AddRange(IEnumerable<IElement> elements)
        // {
        // 	foreach (var element in elements)
        // 	{
        // 		Add(element);
        // 	}
        // }

        /// <summary>
        /// Add an element to this collection if it does not already exist.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public Guid AddIfNew(IElement element)
        {
            var existing = this.FirstOrDefault(item => item.Equals(element));
            if (existing == null)
            {
                Add(element);
                return element.Uuid;
            }
            else
            {
                return existing.Uuid;
            }
        }
    }
}
