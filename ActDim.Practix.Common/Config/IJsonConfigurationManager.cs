using System.Threading.Tasks;

namespace ActDim.Practix.Config
{
    /// <summary>
    /// Contract for loading and saving JSON-backed configuration file models.
    /// </summary>
    public interface IJsonConfigurationManager
    {
        /// <summary>
        /// Asynchronously loads configuration settings of type <typeparamref name="T"/> from the specified file path.
        /// </summary>
        /// <typeparam name="T">The configuration target class type.</typeparam>
        /// <param name="path">The configuration file path.</param>
        /// <returns>The deserialized configuration model instance.</returns>
        Task<T> LoadAsync<T>(string path) where T : class, new();

        /// <summary>
        /// Asynchronously serializes and saves configuration options to the specified file path.
        /// </summary>
        /// <typeparam name="T">The configuration options model type.</typeparam>
        /// <param name="options">The options model instance to persist.</param>
        /// <param name="path">The target file path.</param>
        Task SaveAsync<T>(T options, string path) where T : class;
    }
}
