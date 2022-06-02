using System.ComponentModel.DataAnnotations;

namespace ExDrive.Authentication
{
    public class RegisterModel
    {
        [EmailAddress]
        [Required(ErrorMessage = "Email is required")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }

        public RegisterModel()
        {

        }

        public RegisterModel(string email, string password)
        {
            Email = email;
            Password = password;
        }
    }
}