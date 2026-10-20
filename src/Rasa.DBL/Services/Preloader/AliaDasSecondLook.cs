using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    using static AliaDasNpcs;

    /// <summary>
    /// Alia Das after a second look round in game (Alia_das_second_look).
    ///
    ///  - The CELLAR Arena link's pads were the outpost waypoint (arch_hum_outpost_waypoint,
    ///    9 m across). They become the human teleporter (UsableTwoStateHumTeleporter,
    ///    arch_hum_teleporter: 2.7 m across, its field 3.6 m high), the one the arena's red
    ///    boxing-map teleporter is a copy of. At Alia Das its centre goes 0.26 m east of the
    ///    link's, to 749.9, so that its 1.34 m clears the wall the arena sign hangs on (the floor
    ///    ends at 748.5); the link's 1.5 m circle still takes in the whole of it.
    ///  - Engineer Salter: Motor Assist 03 (avatar_hum_m_armor_light_v03) was a sleeveless vest
    ///    over bare arms and a cloth cap. He wears Motor Assist 11 (avatar_hum_m_armor_light_v11):
    ///    a long-sleeved shirt and trousers strapped round the thigh and below the knee, still
    ///    brown; and the Reflective 1 helmet (Clothing_Armor_Medium_1_Helm,
    ///    avatar_hum_m_armor_medium_v01), a plain metal dome, in silver.
    ///  - Dr. Elise Corman: red hair.
    ///  - Captain Spaulding: Clothing_Helmet_3 in green, as Quartermaster Caufield and Arms
    ///    Supplier Oliver wear with the same armour.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike, every change guarded on the row as it was.
    /// </summary>
    public static class AliaDasSecondLook
    {
        public const uint Light11Boots = 22033, Light11Gloves = 22034, Light11Legs = 22036, Light11Torso = 22037;
        public const uint Medium1Helm = 10951;

        public const uint WaypointPad = 25651, HumanTeleporter = 7895, LocalTeleporterType = 1;

        public sealed record PadChange(uint Id, uint MapContextId, double FromX, double ToX, double Z);

        public static readonly PadChange[] Pads =
        {
            new(990, Map, 749.6406, 749.9, 382.5547),
            new(991, 20000009, 13.1962, 13.1962, -4.4361),
        };

        public static readonly Look[] Looks =
        {
            new(Salter, "Engineer Salter: Motor Assist 11 in brown, a silver metal helmet",
                new[] { new Slot(1, AliaDasTouchups.Light03Helm, Silver), new Slot(2, AliaDasTouchups.Light03Boots, Brown), new Slot(3, AliaDasTouchups.Light03Gloves, Brown),
                        new Slot(13, RifleElectric, 1), new Slot(14, AliaDasTouchups.Hair05, DarkHair), new Slot(15, AliaDasTouchups.Light03Torso, Brown),
                        new Slot(16, AliaDasTouchups.Light03Legs, Brown), new Slot(17, 24008, SkinLight) },
                new[] { new Slot(1, Medium1Helm, Silver), new Slot(2, Light11Boots, Brown), new Slot(3, Light11Gloves, Brown),
                        new Slot(13, RifleElectric, 1), new Slot(14, AliaDasTouchups.Hair05, DarkHair), new Slot(15, Light11Torso, Brown),
                        new Slot(16, Light11Legs, Brown), new Slot(17, 24008, SkinLight) }),
            new(AliaDasTouchups.Elise, "Dr. Elise Corman: red hair",
                new[] { new Slot(14, AliaDasTouchups.Hair06, WildernessPostNpcs.BlondHair) },
                new[] { new Slot(14, AliaDasTouchups.Hair06, WildernessPostNpcs.RedHair) }),
            new(Spaulding, "Captain Spaulding: a green helmet",
                new Slot[0],
                new[] { new Slot(1, AliaDasTouchups.Helmet3, Green) }),
        };

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            foreach (var look in Looks)
                foreach (var sql in Change(look.Creature, look.Was, look.Is))
                    yield return sql;

            foreach (var p in Pads)
                yield return SwapPad(p, WaypointPad, p.FromX, HumanTeleporter, p.ToX);
        }

        public static IEnumerable<string> Down()
        {
            foreach (var p in Pads)
                yield return SwapPad(p, HumanTeleporter, p.ToX, WaypointPad, p.FromX);

            foreach (var look in Looks)
                foreach (var sql in Change(look.Creature, look.Is, look.Was))
                    yield return sql;
        }

        private static string SwapPad(PadChange p, uint fromClass, double fromX, uint toClass, double toX) =>
            $"UPDATE teleporter SET class_id = {toClass}, pos_x = {N(toX)} " +
            $"WHERE id = {p.Id} AND type = {LocalTeleporterType} AND map_context_id = {p.MapContextId} AND class_id = {fromClass} " +
            $"AND ABS(pos_x - {N(fromX)}) < 0.01 AND ABS(pos_z - {N(p.Z)}) < 0.01;";
    }
}
