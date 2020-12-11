using Microsoft.EntityFrameworkCore;

namespace Rasa.Context
{
    using Structures;

    public class AuthContext : DbContext
    {
        public AuthContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.Level)
                .HasDefaultValue(0);

            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.LastIp)
                .HasDefaultValue("0.0.0.0");

            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.LastServerId)
                .HasDefaultValue(0);

            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.JoinDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.Locked)
                .HasDefaultValue(false);

            modelBuilder.Entity<AuthAccountEntry>()
                .Property(e => e.Validated)
                .HasDefaultValue(false);
        }

        public DbSet<AuthAccountEntry> AuthAccountEntries { get; set; }
    }
}