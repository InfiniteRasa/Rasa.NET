using Microsoft.EntityFrameworkCore;

namespace Rasa.Context
{
    using Structures;

    public class AuthContext : DbContext
    {
        public DbSet<AuthAccountEntry> AuthAccountEntries { get; set; }
    }
}