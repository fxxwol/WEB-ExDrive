using ExDrive.Configuration;
using ExDrive.Authentication;
using ExDrive.Services;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

using System.Text;

namespace ExDrive.Models
{
    public class UploadTempBotAsync : UploadFileAsync
    {
        public async Task UploadFileAsync(Stream stream, long bytesRemain, string tempName,
                                                Files newFile, ApplicationDbContext applicationDbContext)
        {
            await stream.CopyToAsync(MemoryStream);

            FullPath = Path.Combine(_scanningPath, tempName);

            await CreateFile(newFile.FilesId);

            try
            {
                ScanFileForViruses(Path.Combine(FullPath, newFile.FilesId));

                await UploadBlobBlock(newFile, _tempFileContainer, bytesRemain);
            }
            catch (Exception)
            {
                throw;
            }

            await stream.DisposeAsync();

            await AddFileToDatabase(applicationDbContext, newFile);
        }

        protected override Task<CloudBlockBlob> CreateNewBlob(Files newFile, string containerName)
        {
            CloudStorageAccount StorageAccount = CloudStorageAccount.Parse(ConnectionStrings.GetStorageConnectionString());

            CloudBlobClient BlobClient = StorageAccount.CreateCloudBlobClient();

            CloudBlobContainer Container = BlobClient.GetContainerReference(containerName);

            return Task.FromResult(Container.GetBlockBlobReference(newFile.FilesId));
        }

        private static readonly string _tempFileContainer = "botfiles";
    }
}
