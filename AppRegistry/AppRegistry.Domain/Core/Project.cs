using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ActDim.AppRegistry.Domain.Core
{
    /// <summary>
    /// Profile/Schema
    /// </summary>
    public class Project : IEntity<Guid>
    {
        [Column("project_id")]
        public Guid Id { get; set; }
        
        public string Name { get; set; }
        
        public string Slug { get; set; }
        
        public DateTimeOffset CreatedAt { get; set; }
        
        public DateTimeOffset UpdatedAt { get; set; }

        public string Description { get; set; }

        public JsonDocument Metadata { get; set; }

        // OrgIds

        public string EntityTypeCode
        {
            get => Core.EntityTypeCode.Project;
            set
            {
                if (value != Core.EntityTypeCode.Project)
                    throw new ArgumentException($"Invalid value: {value}", nameof(value));
            }
        }
    }
}
