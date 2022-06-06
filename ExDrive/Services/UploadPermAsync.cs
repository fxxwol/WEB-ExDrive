﻿using System.Text;

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
            CloudStorageAccount StorageAccount = CloudStorageAccount.Parse(ConnectionStrings.GetStorageConnectionString());

            CloudBlobClient BlobClient = StorageAccount.CreateCloudBlobClient();

            CloudBlobContainer Container = BlobClient.GetContainerReference(userId);
            await Container.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Off, 
                                                   new BlobRequestOptions(), new OperationContext());

            CloudBlockBlob blob = Container.GetBlockBlobReference(newFile.FilesId);
            HashSet<string> blocklist = new HashSet<string>();

            MemoryStream ms = new MemoryStream();
            var filems = formFile.MyFile.OpenReadStream();
            await filems.CopyToAsync(ms);

            string fullpath = Path.Combine(_scanningPath, userId);
            System.IO.Directory.CreateDirectory(fullpath);

            filems.Position = 0;
            using (var fileStream = new FileStream(Path.Combine(fullpath, newFile.FilesId),
                                                    FileMode.Create, FileAccess.Write))
            {
                await filems.CopyToAsync(fileStream);
            }

            try
            {
                var scanner = new AntiVirus.Scanner();
                var scanresult = scanner.ScanAndClean(Path.Combine(fullpath, newFile.FilesId));

                var isinfected = scanresult.ToString();

                if (isinfected == "VirusFound")
                {
                    throw new Exception("File may be malicious");
                }
            }
            catch (Exception)
            {
                await filems.DisposeAsync();
                await ms.DisposeAsync();

                return;
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

                await blob.PutBlockAsync(base64BlockId, new MemoryStream(bytesToSend, true), null);

                blocklist.Add(base64BlockId);

            } while (bytesRemain > 0);

            applicationDbContext.Files.Add(newFile);
            applicationDbContext.SaveChanges();

            await blob.PutBlockListAsync(blocklist);
            await filems.DisposeAsync();
            await ms.DisposeAsync();

            if (blob.ExistsAsync().Result == false)
            {
                throw new Exception("Failed at creating the blob specified");
            }

            System.IO.Directory.Delete(fullpath, true);
        }
    }
}