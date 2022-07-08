using Microsoft.Data.SqlClient;

using ExDrive.Configuration;
using ExDrive.Models;
using ExDrive.Helpers;

namespace ExDrive.Services
{
    public class DeletedFiles
    {
        public static List<NameInstance> GetDeletedFilesDB(string _userId)
        {
            string fileName;
            string fileNameWithoutFormat;

            var deleted = new List<NameInstance>();

            using (SqlConnection sqlConnection = new SqlConnection(ConnectionStrings.GetSqlConnectionString()))
            {
                sqlConnection.Open();
                using (SqlCommand sqlCommand = new SqlCommand($"SELECT Name, FilesId, Favourite FROM dbo.Files WHERE HasAccess='{_userId}' AND IsTemporary=1 ORDER BY FilesId ASC", sqlConnection))
                {
                    using (SqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {

                            fileName = (string)reader["Name"];

                            var withoutFormat = new FindNameWithoutFormat();
                            fileNameWithoutFormat = withoutFormat.FindWithoutFormat(fileName);

                            deleted.Add(new NameInstance(fileName, fileNameWithoutFormat,
                                        (string)reader["FilesId"], (bool)reader["Favourite"]));
                        }
                    }
                }
            }

            return deleted;
        }
    }
}
