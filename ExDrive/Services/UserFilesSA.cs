using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using ExDrive.Configuration;

namespace ExDrive.Models
{
    public class UserFilesSA
    {
        public static IEnumerable<BlobItem> GetUserFilesSA(string _userId)
        {
            if (string.IsNullOrWhiteSpace(_userId))
            {
                return Enumerable.Empty<BlobItem>();
            }

            try
            {
                BlobContainerClient containerClient = new(ConnectionStrings.GetStorageConnectionString(), _userId);

                if (!containerClient.Exists())
                {
                    throw new Exception();
                }

                IEnumerable<BlobItem> blobs = containerClient.GetBlobs();

                if (blobs == null)
                {
                    throw new Exception();

                }

                return blobs;
            }
            catch (Exception)
            {
                return Enumerable.Empty<BlobItem>();
            }
        }
    }
}
