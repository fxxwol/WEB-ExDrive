using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

using Azure.Storage.Blobs;

using ExDrive.Configuration;
using ExDrive.Authentication;
using ExDrive.Models;

namespace ExDrive.Services
{
    public class Trashcan
    {
        public static async Task DeleteFile(string filename, string userId, ApplicationDbContext context)
        {
            BlobContainerClient containerDest = new BlobContainerClient(ConnectionStrings.GetStorageConnectionString(), "trashcan");
            BlobContainerClient containerSource = new BlobContainerClient(ConnectionStrings.GetStorageConnectionString(), userId);

            var storageAccount = CloudStorageAccount.Parse(ConnectionStrings.GetStorageConnectionString());
            var blobClient = storageAccount.CreateCloudBlobClient();

            var sourceContainerName = userId;

            var sourceContainer = blobClient.GetContainerReference(sourceContainerName);

            CloudBlockBlob sourceBlob = sourceContainer.GetBlockBlobReference(filename);

            MemoryStream memStream = new MemoryStream();
            await sourceBlob.DownloadToStreamAsync(memStream);
            memStream.Position = 0;

            await containerDest.UploadBlobAsync(filename, memStream);
            await containerSource.DeleteBlobAsync(filename);
            await memStream.FlushAsync();

            Files? todelete = context.Files.Find(filename);
            if (todelete != null)
            {
                Files? modified = todelete;
                modified.IsTemporary = true;
                context.Files.Update(todelete).OriginalValues.SetValues(modified);
            }
            context.SaveChanges();
        }
        public static async Task FileRecovery(string filename, string userId, ApplicationDbContext context)
        {
            BlobContainerClient containerDest = new BlobContainerClient(ConnectionStrings.GetStorageConnectionString(), userId);
            BlobContainerClient containerSource = new BlobContainerClient(ConnectionStrings.GetStorageConnectionString(), "trashcan");

            var storageAccount = CloudStorageAccount.Parse(ConnectionStrings.GetStorageConnectionString());
            var blobClient = storageAccount.CreateCloudBlobClient();

            var sourceContainerName = "trashcan";

            var sourceContainer = blobClient.GetContainerReference(sourceContainerName);

            CloudBlockBlob sourceBlob = sourceContainer.GetBlockBlobReference(filename);

            MemoryStream memStream = new MemoryStream();
            await sourceBlob.DownloadToStreamAsync(memStream);
            memStream.Position = 0;

            await containerDest.UploadBlobAsync(filename, memStream);
            await containerSource.DeleteBlobAsync(filename);
            await memStream.FlushAsync();

            Files? torecover = context.Files.Find(filename);
            if (torecover != null)
            {
                Files? modified = torecover;
                modified.IsTemporary = false;
                context.Files.Update(torecover).OriginalValues.SetValues(modified);
            }
            context.SaveChanges();
        }

    }
}
