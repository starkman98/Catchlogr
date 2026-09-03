using Catchlogr.Web.Configuration;
using Catchlogr.Web.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddOptions<ApiOptions>()
    .Bind(builder.Configuration.GetSection(ApiOptions.SectionName))
    .Validate(
        options => options.BaseUrl is { IsAbsoluteUri: true } &&
            options.BaseUrl.Scheme == Uri.UriSchemeHttps,
        "Api:BaseUrl must be an absolute HTTPS URL.")
    .Validate(
        options => options.Timeout is > 0 and <= 120,
        "Api:Timeout must be between 1 and 120 seconds.")
    .ValidateOnStart();

builder.Services.AddHttpClient<IIdentityApiClient, IdentityApiClient>(
    (serviceProvider, client) =>
    {
        var options = serviceProvider
            .GetRequiredService<IOptions<ApiOptions>>()
            .Value;
        client.BaseAddress = options.BaseUrl;
        client.Timeout = TimeSpan.FromSeconds(options.Timeout);
    });

var app = builder.Build();

app.Use((context, next) =>
{
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    return next(context);
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
