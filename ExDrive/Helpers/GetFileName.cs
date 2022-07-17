using ExDrive.Models;

namespace ExDrive.Helpers
{
    public class GetFileName
    {
        public string GetName(UserFile file, string path, int position)
        {
            var downloadedFiles = Directory.GetFiles(path).ToList();

            var currentName = Path.Combine(path, file.Name);

            if (downloadedFiles.Contains(currentName))
            {
                return file.NoFormat + $"({position})" +
                                new FindFileFormat().FindFormat(file.Name);
            }
            else
            {
                return file.Name;
            }
        }
    }
}
