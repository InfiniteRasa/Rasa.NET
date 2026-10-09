using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Compatibility
{
    [TestClass]
    public class PlatformCompatibilityTests
    {
        [TestMethod]
        public void RepositoryPinsVerifiedDotNetSdk()
        {
            var repositoryRoot = FindRepositoryRoot();
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(repositoryRoot, "global.json")));
            var sdk = document.RootElement.GetProperty("sdk");

            Assert.AreEqual("10.0.401", sdk.GetProperty("version").GetString());
            Assert.AreEqual("disable", sdk.GetProperty("rollForward").GetString());
            Assert.IsFalse(sdk.GetProperty("allowPrerelease").GetBoolean());
        }

        [TestMethod]
        public void EverySolutionProjectTargetsDotNet10()
        {
            var repositoryRoot = FindRepositoryRoot();
            var solution = File.ReadAllText(Path.Combine(repositoryRoot, "Rasa.NET.sln"));
            var projectPaths = solution.Split('\n')
                .Where(line => line.StartsWith("Project(", System.StringComparison.Ordinal))
                .Select(line => line.Split(',')[1].Trim().Trim('"'))
                .Where(path => path.EndsWith(".csproj", System.StringComparison.OrdinalIgnoreCase))
                .ToArray();

            CollectionAssert.AreEquivalent(
                new[]
                {
                    "Rasa.Auth", "Rasa.DBL", "Rasa.Shared", "Rasa.Utils", "Rasa.Game", "Rasa.Test",
                    "Rasa.Communicator", "Rasa.ClientData", "Rasa.Navigation", "Rasa.NavMesh", "Rasa.Missions"
                },
                projectPaths.Select(path =>
                    Path.GetFileNameWithoutExtension(path.Replace('\\', Path.DirectorySeparatorChar))).ToArray(),
                "The solution retains the mission runtime, not the retired mission-pack publishing project.");
            foreach (var projectPath in projectPaths)
            {
                var project = XDocument.Load(Path.Combine(
                    repositoryRoot,
                    projectPath.Replace('\\', Path.DirectorySeparatorChar)));
                Assert.AreEqual("net10.0", ReadProperty(project, "TargetFramework"), projectPath);
            }
        }

        [TestMethod]
        public void NavigationProjectsKeepPr95Dependencies()
        {
            var repositoryRoot = FindRepositoryRoot();
            var game = XDocument.Load(Path.Combine(repositoryRoot, "src", "Rasa.Game", "Rasa.Game.csproj"));
            var navigation = XDocument.Load(Path.Combine(repositoryRoot, "src", "Rasa.Navigation", "Rasa.Navigation.csproj"));
            var navMesh = XDocument.Load(Path.Combine(repositoryRoot, "src", "Rasa.NavMesh", "Rasa.NavMesh.csproj"));

            CollectionAssert.Contains(ReadProjectReferences(game).ToArray(), @"..\Rasa.Navigation\Rasa.Navigation.csproj");
            Assert.AreEqual("2026.3.1", ReadPackageVersion(navigation, "DotRecast.Detour"));
            Assert.AreEqual("2026.3.1", ReadPackageVersion(navMesh, "DotRecast.Recast"));
        }

        [TestMethod]
        public void DockerServicesRunWhereRequiredConfigurationAndAssetsExist()
        {
            var repositoryRoot = FindRepositoryRoot();
            var compose = ComposeLayout.Parse(
                File.ReadAllText(Path.Combine(repositoryRoot, "compose.dev.yml")));
            var image = DockerImageLayout.Create(repositoryRoot);

            AssertServiceLayout(
                repositoryRoot,
                compose.GetService("auth"),
                image,
                "Rasa.Auth",
                "/app/auth",
                "auth",
                "Auth");
            AssertServiceLayout(
                repositoryRoot,
                compose.GetService("game"),
                image,
                "Rasa.Game",
                "/app/game",
                "game",
                "Char",
                "World");

            AssertEntrypoint(repositoryRoot, image);
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Rasa.NET.sln")))
                directory = directory.Parent;

            Assert.IsNotNull(directory, "Could not locate the repository root from the test output directory.");
            return directory.FullName;
        }

        private static string ReadProperty(XDocument project, string name)
        {
            return project.Descendants(name).Single().Value;
        }

        private static IEnumerable<string> ReadProjectReferences(XDocument project)
        {
            return project.Descendants("ProjectReference")
                .Select(reference => reference.Attribute("Include")?.Value)
                .Where(reference => reference != null);
        }

        private static string ReadPackageVersion(XDocument project, string package)
        {
            return project.Descendants("PackageReference")
                .Single(reference => reference.Attribute("Include")?.Value == package)
                .Attribute("Version")?.Value;
        }

        private static void AssertEntrypoint(string repositoryRoot, DockerImageLayout image)
        {
            var dockerfile = DockerfileModel.Parse(File.ReadAllText(Path.Combine(repositoryRoot, "Dockerfile")));
            const string entrypoint = "/usr/local/bin/docker-entrypoint.sh";

            Assert.IsTrue(dockerfile.Instructions.Any(instruction =>
                instruction.Name == "ENTRYPOINT" && instruction.Arguments.Contains(entrypoint, StringComparison.Ordinal)),
                "The runtime image must start through docker-entrypoint.sh.");
            Assert.IsTrue(dockerfile.Instructions.Any(instruction =>
                instruction.Name == "RUN" && instruction.Arguments.Contains("chmod +x " + entrypoint, StringComparison.Ordinal)),
                "The runtime entrypoint must be executable.");
            Assert.IsTrue(image.ContainsFile(entrypoint), "The entrypoint must be present in the runtime image.");

            var script = File.ReadAllText(Path.Combine(repositoryRoot, "docker-entrypoint.sh"));
            StringAssert.Contains(script, "cd /app/auth");
            StringAssert.Contains(script, "exec dotnet Rasa.Auth.dll");
            StringAssert.Contains(script, "cd /app/game");
            StringAssert.Contains(script, "exec dotnet Rasa.Game.dll");
        }

        private static void AssertServiceLayout(
            string repositoryRoot,
            ComposeService service,
            DockerImageLayout image,
            string projectName,
            string runtimeDirectory,
            string entrypointArgument,
            params string[] databaseNames)
        {
            // Compose passes a selector to the Dockerfile ENTRYPOINT; the entrypoint
            // changes into the service's runtime directory and executes its DLL.
            Assert.IsNull(service.WorkingDirectory,
                $"{service.Name}: working_dir comes from the Dockerfile and entrypoint.");
            CollectionAssert.AreEqual(
                new[] { entrypointArgument },
                service.Command.ToArray(),
                service.Name);

            AssertImageFile(image, runtimeDirectory, projectName + ".dll", service.Name);
            AssertImageFile(image, runtimeDirectory, "appsettings.json", service.Name);
            AssertImageFile(image, runtimeDirectory, "databasesettings.json", service.Name);

            using var databaseSettings = JsonDocument.Parse(File.ReadAllText(
                Path.Combine(repositoryRoot, "src", "Rasa.DBL", "databasesettings.json")));
            Assert.AreEqual(
                "Sqlite",
                databaseSettings.RootElement
                    .GetProperty("Databases")
                    .GetProperty("Provider")
                    .GetString());

            foreach (var databaseName in databaseNames)
            {
                var fileName = databaseSettings.RootElement
                    .GetProperty("Databases")
                    .GetProperty(databaseName)
                    .GetProperty("Database")
                    .GetString() + ".db";
                AssertDatabaseVolume(service, runtimeDirectory, fileName);
            }

            if (projectName != "Rasa.Game")
                return;

            AssertVolumeAtDirectory(service, runtimeDirectory, "appsettings.env.json");
            using var appSettings = JsonDocument.Parse(File.ReadAllText(
                Path.Combine(repositoryRoot, "src", "Rasa.Game", "appsettings.json")));
            var gameData = appSettings.RootElement.GetProperty("GameDataConfig");
            var knowledgeBasePath = PosixPath.Resolve(
                runtimeDirectory,
                gameData.GetProperty("KnowledgeBaseFile").GetString());
            var navMeshPath = PosixPath.Resolve(
                runtimeDirectory,
                gameData.GetProperty("NavMeshPath").GetString());
            Assert.IsTrue(image.ContainsFile(knowledgeBasePath),
                $"Game image layout is missing {knowledgeBasePath}.");
            Assert.IsTrue(image.ContainsDirectory(navMeshPath),
                $"Game image layout is missing {navMeshPath}.");
            Assert.IsTrue(image.ContainsFileBelow(navMeshPath, ".nav"),
                $"Game image layout contains no navmesh files below {navMeshPath}.");
        }

        private static void AssertDatabaseVolume(ComposeService service, string runtimeDirectory, string fileName)
        {
            var destination = PosixPath.Resolve(runtimeDirectory, fileName);
            var volume = service.Volumes.SingleOrDefault(mount => mount.Destination == destination);

            Assert.IsNotNull(volume, $"{service.Name} does not mount its database at {destination}.");
            Assert.AreEqual("bind", volume.Type, $"{service.Name}: database must use a bind mount.");
            Assert.AreEqual("./" + fileName, volume.Source, $"{service.Name}: unexpected database source.");
            Assert.IsTrue(volume.CreateHostPath == false,
                $"{service.Name}: a missing {fileName} must not be auto-created as a directory.");
        }

        private static void AssertImageFile(DockerImageLayout image, string runtimeDirectory,
            string relativePath, string serviceName)
        {
            var path = PosixPath.Resolve(runtimeDirectory, relativePath);
            Assert.IsTrue(image.ContainsFile(path),
                $"{serviceName} image layout is missing {path}.");
        }

        private static void AssertVolumeAtDirectory(ComposeService service,
            string runtimeDirectory, string relativePath)
        {
            var destination = PosixPath.Resolve(runtimeDirectory, relativePath);
            CollectionAssert.Contains(
                service.VolumeDestinations.ToArray(),
                destination,
                $"{service.Name} does not mount {destination}.");
        }
    }
}
