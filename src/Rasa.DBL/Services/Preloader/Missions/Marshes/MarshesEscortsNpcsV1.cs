using System.Collections.Generic;
using System.Linq;
using Rasa.Missions.Content;
using Rasa.Missions.Content.Marshes;
using Rasa.Missions.Scenes;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// The Marshes set 9 (escorts) NPCs, Valverde Marshes (map 1454): the escorted (the Forean Warrior of 497, Retread Otto of
    /// 888, Lieutenant Bruncke of 889, the replacement soldiers of 1023: scene creatures, no pool), and 1023's three lookout
    /// officers Lieutenant Belfast, Delaio and Urqhart (in pools). MP Price (upstream, 510163) gives his package 386 to Bruncke
    /// (user decision U1, the triage; the Pools' Sheen precedent of a package change on an upstream row) and takes a
    /// server-only one, his id.
    ///
    /// Ids (block 760300-760399, MarshesPaludosNpcsV1; checked 2026-11-02 against the World DB and the code of all worktrees:
    /// 760332-760399 free): 760332 the Forean Warrior, 760333 Otto, 760334 Bruncke, 760335 Belfast, 760336 Delaio, 760337
    /// Urqhart, 760338 the replacement soldier (creature, creature_stat for the escorted; Belfast, Delaio, Urqhart creature,
    /// npc_package and pool; Otto and Bruncke appearance slots copied; Bruncke npc_package 386).
    ///
    /// Sourced: client names 8550 "Retread Otto", 4663 "Lieutenant Bruncke", 8787-8789 Belfast, Delaio, Urqhart
    /// (creaturenamelanguage); packages 386 (objectiveconversation 889,1, Bruncke's "I'll follow you in"), 1101 Belfast, 1102
    /// Delaio, 1100 Urqhart (objectiveconversation 1023,5-7). The Forean Warrior has no client name (class 6043
    /// Redshirt_Forean_Spearman "Forean Warrior"). Ten Ton Hammer Falcon Hold: Belfast at (110, 216, 405). World DB: the
    /// Holy Vengeance escort warrior 750110 (class 6043, the spear), Retread Vincent 510168 (Otto's look), Lieutenant
    /// Morrison 510162 (the officers' look), the Alia Das AFS squad Rifleman 670003 (class 29423, name 8717, its rifle; AliaDasAfsSquad).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3 or lower):
    ///  - The escorted have the escort stats (4000 health, 300 armour, EscortNpcStats) and These Old Bones' speeds (7 run,
    ///    2 walk). Levels: warrior 35 (the triage), Otto 35, Bruncke 37, soldiers 33 (their missions').
    ///  - Delaio and Urqhart at the Corman watchtowers (101.2, 216.8, 530.8) and (45.5, 216.9, 705.2) (the triage), on the
    ///    navmesh; all three walked to from Falcon Hold (probe of 2026-11-02).
    /// </summary>
    public static class MarshesEscortsNpcsV1
    {
        public const uint MapContextId = MarshesMissionDataV1.MapContextId;
        public const uint Vincent = MarshesStalkerWoodsNpcsV1.Vincent, Morrison = 510162, HolyVengeanceWarrior = 750110;
        public const uint Rifleman = AliaDasAfsSquad.Rifleman;  // Alia Das AFS squad Rifleman 670003 (class 29423)
        public const uint EscortRun = 7, EscortWalk = 2;
        public const uint EscortHealth = (uint)EscortNpcStats.Health, EscortArmor = (uint)EscortNpcStats.Armor;

        public const uint Warrior = 760332, WarriorLevel = 35;
        public const uint Otto = 760333, OttoName = 8550, OttoLevel = 35;
        public const uint Bruncke = 760334, BrunckeName = 4663, BrunckeLevel = 37, BrunckePackage = 386;
        public const uint Price = 510163, PricePackage = 510163;        // MP Price (upstream): his server-only package

        public const uint Belfast = 760335, BelfastName = 8787, BelfastPackage = 1101;
        public const uint Delaio = 760336, DelaioName = 8788, DelaioPackage = 1102;
        public const uint Urqhart = 760337, UrqhartName = 8789, UrqhartPackage = 1100;
        public const uint Soldier = 760338, SoldierLevel = 33;

        public static readonly (double X, double Y, double Z) BelfastAt = (110, 216.88, 405);
        public static readonly (double X, double Y, double Z) DelaioAt = (100, 216.88, 530.4);
        public static readonly (double X, double Y, double Z) UrqhartAt = (45.5, 216.88, 704);

        /// <summary>The followers' places behind the owner (as Bootcamp's squad of three).</summary>
        public static readonly ScenePosition[] FollowOffsets = { new(-1.5f, 0f, -2f), new(0f, 0f, -2.5f), new(1.5f, 0f, -2f) };

        public static IEnumerable<string> Up()
        {
            // The escorted: friendly, the escort stats.
            foreach (var (id, comment, row, name, level) in new (uint, string, uint, string, uint)[]
            {
                (Warrior, "Forean Warrior - Marshes (Forean Flu escort)", HolyVengeanceWarrior, "name_id", WarriorLevel),
                (Otto, "Retread Otto - Marshes (Tread Lightly escort)", Vincent, OttoName.ToString(), OttoLevel),
                (Bruncke, "Lieutenant Bruncke - Marshes (AWOL Andy)", Morrison, BrunckeName.ToString(), BrunckeLevel),
                (Soldier, "AFS replacement soldier - Marshes (Rest and Recreation escort)", Rifleman, "name_id", SoldierLevel)
            })
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, '{comment}', class_id, 1, {level}, {EscortHealth}, {name}, {EscortRun}, {EscortWalk}, " +
                             $"action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {row};";
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {row};";
                yield return Stat(id, EscortHealth, EscortArmor);
            }
            yield return $"UPDATE npc_package SET package_id = {PricePackage} WHERE id = {Price} AND package_id = {BrunckePackage};";
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({Bruncke}, {BrunckePackage}, 'Lieutenant Bruncke - Marshes');";

            foreach (var (id, comment, name, package, at) in new[]
            {
                (Belfast, "Lieutenant Belfast - Marshes, east of Falcon Hold (1023)", BelfastName, BelfastPackage, BelfastAt),
                (Delaio, "Lieutenant Delaio - Marshes, the Corman watchtower (1023)", DelaioName, DelaioPackage, DelaioAt),
                (Urqhart, "Lieutenant Urqhart - Marshes, the northern Corman watchtower (1023)", UrqhartName, UrqhartPackage, UrqhartAt)
            })
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, '{comment}', class_id, faction, level, max_hp, {name}, run_speed, walk_speed, action1, " +
                             $"action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {Morrison};";
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {Morrison};";
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, '{comment[..comment.IndexOf(" - ")]} - Marshes');";
                yield return Pool(id, at.X, at.Y, at.Z, MapContextId, id, 1, 1, 20, 0);
            }
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM spawnpool WHERE id IN ({Belfast}, {Delaio}, {Urqhart});";
            yield return $"DELETE FROM npc_package WHERE id IN ({Bruncke}, {Belfast}, {Delaio}, {Urqhart});";
            yield return $"UPDATE npc_package SET package_id = {BrunckePackage} WHERE id = {Price} AND package_id = {PricePackage};";
            yield return $"DELETE FROM creature_stat WHERE id IN ({Warrior}, {Otto}, {Bruncke}, {Soldier});";
            yield return $"DELETE FROM creature_appearance WHERE id BETWEEN {Warrior} AND {Soldier};";
            yield return $"DELETE FROM creature WHERE id BETWEEN {Warrior} AND {Soldier};";
        }

        /// <summary>One leg of an escort (MarshesEscortScene): its objective, start and arrival sequences, the stand objective, the group.</summary>
        public sealed record Leg(uint Objective, uint Start, uint Arrived, uint? Stand, uint Creature, ScenePosition[] At);

        /// <summary>The defeat sequence of leg <paramref name="leg"/>'s member <paramref name="member"/> (1-based).</summary>
        public static uint DefeatSequence(int leg, int member) => 100 + 10 * (uint)leg + (uint)member;

        /// <summary>
        /// The scene of an escort: the run's own creatures of each leg (FollowOffsets for a group), their defeat sequences,
        /// the names MarshesEscortScene reads. The caller adds the mission scenarios of the starts and arrivals.
        /// </summary>
        public static MissionSceneDefinition Scene(uint missionId, params Leg[] legs)
        {
            var scene = new MissionSceneDefinition
            {
                Script = "marshes.escort",
                MapContextId = MapContextId,
                DefeatSequences = new(),
                Names = new() { [MarshesEscortScene.Mission] = missionId, [MarshesEscortScene.Legs] = (uint)legs.Length }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < legs.Length; index++)
            {
                var number = index + 1;
                var leg = legs[index];
                scene.Names[MarshesEscortScene.Objective(number)] = leg.Objective;
                scene.Names[MarshesEscortScene.Start(number)] = leg.Start;
                scene.Names[MarshesEscortScene.Arrived(number)] = leg.Arrived;
                if (leg.Stand is { } stand)
                    scene.Names[MarshesEscortScene.Stand(number)] = stand;
                if (leg.Start != 0)
                    scene.Sequences[leg.Start] = new SceneSequenceDefinition();
                scene.Sequences[leg.Arrived] = new SceneSequenceDefinition();
                for (var member = 1; member <= leg.At.Length; member++)
                {
                    var role = MarshesEscortScene.Role(number, member);
                    scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, leg.Creature, leg.At[member - 1],
                        Orientation: 0, InitiallyInteractable: leg.Stand != null,
                        FollowOffset: leg.At.Length > 1 ? FollowOffsets[(member - 1) % FollowOffsets.Length] : null);
                    var sequence = DefeatSequence(number, member);
                    scene.DefeatSequences[role] = sequence;
                    scene.Sequences[sequence] = new SceneSequenceDefinition();
                }
            }
            return scene;
        }

        /// <summary>The roles of an escort's legs (for tests): leg, then member.</summary>
        public static string[] Roles(int leg, int members) =>
            Enumerable.Range(1, members).Select(member => MarshesEscortScene.Role(leg, member)).ToArray();
    }
}
