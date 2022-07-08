using System.ComponentModel.DataAnnotations;

namespace ExDrive.Authentication
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Email is required")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }
        public LoginModel()
        {
            Email = String.Empty;
            Password = String.Empty;
        }
    }
}