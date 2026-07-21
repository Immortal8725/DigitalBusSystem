using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using DigitalBusSystem.Data;
using DigitalBusSystem.Models;
using DigitalBusSystem.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<JwtTokenService>();

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
const string jwtPlaceholderKey = "ReplaceThisWithAStrongSecretKeyForJwtTokens123!";
if (string.IsNullOrWhiteSpace(jwtSettings.Key))
{
    throw new InvalidOperationException("JWT key is required. Set Jwt:Key in appsettings.json or Jwt__Key as an environment variable.");
}

if (jwtSettings.Key == jwtPlaceholderKey)
{
    Console.WriteLine("Warning: using default JWT key from appsettings.json. Use Jwt__Key as an environment variable in production.");
}

var keyBytes = Encoding.UTF8.GetBytes(jwtSettings.Key);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        NameClaimType = JwtRegisteredClaimNames.UniqueName,
        RoleClaimType = ClaimTypes.Role
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var principal = context.Principal;
            if (principal == null)
            {
                context.Fail("Invalid token");
                return;
            }

            string? userIdClaim = principal.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
                ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? principal.FindFirstValue(ClaimTypes.Name);
            var jti = principal.FindFirstValue(JwtRegisteredClaimNames.Jti);
            if (!int.TryParse(userIdClaim, out var userId) || string.IsNullOrWhiteSpace(jti))
            {
                Console.WriteLine("Token validation failed: invalid token claims");
                context.Fail("Invalid token claims");
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var session = await db.UserSessions.FirstOrDefaultAsync(s => s.UserId == userId && s.SessionId == jti);
            if (session == null)
            {
                context.Fail("Session invalid or expired");
                return;
            }

            if (session.ExpiresAt <= DateTime.UtcNow)
            {
                context.Fail("Session invalid or expired");
                return;
            }
        },
        OnAuthenticationFailed = context => Task.CompletedTask
    };
});

builder.Services.AddAuthorization();

// Configure EF Core with MySQL (DefaultConnection in appsettings.json or env var ConnectionStrings__DefaultConnection)
var conn = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(conn))
{
    throw new InvalidOperationException("Database connection string 'DefaultConnection' is required. Set ConnectionStrings:DefaultConnection in appsettings.json or ConnectionStrings__DefaultConnection as an environment variable.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(conn, ServerVersion.AutoDetect(conn)));

var app = builder.Build();

// Apply migrations at startup
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    Console.WriteLine("Database migration completed.");

    var bootstrapEmail = builder.Configuration["BootstrapAdmin:Email"];
    var bootstrapPassword = builder.Configuration["BootstrapAdmin:Password"];
    if (!string.IsNullOrWhiteSpace(bootstrapEmail) && !string.IsNullOrWhiteSpace(bootstrapPassword))
    {
        var hasAdmin = db.Users.Any(u => u.IsAdmin);
        if (!hasAdmin)
        {
            var existingUser = db.Users.FirstOrDefault(u => u.Email == bootstrapEmail);
            if (existingUser == null)
            {
                var bootstrapAdmin = new User
                {
                    Name = builder.Configuration["BootstrapAdmin:Name"] ?? "Admin",
                    Email = bootstrapEmail,
                    PhoneNumber = builder.Configuration["BootstrapAdmin:PhoneNumber"] ?? "0000000000",
                    Language = builder.Configuration["BootstrapAdmin:Language"] ?? "en",
                    SecurityQuestion = builder.Configuration["BootstrapAdmin:SecurityQuestion"] ?? "Admin account",
                    SecurityAnswer = builder.Configuration["BootstrapAdmin:SecurityAnswer"] ?? "Admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(bootstrapPassword),
                    IsLoggedIn = false,
                    IsAdmin = true
                };
                db.Users.Add(bootstrapAdmin);
                db.SaveChanges();
                Console.WriteLine("Bootstrap admin account created.");
            }
            else if (!existingUser.IsAdmin)
            {
                existingUser.IsAdmin = true;
                db.SaveChanges();
                Console.WriteLine($"Bootstrap admin account promoted existing user '{bootstrapEmail}' to admin.");
            }
            else
            {
                Console.WriteLine($"Bootstrap admin account already exists for '{bootstrapEmail}'.");
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: could not migrate or connect to database: {ex.Message}");
}

// Enable Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Digital Bus System API v1"));
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();