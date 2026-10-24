using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;

    /// <summary>What a catalogue item does besides standing there (decor.tsv's function column).</summary>
    public enum DecorFunction
    {
        /// <summary>Decoration only.</summary>
        None = 0,

        /// <summary>A footlocker of the retail class whose use opens the player's own vault (Lockbox).</summary>
        Storage = 1,

        /// <summary>A retail crafting station: its use opens the crafting window (KraftwerksManager).</summary>
        Crafting = 2,

        /// <summary>A bed or pod: a player who dies in the apartment gets up beside it rather than at the entry.</summary>
        Medbay = 5
    }

    /// <summary>
    /// The plots apartments stand on and the decoration they may hold: three tables in the
    /// server's apartments folder (ApartmentConfig.ContentDirectory), tab separated, a header
    /// row and # comments.
    ///
    ///  - plots.tsv: a plot is a part of a retail map - its context id, a build box every item
    ///    must stand inside, the entry a player arrives at and the plot's own item budget. Only
    ///    maps the retail client ships are listed: an apartment is a copy of the map, which the
    ///    client loads as it loads any instance. A plot of kind 1 or 3 is an apartment's; kind 2
    ///    is a clan base's, kept for when there are clan bases.
    ///  - plot-caps.tsv: how many items of a category a plot takes at its own budget.
    ///  - decor.tsv: the retail entity classes an apartment may hold, each with its category,
    ///    the surface it was made to stand on, its function, the scales it may be shown at, its
    ///    name in the catalogue, and its box (half extents, and the box centre's height over the
    ///    entity's origin).
    ///
    /// A row the server cannot use is left out and said why (a map that is not loaded, a class
    /// the world database does not have, a number that does not read): <see cref="Problems"/>.
    /// </summary>
    public sealed class ApartmentCatalogue
    {
        public sealed class Plot
        {
            public uint Id { get; init; }
            public string Name { get; init; }

            /// <summary>1 an apartment's, 2 a clan base's, 3 either.</summary>
            public int Kind { get; init; }
            public uint MapContextId { get; init; }
            public Vector3 BoxMin { get; init; }
            public Vector3 BoxMax { get; init; }
            public Vector3 Entry { get; init; }
            public float EntryYaw { get; init; }
            public int Budget { get; init; }

            public bool IsApartment => Kind == 1 || Kind == 3;

            /// <summary>The centre of the build box, as far up as the entry.</summary>
            public Vector3 Centre => new Vector3((BoxMin.X + BoxMax.X) / 2, Entry.Y, (BoxMin.Z + BoxMax.Z) / 2);

            /// <summary>Whether a point is inside the build box, give or take <paramref name="slack"/> metres.</summary>
            public bool Contains(Vector3 point, float slack = 0) =>
                point.X >= BoxMin.X - slack && point.X <= BoxMax.X + slack &&
                point.Y >= BoxMin.Y - slack && point.Y <= BoxMax.Y + slack &&
                point.Z >= BoxMin.Z - slack && point.Z <= BoxMax.Z + slack;

            public override string ToString() => $"{Id} {Name}";
        }

        public sealed class Decor
        {
            public uint ClassId { get; init; }
            public string Category { get; init; }

            /// <summary>1 floor, 2 wall, 3 ceiling: where it was made to stand. Every item is placed by its origin, whatever this is.</summary>
            public int Surface { get; init; }
            public DecorFunction Function { get; init; }
            public float ScaleMin { get; init; }
            public float ScaleMax { get; init; }
            public string Name { get; init; }

            /// <summary>The half extents of its box at scale 1.</summary>
            public Vector3 HalfExtents { get; init; }

            /// <summary>How far over the origin the box's centre is, at scale 1.</summary>
            public float CentreY { get; init; }

            public string SurfaceName => Surface switch { 2 => "wall", 3 => "ceiling", _ => "floor" };

            /// <summary>
            /// The smallest and largest corner of the item's box in the world, at a place, a yaw
            /// and a scale: the box turned about the up axis and the box round that taken.
            /// </summary>
            public (Vector3 Min, Vector3 Max) BoundsAt(Vector3 position, double yaw, float scale)
            {
                var cos = (float)Math.Abs(Math.Cos(yaw));
                var sin = (float)Math.Abs(Math.Sin(yaw));
                var half = new Vector3(
                    cos * HalfExtents.X + sin * HalfExtents.Z,
                    HalfExtents.Y,
                    sin * HalfExtents.X + cos * HalfExtents.Z) * scale;
                var centre = position + new Vector3(0, CentreY * scale, 0);

                return (centre - half, centre + half);
            }

            public override string ToString() => $"{ClassId} {Name}";
        }

        public const string PlotsFile = "plots.tsv";
        public const string CapsFile = "plot-caps.tsv";
        public const string DecorFile = "decor.tsv";

        private readonly Dictionary<uint, Plot> _plots = new Dictionary<uint, Plot>();
        private readonly Dictionary<uint, Decor> _decor = new Dictionary<uint, Decor>();
        private readonly Dictionary<(uint Plot, string Category), int> _caps = new Dictionary<(uint, string), int>();

        public IReadOnlyCollection<Plot> Plots => _plots.Values;
        public IReadOnlyCollection<Decor> Items => _decor.Values;

        /// <summary>Why rows were left out, one line each.</summary>
        public List<string> Problems { get; } = new List<string>();

        /// <summary>The categories, in name order, with how many items each has.</summary>
        public IReadOnlyList<(string Category, int Count)> Categories =>
            _decor.Values.GroupBy(d => d.Category, StringComparer.OrdinalIgnoreCase)
                .Select(g => (g.First().Category, g.Count()))
                .OrderBy(c => c.Item1, StringComparer.OrdinalIgnoreCase)
                .ToList();

        /// <summary>The apartment plots, lowest id first: the first is where a new apartment stands.</summary>
        public IReadOnlyList<Plot> ApartmentPlots => _plots.Values.Where(p => p.IsApartment).OrderBy(p => p.Id).ToList();

        public Plot PlotOf(uint id) => _plots.TryGetValue(id, out var plot) ? plot : null;

        public Decor DecorOf(uint classId) => _decor.TryGetValue(classId, out var decor) ? decor : null;

        /// <summary>A category's items, by name.</summary>
        public IReadOnlyList<Decor> InCategory(string category) =>
            _decor.Values.Where(d => string.Equals(d.Category, category, StringComparison.OrdinalIgnoreCase))
                .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(d => d.ClassId)
                .ToList();

        /// <summary>
        /// The items a word names: a class id, a name in full (any case), or else every item
        /// whose name has the word in it. Several back for a word that names more than one.
        /// </summary>
        public IReadOnlyList<Decor> Find(string words)
        {
            words = words?.Trim();

            if (string.IsNullOrEmpty(words))
                return Array.Empty<Decor>();

            if (uint.TryParse(words, NumberStyles.None, CultureInfo.InvariantCulture, out var classId))
                return DecorOf(classId) is { } byId ? new[] { byId } : Array.Empty<Decor>();

            var exact = _decor.Values.Where(d => string.Equals(d.Name, words, StringComparison.OrdinalIgnoreCase)).ToList();

            if (exact.Count > 0)
                return exact;

            return _decor.Values.Where(d => d.Name.Contains(words, StringComparison.OrdinalIgnoreCase))
                .OrderBy(d => d.Name.Length)
                .ThenBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// How many items of a category a plot takes with an apartment's budget, or null for a
        /// category the plot does not cap. A plot's caps are for its own budget; held to a
        /// smaller one they are scaled down with it, never under one.
        /// </summary>
        public int? CapOf(Plot plot, string category, int budget)
        {
            if (plot == null || category == null || !_caps.TryGetValue((plot.Id, category.ToLowerInvariant()), out var cap))
                return null;

            if (plot.Budget <= 0 || budget >= plot.Budget)
                return cap;

            return Math.Max(1, (int)Math.Floor(cap * (double)budget / plot.Budget));
        }

        /// <summary>The number of items an apartment on the plot may have put down: the plot's own budget, held to the server's.</summary>
        public static int BudgetOf(Plot plot, int serverBudget) =>
            plot == null ? 0 : Math.Max(0, Math.Min(plot.Budget, serverBudget));

        /// <summary>
        /// Reads the three tables from a folder. A plot whose map is not one of
        /// <paramref name="isMapLoaded"/>'s, or an item whose class is not one of
        /// <paramref name="isClassKnown"/>'s, is left out (either may be null to take every row).
        /// </summary>
        public static ApartmentCatalogue Load(string directory, Func<uint, bool> isMapLoaded = null, Func<uint, bool> isClassKnown = null)
        {
            var catalogue = new ApartmentCatalogue();

            string Read(string file)
            {
                var path = Path.Combine(directory ?? "", file);

                if (File.Exists(path))
                    return File.ReadAllText(path);

                catalogue.Problems.Add($"{path} is missing.");
                return "";
            }

            catalogue.Parse(Read(PlotsFile), Read(CapsFile), Read(DecorFile), isMapLoaded, isClassKnown);

            return catalogue;
        }

        /// <summary>Reads the three tables from their text.</summary>
        public void Parse(string plots, string caps, string decor, Func<uint, bool> isMapLoaded = null, Func<uint, bool> isClassKnown = null)
        {
            foreach (var (line, row) in Rows(plots, PlotsFile, 15))
            {
                if (!TryUInt(row[0], out var id) || !TryInt(row[2], out var kind) || !TryUInt(row[3], out var map) ||
                    !TryVector(row, 4, out var min) || !TryVector(row, 7, out var max) || !TryVector(row, 10, out var entry) ||
                    !TryFloat(row[13], out var yaw) || !TryInt(row[14], out var budget))
                {
                    Problems.Add($"{PlotsFile} line {line}: a number does not read.");
                    continue;
                }

                var plot = new Plot
                {
                    Id = id, Name = row[1].Trim(), Kind = kind, MapContextId = map, BoxMin = Vector3.Min(min, max), BoxMax = Vector3.Max(min, max),
                    Entry = entry, EntryYaw = yaw, Budget = budget
                };

                if (id == 0 || _plots.ContainsKey(id))
                    Problems.Add($"{PlotsFile} line {line}: plot id {id} is 0 or given twice.");
                else if (isMapLoaded != null && !isMapLoaded(map))
                    Problems.Add($"{PlotsFile} line {line}: plot {id} {plot.Name} is on map {map}, which is not loaded.");
                else if (!plot.Contains(entry, 0.01f))
                    Problems.Add($"{PlotsFile} line {line}: plot {id} {plot.Name}'s entry is outside its build box.");
                else
                    _plots.Add(id, plot);
            }

            foreach (var (line, row) in Rows(caps, CapsFile, 3))
            {
                if (!TryUInt(row[0], out var plotId) || !TryInt(row[2], out var cap) || cap < 0)
                    Problems.Add($"{CapsFile} line {line}: a number does not read.");
                else
                    _caps[(plotId, row[1].Trim().ToLowerInvariant())] = cap;
            }

            foreach (var (line, row) in Rows(decor, DecorFile, 11))
            {
                if (!TryUInt(row[0], out var classId) || !TryInt(row[2], out var surface) || !TryInt(row[3], out var function) ||
                    !TryFloat(row[4], out var scaleMin) || !TryFloat(row[5], out var scaleMax) || !TryVector(row, 7, out var half) ||
                    !TryFloat(row[10], out var centreY))
                {
                    Problems.Add($"{DecorFile} line {line}: a number does not read.");
                    continue;
                }

                var item = new Decor
                {
                    ClassId = classId, Category = row[1].Trim(), Surface = surface,
                    Function = Enum.IsDefined(typeof(DecorFunction), function) ? (DecorFunction)function : DecorFunction.None,
                    ScaleMin = Math.Min(scaleMin, scaleMax), ScaleMax = Math.Max(scaleMin, scaleMax), Name = row[6].Trim(),
                    HalfExtents = Vector3.Abs(half), CentreY = centreY
                };

                if (classId == 0 || _decor.ContainsKey(classId) || item.Name.Length == 0 || item.Category.Length == 0 || item.ScaleMin <= 0)
                    Problems.Add($"{DecorFile} line {line}: class {classId} is 0, given twice, unnamed, uncategorised or has no scale.");
                else if (isClassKnown != null && !isClassKnown(classId))
                    Problems.Add($"{DecorFile} line {line}: class {classId} {item.Name} is not in the world database's entity classes.");
                else
                    _decor.Add(classId, item);
            }
        }

        /// <summary>The data rows of a table: no header, no comments, no blank lines; a row with too few fields is said and skipped.</summary>
        private IEnumerable<(int Line, string[] Row)> Rows(string text, string file, int fields)
        {
            var header = true;
            var number = 0;

            foreach (var raw in (text ?? "").Split('\n'))
            {
                number++;
                var line = raw.TrimEnd('\r');

                if (line.Trim().Length == 0 || line.TrimStart().StartsWith("#"))
                    continue;

                if (header)
                {
                    header = false;
                    continue;
                }

                var row = line.Split('\t');

                if (row.Length < fields)
                {
                    Problems.Add($"{file} line {number}: {row.Length} fields, {fields} wanted.");
                    continue;
                }

                yield return (number, row);
            }
        }

        private static bool TryUInt(string text, out uint value) =>
            uint.TryParse(text?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out value);

        private static bool TryInt(string text, out int value) =>
            int.TryParse(text?.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);

        private static bool TryFloat(string text, out float value) =>
            float.TryParse(text?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value) && float.IsFinite(value);

        private static bool TryVector(string[] row, int at, out Vector3 value)
        {
            value = default;

            if (!TryFloat(row[at], out var x) || !TryFloat(row[at + 1], out var y) || !TryFloat(row[at + 2], out var z))
                return false;

            value = new Vector3(x, y, z);
            return true;
        }
    }
}
