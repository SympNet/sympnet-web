using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using NSwag.AspNetCore;
using NSwag.Generation.Processors.Security;
using SympNet.Infrastructure.Data;
using SympNet.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AuthService>();

// JWT
var jwtKey = builder.Configuration["Jwt:Key"]!;
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// ✅ CORS — permet Blazor sur tous les ports
builder.Services.AddCors(opt => opt.AddPolicy("BlazorPolicy", p =>
    p.WithOrigins(
        "http://localhost:5000",
        "http://localhost:5049",
        "http://localhost:5115",
        "https://localhost:7049",
        "https://localhost:7115"
    )
    .AllowAnyHeader()
    .AllowAnyMethod()));

// NSwag Swagger
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
    config.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("JWT"));
});

var app = builder.Build();

// ✅ CORS doit être AVANT Authentication
app.UseCors("BlazorPolicy");

app.UseOpenApi();
app.UseSwaggerUi(settings =>
{
    settings.Path = "/swagger";
    settings.DocumentPath = "/swagger/v1/swagger.json";
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Auto-migrate
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    Console.WriteLine(" Database ready.");
}
catch (Exception ex)
{
    Console.WriteLine($" Migration failed: {ex.Message}");
    throw;
}

app.Run();