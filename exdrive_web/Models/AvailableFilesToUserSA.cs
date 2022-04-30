using JWTAuthentication.Authentication;
using Microsoft.EntityFrameworkCore;
using Azure.Storage.Blobs;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Text;

namespace exdrive_web.Models
{
    public class AvailableFilesToUserSA
    {
        private const string connectionString = "DefaultEndpointsProtocol=https;AccountName=exdrivefiles;AccountKey=zW8bG071a7HbJ4+D5Pxruz4rL47KEx0XwExd7m5CmYtCNdu8A71/rVvvY/ld8hwJ4nObLnAcDB27KZV/0L92TA==;EndpointSuffix=core.windows.net";
        private const string containerName = "files";
        public static IEnumerable<Azure.Storage.Blobs.Models.BlobItem> GetUserFilesSA(List<string> files)
        {
            BlobContainerClient containerClient = new BlobContainerClient(connectionString, containerName);
            IEnumerable<Azure.Storage.Blobs.Models.BlobItem> blobs = null;
            IEnumerable<Azure.Storage.Blobs.Models.BlobItem> blob;

            blobs = containerClient.GetBlobs().SkipWhile(x => x.Name != files.First());
            files.Skip(1);

            foreach (var file in files)
            {
                blob = containerClient.GetBlobs().SkipWhile(x => x.Name != file);
                if(blob != null)
                    blobs = blobs.Append(blob.First());
            }

            if (blobs != null)
                return blobs;
            else
                throw new Exception("No files belonging to the user were found.");
        }
    }
}
