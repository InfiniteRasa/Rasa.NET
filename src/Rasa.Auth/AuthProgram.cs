using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Rasa
{
    using Database;
    using Misc;

    public class AuthProgram
    {
        public static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).RunConsoleAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return RasaHost.CreateDefaultRasaHost<AuthHostedService>(args)
                .ConfigureServices((hostingContext, services) =>
                {
                    services.AddDbContext<AuthContext>(options =>
                    {
                        options.UseMySql(hostingContext.Configuration.GetSection("DatabaseConnectionString").Value);
                    });
                });
        }
    }
}
