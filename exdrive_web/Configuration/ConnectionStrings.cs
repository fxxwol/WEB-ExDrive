namespace exdrive_web.Configuration
{
    public class ConnectionStrings
    {
        private static ConnectionStrings _instance = null;

        private static Object _mutex = new Object();

        private static string sqlConnection = string.Empty;
        private static string storageConnection = string.Empty;
        private static string virusTotalToken = string.Empty;
        private ConnectionStrings(string _sqlConnectionString,
            string _storageConnectionString, string _virusTotalToken)
        {
            sqlConnection = _sqlConnectionString;
            storageConnection = _storageConnectionString;
            virusTotalToken = _virusTotalToken;
        }

        public static ConnectionStrings GetInstance(string sqlConnectionString,
            string storageConnectionString, string virusTotalToken)
        {
            if (_instance == null)
            {
                lock (_mutex)
                {
                    if (_instance == null)
                    {
                        _instance = new ConnectionStrings(sqlConnectionString, storageConnectionString, virusTotalToken);
                    }
                }
            }

            return _instance;
        }
        public static string GetSqlConnectionString()
        {
            return sqlConnection;
        }
        public static string GetStorageConnectionString()
        {
            return storageConnection;
        }
        public static string GetVirusTotalToken()
        {
            return virusTotalToken;
        }
    }
}
