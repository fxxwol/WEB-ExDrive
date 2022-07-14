using System.Text;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

using ExDrive.Configuration;
using ExDrive.Authentication;
using ExDrive.Models;

using AntiVirus;

namespace ExDrive.Services
{
    public class UploadPermFile : UploadFile
    {
        public async Task UploadFileAsync(UploadInstance formFile, Files newFile,
                                                 string userId, ApplicationDbContext applicationDbContext)
        {
            await formFile.MyFile!.OpenReadStream().CopyToAsync(MemoryStream);

            FullPath = Path.Combine(_scanningPath, userId);

            await CreateFileAsync(newFile.FilesId);

            try
            {
                ScanFileForViruses(Path.Combine(FullPath, newFile.FilesId));

                await UploadBlobBlockAsync(newFile, userId, formFile.MyFile.Length);
            }
            catch(Exception)
            {
                return;
            }
            
            await AddFileToDatabaseAsync(applicationDbContext, newFile);
        }

        protected override async Task<CloudBlockBlob> CreateNewBlobAsync(Files newFile, string containerName)
        {
            CloudStorageAccount StorageAccount = CloudStorageAccount.Parse(ConnectionStrings.GetStorageConnectionString());

            CloudBlobClient BlobClient = StorageAccount.CreateCloudBlobClient();

            CloudBlobContainer Container = BlobClient.GetContainerReference(containerName);
            
            await Container.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Off,
                                                   new BlobRequestOptions(), new OperationContext());
            
            return Container.GetBlockBlobReference(newFile.FilesId);
        }
    }
}
