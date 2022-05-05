using Azure.Storage.Blobs;
using JWTAuthentication.Authentication;
using Microsoft.EntityFrameworkCore;

namespace exdrive_web.Models
{
    public class DeleteTemporary
    {
        private static string connectionString = "DefaultEndpointsProtocol=https;AccountName=exdrivefiles;AccountKey=zW8bG071a7HbJ4+D5Pxruz4rL47KEx0XwExd7m5CmYtCNdu8A71/rVvvY/ld8hwJ4nObLnAcDB27KZV/0L92TA==;EndpointSuffix=core.windows.net";
        private static string containerName = "botfiles";
        public static void DeleteTemporaryFiles()
        {
            BlobContainerClient containerClient = new BlobContainerClient(connectionString, containerName);
            IEnumerable<Azure.Storage.Blobs.Models.BlobItem> blobs = containerClient.GetBlobs().SkipWhile(x => x.Properties.LastModified >= DateTime.UtcNow.AddDays(-1 * 7));
            //IEnumerable<Azure.Storage.Blobs.Models.BlobItem> blobs = containerClient.GetBlobs().SkipWhile(x => x.Properties.LastModified < DateTime.UtcNow.AddDays(-1 * 7));
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer("Server=tcp:exdrive.database.windows.net,1433;Initial Catalog=Exdrive;Persist Security Info=False;User ID=fxxwol;Password=AbCD.123;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
            using (var _context = new ApplicationDbContext(optionsBuilder.Options))
            {
                foreach (Azure.Storage.Blobs.Models.BlobItem blob in blobs)
                {
                    containerClient.DeleteBlobAsync(blob.Name).Wait();
                    var todelete = _context.Files.Find(blob.Name);
                    if (todelete != null)
                        _context.Files.Remove(todelete);
                    _context.SaveChanges();
                }
            }
        }
    }
}
