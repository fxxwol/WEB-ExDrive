using System.Text;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

using ExDrive.Configuration;
using ExDrive.Authentication;
using ExDrive.Models;

namespace ExDrive.Services
{
    public class UploadTempAsync : UploadFileAsync
    {
        //public static async Task UploadFileAsync(UploadInstance formFile, Files newFile, ApplicationDbContext applicationDbContext)
        //{
        //    CloudStorageAccount StorageAccount = CloudStorageAccount.Parse(ConnectionStrings.GetStorageConnectionString());

        //    CloudBlobClient BlobClient = StorageAccount.CreateCloudBlobClient();

        //    CloudBlobContainer Container = BlobClient.GetContainerReference(_tempFileContainer);

        //    CloudBlockBlob blob = Container.GetBlockBlobReference(newFile.FilesId);
        //    HashSet<string> blocklist = new HashSet<string>();

        //    MemoryStream ms = new MemoryStream();
        //    var filems = formFile.MyFile!.OpenReadStream();
        //    await filems.CopyToAsync(ms);

        //    string tempname = Guid.NewGuid().ToString();
        //    string fullpath = Path.Combine(_scanningPath, tempname);
        //    System.IO.Directory.CreateDirectory(fullpath);

        //    filems.Position = 0;
        //    using (var fileStream = new FileStream(Path.Combine(fullpath, newFile.FilesId), FileMode.Create, FileAccess.Write))
        //    {
        //        await filems.CopyToAsync(fileStream);
        //    }

        //    var scanner = new AntiVirus.Scanner();
        //    var scanresult = scanner.ScanAndClean(Path.Combine(fullpath, newFile.FilesId));

        //    var isinfected = scanresult.ToString();

        //    if (isinfected == "VirusFound")
        //    {
        //        await filems.DisposeAsync();
        //        await ms.DisposeAsync();

        //        throw new Exception("File may be malicious");
        //    }

        //    const int pageSizeInBytes = 10485760;
        //    long prevLastByte = 0;
        //    long bytesRemain = formFile.MyFile.Length;

        //    byte[] bytes;

        //    bytes = ms.ToArray();

        //    do
        //    {
        //        long bytesToCopy = Math.Min(bytesRemain, pageSizeInBytes);
        //        byte[] bytesToSend = new byte[bytesToCopy];

        //        Array.Copy(bytes, prevLastByte, bytesToSend, 0, bytesToCopy);
        //        prevLastByte += bytesToCopy;
        //        bytesRemain -= bytesToCopy;

        //        string blockId = Guid.NewGuid().ToString();
        //        string base64BlockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(blockId));

        //        await blob.PutBlockAsync(
        //            base64BlockId,
        //            new MemoryStream(bytesToSend, true),
        //            null
        //            );

        //        blocklist.Add(base64BlockId);

        //    } while (bytesRemain > 0);

        //    applicationDbContext.Files!.Add(newFile);

        //    applicationDbContext.SaveChanges();

        //    await blob.PutBlockListAsync(blocklist);
        //    await filems.DisposeAsync();
        //    await ms.DisposeAsync();

        //    System.IO.Directory.Delete(fullpath, true);
        //}
        //public static async Task UploadFileAsync(MemoryStream stream, Files newFile, ApplicationDbContext applicationDbContext)
        //{
        //    CloudStorageAccount StorageAccount = CloudStorageAccount.Parse(ConnectionStrings.GetStorageConnectionString());

        //    CloudBlobClient BlobClient = StorageAccount.CreateCloudBlobClient();

        //    CloudBlobContainer Container = BlobClient.GetContainerReference(_tempFileContainer);

        //    CloudBlockBlob blob = Container.GetBlockBlobReference(newFile.FilesId);
        //    HashSet<string> blocklist = new HashSet<string>();

        //    const int pageSizeInBytes = 10485760;
        //    long prevLastByte = 0;
        //    long bytesRemain = stream.Length;

        //    byte[] bytes;

        //    bytes = stream.ToArray();

        //    do
        //    {
        //        long bytesToCopy = Math.Min(bytesRemain, pageSizeInBytes);
        //        byte[] bytesToSend = new byte[bytesToCopy];

        //        Array.Copy(bytes, prevLastByte, bytesToSend, 0, bytesToCopy);
        //        prevLastByte += bytesToCopy;
        //        bytesRemain -= bytesToCopy;

        //        string blockId = Guid.NewGuid().ToString();
        //        string base64BlockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(blockId));

        //        await blob.PutBlockAsync(
        //            base64BlockId,
        //            new MemoryStream(bytesToSend, true),
        //            null
        //            );

        //        blocklist.Add(base64BlockId);

        //    } while (bytesRemain > 0);

        //    applicationDbContext.Files!.Add(newFile);

        //    applicationDbContext.SaveChanges();

        //    await blob.PutBlockListAsync(blocklist);
        //    await stream.DisposeAsync();
        //}
        public async Task UploadFileAsync(UploadInstance formFile, Files newFile,
                                                 string tempName, ApplicationDbContext applicationDbContext)
        {
            await formFile.MyFile!.OpenReadStream().CopyToAsync(MemoryStream);

            FullPath = Path.Combine(_scanningPath, tempName);

            await CreateFile(newFile.FilesId);

            try
            {
                ScanFileForViruses(Path.Combine(FullPath, newFile.FilesId));

                await UploadBlobBlock(newFile, _tempFileContainer, formFile.MyFile.Length);
            }
            catch (Exception)
            {
                throw;
            }

            await AddFileToDatabase(applicationDbContext, newFile);
        }

        public async Task UploadFileAsync(MemoryStream stream, Files newFile,
                                                    ApplicationDbContext applicationDbContext)
        {
            MemoryStream = stream;

            try
            {
                stream.Position = 0;

                await UploadBlobBlock(newFile, _tempFileContainer, stream.Length);
            }
            catch (Exception)
            {
                throw;
            }

            await AddFileToDatabase(applicationDbContext, newFile);
        }

        protected override async Task<CloudBlockBlob> CreateNewBlob(Files newFile, string containerName)
        {
            CloudStorageAccount StorageAccount = CloudStorageAccount.Parse(ConnectionStrings.GetStorageConnectionString());

            CloudBlobClient BlobClient = StorageAccount.CreateCloudBlobClient();

            CloudBlobContainer Container = BlobClient.GetContainerReference(containerName);

            return Container.GetBlockBlobReference(newFile.FilesId);
        }

        private static readonly string _tempFileContainer = "botfiles";
    }
}
