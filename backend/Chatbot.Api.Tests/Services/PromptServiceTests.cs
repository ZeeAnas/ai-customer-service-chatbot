using Chatbot.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Chatbot.Api.Tests.Services;

public class PromptServiceTests
{
    [Fact]
    public void GetSystemPrompt_WhenPromptFileExists_ReturnsPromptContent()
    {
        // Arrange
        var temporaryDirectory = CreateTemporaryDirectory();

        try
        {
            var promptsDirectory = Path.Combine(
                temporaryDirectory,
                "Prompts"
            );

            Directory.CreateDirectory(promptsDirectory);

            var promptFilePath = Path.Combine(
                promptsDirectory,
                "MontanaBarberSystemPrompt.txt"
            );

            const string expectedPrompt =
                "You are the customer-service assistant for Montana Barber.";

            File.WriteAllText(promptFilePath, expectedPrompt);

            var environment = new TestWebHostEnvironment
            {
                ContentRootPath = temporaryDirectory
            };

            var promptService = new PromptService(environment);

            // Act
            var result = promptService.GetSystemPrompt();

            // Assert
            Assert.Equal(expectedPrompt, result);
        }
        finally
        {
            DeleteTemporaryDirectory(temporaryDirectory);
        }
    }

    [Fact]
    public void Constructor_WhenPromptFileDoesNotExist_ThrowsFileNotFoundException()
    {
        // Arrange
        var temporaryDirectory = CreateTemporaryDirectory();

        try
        {
            var environment = new TestWebHostEnvironment
            {
                ContentRootPath = temporaryDirectory
            };

            // Act
            var exception = Assert.Throws<FileNotFoundException>(
                () => new PromptService(environment)
            );

            // Assert
            Assert.Contains(
                "system prompt file was not found",
                exception.Message,
                StringComparison.OrdinalIgnoreCase
            );
        }
        finally
        {
            DeleteTemporaryDirectory(temporaryDirectory);
        }
    }

    [Fact]
    public void Constructor_WhenPromptFileIsEmpty_ThrowsInvalidOperationException()
    {
        // Arrange
        var temporaryDirectory = CreateTemporaryDirectory();

        try
        {
            var promptsDirectory = Path.Combine(
                temporaryDirectory,
                "Prompts"
            );

            Directory.CreateDirectory(promptsDirectory);

            var promptFilePath = Path.Combine(
                promptsDirectory,
                "MontanaBarberSystemPrompt.txt"
            );

            File.WriteAllText(promptFilePath, string.Empty);

            var environment = new TestWebHostEnvironment
            {
                ContentRootPath = temporaryDirectory
            };

            // Act
            var exception = Assert.Throws<InvalidOperationException>(
                () => new PromptService(environment)
            );

            // Assert
            Assert.Contains(
                "system prompt file is empty",
                exception.Message,
                StringComparison.OrdinalIgnoreCase
            );
        }
        finally
        {
            DeleteTemporaryDirectory(temporaryDirectory);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            $"Chatbot.Api.Tests-{Guid.NewGuid()}"
        );

        Directory.CreateDirectory(temporaryDirectory);

        return temporaryDirectory;
    }

    private static void DeleteTemporaryDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Chatbot.Api.Tests";

        public IFileProvider WebRootFileProvider { get; set; } =
            new NullFileProvider();

        public string WebRootPath { get; set; } = string.Empty;

        public string EnvironmentName { get; set; } = "Testing";

        public string ContentRootPath { get; set; } = string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } =
            new NullFileProvider();
    }
}