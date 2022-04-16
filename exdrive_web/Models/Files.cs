using System.ComponentModel.DataAnnotations;

namespace exdrive_web.Models
{
    public class Files
    {
        [Required]
        public string FilesId { get; set; }
        public string Name { get; set; }
        public string HasAccess { get; set; }
        [Required]
        public bool IsTemporary { get; set; }
    }
}
