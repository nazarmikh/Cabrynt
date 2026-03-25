namespace Project.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Passenger> Passengers {get;set;}
    public DbSet<Maintenance> Maintenances {get;set;}
    public DbSet<Payment> Payments {get;set;}
    public DbSet<Ride> Rides {get;set;}
    public DbSet<Vehicle> Vehicles {get;set;}
    public DbSet<Ticket> Tickets {get;set;}
    public DbSet<VehicleTelemetry> VehicleTelemetries {get;set;}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Uniquness

        modelBuilder.Entity<Passenger>().HasIndex(e => e.Email).IsUnique();
        modelBuilder.Entity<Vehicle>().HasIndex(n => n.VIN).IsUnique();
        modelBuilder.Entity<Vehicle>().HasIndex(n => n.LicencePlate).IsUnique();
        modelBuilder.Entity<Payment>().HasIndex(n => n.TransactionReference).IsUnique();

        // Length and requirement

        modelBuilder.Entity<Passenger>().Property(e => e.Email).HasMaxLength(254).IsRequired();
        modelBuilder.Entity<Vehicle>().Property(v => v.VIN).IsRequired().HasMaxLength(17).IsRequired();
        modelBuilder.Entity<Vehicle>().Property(v => v.LicencePlate).IsRequired().HasMaxLength(20).IsRequired();

        // Decimal Precision

        modelBuilder.Entity<Payment>().Property(p => p.PayAmount).HasPrecision(18,2);
        modelBuilder.Entity<Maintenance>().Property(c => c.Cost).HasPrecision(18,2);

        // Foreign Keys

        modelBuilder.Entity<VehicleTelemetry>().HasOne(v => v.Vehicle);

        // Constrains
;
        modelBuilder.Entity<Vehicle>().ToTable(y => y.HasCheckConstraint("CK_Vehicle_Year", "\"Year\" >= 1990 AND \"Year\" <= EXTRACT(YEAR FROM CURRENT_DATE)"));
    }
}

