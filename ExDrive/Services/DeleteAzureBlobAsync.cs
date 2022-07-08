using Azure;
using Azure.Storage.Blobs;

using ExDrive.Configuration;
using ExDrive.Models;

namespace ExDrive.Services
{
    public class DeleteAzureBlobAsync
    {
        public async void DeleteBlobAsync(Files? file, string containerName)
        {
            if (file == null)
            {
                throw new ArgumentNullException("Could not delete a blob from " + containerName);
            }

            var containerClient = new BlobContainerClient(ConnectionStrings.GetStorageConnectionString(), containerName);

            try
            {
                await containerClient.DeleteBlobAsync(file.FilesId);
            }
            catch(RequestFailedException exception)
            {
                throw exception;
            }
        }
    }
}
