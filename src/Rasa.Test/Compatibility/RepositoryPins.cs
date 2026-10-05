using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

namespace Rasa.Test.Compatibility
{
    // Version pins that live in more than one file. The guards check that the copies agree rather
    // than that they hold particular versions, so an update (by hand or by Dependabot) that moves
    // every copy together passes, and one that misses a copy fails.
    internal static class RepositoryPins
    {
        private const string SdkImage = "mcr.microsoft.com/dotnet/sdk";
        private const string RuntimeImage = "mcr.microsoft.com/dotnet/runtime";

        internal static string ReadGlobalJsonSdk(string globalJson)
        {
            using var document = JsonDocument.Parse(globalJson);
            return document.RootElement.GetProperty("sdk").GetProperty("version").GetString();
        }

        internal static IReadOnlyList<string> ReadDockerfileImages(string dockerfile)
        {
            // The image of each FROM, without flags such as --platform or a pinned @sha256 digest.
            return DockerfileModel.Parse(dockerfile).Instructions
                .Where(instruction => instruction.Name == "FROM")
                .Select(instruction => instruction.Arguments
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .First(field => !field.StartsWith("--", StringComparison.Ordinal))
                    .Split('@')[0])
                .ToArray();
        }

        // Every SDK stage builds with the SDK global.json pins, and the image runs on that SDK's
        // runtime line. Returns the disagreements; empty when the files agree.
        internal static IReadOnlyList<string> CheckSdk(string globalJson, string dockerfile)
        {
            var problems = new List<string>();
            var sdk = ReadGlobalJsonSdk(globalJson);
            var images = ReadDockerfileImages(dockerfile);
            var sdkTags = images
                .Where(image => image.StartsWith(SdkImage + ":", StringComparison.Ordinal))
                .Select(image => image.Substring(SdkImage.Length + 1))
                .ToArray();

            if (sdkTags.Length == 0)
                problems.Add($"The Dockerfile has no {SdkImage} stage.");
            foreach (var tag in sdkTags.Where(tag => tag != sdk))
                problems.Add($"The Dockerfile builds with {SdkImage}:{tag}, but global.json pins {sdk}.");

            // The runtime image may float on the line (10.0) or pin a patch (10.0.5).
            var final = images.LastOrDefault() ?? string.Empty;
            var sdkLine = string.Join('.', sdk.Split('.').Take(2));
            var runtimeLine = $"{RuntimeImage}:{sdkLine}";
            if (final != runtimeLine && !final.StartsWith(runtimeLine + ".", StringComparison.Ordinal))
                problems.Add($"The Dockerfile's final stage is {final}, not {runtimeLine}.");

            return problems;
        }

        // Packages that must move together share one version in Directory.Packages.props, and the
        // dotnet-ef tool matches the EF Core packages it migrates.
        internal static IReadOnlyList<string> CheckPackages(string packagesProps, string toolManifest)
        {
            var problems = new List<string>();
            var versions = XDocument.Parse(packagesProps).Descendants("PackageVersion")
                .ToDictionary(
                    item => item.Attribute("Include").Value,
                    item => item.Attribute("Version").Value,
                    StringComparer.OrdinalIgnoreCase);

            RequireSameVersion(versions, problems, "DotRecast.Detour", "DotRecast.Recast");
            RequireSameVersion(
                versions,
                problems,
                "Microsoft.EntityFrameworkCore",
                "Microsoft.EntityFrameworkCore.Design",
                "Microsoft.EntityFrameworkCore.Sqlite");
            RequireSameVersion(versions, problems, "MSTest.TestAdapter", "MSTest.TestFramework");

            using var tools = JsonDocument.Parse(toolManifest);
            var dotnetEf = tools.RootElement.GetProperty("tools").GetProperty("dotnet-ef")
                .GetProperty("version").GetString();
            if (versions.TryGetValue("Microsoft.EntityFrameworkCore", out var efCore) && dotnetEf != efCore)
                problems.Add($"dotnet-ef is {dotnetEf} in .config/dotnet-tools.json, but EF Core is {efCore}.");

            return problems;
        }

        private static void RequireSameVersion(
            IReadOnlyDictionary<string, string> versions,
            List<string> problems,
            params string[] packages)
        {
            var missing = packages.Where(package => !versions.ContainsKey(package)).ToArray();
            if (missing.Length != 0)
            {
                problems.Add($"Directory.Packages.props has no version for {string.Join(", ", missing)}.");
                return;
            }

            if (packages.Select(package => versions[package]).Distinct().Count() > 1)
                problems.Add("These packages must share one version: " +
                    string.Join(", ", packages.Select(package => $"{package} {versions[package]}")) + ".");
        }

        internal static string Read(string repositoryRoot, params string[] path) =>
            File.ReadAllText(Path.Combine(new[] { repositoryRoot }.Concat(path).ToArray()));
    }
}
