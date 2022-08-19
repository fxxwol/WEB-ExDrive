using System.Text.RegularExpressions;

namespace ExDrive.Helpers
{
    public class GetValidName
    {
        public string GetName(string newName, string oldNameWithFormat)
        {
            // This expression selects all the symbols from a string
            // which are valid for a file name

            var pickValidName = new Regex("[^\\\\\\/:\\*\\?\"<>\\|]");

            var validSymbols = pickValidName.Matches(newName)
                            .OfType<Match>()
                            .Select(m => m.Groups[0].Value)
                            .ToArray();

            var cleanName = String.Empty;

            foreach (var symbol in validSymbols)
            {
                cleanName += symbol;
            }

            if (String.IsNullOrWhiteSpace(cleanName))
            {
                return oldNameWithFormat;
            }
            else
            {
                return cleanName + new FindFileFormat().FindFormat(oldNameWithFormat);
            }
        }
    }
}
