using DbUp;
using DbUp.Builder;
using DbUp.Engine;
using DbUp.ScriptProviders;
using DbUpTutorial.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace DbUpTutorial.Infrastructure
{
    public class Upgrader
    {
        private readonly IConfigurationRoot _config;
        private readonly AppSettings _appSettings;

        public Upgrader(IConfigurationRoot config, AppSettings appSettings)
        {
            _config = config;
            _appSettings = appSettings;
        }

        public bool Upgrade()
        {
            try
            {
                // setup
                var upgrader = Setup();

                if (upgrader == null)
                {
                    return false;
                }

                // upgrade
                var result = upgrader.PerformUpgrade();

                if (result.Successful)
                {
                    Success();
                }

                return result.Successful;
            }
            catch (Exception ex)
            {
                Error(ex.ToString());
                return false;
            }
        }

        private UpgradeEngine Setup()
        {
            var connectionStringName = "Default";
            var connectionString = _config.GetConnectionString(connectionStringName);

            if (string.IsNullOrEmpty(connectionString))
            {
                Error($"Missing configuration or environment variable 'ConnectionString:{connectionStringName}'");
                return null;
            }

            // by convention scripts are taken from the 'Scripts' folder
            var folderPath = Path.Combine(Environment.CurrentDirectory, "Scripts");

            if (!Directory.Exists(folderPath))
            {
                Error($"Scripts not found at {folderPath}. Ignoring database.");
                return null;
            }

            // by convention migration scripts are taken from the 'Migrations' folder
            var migrationsPath = Path.Combine(folderPath, "Migrations");

            if (!Directory.Exists(migrationsPath))
            {
                Error($"Migrations not found at {migrationsPath}.");
                return null;
            }

            var upgrader = SetupMigrations(migrationsPath, connectionString);

            // by convention programmability scripts are taken from the 'Programmability' folder
            var programmabilityFolderPath = Path.Combine(folderPath, "Programmability");

            if (Directory.Exists(programmabilityFolderPath))
            {
                upgrader = SetupProgrammability(upgrader, programmabilityFolderPath);
            }
            else
            {
                Warning($"Programmability objects not found at {folderPath}.");
            }

            // build upgrader
            return upgrader
                    .WithTransaction()
                    .WithVariable("EnvironmentName", $"'{_appSettings.EnvironmentName}'") // this will be available in scripts as $EnvironmentName$
                    .LogToConsole()
                    .Build();
        }

        private static UpgradeEngineBuilder SetupMigrations(string folderPath, string connectionString)
        {
            return DeployChanges.To
                    .SqlDatabase(connectionString)
                    .WithScriptsFromFileSystem
                    (
                        folderPath,
                        new FileSystemScriptOptions { IncludeSubDirectories = true },
                        new ScriptOptions()
                        {
                            FirstDeploymentAsStartingPoint = false,
                            IncludeSubDirectoryInName = true
                        }
                    );
        }

        private static UpgradeEngineBuilder SetupProgrammability(UpgradeEngineBuilder upgrader, string folderPath)
        {
            return upgrader.WithScriptsFromFileSystem
            (
                folderPath,
                new FileSystemScriptOptions() { IncludeSubDirectories = true },
                new ScriptOptions()
                {
                    RedeployOnChange = true,
                    FirstDeploymentAsStartingPoint = false,
                    IncludeSubDirectoryInName = true
                }
            );
        }

        private static void Error(string message) => WriteToConsole("Error", message, ConsoleColor.Red);

        private static void Success() => WriteToConsole("Success", string.Empty, ConsoleColor.Green);

        private static void Warning(string message) => WriteToConsole("Warning", message, ConsoleColor.Yellow);

        private static void WriteToConsole(string title, string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(title);
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
