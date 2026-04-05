using OrthoBits.Abstractions.DataAccess;

namespace OrthoBits.DataAccess.Attributes
{
	public class TableAttribute : Attribute
	{
        /// <summary>
        /// Convension
        /// </summary>
        public DbProviderType ProviderType { get; }

		public string Name { get; }

        public TableAttribute(string name = null): this(DbProviderType.GenericSQL, name)
        {
            
        }

        public TableAttribute(DbProviderType providerType, string name = null)
		{
			ProviderType = providerType;
			Name = name;
		}
	}
}
