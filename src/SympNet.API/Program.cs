using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using NSwag.AspNetCore;
using NSwag.Generation.Processors.Security;
using SympNet.Domain.Entities;
using SympNet.Infrastructure.Data;
using SympNet.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

// ─────────────────────────────────────────────────────────────
//  DATABASE
// ─────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ─────────────────────────────────────────────────────────────
//  SERVICES
// ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ConsultationService>();
// Enregistrer AIAnalysisService
// Enregistrer AIAnalysisService
builder.Services.AddHttpClient<AIAnalysisService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["AIService:Url"] ?? "http://localhost:8000");
    client.Timeout = TimeSpan.FromSeconds(60);
});

// ─────────────────────────────────────────────────────────────
//  JWT AUTHENTICATION
// ─────────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is missing from configuration.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwtKey)),
            // FIX: remove the default 5-minute clock skew so tokens
            // expire exactly when you configured them to
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// ─────────────────────────────────────────────────────────────
//  CORS — read allowed origins from config (not hardcoded)
// ─────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? ["http://localhost:5115", "https://localhost:7115"]; // fallback for dev

builder.Services.AddCors(opt => opt.AddPolicy("BlazorPolicy", p =>
    p.WithOrigins(allowedOrigins)
     .AllowAnyHeader()
     .AllowAnyMethod()));

// ─────────────────────────────────────────────────────────────
//  SWAGGER (NSwag) — dev only
// ─────────────────────────────────────────────────────────────
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenApiDocument(config =>
    {
        config.Title = "SympNet API";
        config.Version = "v1";
        config.AddSecurity("JWT", new OpenApiSecurityScheme
        {
            Type = OpenApiSecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = OpenApiSecurityApiKeyLocation.Header,
            Description = "Enter your JWT token"
        });
        config.OperationProcessors.Add(
            new AspNetCoreOperationSecurityScopeProcessor("JWT"));
    });
}

// ─────────────────────────────────────────────────────────────
//  BUILD
// ─────────────────────────────────────────────────────────────
var app = builder.Build();
// Après builder.Build(), ajoutez :
var emailConfig = builder.Configuration.GetSection("Email");
Console.WriteLine("=== EMAIL CONFIGURATION ===");
Console.WriteLine($"From: {emailConfig["From"]}");
Console.WriteLine($"SmtpHost: {emailConfig["SmtpHost"]}");
Console.WriteLine($"SmtpPort: {emailConfig["SmtpPort"]}");
Console.WriteLine($"Username: {emailConfig["Username"]}");
Console.WriteLine($"Password length: {emailConfig["Password"]?.Length ?? 0}");
Console.WriteLine("===========================");

// ─────────────────────────────────────────────────────────────
//  MIDDLEWARE PIPELINE
//  Order matters: CORS → HTTPS → Auth → Controllers
// ─────────────────────────────────────────────────────────────
app.UseCors("BlazorPolicy");          // must be before Authentication
app.UseHttpsRedirection();            // FIX: was missing

if (app.Environment.IsDevelopment())  // FIX: Swagger only in dev
{
    app.UseOpenApi();
    app.UseSwaggerUi(settings =>
    {
        settings.Path = "/swagger";
        settings.DocumentPath = "/swagger/v1/swagger.json";
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ─────────────────────────────────────────────────────────────
//  DATABASE MIGRATION — dev/staging only, never blindly in prod
// ─────────────────────────────────────────────────────────────
if (!app.Environment.IsProduction())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
        Console.WriteLine("✅ Database migrated successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Migration failed: {ex.Message}");
        throw; // still crash — don't start with a broken DB
    }
}

// ─────────────────────────────────────────────────────────────
//  ADMIN SEEDER — creates default admin if none exists
//  Credentials come from config (appsettings / secrets / env vars)
// ─────────────────────────────────────────────────────────────
await SeedAdminAsync(app);

app.Run();

// ─────────────────────────────────────────────────────────────
//  LOCAL FUNCTION — admin seeder
// ─────────────────────────────────────────────────────────────
static async Task SeedAdminAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var adminEmail = app.Configuration["Admin:Email"];
    var adminPassword = app.Configuration["Admin:Password"];

    if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
    {
        Console.WriteLine("⚠️  Admin:Email or Admin:Password not configured — skipping seed.");
        return;
    }

    if (!db.Users.Any(u => u.Role == "Admin"))
    {
        db.Users.Add(new User
        {
            Email = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            Role = "Admin",
            IsActive = true
        });
        await db.SaveChangesAsync();
        Console.WriteLine($"✅ Admin seeded: {adminEmail}");
    }
}