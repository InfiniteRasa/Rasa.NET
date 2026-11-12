using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Trapped! (1052) with the crew out of the station's hostile reach (to-fix row 15, 2026-11-03). A creature scans its
    /// AggroRange (18 m by default, Creature.AggroRange) round where it stands, and a pool's creatures stand anywhere in its
    /// circle, so a crew member less than 18 m outside a hostile circle is in reach. MarshesTrappedV2's crew stood 15.1-19.2
    /// m outside Thrax pool 531291's circle (radius 18). The four now stand 8 m west and 4 m south of their V2 places, at
    /// least 24 m outside every hostile circle (ESTIMATE, navmesh probe 2026-11-03, each walked to from Samuel's World DB
    /// spot): the stand-in at (188.4, 219.6, -673.1), the Retreads at (182, -669), (190, -682), (184, -678). The approach area
    /// and the markers move with the stand-in. The public Samuel stays at the World DB's spot (upstream pool 510167).
    /// </summary>
    public static class MarshesTrappedV3
    {
        public static readonly ScenePosition SamuelAt = new(188.4f, 219.6f, -673.1f);

        public static readonly ScenePosition[] RetreadsAt =
        {
            new(182f, 219.58f, -669f), new(190f, 219.85f, -682f), new(184f, 218.81f, -678f)
        };

        public static void Up(MigrationBuilder migration) => MarshesTrappedV1.Write(migration, SamuelAt, RetreadsAt, approach: true);

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MarshesTrappedV1.MissionId);
    }
}
