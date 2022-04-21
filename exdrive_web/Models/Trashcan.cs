using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace exdrive_web.Models
{
    public class Trashcan
    {
        private static string connectionString = "DefaultEndpointsProtocol=https;AccountName=exdrivefiles;AccountKey=zW8bG071a7HbJ4+D5Pxruz4rL47KEx0XwExd7m5CmYtCNdu8A71/rVvvY/ld8hwJ4nObLnAcDB27KZV/0L92TA==;EndpointSuffix=core.windows.net";
        private static string filename = "77dedb09-52d0-4e52-abf8-1e35fd33e54b.exe";

        public static async Task TransferFilesAsync()
        {
            BlobContainerClient containerDest = new BlobContainerClient(connectionString, "botfiles");
            BlobContainerClient containerSource = new BlobContainerClient(connectionString, "files");

            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();

            // details of our source file
            var sourceContainerName = "files";

            var sourceContainer = blobClient.GetContainerReference(sourceContainerName);

            CloudBlockBlob sourceBlob = sourceContainer.GetBlockBlobReference(filename);

            MemoryStream memStream = new MemoryStream();
            await sourceBlob.DownloadToStreamAsync(memStream);
            memStream.Position = 0;

            await containerDest.UploadBlobAsync(filename, memStream);
            await containerSource.DeleteBlobAsync(filename);
            await memStream.FlushAsync();
        }
    }
}
