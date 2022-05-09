using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using JWTAuthentication.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace exdrive_web.Models
{
    public class Trashcan
    {
        private static string connectionString = "DefaultEndpointsProtocol=https;AccountName=exdrivefiles;AccountKey=zW8bG071a7HbJ4+D5Pxruz4rL47KEx0XwExd7m5CmYtCNdu8A71/rVvvY/ld8hwJ4nObLnAcDB27KZV/0L92TA==;EndpointSuffix=core.windows.net";

        public static async Task DeleteFile(string filename, string _userId)
        {
            BlobContainerClient containerDest = new BlobContainerClient(connectionString, "trashcan");
            BlobContainerClient containerSource = new BlobContainerClient(connectionString, _userId);

            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();

            var sourceContainerName = _userId;

            var sourceContainer = blobClient.GetContainerReference(sourceContainerName);

            CloudBlockBlob sourceBlob = sourceContainer.GetBlockBlobReference(filename);

            MemoryStream memStream = new MemoryStream();
            await sourceBlob.DownloadToStreamAsync(memStream);
            memStream.Position = 0;

            await containerDest.UploadBlobAsync(filename, memStream);
            await containerSource.DeleteBlobAsync(filename);
            await memStream.FlushAsync();

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer("Server=tcp:exdrive.database.windows.net,1433;Initial Catalog=Exdrive;Persist Security Info=False;User ID=fxxwol;Password=AbCD.123;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
            using (var _context = new ApplicationDbContext(optionsBuilder.Options))
            {
                    var todelete = _context.Files.Find(filename);
                if (todelete != null)
                        _context.Files.Remove(todelete);
                _context.SaveChanges();
            }
        }
        public static async Task FileRecovery(string filename)
        {
            BlobContainerClient containerDest = new BlobContainerClient(connectionString, "files");
            BlobContainerClient containerSource = new BlobContainerClient(connectionString, "botfiles");

            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();

            var sourceContainerName = "trashcan";

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
