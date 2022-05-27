using exdrive_web.Controllers;
using exdrive_web.Models;
using JWTAuthentication.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ExDrive.Tests
{
    public class StorageControllerTests
    {
        [Fact]
        public void Foo()
        {
            //private readonly UserManager<ApplicationUser> userManager;
            //private readonly RoleManager<IdentityRole> roleManager;
            //private readonly IConfiguration _configuration;
            //    IUserStore<TUser> store, IOptions< IdentityOptions > optionsAccessor, 
            //    IPasswordHasher<TUser> passwordHasher, IEnumerable< IUserValidator < TUser >> userValidators,
            //    IEnumerable<IPasswordValidator<TUser>> passwordValidators, ILookupNormalizer keyNormalizer, 
            //    IdentityErrorDescriber errors, IServiceProvider services, ILogger<UserManager<TUser>> logger

            var userStoreMock = new Mock<IUserStore<object>>();
            var optionsMock = new Mock<IOptions<IdentityOptions>>();
            var passwordHasherMock = new Mock<IPasswordHasher<object>>();
            var enumerableUserMock = new Mock<IEnumerable<IUserValidator<object>>>();
            var enumerablePasswordMock = new Mock<IEnumerable<IPasswordValidator<object>>>();
            var lookupNormalizerMock = new Mock<ILookupNormalizer>();
            var identityErrorDescriberMock = new Mock<IdentityErrorDescriber>();
            var serviceProviderMock = new Mock<IServiceProvider>();
            var loggerMock = new Mock<ILogger<UserManager<object>>>();

            var roleManagerMock = new Mock<RoleManager<object>>();
            //var userManagerMock = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object, optionsMock.Object, 
            //    passwordHasherMock.Object, enumerableUserMock.Object, enumerablePasswordMock.Object, lookupNormalizerMock.Object,
            //    identityErrorDescriberMock.Object, serviceProviderMock.Object, loggerMock.Object);
            var userManagerMock = new Mock<UserManager<ApplicationUser>>();
        }

    }
}
