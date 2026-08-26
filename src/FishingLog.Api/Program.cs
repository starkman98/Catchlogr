using FishingLog.Api.Endpoints;
using FishingLog.Api.Infrastructure;
using FishingLog.Application.Interfaces;
using FishingLog.Application.Services;
using FishingLog.Application.Validators;
using FishingLog.Domain.Interfaces;
using FishingLog.Infrastructure.Persistence;
using FishingLog.Infrastructure.Repositories;
using FishingLog.Infrastructure.Location;
using FishingLog.Infrastructure.Weather;
using FishingLog.Infrastructure.Photos;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using FishingLog.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using FishingLog.Api.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<FishingLogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing connection string.")));

builder.Services.AddAuthorization();

builder.Services
    .AddIdentityApiEndpoints<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        // Keep false while initially building the authentication flow.
        // Enable before production after implementing an email sender.
        options.SignIn.RequireConfirmedEmail = false;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<FishingLogDbContext>();

builder.Services.Configure<BearerTokenOptions>(
    IdentityConstants.BearerScheme,
    options =>
    {
        options.BearerTokenExpiration = TimeSpan.FromMinutes(30);
        options.RefreshTokenExpiration = TimeSpan.FromDays(14);

        //For the first checkpoint, you can leave Identity’s default password policy unchanged. Before production, define the policy
        //intentionally and consider checking passwords against a breached-password list.
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();

// --- Repositories ---
builder.Services.AddScoped<IFishingTripRepository, FishingTripRepository>();
builder.Services.AddScoped<ICatchRepository, CatchRepository>();
builder.Services.AddScoped<ICatchPhotoRepository, CatchPhotoRepository>();

// --- Services ---
builder.Services.AddScoped<IFishingTripService, FishingTripService>();
builder.Services.AddScoped<ICatchService, CatchService>();
builder.Services.AddScoped<IPhotoService, PhotoService>();
builder.Services.AddSingleton<IMoonPhaseService, MoonPhaseService>();
builder.Services.AddSingleton<IPhotoObjectStorage>(_ =>
{
    return new LocalPhotoStorage(Path.Combine(
        builder.Environment.ContentRootPath,
        "private-photos"));
});

// --- Validators (registers all validators in the Application assembly) ---
builder.Services.AddValidatorsFromAssemblyContaining<CreateFishingTripRequestValidator>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
    options.AddPolicy("AllowedConfiguredOrigins", policy =>
        policy.WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<FishingLogDbContext>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOptions<OpenMeteoOptions>()
    .Bind(builder.Configuration.GetSection(
        OpenMeteoOptions.SectionName));

builder.Services.AddOptions<LocationIqOptions>()
    .Bind(builder.Configuration.GetSection(
        LocationIqOptions.SectionName))
    .Validate(
        options => options.BaseUri.IsAbsoluteUri &&
                   options.BaseUri.Scheme == Uri.UriSchemeHttps,
        "LocationIQ base URI must be an absolute HTTPS URI.")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.ApiKey),
        "LocationIQ API key is required.")
    .ValidateOnStart();

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddHttpClient<IWeatherService, OpenMeteoWeatherService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services
    .AddHttpClient<ILocationSearchService, LocationIqLocationSearchService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(10);
    })
    // LocationIQ requires its key in the URL, so request logging is disabled
    // for this client to prevent the secret from being written to logs.
    .RemoveAllLoggers();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowedConfiguredOrigins");
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapFishingTripEndpoints();
app.MapCatchEndpoints();
app.MapLocationEndpoints();
app.MapPhotoEndpoints();
app.MapAuthenticationEndpoints();

app.Run();

/// <summary>
/// Exposes the top-level API entry point to integration-test hosts.
/// </summary>
public partial class Program;
