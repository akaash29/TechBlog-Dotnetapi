using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using UserRegistration.Application;
using UserRegistration.Application.Common.Interfaces;
using UserRegistration.Hubs;
using UserRegistration.Infrastructure;
using UserRegistration.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// The hub itself (and the IHubContext it needs) lives here in the API
// project, so the notifier implementation does too — Application only
// knows the IRealtimeMessageNotifier abstraction (see that interface for why).
builder.Services.AddSignalR();
builder.Services.AddScoped<IRealtimeMessageNotifier, SignalRMessageNotifier>();

// Lets the Angular dev server (a different origin) call this API with an
// Authorization header. Origins come from config so prod can be locked
// down to the real deployed frontend without a code change.
const string AngularClientCorsPolicy = "AngularClient";
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? ["http://localhost:4200", "https://localhost:4200"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularClientCorsPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Replaces the default 401/403 response handling so authorization failures come back as
// ProblemDetails JSON, consistent with GlobalExceptionHandler's error shape.
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemDetailsAuthorizationMiddlewareResultHandler>();

// Output caching for the read-heavy, publicly-reachable GET endpoints (feed,
// journal, top posts, categories, post detail) — these get hit far more
// often than they change. Short TTLs plus tag-based eviction (see
// OutputCacheInvalidator) keep it from ever showing stale like/comment/view
// counts for longer than the eviction takes to run, which is effectively
// immediate.
builder.Services.AddOutputCache(options =>
{
    // Rarely changes (there's no create/update/delete endpoint for it at
    // all) and isn't personalized — a long TTL is free performance.
    options.AddPolicy("Categories", policy => policy.Expire(TimeSpan.FromMinutes(10)).Tag("categories"));

    // Journal/top-posts listings already filter to published posts at the
    // query level, so nothing here can vary by who's asking.
    options.AddPolicy("BlogPostsPublic", policy => policy.Expire(TimeSpan.FromSeconds(20)).Tag("blogposts"));

    // Feed ("foryou" personalizes by caller), suggested-for-you, and a
    // single post's detail (a draft is only visible to its own author) all
    // depend on who's asking, so the cache key has to include that.
    options.AddPolicy("BlogPostsPersonalized", policy =>
        policy.Expire(TimeSpan.FromSeconds(15)).SetVaryByHeader("Authorization").Tag("blogposts"));
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "User Registration API",
        Version = "v1",
        Description = "A CQRS + Mediator pattern Web API for user registration and management."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter a valid JWT access token (without the 'Bearer ' prefix).",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "User Registration API v1");
    });
}

app.UseHttpsRedirection();

// Must run before auth middleware so preflight (OPTIONS) requests are
// answered without needing to pass authentication.
app.UseCors(AngularClientCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

// Skipped in integration tests (CustomWebApplicationFactory sets this
// environment name): tests for two different scenarios can otherwise hit
// the exact same cached GET URL (e.g. an unfiltered /journal or /top call)
// within the cache's TTL and see each other's stale results — caching is a
// production performance layer, not something the test suite should have
// to route around.
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseOutputCache();
}

app.MapControllers();
app.MapHub<MessagesHub>("/hubs/messages");

app.Run();

// Makes the top-level statements' otherwise-internal generated Program class
// visible to UserRegistration.IntegrationTests, which needs it as the type
// parameter for WebApplicationFactory<Program>.
public partial class Program
{
}
