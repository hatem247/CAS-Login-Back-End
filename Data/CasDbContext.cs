using CAS_Login_Back_End.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CAS_Login_Back_End.Data;

/// <summary>
/// Entity Framework Core DbContext for CAS API.
/// Configures entities, relationships, and constraints.
/// </summary>
public class CasDbContext : DbContext
{
    public CasDbContext(DbContextOptions<CasDbContext> options) : base(options)
    {
    }

    public DbSet<Account> Accounts { get; set; }

    public DbSet<Login> Logins { get; set; }

    public DbSet<Role> Roles { get; set; }

    public DbSet<AccountRole> AccountRoles { get; set; }

    public DbSet<BusinessEntity> BusinessEntities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Account configuration
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccountId);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.FullNameEn)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.FullNameAr)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(e => e.Email)
                .IsUnique();
        });

        // Login configuration
        modelBuilder.Entity<Login>(entity =>
        {
            entity.HasKey(e => e.LoginId);

            entity.Property(e => e.AccountId)
                .IsRequired();

            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(1024);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.Account)
                .WithMany(a => a.Logins)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.AccountId);
        });

        // Role configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(128);

            entity.Property(e => e.Description)
                .HasMaxLength(512);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(e => e.Name)
                .IsUnique();
        });

        // BusinessEntity configuration
        modelBuilder.Entity<BusinessEntity>(entity =>
        {
            entity.HasKey(e => e.BusinessEntityId);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.Description)
                .HasMaxLength(512);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(e => e.Name)
                .IsUnique();
        });

        // AccountRole configuration
        modelBuilder.Entity<AccountRole>(entity =>
        {
            entity.HasKey(e => e.AccountRoleId);

            entity.Property(e => e.AccountId)
                .IsRequired();

            entity.Property(e => e.RoleId)
                .IsRequired();

            entity.Property(e => e.BusinessEntityId)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Relationships
            entity.HasOne(e => e.Account)
                .WithMany(a => a.AccountRoles)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Role)
                .WithMany(r => r.AccountRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.BusinessEntity)
                .WithMany(be => be.AccountRoles)
                .HasForeignKey(e => e.BusinessEntityId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint: One role per account per business entity
            entity.HasIndex(e => new { e.AccountId, e.BusinessEntityId })
                .IsUnique();

            entity.HasIndex(e => e.RoleId);
            entity.HasIndex(e => e.BusinessEntityId);
        });
    }
}
