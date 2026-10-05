// Splits the tests in a test assembly into lanes, one test-host process per lane, for CI.
//
//   dotnet run .github/scripts/TestShards.cs -- <Rasa.Test.dll> <lane count> <output dir> [timings dir]
//
// Writes <output dir>/lane-<i>.filter, a `dotnet test --filter` expression for each lane. Tests are
// found in the compiled assembly (every [TestMethod] on a non-abstract [TestClass] type) and dealt
// out slowest first, each to the lane with the least work so far. A test's work is its duration in
// the .trx files under [timings dir] (earlier runs, averaged over the runs that ran it, because one
// run's timings carry its runners' speed), or, where those files don't cover it, its number of
// cases (each [DataRow] is one) times the average seconds per case.
//
// Whole classes are the unit, except a class bigger than half a lane's share, which is split into
// its methods so one slow class doesn't set the run's length. A method's data rows stay together.
//
// Every lane but the last lists what it runs; the last runs everything the others don't list. So a
// test this script fails to find, or one added since, still runs exactly once, in the last lane.

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;

if (args.Length < 3)
{
    Console.Error.WriteLine("usage: TestShards.cs <assembly> <lane count> <output dir> [timings dir]");
    return 2;
}

var assemblyPath = args[0];
var laneCount = int.Parse(args[1]);
var outputDir = args[2];
string? timingsDir = args.Length > 3 ? args[3] : null;

if (laneCount < 1)
{
    Console.Error.WriteLine("lane count must be at least 1");
    return 2;
}

var classes = FindTests(assemblyPath);
if (classes.Count == 0)
{
    Console.Error.WriteLine($"no [TestClass] types found in {assemblyPath}");
    return 1;
}

var timings = ReadTimings(timingsDir);
var timedClasses = classes.Keys.Where(c => timings.ContainsKey((c, null))).ToList();
var timedCases = timedClasses.Sum(c => classes[c].Values.Sum());
var secondsPerCase = timedCases > 0 ? timedClasses.Sum(c => timings[(c, null)]) / timedCases : 1.0;

double MethodWeight(string cls, string method) =>
    timings.TryGetValue((cls, method), out var seconds) ? seconds : classes[cls][method] * secondsPerCase;
double ClassWeight(string cls) =>
    timings.TryGetValue((cls, null), out var seconds) ? seconds : classes[cls].Keys.Sum(m => MethodWeight(cls, m));

var total = classes.Keys.Sum(ClassWeight);
var splitAbove = total / laneCount / 2;
var units = new List<(string Term, double Weight)>();
foreach (var cls in classes.Keys)
{
    if (ClassWeight(cls) > splitAbove && classes[cls].Count > 1)
        units.AddRange(classes[cls].Keys.Select(m => ($"FullyQualifiedName={cls}.{m}", MethodWeight(cls, m))));
    else
        units.Add(($"ClassName={cls}", ClassWeight(cls)));
}

var lanes = Enumerable.Range(0, laneCount).Select(_ => new List<string>()).ToArray();
var load = new double[laneCount];
foreach (var unit in units.OrderByDescending(u => u.Weight).ThenBy(u => u.Term, StringComparer.Ordinal))
{
    var target = Array.IndexOf(load, load.Min());
    lanes[target].Add(unit.Term);
    load[target] += unit.Weight;
}

Directory.CreateDirectory(outputDir);
var listed = lanes.Take(laneCount - 1).SelectMany(l => l).OrderBy(t => t, StringComparer.Ordinal).ToList();
for (var i = 0; i < laneCount; i++)
{
    string filter;
    if (i < laneCount - 1)
        filter = lanes[i].Count > 0 ? string.Join("|", lanes[i]) : "ClassName=__nothing_in_this_lane__";
    else
        filter = listed.Count > 0
            ? string.Join("&", listed.Select(t => t.Replace("=", "!=")))
            : "FullyQualifiedName!=__run_everything__";
    File.WriteAllText(Path.Combine(outputDir, $"lane-{i}.filter"), filter);
}

var timed = timings.Count > 0;
string Amount(double work) => timed ? $"{work / 60:0.0} min" : $"{work:0} cases";
var report = new List<string>
{
    $"{classes.Count} test classes, {classes.Values.Sum(m => m.Values.Sum())} cases, in {laneCount} lanes; " +
    (timed
        ? $"weighted by earlier durations ({classes.Count - timedClasses.Count} classes estimated from case counts)."
        : "weighted by case counts (no earlier results)."),
    $"Total {Amount(total)}, ideal {Amount(total / laneCount)} per lane, largest unit {Amount(units.Max(u => u.Weight))}; " +
    $"{units.Count(u => u.Term.StartsWith("FullyQualifiedName="))} methods split out of {classes.Keys.Count(c => ClassWeight(c) > splitAbove && classes[c].Count > 1)} classes.",
    "",
    "| Lane | Units | Planned |",
    "|---|---|---|",
};
for (var i = 0; i < laneCount; i++)
    report.Add($"| {i} | {(i < laneCount - 1 ? lanes[i].Count.ToString() : $"rest ({lanes[i].Count})")} | {Amount(load[i])} |");
