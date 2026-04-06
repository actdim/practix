using ActDim.Practix.Abstractions.Json;
using ActDim.Practix.Abstractions.Mapping;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Practix.Abstractions.DataAccess.Generic
{
    /// <summary>
    /// IEntityFetcher
    /// </summary>
    /// <typeparam name="T">TEntity</typeparam>
	public interface IDbFetcher<T> // IDbDataFetcher
    {
        /// <summary>
        /// Map data row to object
        /// </summary>
        /// <param name="reader">data reader</param>
        /// <param name="mapper"></param>
        /// <param name="serializer">common mapper</param>
        /// <returns></returns>
        IList<T> Fetch(DbDataReader reader, IMapper mapper, IJsonSerializer serializer);
        /// <summary>
        /// Map data row to object
        /// </summary>
        /// <param name="reader">data reader</param>
        /// <param name="mapper"></param>
        /// <param name="serializer">common mapper</param>
        /// <returns></returns>
        Task<IList<T>> FetchAsync(DbDataReader reader, IMapper mapper, IJsonSerializer serializer);
        /// <summary>
        /// Map data row to object (with cancellation support)
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="mapper"></param>
        /// <param name="serializer"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IList<T>> FetchAsync(DbDataReader reader, IMapper mapper, IJsonSerializer serializer,
            CancellationToken cancellationToken);

    }
}