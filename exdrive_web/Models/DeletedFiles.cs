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
            using (SqlConnection con = new SqlConnection("Server=tcp:exdrive.database.windows.net,1433;Initial Catalog=Exdrive;Persist Security Info=False;User ID=fxxwol;Password=AbCD.123;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"))
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand($"SELECT Name, FilesId FROM dbo.Files WHERE HasAccess='{_userId}' AND IsTemporary=1 ORDER BY FilesId ASC", con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            noformat = "";
                            name = (string)reader["Name"];
                            for (int i = 0; i < name.LastIndexOf('.'); i++)
                                noformat += name.ElementAt(i);

                            deleted.Add(new NameInstance(name, noformat, (string)reader["FilesId"]));
                        }
                    }
                }
            }

            return deleted;
        }
    }
}
