using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ExDrive.Helpers
{
    public class GenerateAuthenticationClaims
    {
        public List<Claim> Generate(string claimType, string claimValue)
        {
            return new List<Claim>
                {
                    new Claim(claimType, claimValue),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };
        }
    }
}
