using Microsoft.Extensions.Configuration;

namespace MyConsoleApp.Settings
{
    public static class AppSettings
    {
        public static DatabaseSettings LoadSettings()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var settings = new DatabaseSettings();
            config.GetSection("Database").Bind(settings);
            return settings;
        }
    }
}