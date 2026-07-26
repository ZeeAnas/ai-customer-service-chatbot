using Chatbot.Api.Configuration;
using Chatbot.Api.Interfaces;
using Chatbot.Api.Middleware;
using Chatbot.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// OpenAI configuration
builder.Services
    .AddOptions<OpenAiOptions>()
    .Bind(builder.Configuration.GetSection(OpenAiOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.ApiKey),
        "The OpenAI API key is missing."
    )
    .ValidateOnStart();

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
