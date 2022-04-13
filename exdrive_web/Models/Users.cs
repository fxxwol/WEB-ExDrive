using System.ComponentModel.DataAnnotations;

namespace exdrive_web.Models
{
    public class Users
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
