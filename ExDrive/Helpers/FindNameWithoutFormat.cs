namespace ExDrive.Helpers
{
    public class FindNameWithoutFormat
    {
        public string FindWithoutFormat(string fileName)
        {
            if (String.IsNullOrWhiteSpace(fileName))
            {
                return String.Empty;
            }

            var name = String.Empty;

            for (var characterPosition = 0;
                characterPosition < fileName.Length && characterPosition < fileName.LastIndexOf('.');
                characterPosition++)
            {
                name += fileName.ElementAt(characterPosition);
            }

            return name;
        }
    }
}
