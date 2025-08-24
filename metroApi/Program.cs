using Microsoft.EntityFrameworkCore;
using metroApi.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add Entity Framework Core with SQL Server (replace with UseSqlite or other provider when ready)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Firebase Authentication (requires Firebase Admin SDK setup)
builder.Services.AddAuthentication("Firebase")
    .AddScheme<AuthenticationSchemeOptions, FirebaseAuthenticationHandler>("Firebase", null);

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

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map controller endpoints
app.MapControllers();

app.Run();

// Placeholder for Firebase Authentication Handler (implement this separately)
public class FirebaseAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public FirebaseAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // TODO: Implement Firebase token validation
        // 1. Extract Bearer token from Authorization header
        // 2. Use FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token)
        // 3. Create ClaimsPrincipal with user ID, email, etc.
        // Example (requires Firebase Admin SDK setup):
        /*
        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        var firebaseUser = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, firebaseUser.Uid),
            new Claim(ClaimTypes.Email, firebaseUser.Email)
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
        */
        return Task.FromResult(AuthenticateResult.NoResult());
    }
}