using System.Collections.Generic;
using System.Linq;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The retail body tints of Valverde Plateau, Pools and Marshes creatures, and of the Boargar (creature_tint,
    /// CreatureTintEntry): hue and hue2 both the colour given, alpha 255, or a row with no tint,
    /// the model's stock colours. A creature not listed keeps the random tint (Creature.TintAtSpawn).
    /// Short source codes per group; the full sources are not kept in the code.
    ///
    /// Every RGB value is an ESTIMATE: no retail capture of a creature's tints exists. Each group
    /// says what is estimated (the value, the colour, or that the stock colours match the image).
    ///
    /// Data only, plain SQL valid on SQLite and MySQL: Up inserts only rows that are not there,
    /// Down deletes only rows that still have the values Up wrote; each is safe to run twice.
    /// </summary>
    public static class ValverdeCreatureTints
    {
        /// <summary>A row with no tint: the model's stock colours.</summary>
        private static readonly uint? Stock = null;

        /// <summary>A colour as Color.Hue packs it: (A &lt;&lt; 24) | (B &lt;&lt; 16) | (G &lt;&lt; 8) | R, alpha 255.</summary>
        public static uint Rgb(byte red, byte green, byte blue) =>
            0xFF000000u | ((uint)blue << 16) | ((uint)green << 8) | red;

        /// <summary>Each creature row and its tint (hue = hue2), or Stock.</summary>
        public static readonly IReadOnlyList<(uint CreatureId, uint? Hue)> Rows = new (uint, uint?)[]
        {
            // Boargar, Concordia Wilderness and Palisades: stock colours, no tint. A retail Young Forest Boargar
            // (L6, YT-WALK) shows the stock pale body and dark red crest. ESTIMATE: stock matches the image (Medium).
            (35, Stock),                     // Wilderness: Boargar General Spawn, level 2
            (44, Stock),                     // Wilderness: Young Forest Boargar, level 5
            (58, Stock),                     // Wilderness: NPC_Creature_Boargar, level 8
            (540007, Stock),                 // Palisades: Boargar - Palisades, level 23

            // Stock colours, no tint (the sources show stock colours or no tint effect). ESTIMATE: stock matches the image (Medium).
            (520053, Stock),                 // Plateau: Mox Alpha Male, level 32
            (540015, Stock),                 // Plateau: Tomb Miasma, level 27
            (540016, Stock),                 // Plateau: Mox Adult, level 28
            (750021, Stock),                 // Plateau: Vehicle_Bane_Stalker, level 31
            (750022, Stock),                 // Plateau: Tomb Miasma, level 30
            (750023, Stock),                 // Plateau: Tomb Miasma Bull, level 31
            (750026, Stock),                 // Plateau: Advanced Juggernaut Prototype, level 32
            (750028, Stock),                 // Plateau: NPC_Holographic_Eloh, level 34
            (750037, Stock),                 // Plateau: Desolation Quarry Surveillance Sentinel One, level 31
            (750038, Stock),                 // Plateau: Desolation Quarry Surveillance Sentinel Two, level 31
            (750039, Stock),                 // Plateau: Desolation Quarry Surveillance Sentinel Three, level 31
            (750040, Stock),                 // Plateau: Desolation Quarry Surveillance Sentinel Four, level 31
            (750041, Stock),                 // Plateau: Desolation Quarry Surveillance Sentinel Five, level 31
            (531097, Stock),                 // Pools: Maw Bloom, level 31
            (540004, Stock),                 // Pools: Root Miasma, level 30
            (540009, Stock),                 // Pools: Fiend Pup Howler, level 30
            (540011, Stock),                 // Pools: Mox Alpha Male, level 31
            (650256, Stock),                 // Pools: Thrax Cavalryman, level 30
            (760226, Stock),                 // Pools: Mutated Miasma, level 33
            (760230, Stock),                 // Pools: Mox Alpha Male, level 33
            (531052, Stock),                 // Marshes: Maw Bloom, level 34
            (540002, Stock),                 // Marshes: Vehicle_Bane_Stalker, level 34
            (540012, Stock),                 // Marshes: Maw Bloom, level 34
            (540013, Stock),                 // Marshes: Mox Alpha Male, level 34
            (540014, Stock),                 // Marshes: Swamp Grubber Bull, level 33
            (760315, Stock),                 // Marshes: Thrax Cavalryman, level 36
            (760331, Stock),                 // Marshes: Bane_Lightbender_Standard, level 36
            (760355, Stock),                 // Marshes: Forean Prisoner Leader, level 33

            // Stock colours, no tint (the sources show stock colours or no tint effect). Not estimated: a tint has no effect on the model.
            (531087, Stock),                 // Plateau: Shield Drone Slayer, level 28
            (551004, Stock),                 // Plateau: Emplacement_Bane_Turret_Standard, level 32
            (592004, Stock),                 // Plateau: Emplacement_Bane_Turret_Standard, level 30
            (750013, Stock),                 // Plateau: Maligo Elite Guardmaster, level 32
            (520055, Stock),                 // Pools: Cavalon, level 35
            (531094, Stock),                 // Pools: Shield Drone Controller, level 31
            (551005, Stock),                 // Pools: Emplacement_Bane_Turret_Standard, level 35
            (592005, Stock),                 // Pools: Emplacement_Bane_Turret_Standard, level 33
            (531050, Stock),                 // Marshes: Shield Drone Controller, level 34
            (531053, Stock),                 // Marshes: Treeback Adult, level 36
            (551006, Stock),                 // Marshes: Emplacement_Bane_Turret_Standard, level 38
            (592006, Stock),                 // Marshes: Emplacement_Bane_Turret_Standard, level 36

            // Stock colours, no tint (the sources show stock colours or no tint effect). ESTIMATE: stock matches the image (Low-Medium).
            (531085, Stock),                 // Plateau: Caretaker Croaker, level 28
            (540003, Stock),                 // Plateau: Warnet Soldier, level 27
            (540017, Stock),                 // Plateau: Filcher Rook, level 27
            (750012, Stock),                 // Plateau: Maligo Elite Guard, level 31
            (531092, Stock),                 // Pools: Caretaker Healer, level 31
            (531095, Stock),                 // Pools: Hunter Tracker, level 30
            (531096, Stock),                 // Pools: Hunter Lieutenant Tracker, level 33
            (540010, Stock),                 // Pools: Xanx Hunter, level 30
            (531048, Stock),                 // Marshes: Caretaker Healer, level 34
            (531051, Stock),                 // Marshes: Hunter Tracker, level 33
            (531054, Stock),                 // Marshes: Hunter Lieutenant Tracker, level 36
            (760351, Stock),                 // Marshes: Bane_Forean_Machina, level 36

            // Stock colours, no tint (the sources show stock colours or no tint effect). ESTIMATE: stock matches the image (Low).
            (750024, Stock),                 // Plateau: Infected Forean, level 30

            // Thrax red at levels 27-38 (FT, TW-Gren, TT, TRp). ESTIMATE: value; colour from the rank rule.
            (520052, Rgb(110, 8, 0)),        // Plateau: Inquisitor Krakatus, level 32
            (531082, Rgb(110, 8, 0)),        // Plateau: Thrax Soldier Mark III, level 27
            (531083, Rgb(110, 8, 0)),        // Plateau: Bane_Thrax_Grenadier, level 27
            (531084, Rgb(110, 8, 0)),        // Plateau: Thrax Technician Specialist, level 28
            (750062, Rgb(110, 8, 0)),        // Plateau: Thrax Soldier Mark IV, level 31
            (750063, Rgb(110, 8, 0)),        // Plateau: Thrax Infantry Warrant Officer, level 31
            (520060, Rgb(110, 8, 0)),        // Pools: Lililax, level 35
            (531089, Rgb(110, 8, 0)),        // Pools: Thrax Soldier Mark IV, level 30
            (531090, Rgb(110, 8, 0)),        // Pools: Bane_Thrax_Grenadier, level 30
            (531091, Rgb(110, 8, 0)),        // Pools: Thrax Technician Sergeant, level 31
            (760210, Rgb(110, 8, 0)),        // Pools: Nakrinx, level 33
            (760211, Rgb(110, 8, 0)),        // Pools: Thrax Soldier Mark IV, level 30
            (760216, Rgb(110, 8, 0)),        // Pools: Gaite, level 36
            (760217, Rgb(110, 8, 0)),        // Pools: Kwel, level 36
            (531045, Rgb(110, 8, 0)),        // Marshes: Thrax Soldier Mark IV, level 33
            (531046, Rgb(110, 8, 0)),        // Marshes: Bane_Thrax_Grenadier, level 33
            (531047, Rgb(110, 8, 0)),        // Marshes: Thrax Technician Sergeant, level 34
            (760314, Rgb(110, 8, 0)),        // Marshes: Conscript Kaav, level 37
            (760356, Rgb(110, 8, 0)),        // Marshes: Thrax Soldier Mark IV, level 33

            // Kael plates red as the Thrax (TRp, TW-Kael). ESTIMATE: value; colour from the rank rule.
            (520051, Rgb(110, 8, 0)),        // Plateau: Goliath, level 32
            (531088, Rgb(110, 8, 0)),        // Plateau: Kael Corporal, level 28

            // Officers white/grey (TRp; TW-Ovs conflicts). ESTIMATE: colour from the rank rule. The value is the one
            // seen as light silver/white-grey in the real client (2026-11-06); 110 and 150 showed no grey, 200 a dark grey.
            (520054, Rgb(255, 255, 255)),    // Plateau: Thrax Preceptor Vorvaak, level 32
            (531086, Rgb(255, 255, 255)),    // Plateau: Thrax Commander, level 30
            (750088, Rgb(255, 255, 255)),    // Plateau: Watchman Zyrutha, level 32
            (750089, Rgb(255, 255, 255)),    // Plateau: Overseer Kythura, level 33
            (520062, Rgb(255, 255, 255)),    // Pools: Overseer Prysiam, level 35
            (531093, Rgb(255, 255, 255)),    // Pools: Thrax Commander, level 33
            (520029, Rgb(255, 255, 255)),    // Marshes: Commander Barzak, level 38
            (520030, Rgb(255, 255, 255)),    // Marshes: Commander Sto, level 38
            (520032, Rgb(255, 255, 255)),    // Marshes: Overseer Cast'ka, level 38
            (520033, Rgb(255, 255, 255)),    // Marshes: Overseer Koorva, level 38
            (520034, Rgb(255, 255, 255)),    // Marshes: Overseer Nivvik, level 38
            (520035, Rgb(255, 255, 255)),    // Marshes: Overseer Vesh, level 38
            (531049, Rgb(255, 255, 255)),    // Marshes: Thrax Commander, level 36

            // Goriam: purple/violet over yellow-green stock (TW-Goriam); additive tint cannot remove green. ESTIMATE: RGB value read from the image (no retail capture).
            (520056, Rgb(110, 0, 120)),      // Pools: Goriam, level 35

            // Grumble: pale yellow-green shell with crimson streaks (TW-Grumble, small image). (30, 40, 0) showed no green in the client; (120, 140, 40) shows yellow-green / olive. ESTIMATE: RGB value read from the image (no retail capture).
            (650248, Rgb(120, 140, 40)),        // Pools: Grumble, level 35

            // Oakril: no image; amber/oak brown over the pale Tree Lurker. (40, 25, 0) drew the model flat black in the client; (200, 150, 90) shows amber bark. ESTIMATE: colour and value (no image).
            (650249, Rgb(200, 150, 90)),        // Pools: Oakril, level 35

            // Cainynix: dark maroon/plum on reddish-brown stock (TW-Cainynix). ESTIMATE: RGB value; (60, 0, 40) showed bright red
            // in the real client, (40, 0, 90) plum (2026-11-06).
            (650247, Rgb(40, 0, 90)),        // Pools: Cainynix, level 35

            // Fearce: no image; Bane boss rank red-orange on the black armour. ESTIMATE: colour and value (no image).
            (760218, Rgb(90, 20, 0)),        // Pools: Fearce, level 36

            // Iceram: no image; icy blue after the name. ESTIMATE: colour and value (no image).
            (520057, Rgb(0, 40, 90)),        // Pools: Iceram, level 35

            // Krammitron: dark red/maroon over grey stock (TW-Krammitron). ESTIMATE: RGB value read from the image (no retail capture).
            (520059, Rgb(90, 0, 10)),        // Pools: Krammitron, level 35

            // Orax: crimson (TW-Orax). ESTIMATE: RGB value read from the image (no retail capture).
            (520061, Rgb(130, 10, 0)),       // Pools: Orax, level 35

            // Irix: crimson (TW-Irix). ESTIMATE: RGB value read from the image (no retail capture).
            (520058, Rgb(130, 10, 0)),       // Pools: Irix, level 35

            // Caretaker Fik: no image; teal, matches the Caretaker cyan eye. ESTIMATE: colour and value (no image).
            (520028, Rgb(0, 50, 70)),        // Marshes: Caretaker Fik, level 38

            // Seymour: no image; boss own colour, magenta-red over the orange Maw stalk. ESTIMATE: colour and value (no image).
            (520036, Rgb(60, 0, 30)),        // Marshes: Seymour, level 38

            // Daddy Long-Legs: no image; purple, apart from Krammitron dark red. ESTIMATE: colour and value (no image).
            (520031, Rgb(70, 0, 60)),        // Marshes: Daddy Long-Legs, level 38
        };

        public static IEnumerable<string> Up() => Rows.Select(row =>
            $"INSERT INTO {CreatureTintEntry.TableName} (id, hue, hue2) " +
            $"SELECT {row.CreatureId}, {Sql(row.Hue)}, {Sql(row.Hue)} FROM (SELECT 1 AS one) d " +
            $"WHERE NOT EXISTS (SELECT 1 FROM {CreatureTintEntry.TableName} x WHERE x.id = {row.CreatureId});");

        public static IEnumerable<string> Down() => Rows.Select(row =>
            $"DELETE FROM {CreatureTintEntry.TableName} WHERE id = {row.CreatureId} AND " +
            (row.Hue is { } hue ? $"hue = {hue} AND hue2 = {hue};" : "hue IS NULL AND hue2 IS NULL;"));

        private static string Sql(uint? value) => value?.ToString() ?? "NULL";
    }
}
