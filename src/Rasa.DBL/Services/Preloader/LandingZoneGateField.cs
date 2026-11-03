using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The Landing Zone gate's force field is the point's (Landing_zone_gate_field). Wilderness
    /// posts set it down as the AFS's (WildernessPostNpcs.GateFieldId, world_destructible) and
    /// nothing changed it: with the Bane holding the point it stayed blue, let players walk in
    /// and stopped the Bane's own occupation squads at the gate. Linked to the point
    /// (ControlPointLinkEntry.KindForceField) it is whoever holds it: red while the Bane do.
    ///
    /// The link goes in only while the point and the field's row are there, the row a human
    /// gate field on the point's map, and no point has the field yet. Down takes out this link
    /// alone.
    /// </summary>
    public static class LandingZoneGateField
    {
        public const uint Point = 4;
        public const uint Map = 1220;
        public static uint Field => WildernessPostNpcs.GateFieldId;

        public static IEnumerable<string> Up()
        {
            yield return $"INSERT INTO {ControlPointLinkEntry.TableName} (control_point_id, kind, object_id) " +
                $"SELECT p.id, {ControlPointLinkEntry.KindForceField}, d.id FROM {ControlPointEntry.TableName} p, {WorldDestructibleEntry.TableName} d " +
                $"WHERE p.id = {Point} AND p.map_context_id = {Map} " +
                $"AND d.id = {Field} AND d.class_id = {WildernessPostNpcs.HumanGateField} AND d.map_context_id = {Map} " +
                $"AND NOT EXISTS (SELECT 1 FROM {ControlPointLinkEntry.TableName} x " +
                $"WHERE x.kind = {ControlPointLinkEntry.KindForceField} AND x.object_id = {Field});";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM {ControlPointLinkEntry.TableName} " +
                $"WHERE control_point_id = {Point} AND kind = {ControlPointLinkEntry.KindForceField} AND object_id = {Field};";
        }
    }
}
