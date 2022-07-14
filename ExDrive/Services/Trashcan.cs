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
            var destinationContainer = GetBlobContainerClient(ConnectionStrings.GetStorageConnectionString(),
                                    TrashcanContainerName);
            var sourceContainer = GetBlobContainerClient(ConnectionStrings.GetStorageConnectionString(),
                                    userId);

            var sourceBlob = GetCloudBlockBlobReference(ConnectionStrings.GetStorageConnectionString(),
                                    userId, fileName);

            await MoveBlobAsync(destinationContainer, sourceContainer, sourceBlob);

            await MarkAsDeletedAsync(fileName, context);
        }

        public async Task RecoverFileAsync(string fileName, string userId, ApplicationDbContext context)
        {
            var destinationContainer = GetBlobContainerClient(ConnectionStrings.GetStorageConnectionString(),
                                    userId);
            var sourceContainer = GetBlobContainerClient(ConnectionStrings.GetStorageConnectionString(),
                                    TrashcanContainerName);

            var sourceBlob = GetCloudBlockBlobReference(ConnectionStrings.GetStorageConnectionString(),
                                                    TrashcanContainerName, fileName);

            await MoveBlobAsync(destinationContainer, sourceContainer, sourceBlob);

            await MarkAsRecoveredAsync(fileName, context);
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

        private async Task MoveBlobAsync(BlobContainerClient destinationContainer, BlobContainerClient sourceContainer,
                                                CloudBlockBlob sourceBlob)
        {
            var memoryStream = new MemoryStream();

            await sourceBlob.DownloadToStreamAsync(memoryStream);

            memoryStream.Position = 0;

            await destinationContainer.UploadBlobAsync(sourceBlob.Name, memoryStream);

            await sourceContainer.DeleteBlobAsync(sourceBlob.Name);

            await memoryStream.FlushAsync();
        }

        private async Task MarkAsRecoveredAsync(string fileName, ApplicationDbContext applicationDbContext)
        {
            var toRecover = await applicationDbContext.Files!.FindAsync(fileName);

            if (toRecover != null)
            {
                Files? modified = toRecover;

                modified.IsTemporary = false;

                applicationDbContext.Files.Update(toRecover).OriginalValues.SetValues(modified);
                
                await applicationDbContext.SaveChangesAsync();
            }
        }

        private async Task MarkAsDeletedAsync(string fileName, ApplicationDbContext applicationDbContext)
        {
            var toDelete = await applicationDbContext.Files!.FindAsync(fileName);

            if (toDelete != null)
            {
                Files? modified = toDelete;

                modified.IsTemporary = true;

                applicationDbContext.Files.Update(toDelete).OriginalValues.SetValues(modified);
                
                await applicationDbContext.SaveChangesAsync();
            }
        }

        public Trashcan(string trashcanContainerName)
        {
            TrashcanContainerName = trashcanContainerName;
        }
        private string TrashcanContainerName { get; set; }
    }
}
