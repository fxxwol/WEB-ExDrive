using Azure.Storage.Blobs;
using JWTAuthentication.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace exdrive_web.Models
{
    public class Trashcan
    {
        public static async Task DeleteFile(string filename, string _userId)
        {
            BlobContainerClient containerDest = new BlobContainerClient(ExFunctions.storageConnectionString, "trashcan");
            BlobContainerClient containerSource = new BlobContainerClient(ExFunctions.storageConnectionString, _userId);

            var storageAccount = CloudStorageAccount.Parse(ExFunctions.storageConnectionString);
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
            optionsBuilder.UseSqlServer(ExFunctions.sqlConnectionString);
            using (var _context = new ApplicationDbContext(optionsBuilder.Options))
            {
                Files? todelete = _context.Files.Find(filename);
                if (todelete != null)
                {
                    Files? modified = todelete;
                    modified.IsTemporary = true;
                    _context.Files.Update(todelete).OriginalValues.SetValues(modified);
                }      
                _context.SaveChanges();
            }
        }
        public static async Task FileRecovery(string filename, string _userId)
        {
            BlobContainerClient containerDest = new BlobContainerClient(ExFunctions.storageConnectionString, _userId);
            BlobContainerClient containerSource = new BlobContainerClient(ExFunctions.storageConnectionString, "trashcan");

            var storageAccount = CloudStorageAccount.Parse(ExFunctions.storageConnectionString);
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

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(ExFunctions.sqlConnectionString);
            using (var _context = new ApplicationDbContext(optionsBuilder.Options))
            {
                Files? torecover = _context.Files.Find(filename);
                if (torecover != null)
                {
                    Files? modified = torecover;
                    modified.IsTemporary = false;
                    _context.Files.Update(torecover).OriginalValues.SetValues(modified);
                }
                _context.SaveChanges();
            }
        }

    }
}
