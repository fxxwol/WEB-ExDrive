namespace exdrive_web.Models
{
    public class NameInstance
    {
        public string Name { get; set; }
        public bool IsSelected { get; set; }
        public NameInstance(string name)
        {
            Name = name;
            IsSelected = false;
        }
    }
}
