using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Game;
    using Structures.World;

    /// <summary>
    /// The Logos titles: "Wilderness Mentalist", "Discovered all Logos in Wilderness.", and the
    /// nine like it (titledata 370, 410, 422, 430, 456, 466, 477, 502, 734, 788).
    ///
    /// A character has one when they hold every Logos of the battlefield - whatever they
    /// learned them from, a shrine, a Logos stone or a clone's inheritance - and no mission
    /// stands between. The client's Targets of Opportunity have a "Collect all ... Logos"
    /// objective for eight of the ten; Crucible and Divide have the title and no objective,
    /// and what the Tabula holds is the same question for all ten.
    ///
    /// Which Logos are a battlefield's is where the world puts their shrines (the logos table):
    /// the ones on the battlefield's own map. Its instances are not counted - the Wilderness
    /// objective as the server's recovered definition of it has it (MissionDefinitionCatalog,
    /// mission 1449 objective 8) is the twelve of the open map, with none of the ten in Pravus
    /// Research, the Caves of Donn, Crater Lake or Guardian Prominence - except Quasso Station
    /// for Thunderhead, which its objective names: "Collect all of the Logos elements on
    /// Thunderhead and Quasso Station". Earth (408) stands on Wilderness and is not one of the
    /// twelve; it is left out by name (<see cref="Zone.NotCounted"/>).
    ///
    /// Looked at when a Logos is learned (CharacterManager.TryAddLogos) and when a character
    /// comes onto a map (ManifestationManager.AssignPlayer), which is where one who had them
    /// all before this was here, or was cloned from one who had, is given theirs. A title once
    /// given stays: a GM's .removelogos takes none back.
    /// </summary>
    public static class LogosTitles
    {
        /// <summary>One battlefield's title.</summary>
        public sealed class Zone
        {
            public uint TitleId { get; }
            public string Name { get; }

            /// <summary>The maps whose shrines count: the battlefield's own, and Quasso Station for Thunderhead.</summary>
            public IReadOnlyList<uint> Maps { get; }

            /// <summary>Logos with a shrine on those maps that the title does not ask for.</summary>
            public IReadOnlyList<uint> NotCounted { get; }

            internal Zone(uint titleId, string name, uint[] maps, params uint[] notCounted)
            {
                TitleId = titleId;
                Name = name;
                Maps = maps;
                NotCounted = notCounted;
            }
        }

        public static readonly IReadOnlyList<Zone> Zones = new[]
        {
            new Zone(370, "Wilderness", new uint[] { 1220 }, 408),      // Wilderness Mentalist; not Earth
            new Zone(466, "Divide", new uint[] { 1148 }),               // Divide Mentalist
            new Zone(502, "Palisades", new uint[] { 1244 }),            // Palisades Mentalist
            new Zone(410, "Plateau", new uint[] { 1497 }),              // Plateau Mentalist
            new Zone(477, "Pools", new uint[] { 1304 }),                // Pools Mentalist
            new Zone(734, "Mires", new uint[] { 1759 }),                // Mires Logos Master
            new Zone(430, "Ashen Desert", new uint[] { 1734 }),         // Desert Mentalist
            new Zone(788, "Thunderhead", new uint[] { 1911, 2105 }),    // Thunderhead Historian; and Quasso Station
            new Zone(456, "Abyss", new uint[] { 2028 }),                // Abyss Mentalist
            new Zone(422, "Crucible", new uint[] { 1993 })              // Crucible Mentalist
        };

        private static IReadOnlyDictionary<uint, HashSet<uint>> _logos = new Dictionary<uint, HashSet<uint>>();

        /// <summary>
        /// Works out each title's Logos from the world's shrines (LogosManager.LogosInit). A
        /// shrine counts whether or not its map is loaded: a battlefield the server has not got
        /// is one whose title cannot be had, not one that asks for nothing.
        /// </summary>
        public static void Load(IEnumerable<LogosEntry> shrines)
        {
            var byMap = (shrines ?? Enumerable.Empty<LogosEntry>()).ToLookup(shrine => shrine.MapContextId, shrine => shrine.Id);

            _logos = Zones.ToDictionary(
                zone => zone.TitleId,
                zone => zone.Maps.SelectMany(map => byMap[map]).Where(id => id != 0 && !zone.NotCounted.Contains(id)).ToHashSet());
        }

        /// <summary>The Logos a title asks for; none for a title that is not one of these, or before <see cref="Load"/>.</summary>
        public static IReadOnlyCollection<uint> LogosOf(uint titleId) =>
            _logos.TryGetValue(titleId, out var logos) ? logos : (IReadOnlyCollection<uint>)System.Array.Empty<uint>();

        /// <summary>The character has learned a Logos: the titles it is part of, if it was the last they lacked.</summary>
        public static void Collected(Client client, uint logosId) =>
            Give(client, zone => _logos.TryGetValue(zone.TitleId, out var logos) && logos.Contains(logosId));

        /// <summary>The character has come onto a map: every title their Tabula has earned and they have not got.</summary>
        public static void CatchUp(Client client) => Give(client, _ => true);

        private static void Give(Client client, System.Func<Zone, bool> concerns)
        {
            var player = client?.Player;

            if (player == null)
                return;

            lock (client.SyncRoot)
            {
                HashSet<uint> held = null;

                foreach (var zone in Zones)
                {
                    if (!concerns(zone) || !_logos.TryGetValue(zone.TitleId, out var logos) || logos.Count == 0)
                        continue;

                    lock (player.Titles)
                        if (player.Titles.Contains(zone.TitleId))
                            continue;

                    held ??= player.Logos.ToHashSet();

                    if (logos.IsSubsetOf(held))
                        ManifestationManager.Instance.GrantTitle(client, zone.TitleId);
                }
            }
        }
    }
}
