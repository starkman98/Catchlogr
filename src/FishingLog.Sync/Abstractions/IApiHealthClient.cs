namespace FishingLog.Sync.Abstractions;

public interface IApiHealthClient
{
    Task<bool> IsHealthyAsync(CancellationToken ct = default);
}
