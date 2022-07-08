using System.ComponentModel.DataAnnotations;

namespace ExDrive.Models
{
    public class Files
    {
        [Required]
        public string FilesId { get; set; }
        public string Name { get; set; }
        public string HasAccess { get; set; }
        [Required]
        public bool IsTemporary { get; set; }
        public bool Favourite { get; set; }
        public Files(string id, string name, string access, bool istemporary)
        {
            FilesId = id;
            Name = name;
            HasAccess = access;
            IsTemporary = istemporary;
        }
        public Files()
        {
            FilesId = String.Empty;
            Name = String.Empty;
            HasAccess = "*";
            IsTemporary = true;
        }

        public Files(ref Files file)
        {
            FilesId = file.FilesId;
            Name = file.Name;
            HasAccess = file.HasAccess;
            IsTemporary = file.IsTemporary;
        }
    }
}
