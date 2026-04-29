using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Project.Data;
using Project.Enums;

namespace Project.GraphQL;

public class Query
{
    public async Task<AdminDashboardSummaryGraphQlDto> GetAdminDashboardSummary(
        [Service] AppDbContext dbContext)
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var weekStart = todayStart.AddDays(-6);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var totalUsers = await dbContext.Users.CountAsync(x => x.Role != Role.Vehicle);
        var activeVehicles = await dbContext.Vehicles.CountAsync(x => x.VehicleStatus == VehicleStatus.Active);
        var activeRides = await dbContext.Rides.CountAsync(x => x.RideStatus == RideStatus.InProgress);
        var openTickets = await dbContext.Tickets.CountAsync(x => x.TicketStatus != TicketStatus.Resolved);

        var todayRevenue = await dbContext.Payments
            .Where(x => x.PaymentDate >= todayStart)
            .SumAsync(x => (decimal?)x.PayAmount) ?? 0m;

        var weekRevenue = await dbContext.Payments
            .Where(x => x.PaymentDate >= weekStart)
            .SumAsync(x => (decimal?)x.PayAmount) ?? 0m;

        var monthRevenue = await dbContext.Payments
            .Where(x => x.PaymentDate >= monthStart)
            .SumAsync(x => (decimal?)x.PayAmount) ?? 0m;

        return new AdminDashboardSummaryGraphQlDto(
            totalUsers,
            activeVehicles,
            activeRides,
            openTickets,
            todayRevenue,
            weekRevenue,
            monthRevenue);
    }

    public async Task<List<VehicleGraphQlDto>> GetVehicles(
        [Service] AppDbContext dbContext,
        [Service] TelemetryMongoContext mongoContext)
    {
        var latestTelemetry = await mongoContext.VehicleTelemetries
            .Aggregate()
            .SortByDescending(x => x.TimeStamp)
            .Group(
                x => x.VehicleId,
                g => new LatestTelemetryProjection(
                    g.Key,
                    g.First().Latitude,
                    g.First().Longitude,
                    g.First().CurrentSpeed,
                    g.First().RemainingBatteryPercentage,
                    g.First().HardwareTemperature,
                    g.First().TimeStamp))
            .ToListAsync();

        var telemetryByVehicleId = latestTelemetry.ToDictionary(x => x.VehicleId);

        var vehicles = await dbContext.Vehicles
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .ToListAsync();

        return vehicles.Select(vehicle =>
        {
            telemetryByVehicleId.TryGetValue(vehicle.Id, out var telemetry);

            return new VehicleGraphQlDto(
                vehicle.Id,
                vehicle.VIN,
                vehicle.LicencePlate,
                vehicle.Model,
                vehicle.VehicleType,
                vehicle.VehicleStatus,
                vehicle.Year,
                telemetry?.Battery,
                telemetry?.Latitude,
                telemetry?.Longitude,
                telemetry?.CurrentSpeed,
                telemetry?.HardwareTemperature,
                telemetry?.TimeStamp);
        }).ToList();
    }

    public async Task<List<RideGraphQlDto>> GetRides([Service] AppDbContext dbContext)
    {
        return await dbContext.Rides
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .Include(x => x.PassengerProfile)
            .ThenInclude(x => x.User)
            .OrderByDescending(x => x.RequestTime)
            .Select(x => new RideGraphQlDto(
                x.Id,
                x.DepartureLocation,
                x.DestinationLocation,
                x.Distance,
                x.Duration,
                x.PreferredVehicleType,
                x.EstimatedPrice,
                x.RideStatus,
                x.RequestTime,
                x.Vehicle != null ? x.Vehicle.Id : null,
                x.Vehicle != null ? x.Vehicle.LicencePlate : null,
                x.PassengerProfile.User.Email))
            .ToListAsync();
    }

    public async Task<List<TicketGraphQlDto>> GetTickets([Service] AppDbContext dbContext)
    {
        return await dbContext.Tickets
            .AsNoTracking()
            .Include(x => x.PassengerProfile)
            .ThenInclude(x => x.User)
            .OrderByDescending(x => x.ReportTime)
            .Select(x => new TicketGraphQlDto(
                x.Id,
                x.Subject,
                x.Description,
                x.TicketPriority,
                x.TicketStatus,
                x.ReportTime,
                x.PassengerProfile.UserId,
                x.PassengerProfile.User.Email))
            .ToListAsync();
    }

    public async Task<List<UserGraphQlDto>> GetUsers([Service] AppDbContext dbContext)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Where(x => x.Role != Role.Vehicle)
            .GroupJoin(
                dbContext.PassengerProfiles.AsNoTracking(),
                user => user.Id,
                passenger => passenger.UserId,
                (user, passengers) => new { user, passenger = passengers.FirstOrDefault() })
            .OrderByDescending(x => x.user.AccountCreated)
            .Select(x => new UserGraphQlDto(
                x.user.Id,
                x.passenger != null ? x.passenger.Name : "Admin",
                x.user.Email,
                x.user.Role,
                x.user.AccountCreated,
                x.user.LastLogin,
                x.passenger != null ? x.passenger.Points : 0,
                x.passenger != null ? x.passenger.PreferredPaymentMethod : null))
            .ToListAsync();
    }

    public async Task<List<MaintenanceGraphQlDto>> GetMaintenances(
        [Service] AppDbContext dbContext,
        int? vehicleId = null)
    {
        var query = dbContext.Maintenances
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .AsQueryable();

        if (vehicleId.HasValue)
        {
            query = query.Where(x => x.Vehicle.Id == vehicleId.Value);
        }

        return await query
            .OrderByDescending(x => x.ServiceDate)
            .Select(x => new MaintenanceGraphQlDto(
                x.Id,
                x.Vehicle.Id,
                x.Vehicle.LicencePlate,
                x.ServiceDate,
                x.Description,
                x.TechnicianName,
                x.Cost,
                x.NextInspectionMileage))
            .ToListAsync();
    }

    public async Task<List<TelemetryGraphQlDto>> GetTelemetry(
        [Service] TelemetryMongoContext mongoContext,
        int vehicleId,
        int limit = 20)
    {
        var items = await mongoContext.VehicleTelemetries
            .Find(x => x.VehicleId == vehicleId)
            .SortByDescending(x => x.TimeStamp)
            .Limit(limit)
            .ToListAsync();

        return items.Select(x => new TelemetryGraphQlDto(
            x.Id,
            x.VehicleId,
            x.Latitude,
            x.Longitude,
            x.CurrentSpeed,
            x.RemainingBatteryPercentage,
            x.HardwareTemperature,
            x.TimeStamp)).ToList();
    }

    public async Task<List<SensorDiagnosticGraphQlDto>> GetSensorDiagnostics(
        [Service] TelemetryMongoContext mongoContext,
        int? vehicleId = null,
        int limit = 20)
    {
        var filter = vehicleId.HasValue
            ? Builders<SensorDiagnostic>.Filter.Eq(x => x.VehicleId, vehicleId.Value)
            : Builders<SensorDiagnostic>.Filter.Empty;

        var items = await mongoContext.SensorDiagnostics
            .Find(filter)
            .SortByDescending(x => x.TimeStamp)
            .Limit(limit)
            .ToListAsync();

        return items.Select(x => new SensorDiagnosticGraphQlDto(
            x.Id,
            x.VehicleId,
            x.VehicleTelemetryId,
            x.SensorType,
            x.ErrorCode,
            x.DeviationSeverity,
            x.RawSensorValue,
            x.TimeStamp)).ToList();
    }
}

