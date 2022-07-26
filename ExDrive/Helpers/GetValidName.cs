using System.Text.RegularExpressions;

namespace ExDrive.Helpers
{
    public class GetValidName
    {
        public string GetName(string newName, string oldNameWithFormat)
        {
            var expression = new Regex("[^\\\\\\/:\\*\\?\"<>\\|]");

            var result = expression.Matches(newName)
                            .OfType<Match>()
                            .Select(m => m.Groups[0].Value)
                            .ToArray();

            var name = String.Empty;

            foreach (var match in result)
            {
                name += match;
            }

            if (String.IsNullOrWhiteSpace(name))
            {
                return oldNameWithFormat;
            }
            else
            {
                return name + new FindFileFormat().FindFormat(oldNameWithFormat);
            }
        }
    }
}
