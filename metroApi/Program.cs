using Microsoft.EntityFrameworkCore;
using metroApi.Data;
using Microsoft.OpenApi.Models;
using Npgsql;  // Add this for NpgsqlConnectionStringBuilder

var builder = WebApplication.CreateBuilder(args);

var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

if (env == "Development")
{
    DotNetEnv.Env.Load(".env");
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

Console.WriteLine($"[DEBUG] Connection string: '{connectionString}'");

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("Connection string not found. Ensure environment variables or appsettings are properly configured.");
}

// Validate the format of the connection string before using it
try
{
    var npgsqlBuilder = new NpgsqlConnectionStringBuilder(connectionString);
    // If this succeeds, the connection string is well formed
    Console.WriteLine("[DEBUG] Connection string format is valid.");
}
catch (Exception ex)
{
    throw new Exception($"Connection string format validation failed: {ex.Message}");
}

builder.Services.AddControllers();

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        // Allow localhost for development
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001", "http://127.0.0.1:3000", "http://127.0.0.1:3001")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
        
        // For production, you can add your frontend domain here
        // policy.WithOrigins("https://your-frontend-domain.com")
        //       .AllowAnyHeader()
        //       .AllowAnyMethod()
        //       .AllowCredentials();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Metro API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowReactApp");

app.MapControllers();

app.Run();
