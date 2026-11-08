namespace Catchlogr.Mobile.DevHost;

/// <summary>Defines the local-only development smoke scenario.</summary>
public sealed record DevHostOptions
{
    /// <summary>Gets the API endpoint; only a loopback HTTP or HTTPS origin is accepted.</summary>
    public Uri ApiBaseUri { get; init; } = new("http://127.0.0.1:5001/");

    /// <summary>Gets the name of the server trip created by this run.</summary>
    public string TripName { get; init; } = $"DevHost {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} {Guid.NewGuid():N}";

    /// <summary>Gets whether to attach a fixed synthetic location, enabling server weather enrichment.</summary>
    public bool WithLocation { get; init; }

    /// <summary>Validates settings before any credentials or network calls are used.</summary>
    public void Validate()
    {
        if (!ApiBaseUri.IsAbsoluteUri || !ApiBaseUri.IsLoopback ||
            ApiBaseUri.Scheme is not ("http" or "https") ||
            !string.IsNullOrEmpty(ApiBaseUri.UserInfo) ||
            ApiBaseUri.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(ApiBaseUri.Query) || !string.IsNullOrEmpty(ApiBaseUri.Fragment))
            throw new ArgumentException("The API must be a loopback HTTP/HTTPS origin without credentials, path, query, or fragment.");
        if (string.IsNullOrWhiteSpace(TripName) || TripName.Length > 200)
            throw new ArgumentException("The trip name must contain 1–200 characters.");
    }

    /// <summary>Parses non-secret command-line options, rejecting unknown or repeated options.</summary>
    public static DevHostOptions Parse(string[] args)
    {
        var options = new DevHostOptions();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < args.Length; i++)
        {
            var option = args[i];
            if (!seen.Add(option)) throw new ArgumentException("An option was repeated.");
            switch (option)
            {
                case "--api" when i + 1 < args.Length:
                    if (!Uri.TryCreate(args[++i], UriKind.Absolute, out var uri))
                        throw new ArgumentException("Invalid API origin.");
                    options = options with { ApiBaseUri = uri };
                    break;
                case "--name" when i + 1 < args.Length:
                    options = options with { TripName = args[++i] };
                    break;
                case "--with-location":
                    options = options with { WithLocation = true };
                    break;
                default:
                    throw new ArgumentException("Unknown option or missing value. Use --help for usage.");
            }
        }
        options.Validate();
        return options;
    }
}
