using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ActDim.AppRegistry.Domain.Core
{
    public class Org : IEntity<Guid>
    {
        [Column("org_id")]
        public Guid Id { get; set; }
        
        public string Name { get; set; }
        
        public string Slug { get; set; }
        
        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public string Description { get; set; }

        public JsonDocument Metadata { get; set; }

        public string EntityTypeCode
        {
            get => Core.EntityTypeCode.Org;
            set
            {
                if (value != Core.EntityTypeCode.Org)
                    throw new ArgumentException($"Invalid value: {value}", nameof(value));
            }
        }
    }
}
