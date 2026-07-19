using ActDim.AppRegistry.Domain.Core;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ActDim.AppRegistry.Service;

public class ProjectProvider : IProjectProvider
{
    public Project Project { get; }
    public string ConnectionString { get; }

    public ProjectProvider(IConfiguration configuration, Project project)
    {
        Project = project;

        var builder = new NpgsqlConnectionStringBuilder(
            configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured."));

        builder.SearchPath = $"{Constants.Schema},{project.Slug},public";
        ConnectionString = builder.ToString();
    }
}
