using Azure.Storage.Blobs;
using exdrive_web.Configuration;

namespace exdrive_web.Models
{
    public class UserFilesSA
    {
        public static IEnumerable<Azure.Storage.Blobs.Models.BlobItem> GetUserFilesSA(string _userId)
        {
            if (string.IsNullOrWhiteSpace(_userId))
            {
                return Enumerable.Empty<Azure.Storage.Blobs.Models.BlobItem>();
            }

            try
            {
                BlobContainerClient containerClient = new(ConnectionStrings.GetStorageConnectionString(), _userId);

                if (!containerClient.Exists())
                {
                    throw new Exception();
                }

                IEnumerable<Azure.Storage.Blobs.Models.BlobItem> blobs = containerClient.GetBlobs();

                if (blobs == null)
                {
                    throw new Exception();

                }

                return blobs;
            }
            catch (Exception)
            {
                return Enumerable.Empty<Azure.Storage.Blobs.Models.BlobItem>();
            }
        }
    }
}
