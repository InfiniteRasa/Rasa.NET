using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Compatibility
{
    [TestClass]
    public class RepositoryPinsTests
    {
        private const string GlobalJson = """
            { "sdk": { "version": "10.0.401", "rollForward": "disable", "allowPrerelease": false } }
            """;

        private const string ToolManifest = """
            { "version": 1, "isRoot": true, "tools": { "dotnet-ef": { "version": "9.0.20", "commands": [ "dotnet-ef" ] } } }
            """;

        [TestMethod]
        public void SdkCheckAcceptsAMultiStageDockerfileOnTheGlobalJsonSdk()
        {
            Assert.AreEqual(0, RepositoryPins.CheckSdk(GlobalJson, Dockerfile("10.0.401", "10.0")).Count);
        }

        [TestMethod]
        public void SdkCheckRejectsADockerfileBuildingWithAnotherSdk()
        {
            var problems = RepositoryPins.CheckSdk(GlobalJson, Dockerfile("10.0.402", "10.0"));

            Assert.AreEqual(1, problems.Count);
            StringAssert.Contains(problems[0], "sdk:10.0.402");
        }

        [TestMethod]
        public void SdkCheckRejectsARuntimeFromAnotherLine()
        {
            var problems = RepositoryPins.CheckSdk(GlobalJson, Dockerfile("10.0.401", "11.0"));

            Assert.AreEqual(1, problems.Count);
            StringAssert.Contains(problems[0], "runtime:11.0");
        }

        [TestMethod]
        public void SdkCheckRejectsAnImageThatShipsTheSdk()
        {
            var problems = RepositoryPins.CheckSdk(
                GlobalJson,
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.401\nRUN dotnet build\n");

            Assert.AreEqual(1, problems.Count);
            StringAssert.Contains(problems[0], "final stage");
        }

        [TestMethod]
        public void SdkCheckReadsImagesPastPlatformFlagsDigestsAndPatchPins()
        {
            var dockerfile = """
                FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0.401@sha256:0123 AS build
                FROM mcr.microsoft.com/dotnet/runtime:10.0.12 AS runtime
                """;

            Assert.AreEqual(0, RepositoryPins.CheckSdk(GlobalJson, dockerfile).Count);
            Assert.AreEqual(1, RepositoryPins.CheckSdk(GlobalJson, dockerfile.Replace("10.0.12", "10.01")).Count);
        }

        [TestMethod]
        public void PackageCheckAcceptsMatchingVersions()
        {
            Assert.AreEqual(0, RepositoryPins.CheckPackages(Packages(), ToolManifest).Count);
        }

        [TestMethod]
        public void PackageCheckRejectsDotRecastPackagesThatDisagree()
        {
            var problems = RepositoryPins.CheckPackages(Packages(recast: "2026.4.0"), ToolManifest);

            Assert.AreEqual(1, problems.Count);
            StringAssert.Contains(problems[0], "DotRecast.Recast 2026.4.0");
        }

        [TestMethod]
        public void PackageCheckRejectsEfCorePackagesThatDisagree()
        {
            var problems = RepositoryPins.CheckPackages(Packages(efSqlite: "9.0.21"), ToolManifest);

            Assert.AreEqual(1, problems.Count);
            StringAssert.Contains(problems[0], "Microsoft.EntityFrameworkCore.Sqlite 9.0.21");
        }

        [TestMethod]
        public void PackageCheckRejectsADotnetEfToolOnAnotherVersion()
        {
            var problems = RepositoryPins.CheckPackages(
                Packages(),
                ToolManifest.Replace("9.0.20", "9.0.21"));

            Assert.AreEqual(1, problems.Count);
            StringAssert.Contains(problems[0], "dotnet-ef is 9.0.21");
        }

        [TestMethod]
        public void PackageCheckRejectsAMissingPackage()
        {
            var problems = RepositoryPins.CheckPackages(
                string.Join('\n', Packages().Split('\n').Where(line => !line.Contains("DotRecast.Detour"))),
                ToolManifest);

            Assert.AreEqual(1, problems.Count);
            StringAssert.Contains(problems[0], "no version for DotRecast.Detour");
        }

        private static string Dockerfile(string sdk, string runtime) => $"""
            FROM mcr.microsoft.com/dotnet/sdk:{sdk} AS build
            RUN dotnet build --configuration Release
            FROM mcr.microsoft.com/dotnet/runtime:{runtime} AS runtime
            COPY --from=build /app /app
            USER app
            """;

        private static string Packages(string recast = "2026.3.1", string efSqlite = "9.0.20") => $"""
            <Project>
              <ItemGroup>
                <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="9.0.20" />
                <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.20" />
                <PackageVersion Include="Microsoft.EntityFrameworkCore.Sqlite" Version="{efSqlite}" />
                <PackageVersion Include="DotRecast.Detour" Version="2026.3.1" />
                <PackageVersion Include="DotRecast.Recast" Version="{recast}" />
                <PackageVersion Include="MSTest.TestAdapter" Version="4.4.1" />
                <PackageVersion Include="MSTest.TestFramework" Version="4.4.1" />
              </ItemGroup>
            </Project>
            """;
    }
}
