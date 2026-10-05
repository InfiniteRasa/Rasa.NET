using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    /// <summary>
    /// Sgt. Pierre is held, not fought, until she is freed. She stands in the Bane cache of
    /// Cache of the Day - 7 m from the Atropos Linker of pool 580012, inside the post of pool
    /// 580005 - as an ordinary FRIENDLY creature with no attack of her own, and hostile
    /// creatures seek any friendly one in their scan: the garrison killed her as often as she
    /// came back, before anyone had spoken to her.
    ///
    /// Escape Velocity's public encounter becomes a ManualCombat one
    /// (WildernessLandingZoneV1.EscapeVelocityScene): its actor is out of combat - not sought,
    /// not damaged, CreatureGameplayRules.CanParticipateInCombat - while nobody has the mission
    /// and while its owner is still at the forcefield. The scene's escort routes resume after
    /// combat, and SceneWorldAdapter puts a held actor in combat when it is set on such a
    /// route: from the forcefield's fall she can be fought, and her death fails the escort as
    /// before. A lease that resets holds her again.
    ///
    /// The binding is one document, so changing it means writing all of it. A migration with
    /// no target model cannot use UpdateData, so it is a statement.
    /// </summary>
    public static class WildernessHeldCaptiveV1
    {
        public const uint MissionId = 666;

        public static void Up(MigrationBuilder migration) => migration.Sql(Binding(held: true));

        public static void Down(MigrationBuilder migration) => migration.Sql(Binding(held: false));

        internal static string Binding(bool held)
        {
            var document = JsonSerializer.Serialize(WildernessLandingZoneV1.EscapeVelocityScene(held), MissionContentCodec.Options);

            // Numbers and plain names only: nothing a SQL string of either provider needs escaped.
            if (document.Contains('\'') || document.Contains('\\'))
                throw new InvalidOperationException("Escape Velocity's scene binding cannot be written as a plain SQL string.");

            return $"update mission_scene_binding set bindings = '{document}' "
                + $"where mission_id = {MissionId} and content_revision = '{WildernessMissionDataV1.Revision}';";
        }
    }
}
