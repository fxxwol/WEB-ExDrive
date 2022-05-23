namespace exdrive_web.Helpers
{
    public class FindFileFormat
    {
        public static string FindFormat(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) ||
                fileName.LastIndexOf('.') < 0 ||
                fileName.LastIndexOf('.') == fileName.Length - 1)
            {
                return "";
            }

            string format = "";

            for (int i = fileName.LastIndexOf('.'); i < fileName.Length; i++)
            {
                format += fileName.ElementAt(i);
            }

            return format;
        }
    }
}
