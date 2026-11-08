using System.Net;

namespace Catchlogr.Mobile.DevHost;

/// <summary>Runs the local mobile smoke scenario from the command line.</summary>
internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 1 && args[0] is "--help" or "-h")
        {
            Console.WriteLine("Usage: dotnet run --project src/Catchlogr.Mobile.DevHost -- [--api http://127.0.0.1:5001] [--name trip-name] [--with-location]");
            Console.WriteLine("Creates one server trip using an existing confirmed local account. Local SQLite and memory tokens are removed on exit.");
            Console.WriteLine("Set CATCHLOGR_DEVHOST_EMAIL and CATCHLOGR_DEVHOST_PASSWORD for automation, or answer the prompts. Passwords are never accepted as arguments.");
            return 0;
        }

        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, eventArgs) => { eventArgs.Cancel = true; cancellation.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            var options = DevHostOptions.Parse(args);
            var email = Environment.GetEnvironmentVariable("CATCHLOGR_DEVHOST_EMAIL");
            if (string.IsNullOrWhiteSpace(email))
            {
                Console.Write("Local account email: ");
                email = await Console.In.ReadLineAsync(cancellation.Token);
            }
            var password = Environment.GetEnvironmentVariable("CATCHLOGR_DEVHOST_PASSWORD");
            if (string.IsNullOrEmpty(password)) password = await ReadPasswordAsync(cancellation.Token);
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("An existing local account email and password are required.");
            Guid serverId;
            await using (var session = DevHostSession.Create(options))
            {
                serverId = await DevHostScenario.RunAsync(session, options, email, password, cancellation.Token);
            }
            Console.WriteLine($"Verified trip on the API: {serverId:D}. Local session removed; the server trip remains.");
            return 0;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            Console.Error.WriteLine("DevHost cancelled. A trip may already exist on the API.");
            return 130;
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }
        catch (HttpRequestException exception)
        {
            Console.Error.WriteLine(exception.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest
                ? "Local API login was rejected. Check credentials and email confirmation."
                : $"Local API request failed (HTTP {(exception.StatusCode.HasValue ? ((int)exception.StatusCode).ToString() : "unavailable")}). Check API startup and HTTPS trust. A trip may already exist on the API.");
            return 1;
        }
        catch (Exception exception)
        {
            // Messages/stack traces can contain request bodies, private paths, or credentials.
            Console.Error.WriteLine($"DevHost failed ({exception.GetType().Name}). Sync or cleanup was not verified; a trip may already exist on the API.");
            return 1;
        }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private static async Task<string> ReadPasswordAsync(CancellationToken ct)
    {
        Console.Write("Local account password: ");
        if (Console.IsInputRedirected) return await Console.In.ReadLineAsync(ct) ?? string.Empty;
        var password = new System.Text.StringBuilder();
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0) password.Length--;
            }
            else if (!char.IsControl(key.KeyChar)) password.Append(key.KeyChar);
        }
        Console.WriteLine();
        return password.ToString();
    }
}
