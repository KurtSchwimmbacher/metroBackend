using Microsoft.EntityFrameworkCore;
using metroApi.Data;
using Microsoft.OpenApi.Models;
using Npgsql;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;

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

// Add controllers and configure JSON options for cycle reference handling
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

Console.WriteLine("[DEBUG] JSON ReferenceHandler: IgnoreCycles applied");

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
    options.UseNpgsql(connectionString)
           .UseLazyLoadingProxies(false)); // or true to enable

// Add HttpClient for EmailJS
builder.Services.AddHttpClient<OrderEmailController>();

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

Console.WriteLine($"[DEBUG] ASPNETCORE_ENVIRONMENT = {app.Environment.EnvironmentName}");

// Development error page
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Production exception handler (redirect, etc.)
    app.UseExceptionHandler("/Error");
    app.UseHsts();

    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseCors(corsPolicyName);

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

// Run EF Core migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.Migrate();
        Console.WriteLine("[DEBUG] Database migration applied successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Database migration failed: {ex.Message}");
        throw;
    }
}

Console.WriteLine("[DEBUG] Final Program.cs is running with IgnoreCycles JSON options");

app.Run();
