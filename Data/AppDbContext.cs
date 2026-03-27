namespace Project.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Passenger> Passengers {get;set;}
    public DbSet<Maintenance> Maintenances {get;set;}
    public DbSet<Payment> Payments {get;set;}
    public DbSet<Ride> Rides {get;set;}
    public DbSet<Vehicle> Vehicles {get;set;}
    public DbSet<Ticket> Tickets {get;set;}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Uniqueness
        modelBuilder.Entity<User>().HasIndex(e => e.Email).IsUnique();
        modelBuilder.Entity<Vehicle>().HasIndex(n => n.VIN).IsUnique();
        modelBuilder.Entity<Vehicle>().HasIndex(n => n.LicencePlate).IsUnique();
        modelBuilder.Entity<Payment>().HasIndex(n => n.TransactionReference).IsUnique();

        // Length and requirement
        modelBuilder.Entity<User>().Property(e => e.Email).HasMaxLength(254).IsRequired();
        modelBuilder.Entity<User>().Property(e => e.PasswordHash).IsRequired();
        modelBuilder.Entity<Passenger>().Property(e => e.Name).HasMaxLength(254).IsRequired();
        modelBuilder.Entity<Passenger>().Property(e => e.HomeAddress).HasMaxLength(300).IsRequired();
        modelBuilder.Entity<Passenger>().Property(e => e.PreferredPaymentMethod).HasMaxLength(100).IsRequired();

        modelBuilder.Entity<Vehicle>().Property(v => v.VIN).HasMaxLength(17).IsRequired();
        modelBuilder.Entity<Vehicle>().Property(v => v.LicencePlate).HasMaxLength(20).IsRequired();
        modelBuilder.Entity<Vehicle>().Property(v => v.Model).HasMaxLength(100).IsRequired();

        modelBuilder.Entity<Ticket>().Property(t => t.Subject).HasMaxLength(200).IsRequired();
        modelBuilder.Entity<Ticket>().Property(t => t.Description).HasMaxLength(2000).IsRequired();
        modelBuilder.Entity<Payment>().Property(p => p.TransactionReference).HasMaxLength(100).IsRequired();

        // Decimal precision
        modelBuilder.Entity<Payment>().Property(p => p.PayAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Maintenance>().Property(c => c.Cost).HasPrecision(18, 2);

        // Foreign keys and delete behavior
        modelBuilder.Entity<Ticket>()
            .HasOne(p => p.Passenger)
            .WithMany(p => p.Tickets)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne(r => r.Ride)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ride>()
            .HasOne(p => p.Passenger)
            .WithMany(r => r.Rides)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ride>()
            .HasOne(c => c.Vehicle)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Maintenance>()
            .HasOne(p => p.Vehicle)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        // Constraints
        modelBuilder.Entity<Vehicle>()
            .ToTable(y => y.HasCheckConstraint(
                "CK_Vehicle_Year",
                "\"Year\" >= 1970 AND \"Year\" <= EXTRACT(YEAR FROM CURRENT_DATE)"));

        modelBuilder.Entity<Passenger>()
            .ToTable(p => p.HasCheckConstraint("CK_Passenger_Points", "\"Points\" >= 0"));

        modelBuilder.Entity<Payment>()
            .ToTable(p => p.HasCheckConstraint("CK_Payment_PayAmount", "\"PayAmount\" >= 0"));

        modelBuilder.Entity<Maintenance>()
            .ToTable(m => m.HasCheckConstraint("CK_Maintenance_Cost", "\"Cost\" >= 0"));

        modelBuilder.Entity<Maintenance>()
            .ToTable(m => m.HasCheckConstraint("CK_Maintenance_NextInspectionMileage", "\"NextInspectionMileaga\" > 0"));

        modelBuilder.Entity<Maintenance>()
            .ToTable(m => m.HasCheckConstraint("CK_Maintenance_ServiceDate", "\"ServiceDate\" <= CURRENT_DATE"));

    }
}

