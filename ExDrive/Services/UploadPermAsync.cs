using System.Text;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

using ExDrive.Configuration;
using ExDrive.Authentication;
using ExDrive.Models;

using AntiVirus;

namespace ExDrive.Services
{
    public class UploadPermAsync : UploadFileAsync
    {
        public async Task UploadFileAsync(UploadInstance formFile, Files newFile,
                                                 string userId, ApplicationDbContext applicationDbContext)
        {
            await formFile.MyFile!.OpenReadStream().CopyToAsync(MemoryStream);

            FullPath = Path.Combine(_scanningPath, userId);

            await CreateFile(newFile.FilesId);

            try
            {
                ScanFileForViruses(Path.Combine(FullPath, newFile.FilesId));

                await UploadBlobBlock(newFile, userId, formFile.MyFile.Length);
            }
            catch(Exception)
            {
                return;
            }
            
            await AddFileToDatabase(applicationDbContext, newFile);   
        }

        //public async ValueTask DisposeAsync()
        //{
        //    if (MemoryStream != null)
        //        await MemoryStream.DisposeAsync();

        //    if (!String.IsNullOrEmpty(FullPath))
        //        Directory.Delete(FullPath, true);
        //}

        protected override async Task<CloudBlockBlob> CreateNewBlob(Files newFile, string userId)
        {
            CloudStorageAccount StorageAccount = CloudStorageAccount.Parse(ConnectionStrings.GetStorageConnectionString());

            CloudBlobClient BlobClient = StorageAccount.CreateCloudBlobClient();

            CloudBlobContainer Container = BlobClient.GetContainerReference(userId);
            
            await Container.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Off,
                                                   new BlobRequestOptions(), new OperationContext());
            
            return Container.GetBlockBlobReference(newFile.FilesId);
        }

        //private async Task CreateFile(string name)
        //{
        //    Directory.CreateDirectory(FullPath);

        //    using (var fileStream = new FileStream(Path.Combine(FullPath, name),
        //                                            FileMode.Create, FileAccess.Write))
        //    {
        //        MemoryStream.Position = 0;

        //        await MemoryStream.CopyToAsync(fileStream);
        //    }
        //}

        //void ScanFileForViruses(string filePath)
        //{
        //    var scanner = new Scanner();

        //    var scanResult = scanner.ScanAndClean(filePath);

        //    if (scanResult == ScanResult.VirusFound)
        //    {
        //        throw new Exception("File is infected");
        //    }
        //}

        

        //async Task UploadBlobBlock(Files file, string name,long bytesRemain, long prevLastByte = 0)
        //{
        //    var blob = await CreateNewBlob(file, name);

        //    MemoryStream.Position = 0;

        //    var bytes = MemoryStream.ToArray();

        //    var blocklist = new HashSet<string>();
            
        //    do
        //    {
        //        long bytesToCopy = Math.Min(bytesRemain, _pageSizeInBytes);
        //        byte[] bytesToSend = new byte[bytesToCopy];

        //        Array.Copy(bytes, prevLastByte, bytesToSend, 0, bytesToCopy);

        //        prevLastByte += bytesToCopy;
        //        bytesRemain -= bytesToCopy;

        //        string blockId = Guid.NewGuid().ToString();
        //        string base64BlockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(blockId));

        //        await blob.PutBlockAsync(base64BlockId, new MemoryStream(bytesToSend, true), null);

        //        blocklist.Add(base64BlockId);

        //    } while (bytesRemain > 0);

        //    await blob.PutBlockListAsync(blocklist);
        //}

        //private MemoryStream MemoryStream { get; set; } = new();

        //private string FullPath { get; set; } = String.Empty;

        //private static readonly long _pageSizeInBytes = 10485760;

        //private static readonly string _scanningPath = "C:\\Users\\Public\\scanning";
    }
}
