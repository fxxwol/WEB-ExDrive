using JWTAuthentication.Authentication;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Text;

namespace exdrive_web.Models
{
    public class UserFilesDB
    {
        public static List<string> GetUserFilesDB(string _userId)
        {
            List<string> files = new List<string>();
            using (SqlConnection con = new SqlConnection("Server=tcp:exdrive.database.windows.net,1433;Initial Catalog=Exdrive;Persist Security Info=False;User ID=fxxwol;Password=AbCD.123;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"))
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand($"SELECT FilesId FROM dbo.Files WHERE HasAccess='{_userId}'", con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            files.Add((string)reader["FilesId"]);
                        }
                    }
                }
            }

            return files;
        }
    }
}