foreach (var line in report)
    Console.WriteLine(line);
var summary = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
if (!string.IsNullOrEmpty(summary))
    File.AppendAllLines(summary, report.Prepend("### Test lanes").Append(""));
return 0;

// Test class full name -> test method name -> number of cases (each [DataRow] is one).
static Dictionary<string, Dictionary<string, int>> FindTests(string path)
{
    using var stream = File.OpenRead(path);
    using var pe = new PEReader(stream);
    var md = pe.GetMetadataReader();
    var result = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);

    foreach (var handle in md.TypeDefinitions)
    {
        var type = md.GetTypeDefinition(handle);
        if ((type.Attributes & TypeAttributes.Abstract) != 0 || !HasAttribute(md, type.GetCustomAttributes(), "TestClassAttribute"))
            continue;

        var methods = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var methodHandle in type.GetMethods())
        {
            var method = md.GetMethodDefinition(methodHandle);
            var attributes = method.GetCustomAttributes();
            if (!HasAttribute(md, attributes, "TestMethodAttribute") && !HasAttribute(md, attributes, "DataTestMethodAttribute"))
                continue;
            methods[md.GetString(method.Name)] =
                Math.Max(1, attributes.Count(a => AttributeName(md, md.GetCustomAttribute(a)) == "DataRowAttribute"));
        }

        if (methods.Count > 0)
            result[FullName(md, type)] = methods;
    }

    return result;
}

static bool HasAttribute(MetadataReader md, CustomAttributeHandleCollection attributes, string name) =>
    attributes.Any(a => AttributeName(md, md.GetCustomAttribute(a)) == name);

static string? AttributeName(MetadataReader md, CustomAttribute attribute)
{
    switch (attribute.Constructor.Kind)
    {
        case HandleKind.MemberReference:
            var parent = md.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent;
            return parent.Kind == HandleKind.TypeReference
                ? md.GetString(md.GetTypeReference((TypeReferenceHandle)parent).Name)
                : null;
        case HandleKind.MethodDefinition:
            var declaring = md.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType();
            return md.GetString(md.GetTypeDefinition(declaring).Name);
        default:
            return null;
    }
}

static string FullName(MetadataReader md, TypeDefinition type)
{
    var name = md.GetString(type.Name);
    var declaring = type.GetDeclaringType();
    if (!declaring.IsNil)
        return FullName(md, md.GetTypeDefinition(declaring)) + "+" + name;
    var ns = md.GetString(type.Namespace);
    return ns.Length > 0 ? ns + "." + name : name;
}

// (class, method) -> seconds, and (class, null) -> the class's total, from the .trx files under the
// directory. Each file is one lane of one run, so a test appears in one file per run that ran it;
// a test's duration is its average over those runs, and a class's is the sum of its methods'.
static Dictionary<(string Class, string? Method), double> ReadTimings(string? dir)
{
    var sums = new Dictionary<(string, string?), double>();
    var runs = new Dictionary<(string, string?), int>();
    if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
        return sums;

    XNamespace t = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
    foreach (var file in Directory.EnumerateFiles(dir, "*.trx", SearchOption.AllDirectories))
    {
        var doc = XDocument.Load(file);
        var testOf = doc.Descendants(t + "UnitTest").ToDictionary(
            u => (string?)u.Attribute("id") ?? "",
            u => u.Element(t + "TestMethod"));
        var seen = new HashSet<(string, string?)>();
        foreach (var r in doc.Descendants(t + "UnitTestResult"))
        {
            if (!testOf.TryGetValue((string?)r.Attribute("testId") ?? "", out var test)
                || (string?)test?.Attribute("className") is not { } cls
                || (string?)test.Attribute("name") is not { } method
                || !TimeSpan.TryParse((string?)r.Attribute("duration"), out var duration))
                continue;
            sums[(cls, method)] = sums.GetValueOrDefault((cls, method)) + duration.TotalSeconds;
            if (seen.Add((cls, method)))
                runs[(cls, method)] = runs.GetValueOrDefault((cls, method)) + 1;
        }
    }

    var result = new Dictionary<(string Class, string? Method), double>();
    foreach (var (key, seconds) in sums)
    {
        var average = seconds / runs[key];
        result[key] = average;
        result[(key.Item1, null)] = result.GetValueOrDefault((key.Item1, null)) + average;
    }

    return result;
}
