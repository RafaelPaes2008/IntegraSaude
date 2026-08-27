using System.Text;
using IntegraSaude.Api.Data;
using IntegraSaude.Api.Models;
using IntegraSaude.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuditService>();

var useInMemory = builder.Configuration.GetValue("Database:UseInMemory", false)
                  || builder.Environment.IsEnvironment("Testing");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (useInMemory)
        options.UseInMemoryDatabase("IntegraSaude");
    else
        options.UseNpgsql(builder.Configuration.GetConnectionString("Default"));
});

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(opt =>
    {
        opt.Password.RequiredLength = 8;
        opt.Password.RequireNonAlphanumeric = false;
        opt.User.RequireUniqueEmail = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "IntegraSaude-chave-academica-min-32-caracteres!";
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("web", p => p
        .WithOrigins(origins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

await SeedData.InitializeAsync(app.Services);

app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

var webRoot = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "IntegraSaude.Web"));
if (Directory.Exists(webRoot))
{
    var provider = new PhysicalFileProvider(webRoot);
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = provider,
        DefaultFileNames = ["index.html"]
    });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = provider, RequestPath = "" });
    app.MapFallback(async ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.StatusCode = 404;
            return;
        }
        ctx.Response.ContentType = "text/html; charset=utf-8";
        await ctx.Response.SendFileAsync(Path.Combine(webRoot, "index.html"));
    });
}

app.Run();

public partial class Program;
