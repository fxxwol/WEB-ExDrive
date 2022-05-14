using JWTAuthentication.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Text;

namespace exdrive_web.Models
{
    public class UploadTempBotAsync
    {
        public static async Task UploadFileAsync(Stream stream, long bytesRemain, Files newFile)
        {
            CloudStorageAccount StorageAccount = CloudStorageAccount.Parse(ExFunctions.storageConnectionString);

            CloudBlobClient BlobClient = StorageAccount.CreateCloudBlobClient();

            CloudBlobContainer Container = BlobClient.GetContainerReference("botfiles");

            CloudBlockBlob blob = Container.GetBlockBlobReference(newFile.FilesId);
            HashSet<string> blocklist = new HashSet<string>();

            const int pageSizeInBytes = 10485760;
            long prevLastByte = 0;

            byte[] bytes;

            MemoryStream ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Position = 0;
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

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(ExFunctions.sqlConnectionString);
            using (var _context = new ApplicationDbContext(optionsBuilder.Options))
            {
                _context.Files.Add(newFile);
                _context.SaveChanges();
            }

            await blob.PutBlockListAsync(blocklist);
            await stream.DisposeAsync();
            await ms.DisposeAsync();

            if (blob.ExistsAsync().Result == false)
                throw new Exception("Failed at creating the blob specified");
        }
    }
}
