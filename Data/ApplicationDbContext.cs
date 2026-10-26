// File: Data/ApplicationDbContext.cs
using System.Linq.Expressions;
using System.Reflection;
using aractakipsys.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace aractakipsys.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Driver> Drivers { get; set; } = null!;
    public DbSet<Vehicle> Vehicles { get; set; } = null!;
    public DbSet<Assignment> Assignments { get; set; } = null!;
    public DbSet<Maintenance> Maintenances { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<VehicleRequest> VehicleRequests { get; set; } = null!;
    public DbSet<VehicleFaultReport> VehicleFaultReports { get; set; } = null!;
    public DbSet<AssignmentDriver> AssignmentDrivers { get; set; } = null!;
    public DbSet<AssignmentRouteChange> AssignmentRouteChanges { get; set; } = null!;
    public DbSet<DriverLicense> DriverLicenses { get; set; } = null!;
    public DbSet<VehicleDocument> VehicleDocuments { get; set; } = null!;
    public DbSet<VehicleTire> VehicleTires { get; set; } = null!;
    public DbSet<RequestDepartment> RequestDepartments { get; set; } = null!;
    public DbSet<RequestVehicleType> RequestVehicleTypes { get; set; } = null!;
    public DbSet<RoleDelegation> RoleDelegations { get; set; } = null!;
    public DbSet<SystemDefinition> SystemDefinitions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Global Query Filter for BaseAuditableEntity (IsDeleted == false)
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(BaseAuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(BaseAuditableEntity.IsDeleted));
                var condition = Expression.Lambda(Expression.Not(property), parameter);
                builder.Entity(entityType.ClrType).HasQueryFilter(condition);
            }
        }

        builder.Entity<VehicleRequest>()
            .HasOne(r => r.ContactUser)
            .WithMany()
            .HasForeignKey(r => r.ContactUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
