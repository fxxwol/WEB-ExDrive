using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

using ExDrive.Configuration;

namespace ExDrive.Services
{
    public class DownloadAzureFile
    {
        public async Task<Stream> DownloadFile(string fileId, string userId)
        {
            var sourceBlob = GetContainerReference(ConnectionStrings.GetStorageConnectionString(),
                                        fileId, userId);

            var memorySteam = new MemoryStream();

            await sourceBlob.DownloadToStreamAsync(memorySteam);

            memorySteam.Position = 0;

            return memorySteam;
        }
        private CloudBlockBlob GetContainerReference(string connectionString, string fileId, string containerName)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);

            var blobClient = storageAccount.CreateCloudBlobClient();

            var sourceContainer = blobClient.GetContainerReference(containerName);

            return sourceContainer.GetBlockBlobReference(fileId);
        }
    }
}
