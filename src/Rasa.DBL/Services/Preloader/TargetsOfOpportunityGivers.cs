using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Targets of Opportunity of six battlefields given by their officers, and the bosses of
    /// two of them stood up, from a 2008 fan guide of the missions ("Just the Facts! Targets of
    /// Opportunity - Concordia" and "- Torden", the Giddy Gamer, 2008/02/03), whose coordinates
    /// are the game's /loc - x, height, y - and match the maps where there is something to
    /// match: Ista Kezever 14 m from the crashed dropship the guide puts him by, Tharvox 6 m
    /// from the Logos dispenser he guards, Vilescorn 25 m from Tahrendra Base's door.
    ///
    /// The givers. Each mission was offered by radio on arriving on its battlefield and handed
    /// in by radio; now the guide's officer offers it and takes it back, as Lieutenant Colonel
    /// Cimoch does Wilderness's at Alia Das. Each is an AFS officer in the blue uniform the
    /// other mission officers wear, pinned where they stand:
    ///  - Divide (1582): Agent Garrett at Foreas Base, in the ring of mission officers there;
    ///  - Palisades (1809): Surveyor Savinelli at Foreas Base too, beside them ("Salvinelli" in
    ///    the guide; the client's name table, and its mission 1810 "Talk to Surveyor
    ///    Savinelli", have Savinelli);
    ///  - Plains (1829): Sergeant Dekay at Irendas Penal Colony, by its vendors;
    ///  - Incline (1739): Captain Caruthers at Raintree Post, on the post's own label;
    ///  - Mires (1585): Commander Tumlinson in the Incline tunnel, a few metres down from where
    ///    a player coming from Incline arrives, facing them;
    ///  - Abyss (1593): Lieutenant Colonel Brigman at Icarus Post, by its waypoint.
    /// The missions' own text says nothing against it, but for Abyss's, which tells the player
    /// to report to "Lieutenant Colonel Kelly at Tantalus Base": no such name is in the client,
    /// and Brigman is.
    ///
    /// The bosses. The seven Mires officers ("Kill all 7 Bane Mires Officers", the Mires
    /// Assassin) and the Plains' Stalker Overlord, the one of its four mini-bosses
    /// (Bushwacker) the world had no row for. Each is named as the client names it, so the
    /// titles count them as they are (BossTitles). One pool each, 500 to come back, as the
    /// other named bosses. Level as the zone's band has it - the highest of the Bane the
    /// battlefield's pools set down, 41 on Mires and 40 on Plains, as the Plains' other three -
    /// and health, armour and abilities as their kind's bosses have them, the damage scaled
    /// from the kin's level to theirs as the region rows are (x 2^(levels / 8),
    /// FamilyAbilities). Where the guide gives no kind, a Thrax soldier, as most officers are.
    ///  - Phost'Benon, a Lightbender, by the Bane wall outside the Energy Weapons Center;
    ///  - Honjerhyn, on the Bane bridge by the Fluxite Mines (ArchBaneOutpostBridgeV02 at
    ///    -401, 882). The guide's -385, 665 is open ground with nothing on it; a later guide
    ///    has him on the bridge next to the mines' entrance, and this is the only bridge;
    ///  - Rendobar, a Predator, among the Bane pipes south of the mines;
    ///  - Ista Kezever, west of the Energy Weapons Center by the crashed dropship;
    ///  - Tharvox, a Technician, guarding the Logos: Location;
    ///  - Vilescorn, a Caretaker, by the teleporter at Tahrendra Base;
    ///  - Defiler Jemmert, whom the guide does not place: its seventh officer is Dedarrlik,
    ///    "bugged", near Tahrendra Base, and the client's list has Jemmert in his place. He
    ///    stands at the cave mouth north of Tahrendra Base, 50 m from Vilescorn - a guess;
    ///  - the Stalker Overlord, south of Geyser Chimney Basin's control point, whose gates the
    ///    guide says he assaults.
    ///
    /// And two bosses off the givers' ground, where the boss placement had put them on the
    /// place's map label: the Four Horsemen, all four on Raintree Post where Caruthers stands
    /// (their mission has them "patrolling in the Raintree Forest near Raintree Post"), go out
    /// into the forest 107 m east of the post, 55 m clear of its turret; and the Warnet Queen,
    /// on Foreas Base where Garrett and Savinelli stand, is taken away - she is the boss of the
    /// Warnet Caverns, brought out by a lure in "Kill the Queen", and nothing puts her in
    /// Divide.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike; Down puts everything back as it was.
    /// </summary>
    public static class TargetsOfOpportunityGivers
    {
        public const string Revision = TargetsOfOpportunitySeed.Revision;

        // ---- the givers ----

        public sealed record Giver(uint Id, uint MissionId, string Name, uint NameId, uint Map, double X, double Y, double Z, double Rotation, string Place);

        public static readonly Giver[] Givers =
        {
            new(510300, 1582, "Agent Garrett", 10136, 1148, -54.0, 116.37222, 464.3, 1.5708, "Foreas Base"),
            new(510301, 1809, "Surveyor Savinelli", 9899, 1148, -47.0, 117.29884, 470.0, 1.1, "Foreas Base"),
            new(510302, 1829, "Sergeant Dekay", 8908, 1764, 291.0, 433.2, -84.0, 6.1, "Irendas Penal Colony"),
            new(510303, 1739, "Captain Caruthers", 10022, 1761, -175.0, 278.97867, 305.0, 6.19, "Raintree Post"),
            new(510304, 1585, "Commander Tumlinson", 9950, 1759, 546.0, 233.03178, 836.0, 3.695, "the Incline tunnel"),
            new(510305, 1593, "Lieutenant Colonel Brigman", 10003, 2028, -10.0, 535.9454, -553.0, 2.46, "Icarus Post")
        };

        public const uint SwapsetMale = 3846;

        /// <summary>The mission officers' uniform (Captain Rankin's at Foreas Base): boots, pistol, hair, tunic, trousers, face.</summary>
        public static readonly (uint Slot, uint ClassId, uint Color)[] Uniform =
        {
            (2, 4021, 4294934528), (13, 6271, 1), (14, 9781, 4278655809), (15, 4023, 4294934528), (16, 4022, 4294934528), (17, 24008, 4286690539)
        };

        // ---- the bosses ----

        public sealed record Attack(uint Id, string Description, uint ActionId, uint ArgId, double RangeMin, double RangeMax,
            uint Cooldown, uint Windup, uint MinDamage, uint MaxDamage, uint DamageType);

        /// <summary>Each from its kin's row named, the damage scaled to the new boss's level.</summary>
        public static readonly Attack[] Attacks =
        {
            // Thrax officers, level 41, from Overseer Turquar's (520024, 42): 57018, 61024, 68023, 68024.
            new(78001, "Mires Thrax officer thrax_lightning", 449, 1, 1.0, 40.0, 5000, 500, 504, 1000, 13),
            new(78002, "Mires Thrax officer thrax_scourge", 455, 1, 0.0, 10.0, 20000, 500, 32, 72, 1),
            new(78003, "Mires Thrax officer thrax_shrapnel", 278, 1, 0.0, 12.0, 15000, 500, 248, 504, 1),
            new(78004, "Mires Thrax officer thrax_force_blast", 451, 1, 1.0, 40.0, 5000, 500, 504, 1000, 7),
            // Rendobar, from the Alpha Class Recon Predator's (520022, 42): 64009, 60006, 64010.
            new(78005, "Rendobar predator weapon 1/85", 1, 85, 1.0, 50.0, 3000, 0, 587, 880, 6),
            new(78006, "Rendobar predator_death", 407, 1, 0.0, 0.0, 0, 0, 800, 1200, 1),
            new(78007, "Rendobar predator_missile", 426, 1, 1.0, 40.0, 8000, 0, 2000, 2608, 1),
            // Phost'Benon, from Hygax's (520039, 28): 54013.
            new(78008, "Phost-Benon lightbender_quill", 202, 1, 1.0, 10.0, 4000, 2266, 207, 401, 1),
            // Tharvox, from Davinx's (520037, 28): 56017.
            new(78009, "Tharvox thrax_decay", 450, 1, 1.0, 30.0, 10000, 500, 71, 151, 4),
            // Vilescorn, from Caretaker Mordra's (520015, 20): 76030.
            new(78010, "Vilescorn caretaker_attack", 395, 3, 1.0, 40.0, 1000, 700, 401, 598, 1),
            // The Stalker Overlord, level 40, from the Marshes Stalker's (540002, 34): 55003, 65042, 65043.
            new(78011, "Stalker Overlord weapon 1/78", 1, 78, 1.0, 60.0, 1500, 0, 404, 605, 6),
            new(78012, "Stalker Overlord stalker_ovulate", 441, 1, 0.0, 20.0, 60000, 0, 74, 146, 1),
            new(78013, "Stalker Overlord stalker_egg_drop", 442, 1, 0.0, 0.0, 0, 3666, 2751, 3301, 5)
        };

        public sealed record Boss(uint Id, string Comment, uint ClassId, uint Level, uint NameId, uint Map, double X, double Y, double Z, uint[] Actions)
        {
            public uint MaxHp => 100 * Level + 500;
            public uint Armor => 5 * Level + 200;
        }

        public const uint ThraxRifleBoss = 10504, TechnicianBoss = 10502, LightbenderBoss = 10333, CaretakerBoss = 10321;
        public const uint Predator = 3902, Stalker = 3781;
        private static readonly uint[] ThraxOfficer = { 34, 78001, 61023, 78002, 78003, 78004 };

        public static readonly Boss[] Bosses =
        {
            new(520066, "Phost'Benon - Mires", LightbenderBoss, 41, 8840, 1759, -280.0, 227.89764, 120.0, new uint[] { 30, 78008 }),
            new(520067, "Honjerhyn - Mires", ThraxRifleBoss, 41, 8841, 1759, -401.0, 259.41602, 882.0, ThraxOfficer),
            new(520068, "Rendobar - Mires", Predator, 41, 8835, 1759, -525.3156, 231.9531, 439.6213, new uint[] { 78005, 78006, 63010, 78007 }),
            new(520069, "Defiler Jemmert - Mires", ThraxRifleBoss, 41, 10626, 1759, -800.0, 263.83447, 140.0, ThraxOfficer),
            new(520070, "Tharvox - Mires", TechnicianBoss, 41, 9893, 1759, -715.0, 256.101, 532.0, new uint[] { 70009, 78009, 56018, 62054, 65021, 68065 }),
            new(520071, "Vilescorn - Mires", CaretakerBoss, 41, 9894, 1759, -810.0, 264.21805, 90.0, new uint[] { 78010, 62033, 62034 }),
            new(520072, "Ista Kezever - Mires", ThraxRifleBoss, 41, 9898, 1759, -475.0, 232.07631, 350.0, ThraxOfficer),
            new(520073, "Stalker Overlord - Plains", Stalker, 40, 8794, 1764, 160.0, 428.80002, 180.0, new uint[] { 78011, 78012, 78013 })
        };

        public const uint BossRespawn = 500;

        // ---- the bosses moved off the givers' ground ----

        public sealed record Move(uint Pool, string Who, double FromX, double FromY, double FromZ, double ToX, double ToY, double ToZ);

        /// <summary>The Four Horsemen, from Raintree Post's label into the Raintree forest east of it, 4 m apart.</summary>
        public static readonly Move[] Horsemen =
        {
            new(520025, "Taskmaster Puuk", -175.0, 279.3, 309.5, -80.0, 269.65887, 215.0),
            new(520024, "Overseer Turquar", -175.0, 279.3, 309.5, -76.0, 269.23434, 215.0),
            new(520026, "Thrall Bruunk", -175.0, 279.3, 309.5, -84.0, 270.6711, 215.0),
            new(520027, "Thrall Yquog", -175.0, 279.3, 309.5, -80.0, 269.61252, 219.0)
        };

        /// <summary>The Warnet Queen's pool on Foreas Base's label, as BossSpawnpoolPreloader seeded it.</summary>
        public const uint WarnetQueenPool = 520046;
        public const float WarnetQueenX = -42.3f, WarnetQueenY = 116.3f, WarnetQueenZ = 478.7f;

        private const string PoolColumns =
            "id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
            "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
            "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
            "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius";

        public static IEnumerable<string> Up()
        {
            // The bosses: their attacks, rows, stats and pools.
            foreach (var attack in Attacks)
                yield return "insert into creature_action (id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage, damage_type) "
                    + $"values ({attack.Id}, {S(attack.Description)}, {attack.ActionId}, {attack.ArgId}, {F(attack.RangeMin)}, {F(attack.RangeMax)}, "
                    + $"{attack.Cooldown}, {attack.Windup}, {attack.MinDamage}, {attack.MaxDamage}, {attack.DamageType});";

            foreach (var boss in Bosses)
            {
                var actions = Enumerable.Range(0, 8).Select(index => index < boss.Actions.Length ? boss.Actions[index] : 0u);
                yield return Creature(boss.Id, boss.Comment, boss.ClassId, 0, boss.Level, boss.MaxHp, boss.NameId, 9, 5, actions);
                yield return $"insert into creature_stat (id, body, mind, spirit, health, armor) values ({boss.Id}, 15, 15, 15, {boss.MaxHp}, {boss.Armor});";
                yield return Pool(boss.Id, BossRespawn, boss.X, boss.Y, boss.Z, 0, boss.Map, boss.Id);
            }

            // The givers: their rows, uniforms and posts.
            foreach (var giver in Givers)
            {
                yield return Creature(giver.Id, $"{giver.Name} - {giver.Place}", SwapsetMale, 1, 10, 1000, giver.NameId, 0, 0, Enumerable.Repeat(0u, 8));
                yield return "insert into creature_appearance (id, slot_id, class_id, color) values "
                    + string.Join(", ", Uniform.Select(slot => $"({giver.Id}, {slot.Slot}, {slot.ClassId}, {slot.Color})")) + ";";
                yield return Pool(giver.Id, 20, giver.X, giver.Y, giver.Z, giver.Rotation, giver.Map, giver.Id);
            }

            // Their missions: given and handed in by them, not by radio.
            foreach (var giver in Givers)
            {
                yield return $"update mission_content_definition set giver_id = {giver.Id}, receiver_id = {giver.Id}, radio_completeable = 0 "
                    + $"where mission_id = {giver.MissionId} and content_revision = '{Revision}' and giver_id is null and receiver_id is null;";
                yield return $"delete from mission_channel_policy where mission_id = {giver.MissionId} and content_revision = '{Revision}';";
            }

            // The Horsemen into the forest; the Warnet Queen out of Foreas Base.
            foreach (var move in Horsemen)
                yield return $"update spawnpool set pos_x = {F(move.ToX)}, pos_y = {F(move.ToY)}, pos_z = {F(move.ToZ)} "
                    + $"where id = {move.Pool} and map_context_id = 1761 and pos_x = {F(move.FromX)} and pos_z = {F(move.FromZ)};";
            yield return $"delete from spawnpool where id = {WarnetQueenPool} and map_context_id = 1148;";
        }

        public static IEnumerable<string> Down()
        {
            yield return Pool(WarnetQueenPool, BossRespawn, WarnetQueenX, WarnetQueenY, WarnetQueenZ, 0, 1148, WarnetQueenPool);
            foreach (var move in Horsemen)
                yield return $"update spawnpool set pos_x = {F(move.FromX)}, pos_y = {F(move.FromY)}, pos_z = {F(move.FromZ)} "
                    + $"where id = {move.Pool} and map_context_id = 1761 and pos_x = {F(move.ToX)} and pos_z = {F(move.ToZ)};";

            foreach (var giver in Givers)
            {
                var zone = TargetsOfOpportunitySeed.Zones.Single(candidate => candidate.MissionId == giver.MissionId);
                yield return "insert into mission_channel_policy (mission_id, content_revision, acceptance_channel, completion_channel, radio_sources) "
                    + $"values ({giver.MissionId}, '{Revision}', 2, 2, {S(TargetsOfOpportunitySeed.RadioSources(zone))});";
                yield return $"update mission_content_definition set giver_id = null, receiver_id = null, radio_completeable = 1 "
                    + $"where mission_id = {giver.MissionId} and content_revision = '{Revision}' and giver_id = {giver.Id};";
            }

            var givers = string.Join(", ", Givers.Select(giver => giver.Id));
            var bosses = string.Join(", ", Bosses.Select(boss => boss.Id));
            yield return $"delete from spawnpool where id in ({givers}, {bosses});";
            yield return $"delete from creature_appearance where id in ({givers});";
            yield return $"delete from creature_stat where id in ({bosses});";
            yield return $"delete from creature where id in ({givers}, {bosses});";
            yield return $"delete from creature_action where id in ({string.Join(", ", Attacks.Select(attack => attack.Id))});";
        }

        private static string Creature(uint id, string comment, uint classId, uint faction, uint level, uint maxHp, uint nameId,
            uint runSpeed, uint walkSpeed, IEnumerable<uint> actions) =>
            "insert into creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, "
            + "action1, action2, action3, action4, action5, action6, action7, action8) values "
            + $"({id}, {S(comment)}, {classId}, {faction}, {level}, {maxHp}, {nameId}, {runSpeed}, {walkSpeed}, {string.Join(", ", actions)});";

        private static string Pool(uint id, uint respawn, double x, double y, double z, double rotation, uint map, uint creature) =>
            $"insert into spawnpool ({PoolColumns}) values ({id}, 0, 0, {respawn}, {F(x)}, {F(y)}, {F(z)}, {F(rotation)}, {map}, {creature}, 1, 1, "
            + "0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);";

        private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string text) => "'" + text.Replace("'", "''") + "'";
    }
}
