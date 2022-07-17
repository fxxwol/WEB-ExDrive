using System.IO.Compression;

namespace ExDrive.Services
{
    public class CreateArchive
    {
        public MemoryStream Create(List<string> files, string path)
        {
            var memoryStream = new MemoryStream();

            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                files.ForEach(file =>
                {
                    archive.CreateEntryFromFile(file, file.Replace(path + "\\", string.Empty));
                });
            }

            return memoryStream;
        }
    }
}
