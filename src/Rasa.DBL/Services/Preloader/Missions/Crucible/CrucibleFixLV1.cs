using System.Collections.Generic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Ligo Crucible fix job L (real client 2026-11-08 14:2x-14:34, to-fix row 81; NOTES-crucible-triage mechanics item "Fix L"),
    /// a delta on migration 20271108002000 (set 11b, CrucibleLeakyPipesV1), whose data class stays as it is.
    ///
    /// The play: after Jaikev, Dhimand's copy of Leaky Pipes (1398) stood at (-25.2, 94.5, 410.1), 28 m from his post
    /// (-40, 94.61, 386) and 4.5 m from the Incurable pool 770051 (-22, 94.69, 416); uses from the north gave "You cannot see
    /// your target from here". Probe (scratch test, 2026-11-08, removed): the copy (Friendly, run 7 / walk 2 since set 11b,
    /// no pose) joins the fight of the hub's Incurables with the player nearby, chases them to their pool and is still
    /// fighting there 500 s later: invulnerable, the leash 60 m (BehaviorManager.MaxChaseDistance), the pool's Incurables
    /// come back every 90 s. Moving the post would not stop that.
    ///
    /// Now: the copy takes up the pose Standing when it comes (scene sequence 0, PoseActorIntent): it holds a post
    /// (NpcPoses.HoldsPost: no stroll), and as an invulnerable posted scene actor it does not go looking for a fight when it
    /// stands idle (CreatureGameplayRules.KeepsOutOfFights). Following the player or on its route to the repair place it
    /// scans as before. Hit, it does not fight back (an Invulnerable actor's hit never reaches Threat.FromDamage). The waves
    /// still go for it; at the repair place he no longer fights them, the player kills them (hidden 6 counts each death,
    /// whoever kills it). Its post after the route is the repair place (the route's scripted move sets its home there).
    /// Marker 2 stays on (-40, 94.61, 386).
    ///
    /// Reconstructed (ESTIMATE): no source says Dhimand does not fight; the client has him as a quest NPC who "programmed the
    /// Warden Bots to protect me" (12048), and before set 11b (speeds 0) he could not leave his post.
    /// </summary>
    public static class CrucibleFixLV1
    {
        public const uint MissionId = CrucibleLeakyPipesV1.MissionId;
        public const byte StandingPose = CrucibleLeakyPipesV1.StandingPose;
        public const string PoseKey = "dhimand-holds-his-post";

        /// <summary>1398's evidence row (set 11b wrote 1-3).</summary>
        public const uint Evidence = 4;

        public const string Source = "repository:CrucibleFixLV1";

        public const string Note =
            "Fix job L (to-fix row 81): after Jaikev, Dhimand's copy stood at (-25.2, 94.5, 410.1) by the Incurable pool 770051, " +
            "28 m off his post (-40, 94.61, 386): a scratch probe showed it joins the hub Incurables' fight with the player and " +
            "stays in it (invulnerable, leash 60 m, the pool respawns). Now the copy takes the pose Standing as it comes " +
            "(scene sequence 0): it holds its post and, invulnerable and posted, seeks no fight while it stands idle; following " +
            "or on its route it is as before, the waves still go for it. ESTIMATE: that he does not fight (no source).";

        /// <summary>The scene as set 11b bound it.</summary>
        public static MissionSceneDefinition Before() => CrucibleLeakyPipesV1.Scene();

        /// <summary>The same, the copy posed Standing as it comes.</summary>
        public static MissionSceneDefinition After()
        {
            var scene = Before();
            scene.Sequences[0].World.Add(new PoseActorIntent(PoseKey, CrucibleLeakyPipesV1.DhimandRole, StandingPose));
            return scene;
        }

        public static IEnumerable<string> UpSql()
        {
            yield return Swap(Before(), After());
        }

        public static IEnumerable<string> DownSql()
        {
            yield return Swap(After(), Before());
        }

        public static void Up(MigrationBuilder migration)
        {
            foreach (var sql in UpSql())
                migration.Sql(sql);
            CrucibleMissionDataV1.Existing(migration, MissionId)
                .Evidence(Evidence, MissionEvidenceSourceKind.Reconstruction, Source, Note, 0.3);
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.Sql($"DELETE FROM mission_evidence WHERE mission_id = {MissionId} " +
                          $"AND content_revision = '{CrucibleMissionDataV1.Revision}' AND evidence_id = {Evidence};");
            foreach (var sql in DownSql())
                migration.Sql(sql);
        }

        /// <summary>The scene's bindings from one to the other, only where they are the one: a second run changes nothing.</summary>
        private static string Swap(MissionSceneDefinition from, MissionSceneDefinition to) =>
            $"UPDATE mission_scene_binding SET bindings = {InstanceMissionSql.S(Json(to))} " +
            $"WHERE mission_id = {MissionId} AND content_revision = {InstanceMissionSql.S(CrucibleMissionDataV1.Revision)} " +
            $"AND bindings = {InstanceMissionSql.S(Json(from))};";

        public static string Json(MissionSceneDefinition scene) => JsonSerializer.Serialize(scene, MissionContentCodec.Options);
    }
}
