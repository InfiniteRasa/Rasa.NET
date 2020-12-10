using Microsoft.EntityFrameworkCore;

namespace Rasa.Context
{
    using Structures;

    public class AuthContext : DbContext
    {
        public AuthContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.
        }

        public DbSet<AuthAccountEntry> AuthAccountEntries { get; set; }
    }
}