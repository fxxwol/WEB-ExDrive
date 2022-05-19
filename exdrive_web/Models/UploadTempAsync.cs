using JWTAuthentication.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Text;
using VirusTotalNet.Results;

namespace exdrive_web.Models
{
    public class UploadTempAsync
    {
        public static async Task UploadFileAsync(UploadInstance formFile, Files newFile, ApplicationDbContext applicationDbContext)
        {
            CloudStorageAccount StorageAccount = CloudStorageAccount.Parse(ExFunctions.storageConnectionString);

            CloudBlobClient BlobClient = StorageAccount.CreateCloudBlobClient();

            CloudBlobContainer Container = BlobClient.GetContainerReference("botfiles");

            CloudBlockBlob blob = Container.GetBlockBlobReference(newFile.FilesId);
            HashSet<string> blocklist = new HashSet<string>();

            MemoryStream ms = new MemoryStream();
            var filems = formFile.MyFile.OpenReadStream();
            filems.CopyToAsync(ms).Wait();

            string tempname = Guid.NewGuid().ToString();
            string fullpath = Path.Combine("C:\\Users\\Public\\scanning", tempname);
            System.IO.Directory.CreateDirectory(fullpath);

            filems.Position = 0;
            using (var fileStream = new FileStream(Path.Combine(fullpath, newFile.FilesId), FileMode.Create, FileAccess.Write))
                filems.CopyToAsync(fileStream).Wait();

            try
            {
                var scanner = new AntiVirus.Scanner();
                var scanresult = scanner.ScanAndClean(Path.Combine(fullpath, newFile.FilesId));
                var isinfected = scanresult.ToString();
                if (isinfected == "VirusFound")
                    throw new Exception("File may be malicious");
            }
            catch (Exception)
            {
                   
            }
            
            const int pageSizeInBytes = 10485760;
            long prevLastByte = 0;
            long bytesRemain = formFile.MyFile.Length;

            byte[] bytes;

            bytes = ms.ToArray();

            do
            {
                long bytesToCopy = Math.Min(bytesRemain, pageSizeInBytes);
                byte[] bytesToSend = new byte[bytesToCopy];

                Array.Copy(bytes, prevLastByte, bytesToSend, 0, bytesToCopy);
                prevLastByte += bytesToCopy;
                bytesRemain -= bytesToCopy;

                string blockId = Guid.NewGuid().ToString();
                string base64BlockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(blockId));

                await blob.PutBlockAsync(
                    base64BlockId,
                    new MemoryStream(bytesToSend, true),
                    null
                    );

                blocklist.Add(base64BlockId);

            } while (bytesRemain > 0);

            applicationDbContext.Files.Add(newFile);

            applicationDbContext.SaveChanges();

            await blob.PutBlockListAsync(blocklist);
            await filems.DisposeAsync();
            await ms.DisposeAsync();

            System.IO.Directory.Delete(fullpath, true);
        }
        public static async Task UploadFileAsync(MemoryStream stream, Files newFile, ApplicationDbContext applicationDbContext)
        {
            CloudStorageAccount StorageAccount = CloudStorageAccount.Parse(ExFunctions.storageConnectionString);

            CloudBlobClient BlobClient = StorageAccount.CreateCloudBlobClient();

            CloudBlobContainer Container = BlobClient.GetContainerReference("botfiles");

            CloudBlockBlob blob = Container.GetBlockBlobReference(newFile.FilesId);
            HashSet<string> blocklist = new HashSet<string>();

            const int pageSizeInBytes = 10485760;
            long prevLastByte = 0;
            long bytesRemain = stream.Length;

            byte[] bytes;

            bytes = stream.ToArray();

            do
            {
                long bytesToCopy = Math.Min(bytesRemain, pageSizeInBytes);
                byte[] bytesToSend = new byte[bytesToCopy];

                Array.Copy(bytes, prevLastByte, bytesToSend, 0, bytesToCopy);
                prevLastByte += bytesToCopy;
                bytesRemain -= bytesToCopy;

                string blockId = Guid.NewGuid().ToString();
                string base64BlockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(blockId));

                await blob.PutBlockAsync(
                    base64BlockId,
                    new MemoryStream(bytesToSend, true),
                    null
                    );

                blocklist.Add(base64BlockId);

            } while (bytesRemain > 0);

            applicationDbContext.Files.Add(newFile);

            applicationDbContext.SaveChanges();

            await blob.PutBlockListAsync(blocklist);
            await stream.DisposeAsync();
        }
    }
}
