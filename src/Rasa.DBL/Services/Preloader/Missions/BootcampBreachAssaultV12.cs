using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions
{
    /// <summary>
    /// The six Thrax through the Proving Grounds' cave-in (BootcampCaveInBreachV9) take the sandbag
    /// post before the bridge, as a wave sent at a defended post should.
    ///
    /// They ran one route, out of the doorwell (300, 64.5) and onto the bridge (329, 64.5), and a
    /// creature on a scripted walk looks for no fight: they ran past the post and its two
    /// Infantrymen to the bridge, and stood there at the end of it. Now each runs its own:
    ///  - out of the doorwell to a line of six facing the post, two rows of three around
    ///    (294.3, 61) - where a GM stood to show it - its waypoint held and fought from
    ///    (EngageRadius <see cref="LineReach"/>): whatever it would pick a fight with within
    ///    that reach - the Infantrymen at the sandbags, 19.5 to 22.5 m off, the player, the
    ///    escorts - it takes on, comes back to the line after each fight and holds
    ///    <see cref="LineHoldMs"/> more, and pushes on only once a hold passes with nothing left;
    ///  - then up onto the bridge, two rows of three across it at x 327 and 330, held and
    ///    fought from too (<see cref="BridgeReach"/>) for <see cref="BridgeHoldMs"/> after the
    ///    last fight, rather than standing there with nothing to do.
    /// Every point is on the bridge's or the floor's walkable mesh. One route each, so the six
    /// do not end on one spot.
    ///
    /// On Capture the Flag's scene as BootcampCaveInFortSquadV11 leaves it: Youngblood's squad and
    /// the rocks' view radius are kept, everything but the wave's routes as it was.
    /// </summary>
    public static class BootcampBreachAssaultV12
    {
        private const string Revision = BootcampMissionDataV1.Revision;

        public const float LineReach = 25f;
        public const uint LineHoldMs = 5000;
        public const float BridgeReach = 20f;
        public const uint BridgeHoldMs = 600000;

        /// <summary>Facing east, at the post and up the bridge (yaw -pi/2, as the doorwell).</summary>
        public const double East = 4.7124;

        /// <summary>The line before the post: front row at x 295.5, back row at x 292.5, z 58.5 / 61 / 63.5, on the floor's mesh.</summary>
        public static readonly ScenePosition[] Line =
        {
            new(295.5f, 120.673f, 58.5f), new(295.5f, 120.679f, 61f), new(295.5f, 120.697f, 63.5f),
            new(292.5f, 120.693f, 58.5f), new(292.5f, 120.690f, 61f), new(292.5f, 120.705f, 63.5f)
        };

        /// <summary>Across the bridge: x 327 and 330, z 62.5 / 64.5 / 66.5, on the bridge's mesh.</summary>
        public static readonly ScenePosition[] Bridge =
        {
            new(330f, 121.788f, 62.5f), new(330f, 121.772f, 64.5f), new(330f, 121.772f, 66.5f),
            new(327f, 121.792f, 62.5f), new(327f, 121.775f, 64.5f), new(327f, 121.788f, 66.5f)
        };

        public static string RouteKey(int index) => $"{BootcampCaveInBreachV9.ChargeRoute}-{index + 1}";

        public static MissionSceneDefinition CaptureTheFlag()
        {
            var scene = BootcampCaveInFortSquadV11.CaptureTheFlag();

            scene.Routes.Remove(BootcampCaveInBreachV9.ChargeRoute);

            var wave = scene.Sequences[BootcampCaveInBreachV9.WaveSequence];
            for (var index = 0; index < Line.Length; index++)
            {
                var role = BootcampCaveInBreachV9.ThraxRole(index);
                var key = RouteKey(index);

                scene.Routes[key] = new SceneRoute(key, new[]
                {
                    new SceneWaypoint(Line[index], East, LineHoldMs, LineReach),
                    new SceneWaypoint(Bridge[index], East, BridgeHoldMs, BridgeReach)
                }, Speed: 7);

                var charge = wave.World.OfType<RunRouteIntent>().Single(intent => intent.Role == role);
                wave.World[wave.World.IndexOf(charge)] = charge with { Route = key };
            }

            return scene;
        }

        public static void Up(MigrationBuilder migration) =>
            MissionDataMigration.UpdateSceneBindings(migration, 1994, Revision, CaptureTheFlag());

        public static void Down(MigrationBuilder migration) =>
            MissionDataMigration.UpdateSceneBindings(migration, 1994, Revision, BootcampCaveInFortSquadV11.CaptureTheFlag());
    }
}
