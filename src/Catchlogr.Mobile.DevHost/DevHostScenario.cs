using Catchlogr.Mobile.Core.Data;
using Catchlogr.Mobile.Core.Services.Authentication;
using Catchlogr.Mobile.Core.ViewModels;
using Catchlogr.Sync.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Catchlogr.Mobile.DevHost;

/// <summary>Exercises real mobile authentication, local trip creation, sync, and authenticated read-back.</summary>
public static class DevHostScenario
{
    /// <summary>Creates one trip and returns its server GUID only after verifying it locally and remotely.</summary>
    /// <remarks>The trip remains on the API after local session disposal. Use a development account.</remarks>
    public static async Task<Guid> RunAsync(
        DevHostSession session, DevHostOptions options, string email, string password, CancellationToken ct = default)
    {
        options.Validate();
        var services = session.Services;
        var authentication = services.GetRequiredService<IAuthenticationService>();
        var user = await authentication.LoginAsync(email, password, ct);
        await services.GetRequiredService<ILocalDatabase>().ActivateAsync(user.Id, ct);
        ct.ThrowIfCancellationRequested();
        var editor = services.GetRequiredService<AddEditFishingTripViewModel>();
        editor.Name = options.TripName;
        if (options.WithLocation)
        {
            await editor.CaptureCurrentLocationCommand.ExecuteAsync(null);
            if (!editor.IsLocationCaptured) throw new InvalidOperationException("Synthetic location capture failed.");
        }
        ct.ThrowIfCancellationRequested();
        await editor.SaveCommand.ExecuteAsync(null);
        var repository = services.GetRequiredService<IFishingTripLocalRepository>();
        var local = (await repository.GetAllAsync(ct)).Single(trip => trip.Name == options.TripName);
        await services.GetRequiredService<ISyncOrchestrator>().SyncAsync(ct);
        ct.ThrowIfCancellationRequested();
        local = await repository.GetByIdAsync(local.Id, ct)
            ?? throw new InvalidOperationException("The local trip disappeared during sync.");
        if (local.IsDirty || !Guid.TryParse(local.ServerId, out var serverId))
            throw new InvalidOperationException("Sync did not upload the trip. No success was established.");
        var remote = await services.GetRequiredService<IFishingTripApiClient>().GetByIdAsync(serverId, ct);
        if (remote is null || remote.Name != options.TripName)
            throw new InvalidOperationException("The uploaded trip could not be verified through the API.");
        return serverId;
    }
}
