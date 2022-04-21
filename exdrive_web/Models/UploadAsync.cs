using JWTAuthentication.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Text;

namespace exdrive_web.Models
{
    public class UploadAsync
    {
        public static async Task UploadFileAsync(IFormFile formFile, string folderPath, Files newFile, MemoryStream ms)
        {
            const string connectionString = "DefaultEndpointsProtocol=https;AccountName=exdrivefiles;AccountKey=zW8bG071a7HbJ4+D5Pxruz4rL47KEx0XwExd7m5CmYtCNdu8A71/rVvvY/ld8hwJ4nObLnAcDB27KZV/0L92TA==;EndpointSuffix=core.windows.net";
            CloudStorageAccount StorageAccount = CloudStorageAccount.Parse(connectionString);

            CloudBlobClient BlobClient = StorageAccount.CreateCloudBlobClient();

            CloudBlobContainer Container = BlobClient.GetContainerReference("botfiles");

            CloudBlockBlob blob = Container.GetBlockBlobReference(newFile.FilesId);
            HashSet<string> blocklist = new HashSet<string>();

            var file = formFile;
            const int pageSizeInBytes = 10485760;
            long prevLastByte = 0;
            long bytesRemain = file.Length;

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
            
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer("Server=tcp:exdrive.database.windows.net,1433;Initial Catalog=Exdrive;Persist Security Info=False;User ID=fxxwol;Password=AbCD.123;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
            using (var _context = new ApplicationDbContext(optionsBuilder.Options))
            {

                _context.Files.Add(newFile);

                _context.SaveChanges();
            }

            await blob.PutBlockListAsync(blocklist);
            File.Delete(folderPath + newFile.FilesId);
        }
    }
}
