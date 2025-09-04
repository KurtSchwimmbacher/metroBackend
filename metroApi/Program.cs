using Microsoft.EntityFrameworkCore;
using metroApi.Data;
using Microsoft.OpenApi.Models;
using DotNetEnv; // Add this using for DotNetEnv

var builder = WebApplication.CreateBuilder(args);

// Determine environment
var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

// Load environment-specific .env file explicitly
var envFileName = env switch
{
    "Production" => ".env.prod",
    "Development" => ".env",
    _ => ".env" // default fallback
};

// Load environment variables from the .env file
Env.Load(envFileName);

// Get the PostgreSQL connection string from environment variables
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("Connection string not found. Ensure the .env file is correctly configured and placed in the project root.");
}
// Add services to the container.
builder.Services.AddControllers();

// Add Entity Framework Core with SQL Server (replace with UseSqlite or other provider when ready)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Metro API",
        Version = "v1",
        Description = "A simple ASP.NET Core Web API for Metro Backend",
        Contact = new OpenApiContact
        {
            Name = "Your Name",
            Email = "your.email@example.com"
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Enable Swagger middleware in Development environment
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Metro API v1");
        c.RoutePrefix = "swagger"; // Access Swagger UI at /swagger
    });
}

// Use HTTPS redirection (optional, comment out if not needed)
app.UseHttpsRedirection();

// Map controller endpoints
app.MapControllers();

app.Run();