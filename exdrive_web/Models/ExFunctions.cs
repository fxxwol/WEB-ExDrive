namespace exdrive_web.Models
{
    public class ExFunctions
    {
        public static string FindFormat(string filename)
        {
            string format = "";
            for (int i = filename.LastIndexOf('.'); i < filename.Length; i++)
                format += filename.ElementAt(i);

            return format;
        }
    }
}
