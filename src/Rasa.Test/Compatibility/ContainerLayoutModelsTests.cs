using System;
using System.IO;
using System.Linq;
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
        public void ComposeLongFormBindMountsRetainTheirTargetAndMissingFileProtection()
        {
            var compose = ComposeLayout.Parse("""
                services:
                  auth:
                    command: ["auth"]
                    volumes:
                      - type: bind
                        source: ./rasaauth.db
                        target: /app/auth/rasaauth.db
                        bind:
                          create_host_path: false
                  game:
                    command: ["game"]
                    volumes:
                      - type: bind
                        source: ./rasachar.db
                        target: /app/game/rasachar.db
                        bind:
                          create_host_path: false
                      - ./appsettings.env.json:/app/game/appsettings.env.json
                """);

            var auth = compose.GetService("auth");
            CollectionAssert.AreEqual(new[] { "auth" }, auth.Command.ToArray());
            Assert.IsNull(auth.WorkingDirectory, "Compose does not need working_dir for an entrypoint-driven image.");
            Assert.AreEqual(1, auth.Volumes.Count);
            Assert.AreEqual("bind", auth.Volumes[0].Type);
            Assert.AreEqual("./rasaauth.db", auth.Volumes[0].Source);
            Assert.AreEqual("/app/auth/rasaauth.db", auth.Volumes[0].Destination);
            Assert.IsTrue(auth.Volumes[0].CreateHostPath == false);

            var game = compose.GetService("game");
            CollectionAssert.AreEqual(new[] { "game" }, game.Command.ToArray());
            Assert.AreEqual(2, game.Volumes.Count);
            Assert.AreEqual("/app/game/rasachar.db", game.Volumes[0].Destination);
            Assert.IsTrue(game.Volumes[0].CreateHostPath == false);
            Assert.AreEqual("/app/game/appsettings.env.json", game.Volumes[1].Destination);
            Assert.IsNull(game.Volumes[1].CreateHostPath,
                "Short-form binds use Docker Compose's default create-host-path behavior.");
        }

        [TestMethod]
        public void ComposeRejectsLongFormMountWithoutAbsoluteTarget()
        {
            Assert.ThrowsExactly<InvalidDataException>(() => ComposeLayout.Parse("""
                services:
                  game:
                    volumes:
                      - type: bind
                        source: ./database.db
                        target: database.db
                """));
        }

        [TestMethod]
        public void MultiStageDockerBuildCopiesOnlyThePackagedOutputsIntoRuntimeImage()
        {
            using var repository = new DockerRepositoryFixture();
            repository.Write("Dockerfile", """
                FROM scratch AS build
                WORKDIR /app
                COPY src /app/src
                COPY Rasa.NET.sln /app
                RUN dotnet build --configuration Release
                RUN mkdir -p /out/auth /out/game
                RUN cp -a /app/src/Auth/bin/Release/net10.0/. /out/auth/
                RUN cp -a /app/src/Game/bin/Release/net10.0/. /out/game/
                FROM scratch AS runtime
                WORKDIR /app
                COPY --from=build /out/auth /app/auth
                COPY --from=build /out/game /app/game
                COPY docker-entrypoint.sh /usr/local/bin/docker-entrypoint.sh
                ENTRYPOINT ["/usr/local/bin/docker-entrypoint.sh"]
                """);
            repository.Write("Rasa.NET.sln", Solution(@"src\Auth\Auth.csproj", @"src\Game\Game.csproj"));
            repository.Write("src/Auth/Auth.csproj", Project("Auth", null, "appsettings.json"));
            repository.Write("src/Auth/appsettings.json", "{}");
            repository.Write("src/Game/Game.csproj", Project("Game", null, "appsettings.json"));
            repository.Write("src/Game/appsettings.json", "{}");
            repository.Write("docker-entrypoint.sh", "#!/bin/sh\n");

            var image = DockerImageLayout.Create(repository.Root);

            Assert.IsTrue(image.ContainsFile("/app/auth/Auth.dll"));
            Assert.IsTrue(image.ContainsFile("/app/auth/appsettings.json"));
            Assert.IsTrue(image.ContainsFile("/app/game/Game.dll"));
            Assert.IsTrue(image.ContainsFile("/app/game/appsettings.json"));
            Assert.IsTrue(image.ContainsFile("/usr/local/bin/docker-entrypoint.sh"));
            Assert.IsFalse(image.ContainsFile("/app/src/Auth/bin/Release/net10.0/Auth.dll"),
                "Build-stage files should not leak into the runtime image.");
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
