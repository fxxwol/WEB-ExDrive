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
            var expected = "Microsoft.AspNetCore.Mvc.OkObjectResult";
            var expectedCount = _users.Count + 1;

            var userManagerMock = MockUserManager(_users);
            userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success).Callback<ApplicationUser, string>((x, y) => _users.Add(x));

            var configurationMock = new Mock<IConfiguration>();

            // Act
            var authenticateController = new AuthenticateController(userManagerMock.Object, configurationMock.Object);

            var result = authenticateController.Register(new RegisterModel("email@gmail.com", "Password_"));

            var actual = result.Result.ToString();
            var actualCount = _users.Count;

            // Assert
            Assert.NotNull(actual);
            Assert.Equal(expected, actual);
            Assert.False(result.IsFaulted);
            Assert.True(result.IsCompleted);
            Assert.True(result.Status == TaskStatus.RanToCompletion);
            Assert.Equal(expectedCount, actualCount);
        }

        [Fact]
        public void Register_InvalidUserCredentials_StatusCode500()
        {
            // Arrange
            var expected = new ObjectResult(null);
            var expectedCount = _users.Count;

            expected.StatusCode = 500;
            expected.Value = new Response { Status = "Error", Message = "User creation failed! Please check user details and try again." };

            var userManagerMock = MockUserManager(_users);
            userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed());

            var configurationMock = new Mock<IConfiguration>();

            // Act
            var authenticateController = new AuthenticateController(userManagerMock.Object, configurationMock.Object);
            
            var result = authenticateController.Register(new RegisterModel("email@gmail.com", "Password_"));

            var actual = result.Result;
            var actualCount = _users.Count;

            // Assert
            Assert.NotNull(actual);
            Assert.Equal(expected.ToString(), actual.ToString());
            Assert.False(result.IsFaulted);
            Assert.True(result.IsCompleted);
            Assert.True(result.Status == TaskStatus.RanToCompletion);
            Assert.Equal(expectedCount, _users.Count);
        }

        [Fact]
        public void Login_ValidUserCredentials_SuccessfulLogin()
        {
            // Arrange
            var expected = "Microsoft.AspNetCore.Mvc.OkObjectResult";

            var userManagerMock = MockUserManager(_users);

            userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>())).Returns(Task.FromResult(GetApplicationUserExample()));
            userManagerMock.Setup(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).Returns(Task.FromResult(true));
            userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>())).Returns(Task.FromResult(GetUserRolesExample()));

            var configurationMock = new Mock<IConfiguration>();

            configurationMock.Setup(x => x[It.IsAny<string>()]).Returns("35490645647470956756090675609");

            // Act
            var authenticateController = new AuthenticateController(userManagerMock.Object, configurationMock.Object);

            var result = authenticateController.Login(GetLoginModelExample());

            var actual = result.Result.ToString();

            // Assert
            Assert.NotNull(actual);
            Assert.Equal(expected, actual);
            Assert.False(result.IsFaulted);
            Assert.True(result.IsCompleted);
            Assert.True(result.Status == TaskStatus.RanToCompletion);
        }

        [Fact]
        public void Login_FaultedJsonWebToken_FaultedResult()
        {
            // Arrange
            var expected = true;

            var userManagerMock = MockUserManager(_users);

            userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>())).Returns(Task.FromResult(GetApplicationUserExample()));
            userManagerMock.Setup(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).Returns(Task.FromResult(true));

            var configurationMock = new Mock<IConfiguration>();

            // Act
            var authenticateController = new AuthenticateController(userManagerMock.Object, configurationMock.Object);

            var result = authenticateController.Login(GetLoginModelExample());
            var actual = result.IsFaulted;

            // Assert
            Assert.Equal(expected, actual);
            Assert.True(result.IsCompleted);
            Assert.False(result.Status == TaskStatus.RanToCompletion);
            Assert.Equal(1, result.Exception.InnerExceptions.Count);
        }

        [Fact]
        public void Login_UnregisteredUser_UnauthorizedResult()
        {
            // Arrange
            var expected = "Microsoft.AspNetCore.Mvc.UnauthorizedResult";

            var userManagerMock = MockUserManager(_users);

            userManagerMock.Setup(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).Returns(Task.FromResult(true));

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
