namespace exdrive_web.Models
{
    public class NameInstance
    {
        public string Name { get; set; }
        public string NoFormat { get; set; }
        public string Id { get; set; }
        public bool IsSelected { get; set; }
        public bool IsFavourite { get; set; }

        public NameInstance(string name, string noformat, string id, bool fav)
        {
            Name = name;
            NoFormat = noformat;
            Id = id;
            IsSelected = false;
            IsFavourite = fav;
        }
    }
}
