using System.ComponentModel.DataAnnotations.Schema;

namespace ActDim.AppRegistry.Domain.Core
{
    /// <summary>
    /// LibraryFolder
    /// </summary>
    public class LibraryFolder : IEntity<Guid>
    {
        [Column("folder_id")]
        public Guid Id { get; set; }

        public string Name { get; set; }
        
        public string Slug { get; set; }
        
        public DateTimeOffset CreatedAt { get; set; }
        
        public DateTimeOffset UpdatedAt { get; set; }

        /// <summary>
        /// TreePath
        /// </summary>
        public string Path { get; set; }

        public string EntityTypeCode { get; set; }
    }
}
