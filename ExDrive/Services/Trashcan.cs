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
        public async Task DeleteFileAsync(string fileName, string userId, ApplicationDbContext context)
        {
            var destinationContainer = new BlobContainerClient(ConnectionStrings.GetStorageConnectionString(),
                                    TrashcanContainerName);
            var sourceContainer = new BlobContainerClient(ConnectionStrings.GetStorageConnectionString(),
                                    userId);

            var sourceBlob = GetCloudBlockBlobReference(ConnectionStrings.GetStorageConnectionString(),
                                    userId, fileName);

            var memoryStream = new MemoryStream();
            
            await sourceBlob.DownloadToStreamAsync(memoryStream);

            memoryStream.Position = 0;

            await destinationContainer.UploadBlobAsync(fileName, memoryStream);
           
            await sourceContainer.DeleteBlobAsync(fileName);
            
            await memoryStream.FlushAsync();

            var toDelete = context.Files!.Find(fileName);

            if (toDelete != null)
            {
                Files? modified = toDelete;

                modified.IsTemporary = true;

                context.Files.Update(toDelete).OriginalValues.SetValues(modified);
            }

            context.SaveChanges();
        }

        public async Task RecoverFileAsync(string fileName, string userId, ApplicationDbContext context)
        {
            var destinationContainer = GetBlobContainerClient(ConnectionStrings.GetStorageConnectionString(),
                                    userId);
            var sourceContainer = GetBlobContainerClient(ConnectionStrings.GetStorageConnectionString(),
                                    TrashcanContainerName);

            var sourceBlob = GetCloudBlockBlobReference(ConnectionStrings.GetStorageConnectionString(),
                                    TrashcanContainerName, fileName);

            var memoryStream = new MemoryStream();
            
            await sourceBlob.DownloadToStreamAsync(memoryStream);

            memoryStream.Position = 0;

            await destinationContainer.UploadBlobAsync(fileName, memoryStream);
            
            await sourceContainer.DeleteBlobAsync(fileName);
            
            await memoryStream.FlushAsync();

            var toRecover = context.Files!.Find(fileName);
            
            if (toRecover != null)
            {
                Files? modified = toRecover;

                modified.IsTemporary = false;

                context.Files.Update(toRecover).OriginalValues.SetValues(modified);
            }

            context.SaveChanges();
        }

        private BlobContainerClient GetBlobContainerClient(string connectionString, string containerName)
        {
            return new BlobContainerClient(connectionString, containerName);
        }

        private CloudBlockBlob GetCloudBlockBlobReference(string connectionString, string containerName, string blobName)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);

            var blobClient = storageAccount.CreateCloudBlobClient();

            var sourceContainer = blobClient.GetContainerReference(containerName);

            return sourceContainer.GetBlockBlobReference(blobName);
        }

        public Trashcan(string trashcanContainerName)
        {
            TrashcanContainerName = trashcanContainerName;
        }
        private string TrashcanContainerName { get; set; }
    }
}
