using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Game;
    using Structures;

    /// <summary>
    /// The boss titles: "Wilderness Assassin", "Assassinated all 6 Thrax Officers in
    /// Wilderness.", and the thirteen like it.
    ///
    /// A character has one when they have killed every boss on its list. No mission is asked:
    /// each kill is recorded for the character when it is made (character_boss_kill, one row a
    /// boss), and the title is given with the kill that completes the list. Kills made before
    /// the record was kept are not known.
    ///
    /// A boss is told by the name the client knows it by (creature.name_id, the client's
    /// creaturenamelanguage) on one of its battlefield's maps - the battlefield and the
    /// instances off it. Not by its creature row: most of the bosses the client names have no
    /// row in the world yet, and one that is given a row with its name counts from then on with
    /// nothing changed here. The map is part of it because a name is not always one creature's:
    /// the ordinary Mox of Pools, Marshes and Howling Maw are named "Mox Alpha Male" as Plateau's
    /// boss is.
    ///
    /// The lists are the client's, from the boss objective of each battlefield's Targets of
    /// Opportunity mission ("Assassin: Kill all 6 Thrax Wilderness Officers: Glognar, Phlegg,
    /// Rankash, Horntail, Old Scratch and Archfiend!"). Where the text and the name table spell
    /// a boss differently the name table's is used: "Jerix" is Overseer Jirex, "Esobac" Esoobac,
    /// "Prototype Juggernaut" the Advanced Juggernaut Prototype, "The Abomination" the Warden
    /// Abomination, "Lawfiod" Lawfoid, "Hubor" and "Rubor" the Overlinkers. Three lists the
    /// client does not name in full:
    ///  - Marshes, "Kill all 9 of the Bosses in Marshes": the nine boss rows the world has there.
    ///  - Pools, "Kill all 10 Special Bane!" (the title is "Hunters Hunter", from "Hunter: Kill
    ///    all the special Bane in Pools"): the ten one-word names the name table has together,
    ///    Cainynix and Lililax (10004, 10005) and Cavalon to Iceram (10014-10021). Seven of the
    ///    eight Pools boss rows are among them; Overseer Prysiam is not.
    ///  - Palisades, "Kill all 9 Important Bane!": the eight its objectives name. The ninth is
    ///    an objective with no text, added after the mission said eight, and is not known.
    /// Descent asks for seven and names eight - "Kill 7 Descent named bosses: Preceptor Gissa,
    /// Preceptor Vaasti, Preceptor Maalev, Preceptor Kai Zul, Y'mas, Snuffy, Billybob, and
    /// Fortuna or Merrick" - so seven of the eight are enough (<see cref="Zone.Needed"/>), and
    /// Fortuna and Merrick are one of them between the two.
    ///
    /// The kill is the killer's and their squad's, as a kill is shared for everything else it is
    /// worth (PartyManager.SharersOf: the members on the map within 200 m of the corpse).
    ///
    /// Looked at when a creature is killed (CreatureManager.HandleCreatureKill) and when a
    /// character comes onto a map (ManifestationManager.AssignPlayer), which gives a title whose
    /// kills are all recorded and which the character has not got.
    /// </summary>
    public static class BossTitles
    {
        /// <summary>One boss of a list: the names it goes by. More than one where the client has more than one for it.</summary>
        public sealed class Boss
        {
            public string Name { get; }
            public IReadOnlyList<uint> NameIds { get; }

            internal Boss(string name, params uint[] nameIds)
            {
                Name = name;
                NameIds = nameIds;
            }
        }

        /// <summary>One battlefield's title.</summary>
        public sealed class Zone
        {
            public uint TitleId { get; }
            public string Name { get; }

            /// <summary>The maps a kill counts on: the battlefield, then the instances off it.</summary>
            public IReadOnlyList<uint> Maps { get; }
            public IReadOnlyList<Boss> Bosses { get; }

            /// <summary>How many of the bosses have to have been killed: all of them, but for Descent.</summary>
            public int Needed { get; }

            internal Zone(uint titleId, string name, uint[] maps, int needed, params Boss[] bosses)
            {
                TitleId = titleId;
                Name = name;
                Maps = maps;
                Bosses = bosses;
                Needed = needed == 0 ? bosses.Length : needed;
            }
        }

        private static Boss B(string name, params uint[] nameIds) => new Boss(name, nameIds);

        public static readonly IReadOnlyList<Zone> Zones = new[]
        {
            // Wilderness Assassin: "Kill all 6 Thrax Wilderness Officers". The six of the
            // server's recovered objective (MissionDefinitionCatalog, mission 1449 objective 6).
            new Zone(367, "Wilderness", new uint[] { 1220, 1416, 1430, 1506, 1721, 2368 }, 0,
                B("Glognar", 6751, 7022), B("Phlegg", 9312), B("Rankash", 9313), B("Horntail", 6719), B("Old Scratch", 6717),
                B("Archfiend", 6716)),

            // Divide Assassin: "Kill 5 Divide Bosses".
            new Zone(465, "Divide", new uint[] { 1148, 1347, 1348, 1349, 1806 }, 0,
                B("Rotting Sal", 6758), B("Caretaker Mordra", 7018), B("Splatter", 7019), B("Queen Stazzle", 7020),
                B("The Meat Grinder", 6712)),

            // Palisades Assassin: "Kill all 9 Important Bane!" - the eight that are named.
            new Zone(509, "Palisades", new uint[] { 1244, 1384, 1394, 1397, 1803 }, 0,
                B("Urm", 20000016), B("Hygax", 9174), B("Kennilaxx", 6706), B("Shahrbaraz", 7023), B("Mirtanz", 9564),
                B("Davinx", 9562), B("Sinatrix", 9565), B("Lawfoid", 9563)),

            // Plateau Assassin: "Kill the 5 Major Bosses on Plateau".
            new Zone(407, "Plateau", new uint[] { 1497, 1502, 1823, 1830, 2029 }, 0,
                B("Goliath", 8654), B("Inquisitor Krakatus", 8605), B("Mox Alpha Male", 8797), B("Preceptor Vorvaak", 6874),
                B("Prototype Juggernaut", 8772)),

            // Hunters Hunter: "Kill all 10 Special Bane!" in Pools.
            new Zone(473, "Pools", new uint[] { 1304, 1465, 1694, 1763 }, 0,
                B("Cainynix", 10004), B("Lililax", 10005), B("Cavalon", 10014), B("Orax", 10015), B("Irix", 10016),
                B("Goriam", 10017), B("Krammitron", 10018), B("Grumble", 10019), B("Oakril", 10020), B("Iceram", 10021)),

            // Marshes Assassin: "Kill all 9 of the Bosses in Marshes".
            new Zone(501, "Marshes", new uint[] { 1454, 1429, 1451, 1700, 1743 }, 0,
                B("Caretaker Fik", 8756), B("Commander Barzak", 8757), B("Commander Sto", 8758), B("Daddy Long-Legs", 8917),
                B("Overseer Cast'ka", 8548), B("Overseer Koorva", 8833), B("Overseer Nivvik", 8624), B("Overseer Vesh", 8834),
                B("Seymour", 8916)),

            // Descent Assassin: "Kill 7 Descent named bosses" of the eight.
            new Zone(514, "Descent", new uint[] { 2047, 2156, 2163 }, 7,
                B("Preceptor Gissa", 9339), B("Preceptor Vaasti", 9670), B("Preceptor Maalev", 9338), B("Preceptor Kai Zul", 9337),
                B("Y'mas", 9737), B("Snuffy", 9738), B("Billy Bob", 9324), B("Fortuna or Merrick", 9331, 9333)),

            // Bushwacker: "Kill all 4 Torden Plains Mini-Bosses".
            new Zone(487, "Plains", new uint[] { 1764, 1773, 2034, 2093 }, 0,
                B("Cracked Tooth", 8864), B("Plains Devil Lord", 8786), B("Stalker Overlord", 8794), B("The Red Kraken", 8897)),

            // Mires Assassin: "Kill all 7 Bane Mires Officers".
            new Zone(733, "Mires", new uint[] { 1759, 2107, 2115, 2125 }, 0,
                B("Phost'Benon", 8840), B("Honjerhyn", 8841), B("Rendobar", 8835), B("Defiler Jemmert", 10626),
                B("Vilescorn", 9894), B("Ista Kezever", 9898), B("Tharvox", 9893)),

            // The Exorcist: "Kill all 4 Four Horsemen" of Incline.
            new Zone(647, "Incline", new uint[] { 1761, 1865, 2085, 2111 }, 0,
                B("Taskmaster Puuk", 6905), B("Overseer Turquar", 6904), B("Thrall Bruunk", 6906), B("Thrall Yquog", 6907)),

            // Desert Assassin: "Kill all 6 Thrax Overseers stationed in Ashen Desert".
            new Zone(428, "Ashen Desert", new uint[] { 1734, 1988, 2055, 2110 }, 0,
                B("Overseer Naxveik", 9353), B("Overseer Exing", 9325), B("Overseer Jirex", 9793), B("Overseer Mitecht", 9792),
                B("Overseer Himbri", 9475), B("Overseer Ulovik", 9363)),

            // Thunderhead Hero: "Kill All 9 Thunderhead Named Bosses". Esoobac is a mine
            // foreman by one of his names.
            new Zone(779, "Thunderhead", new uint[] { 1911, 2103, 2105, 2112 }, 0,
                B("Aramix", 9825), B("Arthox", 9823), B("Tiamox", 9787), B("Esoobac", 9758, 9482), B("Juroaz", 9756),
                B("Irrogar", 10028), B("Painrox", 9809), B("Porthox", 9824), B("Warden Abomination", 6770)),

            // Abyss Assassin: "Kill all 9 Bosses on Abyss".
            new Zone(454, "Abyss", new uint[] { 2028, 2155, 2190, 2203 }, 0,
                B("Nyogthu", 9694), B("Maxxum Lagrotz", 9853), B("Overfiend Grimor", 9181), B("Overseer Hoktul", 9854),
                B("Preceptor Magoz", 9855), B("Granitour Colony King", 9850), B("Overlinker Hubor", 9851),
                B("Overlinker Rubor", 9852), B("Thraxus Machina Super Soldier", 9734)),

            // Crucible Assassin: "Kill the 6 mission-related Thrax Officers on Crucible".
            new Zone(419, "Crucible", new uint[] { 1993, 1977, 2138, 2141, 2146 }, 0,
                B("Executioner Derge", 9179), B("Preceptor Molitor", 9172), B("Archfiend Krubb", 9175), B("Coercer Mungrel", 9176),
                B("Defiler Sedge", 9177), B("Maxxum Ugoretz", 9317))
        };

        private static readonly IReadOnlyDictionary<(uint MapContextId, uint NameId), Zone> ByKill = Zones
            .SelectMany(zone => zone.Maps.SelectMany(map => zone.Bosses.SelectMany(boss => boss.NameIds.Select(nameId => (zone, map, nameId)))))
            .ToDictionary(entry => (entry.map, entry.nameId), entry => entry.zone);

        /// <summary>
        /// Who a kill is recorded for besides the killer: their squad near the corpse. A test
        /// puts its own in place of the squad.
        /// </summary>
        public static Func<Client, Vector3, List<Client>> SharersOf { get; set; } = (killer, corpse) => PartyManager.Instance.SharersOf(killer, corpse);

        /// <summary>The title a creature of this name on this map is a boss of; null when it is no title's.</summary>
        public static Zone ZoneOf(uint mapContextId, uint creatureNameId) =>
            ByKill.TryGetValue((mapContextId, creatureNameId), out var zone) ? zone : null;

        /// <summary>
        /// A creature a player has the kill of. A boss of a title is recorded for them and for
        /// each of their squad who shares the kill, and whoever it was the last for has the title.
        /// </summary>
        public static void Killed(Client killer, MapChannel mapChannel, Creature creature)
        {
            if (killer?.Player == null || creature == null || creature.NameId == 0 || mapChannel?.MapInfo == null)
                return;

            var zone = ZoneOf(mapChannel.MapInfo.MapContextId, creature.NameId);

            if (zone == null)
                return;

            List<Client> sharers = null;

            try
            {
                sharers = SharersOf?.Invoke(killer, creature.Position);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Boss kill of {creature.NameId} on map {mapChannel.MapInfo.MapContextId}: the squad of {killer.Player.FamilyName} could not be found: {e.Message}");
            }

            // Whatever goes wrong with one player's record is theirs alone, and never the kill's.
            foreach (var sharer in sharers ?? new List<Client> { killer })
            {
                try
                {
                    Record(sharer, zone, creature.NameId);
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Boss kill of {creature.NameId} on map {mapChannel.MapInfo.MapContextId} could not be recorded for {sharer?.Player?.FamilyName}: {e.Message}");
                }
            }
        }

        /// <summary>The character has come onto a map: every title their recorded kills have earned and they have not got.</summary>
        public static void CatchUp(Client client)
        {
            var player = client?.Player;

            if (player == null)
                return;

            lock (player.BossKills)
                foreach (var zone in Zones)
                    Give(client, zone);
        }

        /// <summary>Whether the kills are enough of a title's list.</summary>
        public static bool Earned(Zone zone, ICollection<uint> kills) =>
            zone.Bosses.Count(boss => boss.NameIds.Any(kills.Contains)) >= zone.Needed;

        private static void Record(Client client, Zone zone, uint creatureNameId)
        {
            var player = client?.Player;

            if (player == null)
                return;

            // The player's own kills are the lock, and nothing is waited for under it: two
            // players sharing each other's kills at once cannot hold each other up.
            lock (player.BossKills)
                if (ManifestationManager.Instance.RecordBossKill(client, creatureNameId))
                    Give(client, zone);
        }

        private static void Give(Client client, Zone zone)
        {
            var player = client.Player;

            lock (player.Titles)
                if (player.Titles.Contains(zone.TitleId))
                    return;

            if (Earned(zone, player.BossKills))
                ManifestationManager.Instance.GrantTitle(client, zone.TitleId);
        }
    }
}
