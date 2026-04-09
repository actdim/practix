using System.Threading.Tasks;

namespace ActDim.Practix.Config
{
    public interface IJsonConfigurationManager
    {
        Task<T> LoadAsync<T>(string path) where T : class, new();
        Task SaveAsync<T>(T options, string path) where T : class;
    }
}
