using Chatbot.Api.Configuration;
using Chatbot.Api.Interfaces;
using Chatbot.Api.Middleware;
using Chatbot.Api.Services;
using Chatbot.Api.Data;
using Microsoft.EntityFrameworkCore;
using Resend;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();
//Database
builder.Services.AddDbContext<ChatbotDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found")
    )
);

// OpenAI configuration
builder.Services
    .AddOptions<OpenAiOptions>()
    .Bind(builder.Configuration.GetSection(OpenAiOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.ApiKey),
        "The OpenAI API key is missing."
    )
    .ValidateOnStart();


// Lead notification configuration
builder.Services
    .AddOptions<LeadNotificationOptions>()
    .Bind(
        builder.Configuration.GetSection(
            LeadNotificationOptions.SectionName
        )
    )
    .Validate(
        options =>
            !options.Enabled ||
            (
                !string.IsNullOrWhiteSpace(options.FromEmail) &&
                !string.IsNullOrWhiteSpace(options.FromName) &&
                !string.IsNullOrWhiteSpace(options.RecipientEmail)
            ),
        "Lead notification email settings are required when notifications are enabled."
    )
    .Validate(
    options =>
        !options.Enabled ||
        !string.IsNullOrWhiteSpace(
            builder.Configuration["RESEND_APITOKEN"]
        ),
    "The Resend API token is required when lead notifications are enabled."
)

    .ValidateOnStart();

builder.Services.AddResend(options =>
{
    options.ApiToken =
        builder.Configuration["RESEND_APITOKEN"]
        ?? string.Empty;
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Services

builder.Services.Configure<BusinessHoursOptions>(
    builder.Configuration.GetSection(
        BusinessHoursOptions.SectionName
    )
);
builder.Services.AddSingleton<

    IBusinessHoursService,

    BusinessHoursService

>();

builder.Services.AddSingleton<
    IFallbackService,
    FallbackService
>();

builder.Services.AddSingleton<IPromptService, PromptService>();

builder.Services.AddScoped<IHandoffService, HandoffService>();
builder.Services.AddScoped<
    IConversationService,
    ConversationService
>();
builder.Services.AddScoped<ILeadService, LeadService>();
builder.Services.AddScoped<
    ILeadNotificationService,
    LeadNotificationService
>();

builder.Services
    .AddHttpClient<IChatService, ChatService>(client =>
    {
        // Prevents requests from waiting indefinitely if OpenAI
        // or the network does not respond.
        client.Timeout = TimeSpan.FromSeconds(30);
    });


// OpenAPI / Swagger
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Global exception handling
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Keep disabled while HTTPS is not configured locally.
// app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
