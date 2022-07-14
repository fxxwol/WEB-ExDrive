using System.Text;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

using ExDrive.Configuration;
using ExDrive.Authentication;
using ExDrive.Models;

namespace ExDrive.Services
{
    public class UploadTempFile : UploadFile
    {
        public async Task UploadFileAsync(UploadInstance formFile, Files newFile,
                                                 string tempName, ApplicationDbContext applicationDbContext)
        {
            await formFile.MyFile!.OpenReadStream().CopyToAsync(MemoryStream);

            FullPath = Path.Combine(_scanningPath, tempName);

            await CreateFileAsync(newFile.FilesId);

            try
            {
                ScanFileForViruses(Path.Combine(FullPath, newFile.FilesId));

                await UploadBlobBlockAsync(newFile, _tempFileContainer, formFile.MyFile.Length);
            }
            catch (Exception)
            {
                throw;
            }

            await AddFileToDatabaseAsync(applicationDbContext, newFile);
        }

        public async Task UploadFileAsync(MemoryStream stream, Files newFile,
                                                    ApplicationDbContext applicationDbContext)
        {
            MemoryStream = stream;

            try
            {
                stream.Position = 0;

                await UploadBlobBlockAsync(newFile, _tempFileContainer, stream.Length);
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