public record AdminDashboardSummaryGraphQlDto(
    int TotalUsers,
    int ActiveVehicles,
    int ActiveRides,
    int OpenTickets,
    decimal TodayRevenue,
    decimal WeekRevenue,
    decimal MonthRevenue);

public record VehicleGraphQlDto(
    int Id,
    string Vin,
    string LicencePlate,
    string Model,
    VehicleType VehicleType,
    VehicleStatus VehicleStatus,
    int Year,
    double? Battery,
    double? Latitude,
    double? Longitude,
    double? CurrentSpeed,
    double? HardwareTemperature,
    DateTime? LastTelemetryAt);

public record RideGraphQlDto(
    int Id,
    string DepartureLocation,
    string DestinationLocation,
    decimal Distance,
    decimal Duration,
    VehicleType PreferredVehicleType,
    decimal EstimatedPrice,
    RideStatus RideStatus,
    DateTime RequestTime,
    int? VehicleId,
    string? VehicleLicencePlate,
    string PassengerEmail);

public record TicketGraphQlDto(
    int Id,
    string Subject,
    string Description,
    TicketPriority TicketPriority,
    TicketStatus TicketStatus,
    DateTime ReportTime,
    int PassengerUserId,
    string PassengerEmail);

public record UserGraphQlDto(
    int Id,
    string Name,
    string Email,
    Role Role,
    DateTime AccountCreated,
    DateTime LastLogin,
    int Points,
    PaymentMethod? PreferredPaymentMethod);

public record MaintenanceGraphQlDto(
    int Id,
    int VehicleId,
    string VehicleLicencePlate,
    DateTime ServiceDate,
    string Description,
    string TechnicianName,
    decimal Cost,
    int NextInspectionMileage);

public record TelemetryGraphQlDto(
    string Id,
    int VehicleId,
    double Latitude,
    double Longitude,
    double CurrentSpeed,
    double RemainingBatteryPercentage,
    double HardwareTemperature,
    DateTime TimeStamp);

public record SensorDiagnosticGraphQlDto(
    string? Id,
    int VehicleId,
    string? VehicleTelemetryId,
    SensorType SensorType,
    int ErrorCode,
    DeviationSeverity DeviationSeverity,
    string? RawSensorValue,
    DateTime TimeStamp);

internal record LatestTelemetryProjection(
    int VehicleId,
    double Latitude,
    double Longitude,
    double CurrentSpeed,
    double Battery,
    double HardwareTemperature,
    DateTime TimeStamp);
