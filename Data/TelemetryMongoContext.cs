using MongoDB.Bson;
using MongoDB.Driver;
namespace Project.Data;

public class TelemetryMongoContext
{
    private readonly IMongoDatabase _database;
    public TelemetryMongoContext(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<VehicleTelemetry> VehicleTelemetries => _database.GetCollection<VehicleTelemetry>("VehicleTelemetry");
    public IMongoCollection<SensorDiagnostic> SensorDiagnostics => _database.GetCollection<SensorDiagnostic>("SensorDiagnostic");

    public async Task EnsureIndexesAsync()
    {
        var telemetryIdx = new CreateIndexModel<VehicleTelemetry>(
        Builders<VehicleTelemetry>.IndexKeys
            .Ascending(x => x.VehicleId)
            .Descending(x => x.TimeStamp),
        new CreateIndexOptions { Name = "ix_telemetry_vehicle_timestamp" });

        await VehicleTelemetries.Indexes.CreateOneAsync(telemetryIdx);

        var sensorDiagnosticIdx = new CreateIndexModel<SensorDiagnostic>(
            Builders<SensorDiagnostic>.IndexKeys
            .Ascending(x => x.VehicleId)
            .Descending(x => x.TimeStamp),
            new CreateIndexOptions {Name = "ix_sensor_diagnostic_timestamp"}
        );

        await SensorDiagnostics.Indexes.CreateOneAsync(sensorDiagnosticIdx);

        var telemetryValidator = new BsonDocument
        {
            {
                "$jsonSchema", new BsonDocument
                {
                    { "bsonType", "object" },
                    { "required", new BsonArray { "VehicleId", "TimeStamp", "CurrentSpeed", "RemainingBatteryPercentage" } },
                    {
                        "properties", new BsonDocument
                        {
                            { "VehicleId", new BsonDocument { { "bsonType", "int" }, { "minimum", 1 } } },
                            { "CurrentSpeed", new BsonDocument { { "bsonType", new BsonArray { "double", "int", "long", "decimal" } }, { "minimum", 0 } } },
                            { "RemainingBatteryPercentage", new BsonDocument { { "bsonType", new BsonArray { "double", "int", "long", "decimal" } }, { "minimum", 0 }, { "maximum", 100 } } },
                            { "TimeStamp", new BsonDocument { { "bsonType", "date" } } }
                        }
                    }
                }
            }
        };

        var cmd = new BsonDocument
        {
            { "collMod", "VehicleTelemetry" },
            { "validator", telemetryValidator },
            { "validationLevel", "strict" }
        };

        await _database.RunCommandAsync<BsonDocument>(cmd);

        var sensorDiagnosticValidator = new BsonDocument
        {
            {
                "$jsonSchema", new BsonDocument
                {
                    { "bsonType", "object" },
                    { "required", new BsonArray {"VehicleId", "TimeStamp", "ErrorCode", "SensorType"}},
                    {
                        "properties", new BsonDocument
                        {
                            { "VehicleId", new BsonDocument{ {"bsonType", "int"}, {"minimum", 1}}},
                            { "TimeStamp", new BsonDocument { {"bsonType", "date"}}},
                            { "ErrorCode", new BsonDocument { {"bsonType", new BsonArray {"int"}}}},
                            { "SensorType", new BsonDocument { {"bsonType", new BsonArray {"string"}}}}
                        }
                    }
                }
            }
        };

        cmd = new BsonDocument
        {
            { "collMod", "SensorDiagnostic" },
            { "validator", sensorDiagnosticValidator },
            { "validationLevel", "strict" }
        };

        await _database.RunCommandAsync<BsonDocument>(cmd);


    }
}