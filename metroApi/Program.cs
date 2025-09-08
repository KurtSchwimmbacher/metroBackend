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
    options.AddPolicy("OpenCors", policy =>
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

// Use open CORS policy
app.UseCors("OpenCors");

app.UseAuthorization();

app.MapControllers();

app.Run();
