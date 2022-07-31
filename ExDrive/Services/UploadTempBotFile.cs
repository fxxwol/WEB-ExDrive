using ExDrive.Configuration;
using ExDrive.Authentication;
using ExDrive.Services;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

using System.Text;

namespace ExDrive.Models
{
    public class UploadTempBotFile : UploadFile
    {
        public async Task UploadFileAsync(Stream stream, long bytesRemain,
                                          Files newFile, ApplicationDbContext applicationDbContext)
        {
            await stream.CopyToAsync(MemoryStream);

            FullPath = Path.Combine(_scanningPath, Guid.NewGuid().ToString());

            await CreateFileAsync(newFile.FilesId);

            try
            {
                ScanFileForViruses(Path.Combine(FullPath, newFile.FilesId));

                await UploadBlobBlockAsync(newFile, _tempFileContainer, bytesRemain);
            }
            catch (Exception)
            {
                throw;
            }

            await stream.DisposeAsync();

            await AddFileToDatabaseAsync(applicationDbContext, newFile);
        }

        protected override Task<CloudBlockBlob> CreateNewBlobAsync(Files newFile, string containerName)
        {
            CloudStorageAccount StorageAccount = CloudStorageAccount.Parse(ConnectionStrings.GetStorageConnectionString());

            CloudBlobClient BlobClient = StorageAccount.CreateCloudBlobClient();

            CloudBlobContainer Container = BlobClient.GetContainerReference(containerName);

            return Task.FromResult(Container.GetBlockBlobReference(newFile.FilesId));
        }

        private static readonly string _tempFileContainer = "botfiles";
    }
}
