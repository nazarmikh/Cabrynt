using Microsoft.Extensions.Options;

namespace Project.Services;

public sealed record TripDurationModelStatus(
    bool IsEnabled,
    bool IsAvailable,
    string State,
    string? ConfiguredVersion);

public interface ITripDurationModelStatusProvider
{
    TripDurationModelStatus GetStatus();
}

public sealed class TripDurationModelStatusProvider : ITripDurationModelStatusProvider
{
    private TripDurationModelStatus _status;

    public TripDurationModelStatusProvider(IOptions<TripDurationModelOptions> options)
    {
        var modelOptions = options.Value;
        _status = modelOptions.Enabled
            ? new TripDurationModelStatus(true, false, "Initializing", modelOptions.ExpectedVersion)
            : new TripDurationModelStatus(false, false, "Disabled", null);
    }

    public TripDurationModelStatus GetStatus() => Volatile.Read(ref _status);

    public void SetReady(string configuredVersion)
    {
        Volatile.Write(ref _status, new TripDurationModelStatus(true, true, "Ready", configuredVersion));
    }

    public void SetUnavailable(string configuredVersion)
    {
        Volatile.Write(ref _status, new TripDurationModelStatus(true, false, "Unavailable", configuredVersion));
    }
}
