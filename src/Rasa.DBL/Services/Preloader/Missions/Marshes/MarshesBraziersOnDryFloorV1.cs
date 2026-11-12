using System.Collections.Generic;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Bringing the Spirit Home (864): the two braziers out of the temple fountains (review list, tester, real client
    /// 2026-11-04: "the brazier stands in the temple fountain water").
    ///
    /// Why: MarshesBringingTheSpiritHomeV1 put each brazier in the middle of its square of four shrines (-180, 28) and (-236,
    /// 28). The client's .map has a fountain there: a water pool (class 7108 water_foreas_pool_32m_clean_v01, at the square's
    /// middle, its water at 231.14) in a basin about 7 m across whose floor is 230.86, round a Forean statue (25535). The
    /// braziers stood on the basin floor (230.88), 0.26 m under the water. The temple floor round the basin (the stair
    /// corners 20804) is 231.66.
    ///
    /// Now (ESTIMATE, client floor and navmesh probe 2026-11-04): each brazier 5.5 m from its square's middle towards Elder
    /// Bargas, on the temple floor between the basin and the stair's edge: east (-185.5, 231.66, 28), west (-230.5, 231.66,
    /// 28). Both are on the navmesh, have the client floor under them, and are seen from every place 2 m round them (16 of
    /// 16; the west one had stood in the statue's collision, MarshesClientFloorTests' reported place). The altar marker
    /// (-208, 230.88, 28; radius 35) is the middle of the two places still and stays.
    /// </summary>
    public static class MarshesBraziersOnDryFloorV1
    {
        public static readonly (string Role, ScenePosition Before, ScenePosition Now)[] Moves =
        {
            ("altar-east", MarshesBringingTheSpiritHomeV1.Altars[0].At, new ScenePosition(-185.5f, 231.66f, 28f)),
            ("altar-west", MarshesBringingTheSpiritHomeV1.Altars[1].At, new ScenePosition(-230.5f, 231.66f, 28f))
        };

        public static IEnumerable<string> Up()
        {
            foreach (var (role, before, now) in Moves)
                yield return Place(role, before, now);
        }

        public static IEnumerable<string> Down()
        {
            foreach (var (role, before, now) in Moves)
                yield return Place(role, now, before);
        }

        private static string Place(string role, ScenePosition from, ScenePosition to) =>
            MarshesBindings.Place(MarshesBringingTheSpiritHomeV1.MissionId, role, SceneActorKind.Object, MarshesBringingTheSpiritHomeV1.BrazierClass, from, to);
    }
}
