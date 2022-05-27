using exdrive_web.Controllers;
using exdrive_web.Models;
using JWTAuthentication.Authentication;
using JWTAuthentication.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ExDrive.Tests
{
    public class AuthenticateControllerTests
    {
        [Fact]
        public void Register_UserCredentials_SuccessfulRegister()
        {
            // Arrange
            var expected = "Microsoft.AspNetCore.Mvc.OkObjectResult";
            var expectedCount = 6;

            var userManagerMock = MockUserManager(_users);
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
        public void Login_UserCredentials_SuccessfulLogin()
        {
            // Arrange
            var expected = "Microsoft.AspNetCore.Mvc.OkObjectResult";
            var expectedCount = 6;

            var userManagerMock = MockUserManager(_users);
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

        public static Mock<UserManager<ApplicationUser>> MockUserManager(List<ApplicationUser> ls)
        {
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            var userManager = new Mock<UserManager<ApplicationUser>>(userStore.Object, null, null, null, null, null, null, null, null);
            userManager.Object.UserValidators.Add(new UserValidator<ApplicationUser>());
            userManager.Object.PasswordValidators.Add(new PasswordValidator<ApplicationUser>());

            userManager.Setup(x => x.DeleteAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
            userManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                            .ReturnsAsync(IdentityResult.Success).Callback<ApplicationUser, string>((x, y) => ls.Add(x));
            userManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
            userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).Returns(Task.FromResult(GetApplicationUserExample()));

            return userManager;
        }

        private static ApplicationUser GetApplicationUserExample()
        {
            var applicationUser = new ApplicationUser("username", "email@gmail.com");

            applicationUser.Id = Guid.NewGuid().ToString();

            return applicationUser;
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
