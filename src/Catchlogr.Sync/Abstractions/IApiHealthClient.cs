namespace Catchlogr.Sync.Abstractions;

public interface IApiHealthClient
{
    Task<bool> IsHealthyAsync(CancellationToken ct = default);
}
