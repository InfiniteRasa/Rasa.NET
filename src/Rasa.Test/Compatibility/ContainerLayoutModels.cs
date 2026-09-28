using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Rasa.Test.Compatibility
{
    internal sealed class ComposeLayout
    {
        private readonly IReadOnlyDictionary<string, ComposeService> _services;

        private ComposeLayout(IReadOnlyDictionary<string, ComposeService> services)
        {
            _services = services;
        }

        internal static ComposeLayout Parse(string yaml)
        {
            var services = new Dictionary<string, ComposeServiceBuilder>(
                StringComparer.Ordinal);
            ComposeServiceBuilder currentService = null;
            var inServices = false;
            var inVolumes = false;

            foreach (var rawLine in yaml.Replace("\r", string.Empty).Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(rawLine))
                    continue;

                var indent = rawLine.TakeWhile(character => character == ' ').Count();
                var line = StripComment(rawLine.Trim());
                if (line.Length == 0)
                    continue;

                if (indent == 0)
                {
                    inServices = line == "services:";
                    currentService = null;
                    inVolumes = false;
                    continue;
                }

                if (!inServices)
                    continue;

                if (indent == 2 && line.EndsWith(":", StringComparison.Ordinal))
                {
                    var name = line.Substring(0, line.Length - 1);
                    currentService = new ComposeServiceBuilder(name);
                    services.Add(name, currentService);
                    inVolumes = false;
                    continue;
                }

                if (currentService == null)
                    continue;

                if (indent == 4)
                {
                    inVolumes = line == "volumes:";
                    if (inVolumes)
                        continue;

                    var separator = line.IndexOf(':');
                    if (separator < 0)
                        continue;

                    var property = line.Substring(0, separator);
                    var value = Unquote(line.Substring(separator + 1).Trim());
                    switch (property)
                    {
                        case "working_dir":
                            currentService.WorkingDirectory = value;
                            break;
                        case "command":
                            currentService.Command = SplitCommand(value);
                            break;
                    }

                    continue;
                }

                if (inVolumes && indent == 6 && line.StartsWith("- ", StringComparison.Ordinal))
                    currentService.VolumeDestinations.Add(ParseVolumeDestination(
                        Unquote(line.Substring(2).Trim())));
            }

            return new ComposeLayout(services.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Build(),
                StringComparer.Ordinal));
        }

        internal ComposeService GetService(string name)
        {
            if (!_services.TryGetValue(name, out var service))
                throw new InvalidDataException($"Compose service '{name}' was not found.");

            return service;
        }

        private static string ParseVolumeDestination(string value)
        {
            var fields = value.Split(':');
            var destination = fields.FirstOrDefault(field =>
                field.StartsWith("/", StringComparison.Ordinal));
            if (destination == null)
                throw new InvalidDataException($"Volume '{value}' has no absolute container destination.");

            return PosixPath.Normalize(destination);
        }

        private static IReadOnlyList<string> SplitCommand(string value)
        {
            return Regex.Matches(value, @"""[^""]*""|'[^']*'|\S+")
                .Cast<Match>()
                .Select(match => Unquote(match.Value))
                .ToArray();
        }

        private static string StripComment(string value)
        {
            var inSingleQuote = false;
            var inDoubleQuote = false;
            for (var index = 0; index < value.Length; index++)
            {
                switch (value[index])
                {
                    case '\'' when !inDoubleQuote:
                        inSingleQuote = !inSingleQuote;
                        break;
                    case '"' when !inSingleQuote:
                        inDoubleQuote = !inDoubleQuote;
                        break;
                    case '#' when !inSingleQuote && !inDoubleQuote:
                        return value.Substring(0, index).TrimEnd();
                }
            }

            return value;
        }

        private static string Unquote(string value)
        {
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[value.Length - 1] == '"') ||
                 (value[0] == '\'' && value[value.Length - 1] == '\'')))
                return value.Substring(1, value.Length - 2);

            return value;
        }

        private sealed class ComposeServiceBuilder
        {
            internal string Name { get; }
            internal string WorkingDirectory { get; set; }
            internal IReadOnlyList<string> Command { get; set; } = Array.Empty<string>();
            internal List<string> VolumeDestinations { get; } = new List<string>();

            internal ComposeServiceBuilder(string name)
            {
                Name = name;
            }

            internal ComposeService Build() =>
                new ComposeService(Name, WorkingDirectory, Command, VolumeDestinations);
        }
    }

    internal sealed class ComposeService
    {
        internal string Name { get; }
        internal string WorkingDirectory { get; }
        internal IReadOnlyList<string> Command { get; }
        internal IReadOnlyList<string> VolumeDestinations { get; }

        internal ComposeService(
            string name,
            string workingDirectory,
            IReadOnlyList<string> command,
            IReadOnlyList<string> volumeDestinations)
        {
            Name = name;
            WorkingDirectory = PosixPath.Normalize(workingDirectory);
            Command = command;
            VolumeDestinations = volumeDestinations;
        }
    }

    internal sealed class DockerImageLayout
    {
        private readonly HashSet<string> _files =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _directories =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _copiedFiles =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _copiedHostFiles =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private DockerImageLayout()
        {
        }

        internal static DockerImageLayout Create(string repositoryRoot)
        {
            var layout = new DockerImageLayout();
            var dockerfile = DockerfileModel.Parse(
                File.ReadAllText(Path.Combine(repositoryRoot, "Dockerfile")));
            var dockerIgnore = DockerIgnoreMatcher.Load(repositoryRoot);
            var workingDirectory = "/";

            foreach (var instruction in dockerfile.Instructions)
            {
                switch (instruction.Name)
                {
                    case "WORKDIR":
                        workingDirectory = PosixPath.Resolve(
                            workingDirectory,
                            instruction.Arguments);
                        layout.AddDirectory(workingDirectory);
                        break;
                    case "COPY":
                        layout.ApplyCopy(
                            repositoryRoot,
                            workingDirectory,
                            instruction.Arguments,
                            dockerIgnore);
                        break;
                    case "RUN":
                        var build = DotNetBuildCommand.Parse(instruction.Arguments);
                        if (build != null &&
                            build.Configuration.Equals(
                                "Release",
                                StringComparison.OrdinalIgnoreCase))
                            layout.ApplyBuild(workingDirectory, build);
                        break;
                }
            }

            return layout;
        }

        internal bool ContainsFile(string path) =>
            _files.Contains(PosixPath.Normalize(path));

        internal bool ContainsDirectory(string path) =>
            _directories.Contains(PosixPath.Normalize(path));

        internal bool ContainsFileBelow(string directory, string extension)
        {
            var prefix = PosixPath.Normalize(directory).TrimEnd('/') + "/";
            return _files.Any(path =>
                path.StartsWith(prefix, StringComparison.Ordinal) &&
                path.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
        }

        private void ApplyCopy(
            string repositoryRoot,
            string workingDirectory,
            string arguments,
            DockerIgnoreMatcher dockerIgnore)
        {
            var fields = SplitArguments(arguments);
            if (fields.Count != 2)
                throw new InvalidDataException($"Unsupported Docker COPY instruction: {arguments}");

            var source = Path.GetFullPath(Path.Combine(
                repositoryRoot,
                fields[0].Replace('/', Path.DirectorySeparatorChar)));
            var destination = PosixPath.Resolve(workingDirectory, fields[1]);
            var sourceRelativePath = Path.GetRelativePath(repositoryRoot, source)
                .Replace('\\', '/');
            if (dockerIgnore.IsIgnored(sourceRelativePath))
                throw new InvalidDataException(
                    $"Docker COPY source '{fields[0]}' is excluded by .dockerignore.");

            if (File.Exists(source))
            {
                if (fields[1].EndsWith("/", StringComparison.Ordinal) ||
                    _directories.Contains(destination))
                    destination = PosixPath.Resolve(destination, Path.GetFileName(source));
                AddCopiedFile(source, destination);
                return;
            }

            if (!Directory.Exists(source))
                throw new InvalidDataException($"Docker COPY source '{fields[0]}' does not exist.");

            AddDirectory(destination);
            foreach (var file in Directory.EnumerateFiles(
                source,
                "*",
                SearchOption.AllDirectories))
            {
                var repositoryRelative = Path.GetRelativePath(repositoryRoot, file)
                    .Replace('\\', '/');
                if (dockerIgnore.IsIgnored(repositoryRelative))
                    continue;

                var relative = Path.GetRelativePath(source, file).Replace('\\', '/');
                AddCopiedFile(file, PosixPath.Resolve(destination, relative));
            }
        }

        private void ApplyBuild(
            string workingDirectory,
            DotNetBuildCommand build)
        {
            var imageTargetPath = string.IsNullOrEmpty(build.Target)
                ? PosixPath.Resolve(workingDirectory, "Rasa.NET.sln")
                : PosixPath.Resolve(workingDirectory, build.Target);
            if (!_copiedHostFiles.TryGetValue(imageTargetPath, out var hostTargetPath))
                throw new InvalidDataException(
                    $"Docker build target '{imageTargetPath}' was not copied into the image.");

            var extension = Path.GetExtension(hostTargetPath);
            var projectTargets = new List<(string HostPath, string ImagePath)>();
            if (extension.Equals(".sln", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var projectPath in ReadSolutionProjects(hostTargetPath))
                {
                    var hostProjectPath = Path.GetFullPath(Path.Combine(
                        Path.GetDirectoryName(hostTargetPath),
                        projectPath.Replace('\\', Path.DirectorySeparatorChar)));
                    var imageProjectPath = PosixPath.Resolve(
                        PosixPath.GetDirectoryName(imageTargetPath),
                        projectPath.Replace('\\', '/'));
                    RequireCopiedProject(hostProjectPath, imageProjectPath);
                    projectTargets.Add((hostProjectPath, imageProjectPath));
                }
            }
            else if (extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                projectTargets.Add((hostTargetPath, imageTargetPath));
            }
            else
            {
                throw new InvalidDataException(
                    $"Unsupported Docker dotnet build target '{imageTargetPath}'.");
            }

            if (projectTargets.Count == 0)
                throw new InvalidDataException(
                    $"Docker build target '{imageTargetPath}' contains no supported projects.");

            var materializedProjects = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
            foreach (var project in projectTargets)
            {
                AddProjectOutput(
                    project.HostPath,
                    project.ImagePath,
                    build.Configuration,
                    materializedProjects);
            }
        }

        private void AddProjectOutput(
            string projectPath,
            string imageProjectPath,
            string configuration,
            HashSet<string> materializedProjects)
        {
            projectPath = Path.GetFullPath(projectPath);
            if (!materializedProjects.Add(projectPath))
                return;

            var project = XDocument.Load(projectPath);
            var projectDirectory = Path.GetDirectoryName(projectPath);
            var imageProjectDirectory = PosixPath.GetDirectoryName(imageProjectPath);
            var outputDirectory = PosixPath.Resolve(
                imageProjectDirectory,
                $"bin/{configuration}/{ReadTargetFramework(project, projectPath)}");
            var assemblyName = ReadAssemblyName(project, projectPath);

            AddFile(PosixPath.Resolve(outputDirectory, assemblyName + ".dll"));
            AddOutputContent(project, projectDirectory, outputDirectory);

            foreach (var reference in project.Descendants("ProjectReference"))
            {
                var include = reference.Attribute("Include")?.Value;
                if (string.IsNullOrEmpty(include))
                    continue;

                var referencedProject = Path.GetFullPath(Path.Combine(
                    projectDirectory,
                    include.Replace('\\', Path.DirectorySeparatorChar)));
                if (!_copiedFiles.TryGetValue(
                    referencedProject,
                    out var imageReferencedProject))
                    throw new InvalidDataException(
                        $"Referenced project '{referencedProject}' was not copied into the image.");

                AddProjectOutput(
                    referencedProject,
                    imageReferencedProject,
                    configuration,
                    materializedProjects);

                AddReferencedContent(
                    referencedProject,
                    outputDirectory,
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            }
        }

        private void AddReferencedContent(
            string projectPath,
            string consumingOutputDirectory,
            HashSet<string> visitedProjects)
        {
            projectPath = Path.GetFullPath(projectPath);
            if (!visitedProjects.Add(projectPath))
                return;

            var project = XDocument.Load(projectPath);
            var projectDirectory = Path.GetDirectoryName(projectPath);
            AddFile(PosixPath.Resolve(
                consumingOutputDirectory,
                ReadAssemblyName(project, projectPath) + ".dll"));
            AddOutputContent(project, projectDirectory, consumingOutputDirectory);

            foreach (var reference in project.Descendants("ProjectReference"))
            {
                var include = reference.Attribute("Include")?.Value;
                if (string.IsNullOrEmpty(include))
                    continue;

                var referencedProject = Path.GetFullPath(Path.Combine(
                    projectDirectory,
                    include.Replace('\\', Path.DirectorySeparatorChar)));
                if (!_copiedFiles.ContainsKey(referencedProject))
                    throw new InvalidDataException(
                        $"Referenced project '{referencedProject}' was not copied into the image.");

                AddReferencedContent(
                    referencedProject,
                    consumingOutputDirectory,
                    visitedProjects);
            }
        }

        private void AddOutputContent(
            XDocument project,
            string projectDirectory,
            string outputDirectory)
        {
            foreach (var item in project.Descendants()
                .Where(element =>
                    (element.Name.LocalName == "None" ||
                     element.Name.LocalName == "Content") &&
                    element.Elements("CopyToOutputDirectory").Any()))
            {
                var include = item.Attribute("Include")?.Value ??
                    item.Attribute("Update")?.Value;
                if (string.IsNullOrEmpty(include))
                    continue;

                var source = Path.GetFullPath(Path.Combine(
                    projectDirectory,
                    include.Replace('\\', Path.DirectorySeparatorChar)));
                if (!_copiedFiles.ContainsKey(source))
                    continue;

                var targetPath = item.Elements("TargetPath")
                    .Select(element => element.Value)
                    .FirstOrDefault() ??
                    item.Elements("Link")
                        .Select(element => element.Value)
                        .FirstOrDefault() ??
                    include;
                AddFile(PosixPath.Resolve(
                    outputDirectory,
                    targetPath.Replace('\\', '/')));
            }
        }

        private void AddCopiedFile(string hostPath, string imagePath)
        {
            hostPath = Path.GetFullPath(hostPath);
            imagePath = PosixPath.Normalize(imagePath);
            _copiedFiles[hostPath] = imagePath;
            _copiedHostFiles[imagePath] = hostPath;
            AddFile(imagePath);
        }

        private void AddFile(string path)
        {
            path = PosixPath.Normalize(path);
            _files.Add(path);
            AddDirectory(PosixPath.GetDirectoryName(path));
        }

        private void AddDirectory(string path)
        {
            path = PosixPath.Normalize(path);
            while (path.Length > 1 && _directories.Add(path))
                path = PosixPath.GetDirectoryName(path);
            _directories.Add("/");
        }

        private void RequireCopiedProject(
            string hostProjectPath,
            string imageProjectPath)
        {
            if (!_copiedFiles.TryGetValue(hostProjectPath, out var copiedImagePath) ||
                !copiedImagePath.Equals(imageProjectPath, StringComparison.Ordinal))
                throw new InvalidDataException(
                    $"Solution project '{imageProjectPath}' was not copied into the image.");
        }

        private static IReadOnlyList<string> ReadSolutionProjects(string solutionPath)
        {
            return File.ReadLines(solutionPath)
                .Where(line => line.StartsWith("Project(", StringComparison.Ordinal))
                .Select(line => line.Split(','))
                .Where(fields => fields.Length >= 2)
                .Select(fields => fields[1].Trim().Trim('"'))
                .Where(path => path.EndsWith(
                    ".csproj",
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        private static string ReadAssemblyName(
            XDocument project,
            string projectPath)
        {
            return project.Descendants()
                .Where(element => element.Name.LocalName == "AssemblyName")
                .Select(element => element.Value)
                .FirstOrDefault() ??
                Path.GetFileNameWithoutExtension(projectPath);
        }

        private static string ReadTargetFramework(
            XDocument project,
            string projectPath)
        {
            var targetFrameworks = project.Descendants()
                .Where(element =>
                    element.Name.LocalName == "TargetFramework" ||
                    element.Name.LocalName == "TargetFrameworks")
                .SelectMany(element => element.Value.Split(';'))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (targetFrameworks.Length != 1)
                throw new InvalidDataException(
                    $"Project '{projectPath}' must have exactly one target framework.");

            return targetFrameworks[0];
        }

        private static IReadOnlyList<string> SplitArguments(string value)
        {
            return Regex.Matches(value, @"""[^""]*""|'[^']*'|\S+")
                .Cast<Match>()
                .Select(match => match.Value.Trim('"', '\''))
                .ToArray();
        }

        private sealed class DotNetBuildCommand
        {
            internal string Configuration { get; }
            internal string Target { get; }

            private DotNetBuildCommand(string configuration, string target)
            {
                Configuration = configuration;
                Target = target;
            }

            internal static DotNetBuildCommand Parse(string arguments)
            {
                var fields = SplitArguments(arguments);
                if (fields.Count < 2 ||
                    !fields[0].Equals("dotnet", StringComparison.OrdinalIgnoreCase) ||
                    !fields[1].Equals("build", StringComparison.OrdinalIgnoreCase))
                    return null;

                var configuration = "Debug";
                string target = null;
                for (var index = 2; index < fields.Count; index++)
                {
                    var field = fields[index];
                    if (field == "-c" || field == "--configuration")
                    {
                        if (++index >= fields.Count)
                            throw new InvalidDataException(
                                "Docker dotnet build configuration has no value.");
                        configuration = fields[index];
                        continue;
                    }

                    const string configurationPrefix = "--configuration=";
                    if (field.StartsWith(
                        configurationPrefix,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        configuration = field.Substring(configurationPrefix.Length);
                        continue;
                    }

                    if (field == "--no-restore" ||
                        field == "--nologo" ||
                        field == "--no-dependencies" ||
                        field == "--disable-build-servers")
                        continue;

                    if (field.StartsWith("-", StringComparison.Ordinal))
                        throw new InvalidDataException(
                            $"Unsupported Docker dotnet build option '{field}'.");

                    if (target != null)
                        throw new InvalidDataException(
                            "Docker dotnet build has multiple possible targets.");

                    target = field;
                }

                if (target != null &&
                    !target.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) &&
                    !target.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException(
                        $"Unsupported Docker dotnet build target '{target}'.");

                return new DotNetBuildCommand(configuration, target);
            }
        }
    }

    internal sealed class DockerIgnoreMatcher
    {
        private readonly IReadOnlyList<DockerIgnoreRule> _rules;

        private DockerIgnoreMatcher(IReadOnlyList<DockerIgnoreRule> rules)
        {
            _rules = rules;
        }

        internal static DockerIgnoreMatcher Load(string repositoryRoot)
        {
            var path = Path.Combine(repositoryRoot, ".dockerignore");
            if (!File.Exists(path))
                return new DockerIgnoreMatcher(Array.Empty<DockerIgnoreRule>());

            var rules = File.ReadAllLines(path)
                .Select(line => line.Trim())
                .Where(line =>
                    line.Length != 0 &&
                    !line.StartsWith("#", StringComparison.Ordinal))
                .Select(line => new DockerIgnoreRule(line))
                .ToArray();
            return new DockerIgnoreMatcher(rules);
        }

        internal bool IsIgnored(string relativePath)
        {
            relativePath = relativePath.Replace('\\', '/').Trim('/');
            if (relativePath.Length == 0 || relativePath == ".")
                return false;

            var ignored = false;
            foreach (var rule in _rules)
            {
                if (rule.Matches(relativePath))
                    ignored = !rule.Negated;
            }

            return ignored;
        }

        private sealed class DockerIgnoreRule
        {
            private readonly Regex _pattern;

            internal bool Negated { get; }

            internal DockerIgnoreRule(string pattern)
            {
                Negated = pattern.StartsWith("!", StringComparison.Ordinal);
                if (Negated)
                    pattern = pattern.Substring(1);

                pattern = pattern.Replace('\\', '/').Trim('/');
                if (pattern.Length == 0)
                    throw new InvalidDataException("Docker ignore pattern is empty.");

                _pattern = new Regex(
                    CreateRegex(pattern),
                    RegexOptions.CultureInvariant);
            }

            internal bool Matches(string path)
            {
                var candidate = path;
                while (candidate.Length != 0)
                {
                    if (_pattern.IsMatch(candidate))
                        return true;

                    var separator = candidate.LastIndexOf('/');
                    candidate = separator < 0
                        ? string.Empty
                        : candidate.Substring(0, separator);
                }

                return false;
            }

            private static string CreateRegex(string pattern)
            {
                var expression = new StringBuilder("^");
                if (!pattern.Contains('/'))
                    expression.Append("(?:.*/)?");

                for (var index = 0; index < pattern.Length; index++)
                {
                    var character = pattern[index];
                    if (character == '*')
                    {
                        if (index + 1 < pattern.Length &&
                            pattern[index + 1] == '*')
                        {
                            index++;
                            if (index + 1 < pattern.Length &&
                                pattern[index + 1] == '/')
                            {
                                index++;
                                expression.Append("(?:.*/)?");
                            }
                            else
                            {
                                expression.Append(".*");
                            }
                        }
                        else
                        {
                            expression.Append("[^/]*");
                        }

                        continue;
                    }

                    if (character == '?')
                    {
                        expression.Append("[^/]");
                        continue;
                    }

                    expression.Append(Regex.Escape(character.ToString()));
                }

                expression.Append("$");
                return expression.ToString();
            }
        }
    }

    internal sealed class DockerfileModel
    {
        internal IReadOnlyList<DockerfileInstruction> Instructions { get; }

        private DockerfileModel(IReadOnlyList<DockerfileInstruction> instructions)
        {
            Instructions = instructions;
        }

        internal static DockerfileModel Parse(string contents)
        {
            var instructions = new List<DockerfileInstruction>();
            var logicalLine = new StringBuilder();
            foreach (var rawLine in contents.Replace("\r", string.Empty).Split('\n'))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    continue;

                var continued = line.EndsWith("\\", StringComparison.Ordinal);
                logicalLine.Append(continued
                    ? line.Substring(0, line.Length - 1).TrimEnd()
                    : line);
                if (continued)
                {
                    logicalLine.Append(' ');
                    continue;
                }

                var separator = logicalLine.ToString().IndexOf(' ');
                if (separator < 0)
                    throw new InvalidDataException(
                        $"Docker instruction has no arguments: {logicalLine}");

                instructions.Add(new DockerfileInstruction(
                    logicalLine.ToString().Substring(0, separator).ToUpperInvariant(),
                    logicalLine.ToString().Substring(separator + 1).Trim()));
                logicalLine.Clear();
            }

            if (logicalLine.Length != 0)
                throw new InvalidDataException("Dockerfile ends in a continued instruction.");

            return new DockerfileModel(instructions);
        }
    }

    internal sealed class DockerfileInstruction
    {
        internal string Name { get; }
        internal string Arguments { get; }

        internal DockerfileInstruction(string name, string arguments)
        {
            Name = name;
            Arguments = arguments;
        }
    }

    internal static class PosixPath
    {
        internal static string Resolve(string basePath, string path)
        {
            if (path.StartsWith("/", StringComparison.Ordinal))
                return Normalize(path);

            return Normalize(basePath.TrimEnd('/') + "/" + path);
        }

        internal static string GetDirectoryName(string path)
        {
            path = Normalize(path);
            var separator = path.LastIndexOf('/');
            return separator <= 0 ? "/" : path.Substring(0, separator);
        }

        internal static string Normalize(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidDataException("Container path is empty.");

            var absolute = path.StartsWith("/", StringComparison.Ordinal);
            var parts = new List<string>();
            foreach (var part in path.Replace('\\', '/').Split('/'))
            {
                if (part.Length == 0 || part == ".")
                    continue;
                if (part == "..")
                {
                    if (parts.Count == 0)
                        throw new InvalidDataException($"Container path escapes its root: {path}");
                    parts.RemoveAt(parts.Count - 1);
                    continue;
                }

                parts.Add(part);
            }

            return (absolute ? "/" : string.Empty) + string.Join("/", parts);
        }
    }
}
