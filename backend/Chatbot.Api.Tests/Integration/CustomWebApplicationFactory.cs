using Chatbot.Api.Data;
using Chatbot.Api.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Chatbot.Api.Tests.Integration;

public sealed class CustomWebApplicationFactory
    : WebApplicationFactory<Program>,
      IAsyncLifetime
{
    private SqliteConnection? _connection;

    public FakeLeadNotificationService LeadNotificationService
    {
        get;
    } = new();

    protected override void ConfigureWebHost(
        IWebHostBuilder builder
    )
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            "Data Source=:memory:"
        );

        builder.UseSetting(
            "OpenAI:ApiKey",
            "integration-test-api-key"
        );

        builder.UseSetting(
            "LeadNotification:Enabled",
            "false"
        );

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<
                DbContextOptions<ChatbotDbContext>
            >();

            services.RemoveAll<ChatbotDbContext>();
            services.RemoveAll<IChatService>();
            services.RemoveAll<ILeadNotificationService>();

            _connection = new SqliteConnection(
                "Data Source=:memory:"
            );

            _connection.Open();

            services.AddDbContext<ChatbotDbContext>(
                options =>
                {
                    options.UseSqlite(_connection);
                }
            );

            services.AddSingleton<
                IChatService,
                FakeChatService
            >();

            services.AddSingleton<ILeadNotificationService>(
                LeadNotificationService
            );
        });
    }

    public async Task InitializeAsync()
    {
        using var scope =
            Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<ChatbotDbContext>();

        await dbContext.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        await base.DisposeAsync();
    }
}