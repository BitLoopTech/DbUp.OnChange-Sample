using DbUpTutorial.Core;
using DbUpTutorial.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace DbUpTutorial
{
    class Program
    {
        static int Main()
        {
            // configure app settings
            var builder = new ConfigurationBuilder()
                 // get connection string from config file
                 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                 // get connection strings from environment variables
                 .AddEnvironmentVariables(); 

            var config = builder.Build();

            // configure typed app settings
            var settings = new AppSettings();
            config.GetSection("App").Bind(settings);

            // upgrade database
            var upgrader = new Upgrader(config, settings);
            return upgrader.Upgrade() ? 0 : -1;
        }
    }
}
