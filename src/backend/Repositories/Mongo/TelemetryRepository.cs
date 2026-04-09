namespace Project.Repositories;

public interface ITelemetryRepository
{
    Task AddTelemetryAsync(VehicleTelemetry telemetry);
}

public class TelemetryRepository : ITelemetryRepository
{
    private readonly TelemetryMongoContext _context;
    public TelemetryRepository(TelemetryMongoContext context)
    {
        _context = context;
    }
    public async Task AddTelemetryAsync(VehicleTelemetry telemetry)
    {
        await _context.VehicleTelemetries.InsertOneAsync(telemetry);
    }
}
