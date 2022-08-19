using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using ExDrive.Authentication;
using ExDrive.Helpers;

namespace ExDrive.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticateController : ControllerBase
    {
        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await userManager.FindByNameAsync(model.Email);

            var isAuthenticationAttemptValid = user != null && await userManager.CheckPasswordAsync(user, model.Password);

            if (isAuthenticationAttemptValid == false)
            {
                return Unauthorized();
            }

            var userRoles = await userManager.GetRolesAsync(user!);

            var authenticationClaims = new GenerateAuthenticationClaims()
                .Generate(ClaimTypes.Email, user!.Email);

            foreach (var userRole in userRoles)
            {
                authenticationClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var securityToken = new GenerateSecurityToken()
                .Generate(authenticationClaims, _configuration);

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(securityToken),
                expiration = securityToken.ValidTo
            });
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var applicationUser = new ApplicationUser()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
            };

            var result = await userManager.CreateAsync(applicationUser, model.Password);

            if (result.Succeeded)
            {
                return Ok(new Response { Status = "Success", Message = "User created successfully!" });
            }

            return StatusCode(StatusCodes.Status500InternalServerError, new Response 
            { 
                Status = "Error",
                Message = "User creation failed! Please check user details and try again." 
            });
        }

        public AuthenticateController(UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            this.userManager = userManager;
            _configuration = configuration;
        }

        private readonly UserManager<ApplicationUser> userManager;
        private readonly IConfiguration _configuration;
    }
}