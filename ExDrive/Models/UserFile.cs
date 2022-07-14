namespace ExDrive.Models
{
    public class UserFile
    {
        public string Name { get; set; }
        public string NoFormat { get; set; }
        public string Id { get; set; }
        public bool IsSelected { get; set; }
        public bool IsFavourite { get; set; }

        public UserFile(string name, string noformat, string id, bool fav)
        {
            Name = name;
            NoFormat = noformat;
            Id = id;
            IsSelected = false;
            IsFavourite = fav;
        }
    }
}
