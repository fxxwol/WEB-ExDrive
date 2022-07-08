using System.Text;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

using ExDrive.Configuration;
using ExDrive.Authentication;
using ExDrive.Models;

namespace ExDrive.Services
{
    public class UploadPermAsync
    {
        private static readonly string _scanningPath = "C:\\Users\\Public\\scanning";
        public static async Task UploadFileAsync(UploadInstance formFile, Files newFile,
                                                 string userId, ApplicationDbContext applicationDbContext)
        {
            var blob = await CreateNewBlob(newFile, userId);

            var blocklist = new HashSet<string>();

            var memoryStream = new MemoryStream();

            var fileMemoryStream = formFile.MyFile!.OpenReadStream();

            await fileMemoryStream.CopyToAsync(memoryStream);

            string fullpath = Path.Combine(_scanningPath, userId);
            Directory.CreateDirectory(fullpath);

            fileMemoryStream.Position = 0;
            using (var fileStream = new FileStream(Path.Combine(fullpath, newFile.FilesId),
                                                    FileMode.Create, FileAccess.Write))
            {
                await fileMemoryStream.CopyToAsync(fileStream);
            }

            try
            {
                var scanner = new AntiVirus.Scanner();
                var scanResult = scanner.ScanAndClean(Path.Combine(fullpath, newFile.FilesId));

                var isInfected = scanResult.ToString();

                if (isInfected == "VirusFound")
                {
                    throw new Exception("File may be malicious");
                }
            }
            catch (Exception)
            {
                await fileMemoryStream.DisposeAsync();
                await memoryStream.DisposeAsync();

                return;
            }

            const int pageSizeInBytes = 10485760;
            long prevLastByte = 0;
            long bytesRemain = formFile.MyFile.Length;

            byte[] bytes;

            bytes = memoryStream.ToArray();

            do
            {
                long bytesToCopy = Math.Min(bytesRemain, pageSizeInBytes);
                byte[] bytesToSend = new byte[bytesToCopy];

                Array.Copy(bytes, prevLastByte, bytesToSend, 0, bytesToCopy);

                prevLastByte += bytesToCopy;
                bytesRemain -= bytesToCopy;

                string blockId = Guid.NewGuid().ToString();
                string base64BlockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(blockId));

                await blob.PutBlockAsync(base64BlockId, new MemoryStream(bytesToSend, true), null);

                blocklist.Add(base64BlockId);

            } while (bytesRemain > 0);

            applicationDbContext.Files!.Add(newFile);
            applicationDbContext.SaveChanges();

            await blob.PutBlockListAsync(blocklist);
            await fileMemoryStream.DisposeAsync();
            await memoryStream.DisposeAsync();

            if (blob.ExistsAsync().Result == false)
            {
                throw new Exception("Failed at creating the blob specified");
            }

            Directory.Delete(fullpath, true);
        }

        private static async Task<CloudBlockBlob> CreateNewBlob(Files newFile, string userId)
        {
            CloudStorageAccount StorageAccount = CloudStorageAccount.Parse(ConnectionStrings.GetStorageConnectionString());

            CloudBlobClient BlobClient = StorageAccount.CreateCloudBlobClient();

            CloudBlobContainer Container = BlobClient.GetContainerReference(userId);
            await Container.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Off,
                                                   new BlobRequestOptions(), new OperationContext());
            
            return Container.GetBlockBlobReference(newFile.FilesId);
        }
    }
}
