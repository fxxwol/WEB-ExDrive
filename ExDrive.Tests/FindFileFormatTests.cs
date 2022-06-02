#pragma warning disable CS8604

using ExDrive.Helpers;

using Xunit;

namespace ExDrive.Tests
{
    public class FindFileFormatTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData("noformat")]
        [InlineData("noformat.")]
        [InlineData(null)]
        public static void FindFormat_InvalidFileName_ReturnEmptyString(string? fileName)
        {
            // Arrange
            string expected = "";

            // Act
            string actual = FindFileFormat.FindFormat(fileName);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("file.a", ".a")]
        [InlineData("file.cs", ".cs")]
        [InlineData("file.txt", ".txt")]
        [InlineData("file.webm", ".webm")]
        public static void FindFormat_ValidFileName_ReturnFormat(string fileName, string expected)
        {
            // Arrange

            // Act
            string actual = FindFileFormat.FindFormat(fileName);

            // Assert
            Assert.False(String.IsNullOrEmpty(actual));
            Assert.Equal(expected, actual);
        }
    }
}

#pragma warning restore CS8604