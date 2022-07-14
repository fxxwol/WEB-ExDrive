using Microsoft.EntityFrameworkCore;

using Azure.Storage.Blobs;

using ExDrive.Authentication;
using ExDrive.Configuration;

namespace ExDrive.Services
{
    public class DeleteTemporary
    {
        public async void DeleteTemporaryFilesAsync(int days, string containerName)
        {
            var containerClient = GetBlobContainerClient(ConnectionStrings.GetStorageConnectionString(), containerName);

            var blobs = GetBlobsWithAreOlderThan(containerClient, days);

            var optionsBuilder = GetDataBaseOptionsBuilder(ConnectionStrings.GetSqlConnectionString());

            using (var database = new ApplicationDbContext(optionsBuilder.Options))
            {
                foreach (var blob in blobs)
                {
                    await containerClient.DeleteBlobAsync(blob.Name);

                    var toDelete = database.Files!.Find(blob.Name);

                    if (toDelete != null)
                    {
                        database.Files.Remove(toDelete);
                    }

                    database.SaveChanges();
                }
            }
        }
        private BlobContainerClient GetBlobContainerClient(string connectionString, string containerName)
        {
            return new BlobContainerClient(connectionString, containerName);
        }
        private DbContextOptionsBuilder<ApplicationDbContext> GetDataBaseOptionsBuilder(string connectionString)
        {
            return new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString);
        }
        private IEnumerable<Azure.Storage.Blobs.Models.BlobItem> GetBlobsWithAreOlderThan(BlobContainerClient containerClient,
                                                                                        int days)
        {
            return containerClient.GetBlobs().SkipWhile(blob => blob.Properties.LastModified >= GetDeadlineDateTime(days));
        }
        private DateTime GetDeadlineDateTime(int days)
        {
            return DateTime.UtcNow.AddDays(-1 * days);
        }
    }
}
