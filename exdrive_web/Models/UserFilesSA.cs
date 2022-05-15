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
        public static IEnumerable<Azure.Storage.Blobs.Models.BlobItem> GetUserFilesSA(string _userId)
        {
            BlobContainerClient containerClient = new BlobContainerClient(ExFunctions.storageConnectionString, _userId);

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
