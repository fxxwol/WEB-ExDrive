using Azure;
using Azure.Storage.Blobs;
using exdrive_web.Configuration;
using exdrive_web.Models;

namespace exdrive_web.Services
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
