namespace ExDrive.Configuration
{
    public class ConnectionStrings
    {
        private static ConnectionStrings? _instance = null;

        private static Object _mutex = new Object();

        private static string sqlConnection = string.Empty;
        private static string storageConnection = string.Empty;
        private static string virusTotalToken = string.Empty;
        private static string sendGridKey = string.Empty;
        private ConnectionStrings(
            string _sqlConnectionString,
            string _storageConnectionString, 
            string _virusTotalToken,
            string _sendGridKey)
        {
            sqlConnection = _sqlConnectionString;
            storageConnection = _storageConnectionString;
            virusTotalToken = _virusTotalToken;
            sendGridKey = _sendGridKey;
        }

        public static ConnectionStrings GetInstance(
            string sqlConnectionString,
            string storageConnectionString,
            string virusTotalToken,
            string sendGridKey)
        {
            if (_instance == null)
            {
                lock (_mutex)
                {
                    if (_instance == null)
                    {
                        _instance = new ConnectionStrings(sqlConnectionString, storageConnectionString,
                                                            virusTotalToken, sendGridKey);
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

        public static string GetSendGridKey()
        {
            return sendGridKey;
        }
    }
}
