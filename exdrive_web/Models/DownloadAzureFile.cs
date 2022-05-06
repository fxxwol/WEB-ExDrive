using Azure.Storage.Blobs;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace exdrive_web.Models
{
    public class DownloadAzureFile
    {
        private static string connectionString = "DefaultEndpointsProtocol=https;AccountName=exdrivefiles;AccountKey=zW8bG071a7HbJ4+D5Pxruz4rL47KEx0XwExd7m5CmYtCNdu8A71/rVvvY/ld8hwJ4nObLnAcDB27KZV/0L92TA==;EndpointSuffix=core.windows.net";
        public static Stream DownloadFile(string fileid, string userid)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var sourceContainer = blobClient.GetContainerReference(userid);

            CloudBlockBlob sourceBlob = sourceContainer.GetBlockBlobReference(fileid);
            MemoryStream memStream = new MemoryStream();
            sourceBlob.DownloadToStreamAsync(memStream).Wait();
            memStream.Position = 0;

            return memStream;
        }
    }
}
