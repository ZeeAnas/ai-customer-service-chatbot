using Chatbot.Api.Configuration;
using Chatbot.Api.Interfaces;
using Chatbot.Api.Services;
using Chatbot.Api.Middleware;

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
builder.Services
    .AddHttpClient<IChatService, ChatService>(client =>
    {
        // This prevents a request from waiting indefinitely when OpenAI or the network does not respond.
        client.Timeout = TimeSpan.FromSeconds(30);
    });

// OpenAPI / Swagger
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Keep disabled for now if HTTPS is not configured locally.
// app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthorization();

app.MapControllers();

app.Run();
