using System;
using System.Collections.Generic;
using System.Linq;
using FastMember;
using OrthoBits.Abstractions.DataAccess;

namespace OrthoBits.DataAccess.Extensions // OrthoBits.DataAccess.Linq
{
	public static class ChangedEntityExtensions
	{
		public static IEnumerable<string> GetEntityFields<TEntity>(this TEntity entity,
			IEnumerable<string> except = null)
			where TEntity : class
		{
			var accessor = TypeAccessor.Create(typeof(TEntity));
			var members = accessor.GetMembers().Where(x => x.CanRead && x.CanWrite);
			if (except != null)
			{
				var set = new HashSet<string>(except);
				members = members.Where(x => !set.Contains(x.Name));
			}
			return members.Select(x => x.Name);
		}

		public static IDictionary<string, object> GetAllProperties<TEntity>(this TEntity entity)
		{
			var res = new Dictionary<string, object>();
			foreach (var prop in entity.GetType().GetProperties())
			{
				res.Add(prop.Name, prop.GetValue(entity));
			}
			return res;
		}

		/// <summary>
		/// Converts entity to ChangeSet.
		/// Include all properties of entity (if <see cref="includeOnly"/> is not set) 
		/// </summary>
		/// <typeparam name="TEntity"></typeparam>
		/// <param name="entity">source entity</param>
		/// <param name="except">all except this properties</param>
		/// <returns></returns>
		public static ChangedEntity<TEntity> ToChangedEntity<TEntity>(this TEntity entity,
			params string[] except)
			where TEntity : class
		{
			var accessor = TypeAccessor.Create(typeof(TEntity));
			var members = accessor.GetMembers().Where(x => x.CanRead && x.CanWrite);
			if (except != null && except.Length > 0)
			{
				var set = new HashSet<string>(except);
				members = members.Where(x => !set.Contains(x.Name));
			}
			var modifiedFields = members.Select(x => x.Name).ToList();
			return new ChangedEntity<TEntity>()
			{
				Entity = entity, 
				ModifiedFields = modifiedFields
			};
		}

        /// <summary>
        /// CopyChanges
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>        
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static TEntity ApplyChanges<TEntity>(this ChangedEntity<TEntity> from, TEntity to)
            where TEntity : class
        {
            var accessor = TypeAccessor.Create(typeof(TEntity));
            foreach(var f in from.ModifiedFields)
            {
                accessor[to, f] = accessor[from.Entity, f];
            }
            return to;
        }
    }
}