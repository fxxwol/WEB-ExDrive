#pragma warning disable CS8604

using ExDrive.Models;

using Xunit;

namespace ExDrive.Tests
{
    public class UserFilesSATests
    {
        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData("userid")]
        [InlineData(null)]
        public static void Return_EnumerableShouldBeEmpty(string? userid)
        {
            // Arrange

            // Act
            IEnumerable<Azure.Storage.Blobs.Models.BlobItem> actual = UserFilesSA.GetUserFilesSA(userid);

            // Assert
            Assert.Empty(actual);
        }
    }
}

#pragma warning restore CS8604
