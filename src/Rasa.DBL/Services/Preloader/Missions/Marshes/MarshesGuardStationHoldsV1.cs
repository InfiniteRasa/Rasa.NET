using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;
using Rasa.Missions.Content.Marshes;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Head East, Young Man (920): one player with weapons holds the eastern guard station (review list, tester, real client
    /// 2026-11-04 recheck: the station held only with GM kills; with weapon kills wave 1's four Thrax killed it in 38-44 s;
    /// rifleman 2861 died even with GM kills; the two riflemen walked a few metres off their places before the talk).
    ///
    /// Why: Shaw's stand-in (760358, the defence's leader: his death fails the mission) and the two AFS soldiers (760359)
    /// have the escort stats, 4000 hit points; the waves' members go for the wards in turn, the leader first
    /// (MarshesDefenceScene without spare). The soldiers' row is a copy of the Alia Das AFS squad Rifleman's (670003) with its
    /// speeds, so they wander round their places (BehaviorManager.NeverMoves: only a creature with no speed stands still), as
    /// the Marshes NPCs did before to-fix 15 (MarshesNpcsStandStillV1).
    ///
    /// Now (ESTIMATE, no source gives the station's strength):
    ///  - The soldiers stand still: run_speed and walk_speed 0, as MarshesNpcsStandStillV1's NPCs. They shoot from their places.
    ///  - Hit points: Shaw's stand-in three times the escort's, 12000 (as Trapped!'s Samuel, to-fix 29); the soldiers twice,
    ///    8000. creature_stat and max_hp.
    ///  - The scene's spare (MarshesDefenceScene, as Trapped! 1052): the waves go for the soldiers first and for Shaw only
    ///    when neither lives.
    ///  - Wave 1 three Thrax, not four: its second Grenadier (wave-1-4, 124, 217.08, 557) is left out. Wave 2 stays (4 and the
    ///    Stalker).
    /// The binding is written whole both ways: up the new scene, down the scene as MarshesHeadEastV2 and
    /// Marshes_on_the_navmesh (920's ward-3) left it.
    /// </summary>
    public static class MarshesGuardStationHoldsV1
    {
        public const uint ShawHitPoints = 3 * MarshesEastAndNorthNpcsV1.EscortHealth;
        public const uint SoldierHitPoints = 2 * MarshesEastAndNorthNpcsV1.EscortHealth;
        public const string LeftOut = "wave-1-4";

        /// <summary>The waves now: MarshesHeadEastV2's with wave 1's last member left out.</summary>
        public static readonly (uint After, (uint Creature, ScenePosition At)[] Members)[] Waves =
        {
            (MarshesHeadEastV2.Waves[0].After, MarshesHeadEastV2.Waves[0].Members.Take(3).ToArray()),
            MarshesHeadEastV2.Waves[1]
        };

        /// <summary>The binding as the live server has it: MarshesHeadEastV2's, with Marshes_on_the_navmesh's ward-3.</summary>
        public static string Before()
        {
            var json = MarshesBindings.Written(MarshesHeadEastV2.Up, MarshesHeadEastV2.MissionId);
            foreach (var move in MarshesOnTheNavmeshV1.Moves.Where(move => move.Mission == MarshesHeadEastV2.MissionId))
                json = json.Replace(MarshesOnTheNavmeshV1.Actor(move.Role, move.Template, move.Before), MarshesOnTheNavmeshV1.Actor(move.Role, move.Template, move.Now));
            return json;
        }

        /// <summary>The scene now: spare on, wave 1's last member, its defeat sequence and that sequence gone.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = MarshesBindings.Read(Before());
            scene.Names[MarshesDefenceScene.Spare] = 1;
            scene.Actors.Remove(LeftOut);
            scene.Sequences.Remove(scene.DefeatSequences[LeftOut]);
            scene.DefeatSequences.Remove(LeftOut);
            return scene;
        }

        public static void Up(MigrationBuilder migration)
        {
            Stand(migration, still: true);
            Health(migration, ShawHitPoints, SoldierHitPoints);
            MarshesBindings.Write(migration, MarshesHeadEastV2.MissionId, MarshesBindings.Json(Scene()));
        }

        public static void Down(MigrationBuilder migration)
        {
            MarshesBindings.Write(migration, MarshesHeadEastV2.MissionId, Before());
            Health(migration, MarshesEastAndNorthNpcsV1.EscortHealth, MarshesEastAndNorthNpcsV1.EscortHealth);
            Stand(migration, still: false);
        }

        /// <summary>The soldiers' speeds: 0, or the Rifleman row's they were copied with (a derived table: MySQL reads no
        /// subquery on the table it updates).</summary>
        private static void Stand(MigrationBuilder migration, bool still)
        {
            const uint soldier = MarshesEastAndNorthNpcsV1.StationSoldier, rifleman = MarshesEastAndNorthNpcsV1.AfsRifleman;
            migration.Sql(still
                ? $"UPDATE creature SET run_speed = 0, walk_speed = 0 WHERE id = {soldier};"
                : $"UPDATE creature SET run_speed = (SELECT r.run_speed FROM (SELECT run_speed FROM creature WHERE id = {rifleman}) r), " +
                  $"walk_speed = (SELECT r.walk_speed FROM (SELECT walk_speed FROM creature WHERE id = {rifleman}) r) WHERE id = {soldier};");
        }

        private static void Health(MigrationBuilder migration, uint shaw, uint soldier)
        {
            foreach (var (id, health) in new[] { (MarshesEastAndNorthNpcsV1.ShawStandIn, shaw), (MarshesEastAndNorthNpcsV1.StationSoldier, soldier) })
            {
                migration.Sql($"UPDATE creature_stat SET health = {health} WHERE id = {id};");
                migration.Sql($"UPDATE creature SET max_hp = {health} WHERE id = {id};");
            }
        }
    }
}
