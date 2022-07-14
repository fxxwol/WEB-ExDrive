using ExDrive.Configuration;
using ExDrive.Helpers;

using Microsoft.Data.SqlClient;

namespace ExDrive.Models
{
    public class UserFilesDB
    {
        public async Task<List<UserFile>> GetUserFilesDBAsync(string _userId)
        {
            string name;

            var files = new List<UserFile>();

            using (var sqlConnection = new SqlConnection(ConnectionStrings.GetSqlConnectionString()))
            {
                await sqlConnection.OpenAsync();

                using (var sqlCommand = new SqlCommand($"SELECT Name, FilesId, Favourite FROM dbo.Files WHERE HasAccess='{_userId}' " +
                                                       $"AND IsTemporary='0' ORDER BY FilesId ASC", sqlConnection))
                {
                    using (var reader = await sqlCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            name = (string)reader["Name"];

                            files.Add(new UserFile(name, new FindNameWithoutFormat().FindWithoutFormat(name),
                                (string)reader["FilesId"], (bool)reader["Favourite"]));
                        }
                    }
                }
            }

            return files;
        }
    }
}
