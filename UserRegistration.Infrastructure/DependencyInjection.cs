using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Application.Common.Settings;
using UserRegistration.Infrastructure.Analytics;
using UserRegistration.Infrastructure.Persistence;
using UserRegistration.Infrastructure.Realtime;
using UserRegistration.Infrastructure.Repositories;
using UserRegistration.Infrastructure.Security;
using UserRegistration.Infrastructure.Storage;

namespace UserRegistration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IBlogPostRepository, BlogPostRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<ICacheInvalidator, Caching.OutputCacheInvalidator>();
        services.AddScoped<IUploadedImageRepository, UploadedImageRepository>();
        services.AddScoped<IPageViewRepository, PageViewRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddSingleton<IUserPresenceTracker, InMemoryUserPresenceTracker>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.Configure<AzureBlobStorageOptions>(configuration.GetSection(AzureBlobStorageOptions.SectionName));
        services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();

        services.AddMemoryCache();
        services.AddHttpClient<IGeoLocationService, IpApiGeoLocationService>(client =>
        {
            // Free tier is HTTP-only; a paid ip-api plan (or another provider
            // entirely) would just mean changing this base address.
            client.BaseAddress = new Uri("http://ip-api.com");
            client.Timeout = TimeSpan.FromSeconds(3);
        });

        services.AddJwtAuthentication(configuration);

        return services;
    }

    private static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Fail fast at startup if the section is missing, but don't use this
        // snapshot for the token validation parameters below (see comment there).
        _ = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException($"Missing '{JwtSettings.SectionName}' configuration section.");

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

        // Bound via IOptions<JwtSettings> instead of reading `configuration`
        // directly here: this callback runs lazily, the first time the JWT
        // handler actually needs its options (after the host is fully built),
        // so it sees the final, fully-merged configuration — the same one
        // JwtTokenService reads from when it signs tokens. Reading
        // `configuration` eagerly at this point (during service registration,
        // before the host is built) previously baked in whatever value was
        // present at that moment, which silently diverged from the signing
        // key used at runtime whenever something appended config sources
        // after registration (e.g. WebApplicationFactory in integration
        // tests) — tokens were signed with one secret and validated against
        // another, so every authenticated request failed with 401.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((options, jwtSettingsOptions) =>
            {
                var jwtSettings = jwtSettingsOptions.Value;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                // Browsers can't attach an Authorization header to a WebSocket
                // upgrade request, so the SignalR JS client instead puts the
                // token on the query string for its own requests — this reads
                // it back out for exactly that case (only for the hub's own
                // path, so every other endpoint still requires a real header).
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/messages"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }
}
