using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Trapped! (1052) with the crew safe until the player comes near (to-fix row 10, 2026-11-03). In play the stand-in and his
    /// Retreads stood at the Bane Refueling Station from the accept, 490 m from Vincent; with no player near one Retread and
    /// then Samuel died 96 s after the accept and 1052 failed before the player could arrive. The engine has no intent that
    /// changes an actor's gameplay policy (an invulnerable crew until the talk), so the crew now stands only when the player
    /// comes within ApproachRadius of Samuel's place: server-only hidden 3 completes on that area and starts the Approach
    /// scenario, the defence scene's "approach" sequence (MarshesDefenceScene). The public Samuel (package 120, not the
    /// Locate talk's 1120) stays visible until then.
    ///
    /// The station's Thrax pool 531291 (radius 18 at 225.1, -652.7) stood 12 m outside its circle from the Retread at (202,
    /// -672): that Retread now stands at (198, 219.49, -678), 19 m outside (ESTIMATE, navmesh probe 2026-11-03, walked to
    /// from Samuel). Samuel himself stays at the World DB's spot (15.1 m outside, the start-up check's 15 m). What killed
    /// them is not in the log (the scene's Signal lines name no attacker); the station's Thrax are the ESTIMATE.
    /// </summary>
    public static class MarshesTrappedV2
    {
        public const uint Near = 3, Approach = 3, ApproachArea = 1;
        public const double ApproachRadius = 40;

        public static readonly ScenePosition[] RetreadsAt =
        {
            MarshesTrappedV1.RetreadsAt[0], new(198f, 219.49f, -678f), MarshesTrappedV1.RetreadsAt[2]
        };

        public static void Up(MigrationBuilder migration) => MarshesTrappedV1.Write(migration, MarshesTrappedV1.SamuelAt, RetreadsAt, approach: true);

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MarshesTrappedV1.MissionId);
    }
}
