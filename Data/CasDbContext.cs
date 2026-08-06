using Microsoft.EntityFrameworkCore;
using CAS_Login_Back_End.Data.Entities;

namespace CAS_Login_Back_End.Data
{
    public class CasDbContext : DbContext
    {
        public CasDbContext(DbContextOptions<CasDbContext> options) : base(options) { }

        public DbSet<Account> Accounts { get; set; } = null!;
        public DbSet<Login> Logins { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<AccountRole> AccountRoles { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // The existing SQL Server schema uses singular names for these tables.
            // DbSet names are plural, so configure the physical table names explicitly.
            modelBuilder.Entity<Account>(entity =>
            {
                entity.ToTable("Account", "dbo");
                entity.Property(e => e.FullNameEn).HasColumnName("FullNameEN");
                entity.Property(e => e.FullNameAr).HasColumnName("FullNameAR");
                entity.Property(e => e.CreatedAt).HasColumnName("Created_at");
            });
            modelBuilder.Entity<Login>().ToTable("Login", "dbo");
            modelBuilder.Entity<Role>().ToTable("Roles", "dbo");

            modelBuilder.Entity<InterviewScore>(entity =>
            {
                entity.HasOne(d => d.Account)
                    .WithMany(p => p.InterviewScoreAccounts)
                    .HasForeignKey(d => d.AccountId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(d => d.Interviewer)
                    .WithMany(p => p.InterviewScoreInterviewers)
                    .HasForeignKey(d => d.InterviewerId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<Team>(entity =>
            {
                entity.HasOne(d => d.SupervisorAccount)
                    .WithMany(p => p.TeamSupervisorAccounts)
                    .HasForeignKey(d => d.SupervisorAccountId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(d => d.TeamLeaderAccount)
                    .WithMany(p => p.TeamTeamLeaderAccounts)
                    .HasForeignKey(d => d.TeamLeaderAccountId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<AdmissionProfile>(entity =>
            {
                entity.HasKey(e => e.AccountId);

                entity.HasOne(d => d.Account)
                    .WithOne(p => p.AdmissionProfile)
                    .HasForeignKey<AdmissionProfile>(d => d.AccountId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(d => d.Status)
                    .WithMany()
                    .HasForeignKey(d => d.StatusId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<CapstoneSupervisorExtension>(entity =>
            {
                entity.HasKey(e => e.AccountId);

                entity.HasOne(d => d.Account)
                    .WithOne(p => p.CapstoneSupervisorExtension)
                    .HasForeignKey<CapstoneSupervisorExtension>(d => d.AccountId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(d => d.Status)
                    .WithMany(p => p.CapstoneSupervisorExtensions)
                    .HasForeignKey(d => d.StatusId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<ReviewerSupervisorExtension>(entity =>
            {
                entity.HasKey(e => e.AccountId);

                entity.HasOne(d => d.Account)
                    .WithOne(p => p.ReviewerSupervisorExtension)
                    .HasForeignKey<ReviewerSupervisorExtension>(d => d.AccountId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(d => d.Status)
                    .WithMany(p => p.ReviewerSupervisorExtensions)
                    .HasForeignKey(d => d.StatusId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<SuperAdminExtension>(entity =>
            {
                entity.HasKey(e => e.AccountId);

                entity.HasOne(d => d.Account)
                    .WithOne(p => p.SuperAdminExtension)
                    .HasForeignKey<SuperAdminExtension>(d => d.AccountId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(d => d.Status)
                    .WithMany(p => p.SuperAdminExtensions)
                    .HasForeignKey(d => d.StatusId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<StudentExtension>(entity =>
            {
                entity.HasKey(e => e.AccountId);

                entity.HasOne(d => d.Account)
                    .WithOne(p => p.StudentExtension)
                    .HasForeignKey<StudentExtension>(d => d.AccountId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(d => d.Status)
                    .WithMany(p => p.StudentExtensions)
                    .HasForeignKey(d => d.StatusId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<StudentExamResult>(entity =>
            {
                entity.HasKey(e => e.AccountId);

                entity.HasOne(d => d.Account)
                    .WithOne(p => p.StudentExamResult)
                    .HasForeignKey<StudentExamResult>(d => d.AccountId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

        }
    }
}
