﻿using ExDrive.Authentication;
using ExDrive.Models;

using AntiVirus;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Text;

namespace ExDrive.Services
{
    public abstract class UploadFile : IAsyncDisposable, IDisposable
    {
        abstract protected Task<CloudBlockBlob> CreateNewBlobAsync(Files newFile, string containerName);

        protected async Task UploadBlobBlockAsync(Files file, string name, long bytesRemain, long prevLastByte = 0)
        {
            var blob = await CreateNewBlobAsync(file, name);

            MemoryStream.Position = 0;

            var bytes = MemoryStream.ToArray();

            var blocklist = new HashSet<string>();

            do
            {
                long bytesToCopy = Math.Min(bytesRemain, _pageSizeInBytes);
                byte[] bytesToSend = new byte[bytesToCopy];

                Array.Copy(bytes, prevLastByte, bytesToSend, 0, bytesToCopy);

                prevLastByte += bytesToCopy;
                bytesRemain -= bytesToCopy;

                string blockId = Guid.NewGuid().ToString();
                string base64BlockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(blockId));

                await blob.PutBlockAsync(base64BlockId, new MemoryStream(bytesToSend, true), null);

                blocklist.Add(base64BlockId);

            } while (bytesRemain > 0);

            await blob.PutBlockListAsync(blocklist);

            if (blob.ExistsAsync().Result == false)
            {
                throw new Exception("Failed at creating the blob specified");
            }
        }
        
        protected async Task CreateFileAsync(string name)
        {
            Directory.CreateDirectory(FullPath);

            using (var fileStream = new FileStream(Path.Combine(FullPath, name),
                                                    FileMode.Create, FileAccess.Write))
            {
                MemoryStream.Position = 0;

                await MemoryStream.CopyToAsync(fileStream);
            }
        }

        protected void ScanFileForViruses(string filePath)
        {
            var scanner = new Scanner();

            var scanResult = scanner.ScanAndClean(filePath);

            if (scanResult == ScanResult.VirusFound)
            {
                throw new Exception("File may be malicious");
            }
        }

        protected async Task AddFileToDatabaseAsync(ApplicationDbContext applicationDbContext, Files file)
        {
            await applicationDbContext.Files!.AddAsync(file);

            await applicationDbContext.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);

            if (MemoryStream != null)
                await MemoryStream.DisposeAsync();

            if (!String.IsNullOrEmpty(FullPath))
                Directory.Delete(FullPath, true);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            if (MemoryStream != null)
                MemoryStream.Dispose();

            if (!String.IsNullOrEmpty(FullPath))
                Directory.Delete(FullPath, true);
        }

        protected MemoryStream MemoryStream { get; set; } = new();

        protected string FullPath { get; set; } = String.Empty;

        protected static readonly long _pageSizeInBytes = 10485760;

        protected static readonly string _scanningPath = "C:\\Users\\Public\\scanning";
    }
}
