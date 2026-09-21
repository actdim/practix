using RepoDb.Interfaces;

namespace ActDim.Practix.RepoDb.Sql
{
    /// <summary>
    /// Lightweight <see cref="IDbSetting"/> implementation resolving quotation characters for a <see cref="SqlDialect"/>
    /// without requiring external database provider packages or driver dependencies.
    /// </summary>
    public sealed class DialectDbSetting : IDbSetting
    {
        /// <inheritdoc />
        public string OpeningQuote { get; }

        /// <inheritdoc />
        public string ClosingQuote { get; }

        /// <inheritdoc />
        public string ParameterPrefix => "@";

        /// <inheritdoc />
        public string SchemaSeparator => ".";

        /// <inheritdoc />
        public string? DefaultSchema => null;

        /// <inheritdoc />
        public System.Type AverageableType => typeof(double);

        /// <inheritdoc />
        public bool AreTableHintsSupported => false;

        /// <inheritdoc />
        public int DefaultAverageRowSizeInBytes => 10;

        /// <inheritdoc />
        public bool IsDirectionSupported => false;

        /// <inheritdoc />
        public bool IsExecuteReaderDisposable => true;

        /// <inheritdoc />
        public bool IsMultiStatementExecutable => true;

        /// <inheritdoc />
        public bool IsPreparable => false;

        /// <inheritdoc />
        public bool IsUseUpsert => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="DialectDbSetting"/> class.
        /// </summary>
        /// <param name="openingQuote">The opening quote character.</param>
        /// <param name="closingQuote">The closing quote character.</param>
        public DialectDbSetting(string openingQuote, string closingQuote)
        {
            OpeningQuote = openingQuote;
            ClosingQuote = closingQuote;
        }

        /// <summary>
        /// Resolves an <see cref="IDbSetting"/> for the specified <see cref="SqlDialect"/>.
        /// </summary>
        /// <param name="dialect">The target SQL dialect.</param>
        /// <returns>An <see cref="IDbSetting"/> configured with the dialect's quotation characters.</returns>
        public static IDbSetting For(SqlDialect dialect)
        {
            return dialect switch
            {
                SqlDialect.PostgreSql => new DialectDbSetting("\"", "\""),
                SqlDialect.MySql => new DialectDbSetting("`", "`"),
                SqlDialect.Sqlite or SqlDialect.SqlServer or SqlDialect.Standard => new DialectDbSetting("[", "]"),
                _ => new DialectDbSetting("[", "]")
            };
        }
    }
}
