using exdrive_web.Models;

using Xunit;

namespace ExDrive.Tests
{
    public class ExFunctionsTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData("noformat")]
        [InlineData(null)]
        public static void Return_InvalidFileNameWillGiveEmptyString(string? fileName)
        {
            // Arrange
            string expected = "";

            // Act
            string actual = ExFunctions.FindFormat(fileName);

            // Assert
            Assert.Equal(expected, actual);
        }
    }
}
