using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Compatibility
{
    [TestClass]
    public class ContainerLayoutModelsTests
    {
        [TestMethod]
        public void DockerCopyExcludesFilesMatchedByDockerignore()
        {
            using var repository = new DockerRepositoryFixture();
            repository.Write(".dockerignore", """
                **/bin
                **/obj
                **/*.env.json
                .git
                .p0-validation
                """);
            repository.Write("Dockerfile", """
                FROM scratch
                WORKDIR /app
                COPY . /app
                """);
            repository.Write("src/App/App.csproj", "<Project />");
            repository.Write("src/App/visible.json", "{}");
            repository.Write("src/App/appsettings.env.json", "{}");
            repository.Write("src/App/bin/Release/net10.0/host-only.dll", "host");
            repository.Write("src/App/obj/project.assets.json", "{}");
            repository.Write(".git/config", "private");
            repository.Write(".p0-validation/marker.txt", "private");

            var image = DockerImageLayout.Create(repository.Root);

            Assert.IsTrue(image.ContainsFile("/app/src/App/App.csproj"));
            Assert.IsTrue(image.ContainsFile("/app/src/App/visible.json"));
            Assert.IsFalse(image.ContainsFile("/app/src/App/appsettings.env.json"));
            Assert.IsFalse(image.ContainsFile("/app/src/App/bin/Release/net10.0/host-only.dll"));
            Assert.IsFalse(image.ContainsFile("/app/src/App/obj/project.assets.json"));
            Assert.IsFalse(image.ContainsFile("/app/.git/config"));
            Assert.IsFalse(image.ContainsFile("/app/.p0-validation/marker.txt"));
        }

        [TestMethod]
        public void SolutionBuildMaterializesOnlySolutionProjectsAndReferencedContent()
        {
            using var repository = new DockerRepositoryFixture();
            repository.Write(".dockerignore", """
                **/bin
                **/obj
                **/*.env.json
                .git
                .p0-validation
                """);
            repository.Write("Dockerfile", """
                FROM scratch
                WORKDIR /app
                COPY . /app
                RUN dotnet build --configuration Release
                """);
            repository.Write("Rasa.NET.sln", Solution(
                @"src\App\App.csproj",
                @"src\Shared\Shared.csproj"));
            repository.Write("src/App/App.csproj", Project(
                "App",
                @"..\Shared\Shared.csproj",
                "appsettings.json"));
            repository.Write("src/App/appsettings.json", "{}");
            repository.Write("src/App/appsettings.env.json", "{}");
            repository.Write("src/App/bin/Release/net10.0/host-only.dll", "host");
            repository.Write("src/Shared/Shared.csproj", Project(
                "Shared",
                null,
                "sharedsettings.json"));
            repository.Write("src/Shared/sharedsettings.json", "{}");
            repository.Write("src/Decoy/Decoy.csproj", Project("Decoy"));
            repository.Write("src/Decoy/bin/Release/net10.0/Decoy.dll", "host");

            var image = DockerImageLayout.Create(repository.Root);

            Assert.IsTrue(image.ContainsFile("/app/src/App/bin/Release/net10.0/App.dll"));
            Assert.IsTrue(image.ContainsFile("/app/src/App/bin/Release/net10.0/appsettings.json"));
            Assert.IsTrue(image.ContainsFile("/app/src/App/bin/Release/net10.0/sharedsettings.json"));
            Assert.IsTrue(image.ContainsFile("/app/src/Shared/bin/Release/net10.0/Shared.dll"));
            Assert.IsFalse(image.ContainsFile("/app/src/App/bin/Release/net10.0/host-only.dll"));
            Assert.IsFalse(image.ContainsFile("/app/src/App/appsettings.env.json"));
            Assert.IsFalse(image.ContainsFile("/app/src/Decoy/bin/Release/net10.0/Decoy.dll"));
        }

        [TestMethod]
        public void ExplicitProjectBuildMaterializesOnlyItsProjectGraph()
        {
            using var repository = new DockerRepositoryFixture();
            repository.Write(".dockerignore", "**/bin\n**/obj\n");
            repository.Write("Dockerfile", """
                FROM scratch
                WORKDIR /app
                COPY src /app/src
                RUN dotnet build src/App/App.csproj -c Release
                """);
            repository.Write("src/App/App.csproj", Project(
                "App",
                @"..\Shared\Shared.csproj"));
            repository.Write("src/Shared/Shared.csproj", Project("Shared"));
            repository.Write("src/Decoy/Decoy.csproj", Project("Decoy"));

            var image = DockerImageLayout.Create(repository.Root);

            Assert.IsTrue(image.ContainsFile("/app/src/App/bin/Release/net10.0/App.dll"));
            Assert.IsTrue(image.ContainsFile("/app/src/Shared/bin/Release/net10.0/Shared.dll"));
            Assert.IsFalse(image.ContainsFile("/app/src/Decoy/bin/Release/net10.0/Decoy.dll"));
        }

        [TestMethod]
        public void AmbiguousBuildTargetsAreRejected()
        {
            using var repository = new DockerRepositoryFixture();
            repository.Write(".dockerignore", string.Empty);
            repository.Write("Dockerfile", """
                FROM scratch
                WORKDIR /app
                COPY . /app
                RUN dotnet build src/App/App.csproj src/Decoy/Decoy.csproj -c Release
                """);
            repository.Write("Rasa.NET.sln", Solution(@"src\App\App.csproj"));
            repository.Write("src/App/App.csproj", Project("App"));
            repository.Write("src/Decoy/Decoy.csproj", Project("Decoy"));

            Assert.ThrowsExactly<InvalidDataException>(
                () => DockerImageLayout.Create(repository.Root));
        }

        [TestMethod]
        public void FinalStageHoldsOnlyWhatItCopiesFromEarlierStages()
        {
            using var repository = new DockerRepositoryFixture();
            repository.Write(".dockerignore", "**/bin\n**/obj\n");
            repository.Write("Dockerfile", """
                FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
                WORKDIR /app
                COPY src /app/src
                RUN dotnet build src/App/App.csproj -c Release
                FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
                WORKDIR /app
                COPY --from=build --chown=app:app /app/src/App/bin/Release/net10.0 /app/src/App/bin/Release/net10.0
                COPY --chown=app:app assets /app/src/App/bin/Release/net10.0/assets
                USER app
                """);
            repository.Write("src/App/App.csproj", Project(
                "App",
                @"..\Shared\Shared.csproj",
                "appsettings.json"));
            repository.Write("src/App/appsettings.json", "{}");
            repository.Write("src/Shared/Shared.csproj", Project("Shared"));
            repository.Write("assets/map.nav", "mesh");

            var stages = DockerImageLayout.CreateStages(repository.Root);
            var image = DockerImageLayout.Create(repository.Root);

            Assert.AreEqual(2, stages.Count);
            Assert.AreEqual("mcr.microsoft.com/dotnet/runtime:10.0", image.BaseImage);
            Assert.AreEqual("app", image.User);
            Assert.AreEqual("root", stages[0].User);
            Assert.AreEqual("mcr.microsoft.com/dotnet/sdk:10.0.401", stages[0].BaseImage);
            Assert.IsTrue(image.ContainsFile("/app/src/App/bin/Release/net10.0/App.dll"));
            Assert.IsTrue(image.ContainsFile("/app/src/App/bin/Release/net10.0/Shared.dll"));
            Assert.IsTrue(image.ContainsFile("/app/src/App/bin/Release/net10.0/appsettings.json"));
            Assert.IsTrue(image.ContainsFile("/app/src/App/bin/Release/net10.0/assets/map.nav"));
            Assert.IsFalse(image.ContainsFile("/app/src/App/App.csproj"));
            Assert.IsFalse(image.ContainsFile("/app/src/Shared/bin/Release/net10.0/Shared.dll"));
            Assert.IsTrue(stages[0].ContainsFile("/app/src/App/App.csproj"));
        }

        [TestMethod]
        public void StagesAreAddressableByIndexAndInheritTheirParentsWorkingDirectory()
        {
            using var repository = new DockerRepositoryFixture();
            repository.Write(".dockerignore", "**/bin\n**/obj\n");
            repository.Write("Dockerfile", """
                FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:10.0.401 AS source
                WORKDIR /app
                COPY src /app/src
                FROM source AS build
                RUN dotnet build src/App/App.csproj -c Release
                FROM mcr.microsoft.com/dotnet/runtime:10.0
                COPY --from=1 /app/src/App/bin/Release/net10.0 /app
                """);
            repository.Write("src/App/App.csproj", Project("App"));

            var stages = DockerImageLayout.CreateStages(repository.Root);

            Assert.AreEqual("/app", stages[1].WorkingDirectory);
            Assert.IsTrue(stages[1].ContainsFile("/app/src/App/bin/Release/net10.0/App.dll"));
            Assert.IsTrue(stages[2].ContainsFile("/app/App.dll"));
            Assert.AreEqual("/", stages[2].WorkingDirectory);
        }

        [TestMethod]
        public void CopyFromTheCurrentStageIsRejected()
        {
            using var repository = new DockerRepositoryFixture();
            repository.Write(".dockerignore", string.Empty);
            repository.Write("Dockerfile", """
                FROM scratch AS build
                WORKDIR /app
                COPY --from=build /app /copy
                """);

            Assert.ThrowsExactly<InvalidDataException>(
                () => DockerImageLayout.Create(repository.Root));
        }

        [TestMethod]
        public void CopyFromAnUnknownStageIsRejected()
        {
            using var repository = new DockerRepositoryFixture();
            repository.Write(".dockerignore", string.Empty);
            repository.Write("Dockerfile", """
                FROM mcr.microsoft.com/dotnet/runtime:10.0
                COPY --from=build /app /app
                """);

            Assert.ThrowsExactly<InvalidDataException>(
                () => DockerImageLayout.Create(repository.Root));
        }

        [TestMethod]
        public void CopyOfAPathMissingFromTheStageIsRejected()
        {
            using var repository = new DockerRepositoryFixture();
            repository.Write(".dockerignore", string.Empty);
            repository.Write("Dockerfile", """
                FROM scratch AS build
                WORKDIR /app
                FROM mcr.microsoft.com/dotnet/runtime:10.0
                COPY --from=build /app/missing /app
                """);

            Assert.ThrowsExactly<InvalidDataException>(
                () => DockerImageLayout.Create(repository.Root));
        }

        [TestMethod]
        [DataRow("COPY --link src /app/src")]
        [DataRow("COPY --parents src /app/src")]
        [DataRow("COPY src other /app/")]
        [DataRow("COPY --chown=app:app src other /app/")]
        public void UnsupportedCopyFormsAreStillRejected(string copy)
        {
            using var repository = new DockerRepositoryFixture();
            repository.Write(".dockerignore", string.Empty);
            repository.Write("Dockerfile", $"""
                FROM scratch
                WORKDIR /app
                {copy}
                """);
            repository.Write("src/App/App.csproj", Project("App"));
            repository.Write("other/file.txt", "x");

            var exception = Assert.ThrowsExactly<InvalidDataException>(
                () => DockerImageLayout.Create(repository.Root));
            StringAssert.StartsWith(exception.Message, "Unsupported Docker COPY instruction");
        }

        private static string Project(
            string assemblyName,
            string projectReference = null,
            string outputContent = null)
        {
            var reference = projectReference == null
                ? string.Empty
                : $"""
                  <ItemGroup>
                    <ProjectReference Include="{projectReference}" />
                  </ItemGroup>
                  """;
            var content = outputContent == null
                ? string.Empty
                : $"""
                  <ItemGroup>
                    <None Update="{outputContent}">
                      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
                    </None>
                  </ItemGroup>
                  """;

            return $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <AssemblyName>{assemblyName}</AssemblyName>
                  </PropertyGroup>
                {reference}
                {content}
                </Project>
                """;
        }

        private static string Solution(params string[] projectPaths)
        {
            var projects = string.Empty;
            for (var index = 0; index < projectPaths.Length; index++)
            {
                var projectGuid = $"{{00000000-0000-0000-0000-{index + 1:000000000000}}}";
                projects +=
                    $"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = " +
                    $"\"Project{index}\", \"{projectPaths[index]}\", \"{projectGuid}\"\n" +
                    "EndProject\n";
            }

            return $"""
                Microsoft Visual Studio Solution File, Format Version 12.00
                {projects}
                """;
        }

        private sealed class DockerRepositoryFixture : IDisposable
        {
            internal string Root { get; }

            internal DockerRepositoryFixture()
            {
                Root = Path.Combine(
                    FindRepositoryRoot(),
                    ".p0-validation",
                    nameof(ContainerLayoutModelsTests),
                    Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Root);
            }

            internal void Write(string relativePath, string contents)
            {
                var path = Path.Combine(
                    Root,
                    relativePath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, contents);
            }

            public void Dispose()
            {
                if (Directory.Exists(Root))
                    Directory.Delete(Root, true);
            }

            private static string FindRepositoryRoot()
            {
                var directory = new DirectoryInfo(AppContext.BaseDirectory);
                while (directory != null &&
                       !File.Exists(Path.Combine(directory.FullName, "Rasa.NET.sln")))
                    directory = directory.Parent;

                Assert.IsNotNull(directory);
                return directory.FullName;
            }
        }
    }
}
