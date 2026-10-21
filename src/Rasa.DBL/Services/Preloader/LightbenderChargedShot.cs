using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Lightbenders' shot (action 1, argument 149, WEAPON_ATTACK_LIGHT_BENDER) at the pace the
    /// client plays it (Lightbender_charged_shot): every creature_action row of it - Proctor Fulgor
    /// and Phost'Benon's (30), the Wilderness Light Bender Snipers' (630052) - had the 800 ms
    /// cooldown of the placeholder, five shots to the client's one.
    ///
    /// The client's actionArguments (1, 149) are windupDelayMs 1400 and recoveryDelayMs 3033, with
    /// no reuse time of its own: a charge, then the shot and the recovery after it, 4433 ms in all
    /// before the next can start. The server now lands the shot at the end of its windup
    /// (Managers.CreatureWindups.IsChargedShot), and the cooldown, which starts as the windup does,
    /// is the whole of it.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Up changes only rows still holding the
    /// placeholder, so a cooldown a game master has set is left alone, and can run again; Down
    /// puts the placeholder back on the rows that have this one.
    /// </summary>
    public static class LightbenderChargedShot
    {
        public const uint ActionId = 1;
        public const uint ActionArgId = 149;

        /// <summary>The cooldown the rows were seeded with.</summary>
        public const int PlaceholderCooldown = 800;

        /// <summary>The client's windup and recovery for (1, 149).</summary>
        public const int WindupMs = 1400;
        public const int RecoveryMs = 3033;

        public const int Cooldown = WindupMs + RecoveryMs;

        private static string Rows(int cooldown) =>
            $"WHERE action_id = {ActionId} AND action_arg_id = {ActionArgId} AND cooldown = {cooldown}";

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            yield return $"UPDATE creature_action SET cooldown = {Cooldown} {Rows(PlaceholderCooldown)};";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"UPDATE creature_action SET cooldown = {PlaceholderCooldown} {Rows(Cooldown)};";
        }
    }
}
