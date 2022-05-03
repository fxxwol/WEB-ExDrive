namespace exdrive_web.Models
{
    public class FileNamesInstance
    {
        public List<string> FileNames { get; set; }
        public FileNamesInstance(List<string> vs)
        {
            FileNames = vs;
        }
    }
}
