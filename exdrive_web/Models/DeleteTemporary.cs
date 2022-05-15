using Azure.Storage.Blobs;
using JWTAuthentication.Authentication;
using Microsoft.EntityFrameworkCore;

namespace exdrive_web.Models
{
    public class DeleteTemporary
    {
        public static void DeleteTemporaryFiles(int days, string containerName)
        {
            BlobContainerClient containerClient = new BlobContainerClient(ExFunctions.storageConnectionString, containerName);
            IEnumerable<Azure.Storage.Blobs.Models.BlobItem> blobs = containerClient.GetBlobs().SkipWhile(x => x.Properties.LastModified >= DateTime.UtcNow.AddDays(-1 * days));

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(ExFunctions.sqlConnectionString);
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
