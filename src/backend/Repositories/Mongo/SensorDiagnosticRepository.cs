using MongoDB.Driver;

namespace Project.Repositories;

public interface ISensorDiagnosticRepository
{
    Task<SensorDiagnostic?> GetSensorDiagnosticByIdAsync(string id);
    Task<List<SensorDiagnostic>> ListSensorDiagnosticsAsync();
    Task AddSensorDiagnosticAsync(SensorDiagnostic sensorDiagnostic);
}

public class SensorDiagnosticRepository : ISensorDiagnosticRepository
{
    private readonly TelemetryMongoContext _mongoContext;

    public SensorDiagnosticRepository(TelemetryMongoContext mongoContext)
    {
        _mongoContext = mongoContext;
    }


    public async Task AddSensorDiagnosticAsync(SensorDiagnostic sensorDiagnostic)
    {
        await _mongoContext.SensorDiagnostics.InsertOneAsync(sensorDiagnostic);
    }

    public async Task<SensorDiagnostic?> GetSensorDiagnosticByIdAsync(string id)
    {
        return await _mongoContext.SensorDiagnostics.Find(d => d.Id == id).FirstOrDefaultAsync();
    }

    public async Task<List<SensorDiagnostic>> ListSensorDiagnosticsAsync()
    {
        return await _mongoContext.SensorDiagnostics.Find(_ => true).ToListAsync();
    }
}
