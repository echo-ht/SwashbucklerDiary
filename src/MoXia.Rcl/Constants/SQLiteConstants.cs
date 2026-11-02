using Microsoft.Data.Sqlite;

namespace MoXia.Rcl
{
    public class SQLiteConstants
    {
        public const string MainDatabaseFilename = "MoXia.db3";

        public const string PrivacyDatabaseFilename = "MoXia.privacy.db3";

        public const string LogDatabaseFilename = "logs.db3";

        protected static string GetConnectionString(string dataSource)
        {
            return new SqliteConnectionStringBuilder()
            {
                DataSource = dataSource,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared

            }.ToString();
        }
    }
}
