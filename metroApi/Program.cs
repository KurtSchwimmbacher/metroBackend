using Microsoft.EntityFrameworkCore;
using metroApi.Data;
using Microsoft.OpenApi.Models;
using Npgsql;

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

// Validate the format of the connection string
try
{
    var npgsqlBuilder = new NpgsqlConnectionStringBuilder(connectionString);
    Console.WriteLine("[DEBUG] Connection string format is valid.");
}
catch (Exception ex)
{
    throw new Exception($"Connection string format validation failed: {ex.Message}");
}

builder.Services.AddControllers();

// Open CORS policy (allow all)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
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
        Version = "v1"
    });
});

var app = builder.Build();

// Run migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use CORS - MUST be early in the pipeline
app.UseCors();

// Add debugging middleware to log requests
app.Use(async (context, next) =>
{
    Console.WriteLine($"[CORS DEBUG] Request from origin: {context.Request.Headers.Origin}");
    Console.WriteLine($"[CORS DEBUG] Request method: {context.Request.Method}");
    Console.WriteLine($"[CORS DEBUG] Request path: {context.Request.Path}");
    await next();
});

app.MapControllers();

app.Run();
