using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Pgvector.EntityFrameworkCore;
using SentinelKnowledgebase.Infrastructure.Authentication;
using SentinelKnowledgebase.Infrastructure.Data;
using SentinelKnowledgebase.Infrastructure.Repositories;

namespace SentinelKnowledgebase.Infrastructure;

public static class DependencyInjection
{
    public const string AuthScheme = "SentinelAuth";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.PostConfigure<AuthOptions>(options => options.ApplyDefaults());
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly("SentinelKnowledgebase.Migrations");
                npgsqlOptions.UseVector();
            }));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddSignInManager()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        var authOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
        authOptions.ApplyDefaults();
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.JwtSigningKey));

        services.AddAuthentication(options =>
            {
                options.DefaultScheme = AuthScheme;
                options.DefaultAuthenticateScheme = AuthScheme;
                options.DefaultChallengeScheme = AuthScheme;
                options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
            })
            .AddPolicyScheme(AuthScheme, AuthScheme, options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var authorizationHeader = context.Request.Headers.Authorization.ToString();
                    return authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                        ? JwtBearerDefaults.AuthenticationScheme
                        : IdentityConstants.ApplicationScheme;
                };
            })
            .AddCookie(IdentityConstants.ApplicationScheme, options =>
            {
                options.Cookie.Name = "sentinel.auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.SlidingExpiration = true;
                options.LoginPath = "/login";
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole(AuthRoles.Admin));
        });
        
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IRawCaptureRepository, RawCaptureRepository>();
        services.AddScoped<IProcessedInsightRepository, ProcessedInsightRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IEmbeddingVectorRepository, EmbeddingVectorRepository>();
        services.AddScoped<TokenService>();
        services.AddScoped<IdentityBootstrapper>();
        
        return services;
    }
}
