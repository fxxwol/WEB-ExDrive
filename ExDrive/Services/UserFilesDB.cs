using ExDrive.Configuration;
using Microsoft.Data.SqlClient;

namespace ExDrive.Models
{
    // Needs refactoring
    public class UserFilesDB
    {
        public List<UserFile> GetUserFilesDB(string _userId)
        {
            string name;
            string noformat;

            List<UserFile> files = new List<UserFile>();
            using (SqlConnection con = new SqlConnection(ConnectionStrings.GetSqlConnectionString()))
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand($"SELECT Name, FilesId, Favourite FROM dbo.Files WHERE HasAccess='{_userId}' " +
                                                       $"AND IsTemporary='0' ORDER BY FilesId ASC", con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            noformat = "";
                            name = (string)reader["Name"];
                            for (int i = 0; i < name.LastIndexOf('.'); i++)
                            {
                                noformat += name.ElementAt(i);
                            }

                            files.Add(new UserFile(name, noformat, (string)reader["FilesId"], (bool)reader["Favourite"]));
                        }
                    }
                }
            }

            return files;
        }
    }
}
