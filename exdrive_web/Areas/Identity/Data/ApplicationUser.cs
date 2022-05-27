using Microsoft.AspNetCore.Identity;

namespace JWTAuthentication.Authentication
{
    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser()
        {

        }
        public ApplicationUser(string userName, string email)
        {
            UserName = userName;
            Email = email;
        }
    }
}