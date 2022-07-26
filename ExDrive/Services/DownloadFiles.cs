using ExDrive.Helpers;
using ExDrive.Models;

namespace ExDrive.Services
{
    public class DownloadFiles
    {
        public async Task DownloadFileToFolderAsync(string fileId, string userId, string fileName, string path)
        {
            using var fileStream = new FileStream(Path.Combine(path, fileName),
                                                          FileMode.Create, FileAccess.Write);

            using var memoryStream = await new DownloadAzureFile().DownloadFileAsync(fileId, userId);

            memoryStream.Position = 0;

            await memoryStream.CopyToAsync(fileStream);
        }

        public async Task DownloadFilesToFolderAsync(List<UserFile> files, string path, string userId)
        {
            Directory.CreateDirectory(path);

            for (var position = 0; position < files.Count; position++)
            {
                var file = files[position];

                if (file.IsSelected == false)
                    continue;

                await DownloadFileToFolderAsync(file.Id, userId, new GetFileName().GetName(file, path, position), path);
            }
        }
    }
}
