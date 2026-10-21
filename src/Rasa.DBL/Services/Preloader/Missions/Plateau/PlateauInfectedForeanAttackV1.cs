using System.Collections.Generic;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// The Infected Forean's attack (PlateauInfectedForeansV1, creature 650024, action 650026), changed in
    /// its existing row: the goo gun's shot (1/116) plays recovery family 829, which the infected
    /// gunner's skeleton 14036 (mesh 35823) has no animation for - the client's animationdata gives that
    /// skeleton the Forean abilities only: lightning (203), chaff (204), lifeforce funnel (255), decay
    /// (448). The shot played nothing (CreatureAttackAnimationTests).
    ///
    /// Reconstructed (ESTIMATE): CR_FOREAN_LIGHTNING 203/1 (client actionArguments: windup 864, recovery
    /// 931, range 40), electrical as the server's Forean spearman lightning (action 6); the same damage
    /// (147-221) and reuse (2.5 s). The goo gun stays in its hand. Down puts the shot back.
    /// </summary>
    public static class PlateauInfectedForeanAttackV1
    {
        public const uint Weapon = PlateauInfectedForeansV1.Weapon;
        public const uint Lightning = 203, LightningArg = 1, Electrical = 13;

        public static IEnumerable<string> Up()
        {
            yield return "UPDATE creature_action SET description = 'Infected Forean - Plateau forean_lightning 203/1', " +
                $"action_id = {Lightning}, action_arg_id = {LightningArg}, range_max = 40, damage_type = {Electrical} WHERE id = {Weapon};";
        }

        public static IEnumerable<string> Down()
        {
            yield return "UPDATE creature_action SET description = 'Infected Forean - Plateau weapon 1/116', " +
                $"action_id = 1, action_arg_id = 116, range_max = 20, damage_type = 1 WHERE id = {Weapon};";
        }
    }
}
