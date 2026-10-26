using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Compatibility
{
    [TestClass]
    public class MySqlDockerComposeTests
    {
        [TestMethod]
        public void MysqlStackKeepsItsDatabaseAndMigrationsSeparateFromSqlite()
        {
            var directory = new DirectoryInfo(System.AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Rasa.NET.sln")))
                directory = directory.Parent;
            Assert.IsNotNull(directory, "Could not locate the repository root.");
            var root = directory.FullName;
            var compose = File.ReadAllText(Path.Combine(root, "compose.mysql.yml"));
            var overrideFile = File.ReadAllText(Path.Combine(root, "compose.mysql.dev.yml"));
            var sql = File.ReadAllText(Path.Combine(root, "docker", "mysql", "init.sql"));
            var entrypoint = File.ReadAllText(Path.Combine(root, "docker-entrypoint.sh"));
            var auth = File.ReadAllText(Path.Combine(root, "src", "Rasa.Auth", "AuthProgram.cs"));
            var game = File.ReadAllText(Path.Combine(root, "src", "Rasa.Game", "GameProgram.cs"));

            StringAssert.Contains(compose, "image: mysql:8.4");
            StringAssert.Contains(compose, "mysql_data:/var/lib/mysql");
            StringAssert.Contains(compose, "Databases__Provider: MySql");
            StringAssert.Contains(compose, "Databases__Auth__Host: mysql");
            StringAssert.Contains(compose, "Databases__Char__Host: mysql");
            StringAssert.Contains(compose, "Databases__World__Host: mysql");
            StringAssert.Contains(compose, "Databases__Auth__Password: ${MYSQL_PASSWORD:");
            StringAssert.Contains(compose, "command: [\"auth\", \"--migrate\"]");
            StringAssert.Contains(compose, "command: [\"game\", \"--migrate\"]");
            StringAssert.Contains(compose, "condition: service_healthy");
            StringAssert.Contains(compose, "condition: service_completed_successfully");
            Assert.IsFalse(compose.Contains(".db\n"), "The MySQL stack must not mount SQLite databases.");
            StringAssert.Contains(sql, "CREATE DATABASE IF NOT EXISTS `rasachar`");
            StringAssert.Contains(sql, "CREATE DATABASE IF NOT EXISTS `rasaworld`");
            StringAssert.Contains(sql, "GRANT ALL PRIVILEGES ON `rasachar`.*");
            StringAssert.Contains(sql, "GRANT ALL PRIVILEGES ON `rasaworld`.*");

            foreach (var name in new[] { "auth", "game", "mysql-auth-migrate", "mysql-game-migrate" })
                StringAssert.Contains(overrideFile, "  " + name + ": *local-rasa");
            StringAssert.Contains(overrideFile, "build: .");
            StringAssert.Contains(entrypoint, "exec dotnet Rasa.Auth.dll \"$@\"");
            StringAssert.Contains(entrypoint, "exec dotnet Rasa.Game.dll \"$@\"");
            StringAssert.Contains(auth, "DatabaseMigrationRunner.MigrateMySql");
            StringAssert.Contains(game, "DatabaseMigrationRunner.MigrateMySql");
            StringAssert.Contains(auth, ".AddEnvironmentVariables()");
            StringAssert.Contains(game, ".AddEnvironmentVariables()");
        }
    }
}
