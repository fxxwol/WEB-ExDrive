using Azure.Storage.Blobs;
using exdrive_web.Configuration;
using JWTAuthentication.Authentication;
using Microsoft.EntityFrameworkCore;

namespace exdrive_web.Models
{
    public class DeleteTemporary
    {
        public static async void DeleteTemporaryFiles(int days, string containerName)
        {
            BlobContainerClient containerClient = new BlobContainerClient(ConnectionStrings.GetStorageConnectionString(), containerName);
            IEnumerable<Azure.Storage.Blobs.Models.BlobItem> blobs = containerClient.GetBlobs().SkipWhile(x => x.Properties.LastModified >= DateTime.UtcNow.AddDays(-1 * days));

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(ConnectionStrings.GetSqlConnectionString());
            using (var _context = new ApplicationDbContext(optionsBuilder.Options))
            {
                foreach (Azure.Storage.Blobs.Models.BlobItem blob in blobs)
                {
                    await containerClient.DeleteBlobAsync(blob.Name);
                    var todelete = _context.Files.Find(blob.Name);
                    if (todelete != null)
                        _context.Files.Remove(todelete);
                    _context.SaveChanges();
                }
            }
        }
    }
}
