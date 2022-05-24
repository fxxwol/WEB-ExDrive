using exdrive_web.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace exdrive_web.Models
{
    public class DownloadAzureFile
    {
        public static Stream DownloadFile(string fileid, string userid)
        {
            var storageAccount = CloudStorageAccount.Parse(ConnectionStrings.GetStorageConnectionString());
            var blobClient = storageAccount.CreateCloudBlobClient();
            var sourceContainer = blobClient.GetContainerReference(userid);

            CloudBlockBlob sourceBlob = sourceContainer.GetBlockBlobReference(fileid);
            MemoryStream memStream = new();
            sourceBlob.DownloadToStreamAsync(memStream).Wait();
            memStream.Position = 0;

            return memStream;
        }
    }
}
