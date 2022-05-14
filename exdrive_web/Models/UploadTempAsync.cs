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
        public static async Task UploadFileAsync(UploadInstance formFile, Files newFile)
        {
            CloudStorageAccount StorageAccount = CloudStorageAccount.Parse(ExFunctions.storageConnectionString);

            CloudBlobClient BlobClient = StorageAccount.CreateCloudBlobClient();

            CloudBlobContainer Container = BlobClient.GetContainerReference("botfiles");

            CloudBlockBlob blob = Container.GetBlockBlobReference(newFile.FilesId);
            HashSet<string> blocklist = new HashSet<string>();

            MemoryStream ms = new MemoryStream();
            var filems = formFile.MyFile.OpenReadStream();
            filems.CopyToAsync(ms).Wait();
            filems.Position = 0;

            try
            {
                VirusTotalNet.VirusTotal virusTotal = new(ExFunctions.virusTotalToken);
                virusTotal.UseTLS = true;

                FileReport report = await virusTotal.GetFileReportAsync(virusTotal.ScanFileAsync(filems, newFile.FilesId).Result.Resource);
                if (report.Positives != 0)
                    throw new Exception("File may be malicious");
            }
            catch (Exception ex)
            { }
            
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
            
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(ExFunctions.sqlConnectionString);
            using (var _context = new ApplicationDbContext(optionsBuilder.Options))
            {

                _context.Files.Add(newFile);

                _context.SaveChanges();
            }

            await blob.PutBlockListAsync(blocklist);
            await filems.DisposeAsync();
            await ms.DisposeAsync();
        }
    }
}
