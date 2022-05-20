using Microsoft.Data.SqlClient;

namespace exdrive_web.Models
{
    public class UserFilesDB
    {
        public static List<NameInstance> GetUserFilesDB(string _userId)
        {
            string name;
            string noformat;

            List<NameInstance> files = new List<NameInstance>();
            using (SqlConnection con = new SqlConnection(ExFunctions.sqlConnectionString))
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand($"SELECT Name, FilesId, Favourite FROM dbo.Files WHERE HasAccess='{_userId}' AND IsTemporary='0' ORDER BY FilesId ASC", con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            noformat = "";
                            name = (string)reader["Name"];
                            for (int i = 0; i < name.LastIndexOf('.'); i++)
                                noformat += name.ElementAt(i);

                            files.Add(new NameInstance(name, noformat, (string)reader["FilesId"], (bool)reader["Favourite"]));
                        }
                    }
                }
            }

            return files;
        }
    }
}
