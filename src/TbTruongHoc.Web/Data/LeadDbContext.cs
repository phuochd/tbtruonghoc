using Microsoft.EntityFrameworkCore;
using TbTruongHoc.Web.Models;

namespace TbTruongHoc.Web.Data;

/// <summary>
/// Story 1.4 (FR-3): a second, lightweight EF Core <see cref="DbContext"/>
/// alongside Piranha's own <c>MySqlDb</c> - the first non-Piranha DbContext
/// in this repo. Deliberately isolated from Piranha's own schema/migrations:
/// it owns exactly one table (<see cref="FormSubmission"/>s), managed by its
/// own EF Core Migrations under <c>Data/Migrations</c>, and is registered
/// against the same MariaDB connection string/server version as
/// <c>MySqlDb</c> (see Program.cs) - one database, two independently
/// migrated schemas.
/// </summary>
public class LeadDbContext : DbContext
{
    public DbSet<FormSubmission> FormSubmissions => Set<FormSubmission>();

    public LeadDbContext(DbContextOptions<LeadDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FormSubmission>(entity =>
        {
            entity.ToTable("FormSubmission");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FormType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Phone).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ProductOfInterest).HasMaxLength(200);
            entity.Property(e => e.Message).HasMaxLength(2000);
            entity.Property(e => e.LocationAddress).HasMaxLength(500);
            entity.HasIndex(e => e.SiteId);
        });
    }
}
