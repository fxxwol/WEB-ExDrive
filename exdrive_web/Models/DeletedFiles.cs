using Microsoft.Data.SqlClient;

namespace exdrive_web.Models
{
    public class DeletedFiles
    {
        public static List<NameInstance> GetDeletedFilesDB(string _userId)
        {
            string name;
            string noformat;
            List<NameInstance> deleted = new List<NameInstance>();
            using (SqlConnection con = new SqlConnection(ExFunctions.sqlConnectionString))
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand($"SELECT Name, FilesId, Favourite FROM dbo.Files WHERE HasAccess='{_userId}' AND IsTemporary=1 ORDER BY FilesId ASC", con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            noformat = "";
                            name = (string)reader["Name"];
                            for (int i = 0; i < name.LastIndexOf('.'); i++)
                                noformat += name.ElementAt(i);

                            deleted.Add(new NameInstance(name, noformat, (string)reader["FilesId"], (bool)reader["Favourite"]));
                        }
                    }
                }
            }

            return deleted;
        }
    }
}
