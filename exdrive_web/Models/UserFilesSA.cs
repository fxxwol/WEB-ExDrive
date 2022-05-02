using JWTAuthentication.Authentication;
using Microsoft.EntityFrameworkCore;
using Azure.Storage.Blobs;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Text;

namespace exdrive_web.Models
{
    public class UserFilesSA
    {
        private const string connectionString = "DefaultEndpointsProtocol=https;AccountName=exdrivefiles;AccountKey=zW8bG071a7HbJ4+D5Pxruz4rL47KEx0XwExd7m5CmYtCNdu8A71/rVvvY/ld8hwJ4nObLnAcDB27KZV/0L92TA==;EndpointSuffix=core.windows.net";
        public static IEnumerable<Azure.Storage.Blobs.Models.BlobItem> GetUserFilesSA(string _userId)
        {
            BlobContainerClient containerClient = new BlobContainerClient(connectionString, _userId);

            try
            {
                containerClient.Exists();
            }
            catch (Azure.RequestFailedException)
            {
                return null;
            }

            IEnumerable<Azure.Storage.Blobs.Models.BlobItem> blobs;

            blobs = containerClient.GetBlobs();
            if (blobs != null)
                return blobs;
            else
                throw new Exception("No files belonging to the user were found.");
        }
    }
}
