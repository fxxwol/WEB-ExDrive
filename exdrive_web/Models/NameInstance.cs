namespace exdrive_web.Models
{
    public class NameInstance
    {
        public string Name { get; set; }
        public string NoFormat { get; set; }
        public string Id { get; set; }
        public bool IsSelected { get; set; }
        public NameInstance(string name, string noformat, string id)
        {
            Name = name;
            NoFormat = noformat;
            Id = id;
            IsSelected = false;
        }
    }
}
