namespace ExDrive.Helpers
{
    public class FindFileFormat
    {
        public string FindFormat(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) ||
                fileName.LastIndexOf('.') < 0 ||
                fileName.LastIndexOf('.') == fileName.Length - 1)
            {
                return String.Empty;
            }

            var format = String.Empty;

            for (var characterPosition = fileName.LastIndexOf('.'); 
                characterPosition < fileName.Length; characterPosition++)
            {
                format += fileName.ElementAt(characterPosition);
            }

            return format;
        }
    }
}
