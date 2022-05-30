#pragma warning disable CS8602

using JWTAuthentication.Authentication;
using JWTAuthentication.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace ExDrive.Tests
{
    public class AuthenticateControllerTests
    {
        [Fact]
        public void Register_ValidUserCredentials_OkResult()
        {
            // Arrange
            var expectedCount = _users.Count + 1;
            var expectedStatusCode = 200;

            var userManagerMock = MockUserManager(_users);
            userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success).Callback<ApplicationUser, string>((x, y) => _users.Add(x));

            var configurationMock = new Mock<IConfiguration>();

            // Act
            var authenticateController = new AuthenticateController(userManagerMock.Object, configurationMock.Object);

            var actual = authenticateController.Register(new RegisterModel("email@gmail.com", "Password_"))
                        .GetAwaiter().GetResult() as OkObjectResult;

            var actualCount = _users.Count;

            // Assert
            Assert.IsType<OkObjectResult>(actual);
            Assert.Equal(expectedStatusCode, actual.StatusCode);
            Assert.Equal(expectedCount, actualCount);
        }

        [Fact]
        public void Register_InvalidUserCredentials_StatusCode500()
        {
            // Arrange
            var expected = 500;
            var expectedCount = _users.Count;

            var userManagerMock = MockUserManager(_users);
            userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed());

            var configurationMock = new Mock<IConfiguration>();

            // Act
            var authenticateController = new AuthenticateController(userManagerMock.Object, configurationMock.Object);
            
            var actual = authenticateController.Register(new RegisterModel("email@gmail.com", "Password_"))
                            .GetAwaiter().GetResult() as ObjectResult;

            var actualCount = _users.Count;
            
            // Assert
            Assert.NotNull(actual);
            Assert.IsType<ObjectResult>(actual);
            Assert.Equal(expected, actual.StatusCode);
            Assert.Equal(expectedCount, actualCount);
        }

        [Fact]
        public void Login_ValidUserCredentials_SuccessfulLogin()
        {
            // Arrange
            var expectedStatusCode = 200;

            var userManagerMock = MockUserManager(_users);

            userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>())).Returns(Task.FromResult(GetApplicationUserExample()));
            userManagerMock.Setup(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).Returns(Task.FromResult(true));
            userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>())).Returns(Task.FromResult(GetUserRolesExample()));

            var configurationMock = new Mock<IConfiguration>();

            configurationMock.Setup(x => x[It.IsAny<string>()]).Returns("35490645647470956756090675609");

            // Act
            var authenticateController = new AuthenticateController(userManagerMock.Object, configurationMock.Object);

            var actual = authenticateController.Login(GetLoginModelExample())
                            .GetAwaiter().GetResult() as OkObjectResult;

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<OkObjectResult>(actual);
            Assert.Equal(expectedStatusCode, actual.StatusCode);
        }

        [Fact]
        public void Login_FaultedJsonWebToken_FaultedResult()
        {
            // Arrange
            var userManagerMock = MockUserManager(_users);

            userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>())).Returns(Task.FromResult(GetApplicationUserExample()));
            userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>())).Returns(Task.FromResult(GetUserRolesExample()));
            userManagerMock.Setup(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).Returns(Task.FromResult(true));

            var configurationMock = new Mock<IConfiguration>();

            // Act
            var authenticateController = new AuthenticateController(userManagerMock.Object, configurationMock.Object);

            var actual = authenticateController.Login(GetLoginModelExample());

            // Assert
            Assert.NotNull(actual);
            Assert.True(actual.IsFaulted);
        }

        [Fact]
        public void Login_UnregisteredUser_UnauthorizedResult()
        {
            // Arrange
            var expectedStatusCode = 401;

            var userManagerMock = MockUserManager(_users);

            userManagerMock.Setup(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).Returns(Task.FromResult(true));

            var configurationMock = new Mock<IConfiguration>();

            // Act
            var authenticateController = new AuthenticateController(userManagerMock.Object, configurationMock.Object);

            var actual = authenticateController.Login(GetLoginModelExample())
                        .GetAwaiter().GetResult() as UnauthorizedResult;

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<UnauthorizedResult>(actual);
            Assert.Equal(expectedStatusCode, actual.StatusCode);
        }

        [Fact]
        public void Login_WrongPassword_UnauthorizedResult()
        {
            // Arrange
            var expected = "Microsoft.AspNetCore.Mvc.UnauthorizedResult";

            var userManagerMock = MockUserManager(_users);
            userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>())).Returns(Task.FromResult(GetApplicationUserExample()));
            userManagerMock.Setup(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).Returns(Task.FromResult(false));

            var configurationMock = new Mock<IConfiguration>();

            // Act
            var authenticateController = new AuthenticateController(userManagerMock.Object, configurationMock.Object);

            var result = authenticateController.Login(GetLoginModelExample());
            var actual = result.Result.ToString();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expected, actual);
            Assert.True(result.IsCompleted);
            Assert.True(result.Status == TaskStatus.RanToCompletion);
        }

        public static Mock<UserManager<ApplicationUser>> MockUserManager(List<ApplicationUser> ls)
        {
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            var userManager = new Mock<UserManager<ApplicationUser>>(userStore.Object, null, null, null, null, null, null, null, null);
            userManager.Object.UserValidators.Add(new UserValidator<ApplicationUser>());

            userManager.Object.PasswordValidators.Add(new PasswordValidator<ApplicationUser>());

            userManager.Setup(x => x.DeleteAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
            userManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

            return userManager;
        }

        private static ApplicationUser GetApplicationUserExample()
        {
            var applicationUser = new ApplicationUser("username", "email@gmail.com");

            applicationUser.Id = Guid.NewGuid().ToString();

            return applicationUser;
        }
        private static IList<string> GetUserRolesExample()
        {
            IList<string> userRoles = new List<string>();

            return userRoles;
        }

        private static LoginModel GetLoginModelExample()
        {
            var loginModel = new LoginModel();

            loginModel.Email = "email@gmail.com";
            loginModel.Password = "Password_";

            return loginModel;
        }

        private List<ApplicationUser> _users = new()
        {
              new ApplicationUser("User1", "user1@bv.com") { Id = "1" },
              new ApplicationUser("User2", "user2@bv.com") { Id = "2" },
              new ApplicationUser("User3", "user3@bv.com") { Id = "3" },
              new ApplicationUser("User4", "user4@bv.com") { Id = "4" },
              new ApplicationUser("User5", "user5@bv.com") { Id = "5" }
        };

    }
}
#pragma warning restore CS8602