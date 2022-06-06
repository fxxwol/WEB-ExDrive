using Microsoft.EntityFrameworkCore;

using Azure.Storage.Blobs;

using ExDrive.Authentication;
using ExDrive.Configuration;

namespace ExDrive.Services
{
    public class DeleteTemporary
    {
        public static async void DeleteTemporaryFiles(int days, string containerName)
        {
            var containerClient = new BlobContainerClient(ConnectionStrings.GetStorageConnectionString(), containerName);
            var blobs = containerClient.GetBlobs().SkipWhile(x => x.Properties.LastModified >= DateTime.UtcNow.AddDays(-1 * days));

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(ConnectionStrings.GetSqlConnectionString());

            using (var _context = new ApplicationDbContext(optionsBuilder.Options))
            {
                foreach (var blob in blobs)
                {
                    await containerClient.DeleteBlobAsync(blob.Name);

                    var todelete = _context.Files.Find(blob.Name);

                    if (todelete != null)
                    {
                        _context.Files.Remove(todelete);
                    }

                    _context.SaveChanges();
                }
            }
        }
    }
}
