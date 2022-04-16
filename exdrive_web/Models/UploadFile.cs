﻿using Azure.Storage.Blobs;

namespace exdrive_web.Models
{
    public class UploadFile
    {
        private static string connectionString = "DefaultEndpointsProtocol=https;AccountName=exdrivefiles;AccountKey=zW8bG071a7HbJ4+D5Pxruz4rL47KEx0XwExd7m5CmYtCNdu8A71/rVvvY/ld8hwJ4nObLnAcDB27KZV/0L92TA==;EndpointSuffix=core.windows.net";
        private static string containerName = "botfiles";
        private static string folderPath = @"C:\Users\Home\Documents\Storage\";
        public static void UploadFileUsingPath(string fileName)
        {
            var files = Directory.GetFiles(folderPath, fileName, SearchOption.AllDirectories);
            Guid fileId;
            BlobContainerClient containerClient = new BlobContainerClient(connectionString, containerName);
            foreach (var file in files)
            {
                string format = "";
                string newFile;
                for (int i = file.ToString().LastIndexOf('.'); i < file.Length; i++)
                    format += file.ElementAt(i);

                if (format.Length > 0)
                {
                    Files newUpload = new Files();
                    fileId = Guid.NewGuid();
                    newFile = folderPath + fileId + format;
                    File.Move(file, newFile);

                    newUpload.FilesId = fileId.ToString();
                    newUpload.Name = fileName;
                    newUpload.IsTemporary = true;
                    newUpload.HasAccess = "*";
                    var filePathOverCloud = newFile.Replace(folderPath, string.Empty);
                    using (MemoryStream stream = new MemoryStream(File.ReadAllBytes(newFile)))
                    {
                        //containerClient.UploadBlob(filePathOverCloud, stream);
                        containerClient.UploadBlobAsync(filePathOverCloud, stream).Wait();
                        Console.WriteLine(filePathOverCloud + " uploaded");
                    }
                    File.Delete(newFile);
                }
            }
        }
    }
}
