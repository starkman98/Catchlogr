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
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

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

// --- Repositories ---
builder.Services.AddScoped<IFishingTripRepository, FishingTripRepository>();
builder.Services.AddScoped<ICatchRepository, CatchRepository>();

// --- Services ---
builder.Services.AddScoped<IFishingTripService, FishingTripService>();
builder.Services.AddScoped<ICatchService, CatchService>();
builder.Services.AddSingleton<IMoonPhaseService, MoonPhaseService>();

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

app.MapHealthChecks("/health");
app.MapFishingTripEndpoints();
app.MapCatchEndpoints();
app.MapLocationEndpoints();

app.Run();
