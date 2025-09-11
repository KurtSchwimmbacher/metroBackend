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

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
    });


// Define a named CORS policy that allows all origins, headers, and methods
const string corsPolicyName = "AllowAll";

builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicyName, policy =>
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

Console.WriteLine($"[DEBUG] ASPNETCORE_ENVIRONMENT = {app.Environment.EnvironmentName}"); //checking environment


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

app.UseRouting();
app.UseCors(corsPolicyName);

if (!app.Environment.IsDevelopment())
{
    //app.UseHttpsRedirection();
}

// Debug logging middleware for CORS requests
app.Use(async (context, next) =>
{
    if (context.Request.Method == HttpMethods.Options)
    {
        Console.WriteLine("[DEBUG] Handling OPTIONS preflight request");
    }
    Console.WriteLine($"[CORS DEBUG] Origin: {context.Request.Headers.Origin}");
    Console.WriteLine($"[CORS DEBUG] Method: {context.Request.Method}");
    Console.WriteLine($"[CORS DEBUG] Path: {context.Request.Path}");
    await next();
});

app.MapControllers();
app.Run();
