using Rasa.Missions.Scenes;

namespace Rasa.Missions.Content
{
    /// <summary>
    /// The attack orders and escort walks of the zone quest batches (Plateau, Pools). A scene places its attackers where
    /// it likes, often further from their target than the leash from home allows, so they go past it
    /// (AttackActorIntent.PastLeash; 1178: the first wave 100 m and more north of Villaran). An escort stops to fight
    /// when it is hit and walks on after (RunRouteIntent.FightWhenHit; 1178: Villaran walked on under the first wave's
    /// fire). The quest content makes these intents only here (QuestSceneIntentsTests).
    /// </summary>
    public static class QuestSceneIntents
    {
        public static AttackActorIntent Attack(string operationKey, string role, string targetRole = null,
            bool holdTarget = false) =>
            new(operationKey, role, targetRole, HoldTarget: holdTarget, PastLeash: true);

        public static RunRouteIntent Escort(string operationKey, string role, string route) =>
            new(operationKey, role, route, ResumeAfterCombat: true, FightWhenHit: true);
    }
}
