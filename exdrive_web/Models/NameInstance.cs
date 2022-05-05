namespace exdrive_web.Models
{
    public class NameInstance
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public bool IsSelected { get; set; }
        public NameInstance(string name, string id)
        {
            Name = name;
            Id = id;
            IsSelected = false;
        }
    }
}
