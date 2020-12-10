using Microsoft.EntityFrameworkCore.Design;

namespace Rasa.Context
{
    public class DesignTimeAuthContextFactory : IDesignTimeDbContextFactory<AuthContext>
    {
        public AuthContext CreateDbContext(string[] args)
        {
            
        }
    }
}