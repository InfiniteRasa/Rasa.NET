using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    using static AliaDasNpcs;

    /// <summary>
    /// Alia Das after a third look round in game (Alia_das_third_look).
    ///
    /// The colour of an appearance row is a hue. The client's own, for anything given none, is
    /// (0, 0, 0, 255) (colorutil.GetDefaultColor, what Actor._AddAccessory falls back on, and the
    /// client's hue for these classes in physicalentityclasshue): the texture as it was painted.
    /// <see cref="Unhued"/> is that.
    ///
    ///  - Engineer Salter: the whole Mech 1 armour (Clothing_Armor_Mech_1,
    ///    avatar_hum_m_armor_mech_v01) as painted: a brown suit strapped round with harness, grey
    ///    plates and its metal helmet.
    ///  - Dr. Elise Corman: the Corman set she wears (CreatureSwap_Corman_Soldier_1, suit "NPC
    ///    Corman 1 Female") as painted, not tinted brown, and paler red hair.
    ///  - The Field Medic: NPC_Clothing_Officer_2_Vest alone. Its part of the Officer 2 suit is
    ///    the whole uniform, boots to collar (npc_hum_f_officer_v02: one piece from the floor to
    ///    the neck), so the Officer 3 trousers and boots she wore with it are taken off: they were
    ///    a second pair through it. In pure white, the brightest hue there is.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike, every change guarded on the row as it was.
    /// </summary>
    public static class AliaDasThirdLook
    {
        public const uint Unhued = 0xFF000000;
        public static readonly uint PureWhite = Hue(0xFF, 0xFF, 0xFF);
        public static readonly uint PaleRedHair = Hue(0xC8, 0x6E, 0x50);

        public const uint Mech1Helm = 4011, Mech1Boots = 7324, Mech1Gloves = 7325, Mech1Torso = 7326, Mech1Legs = 7327;

        public static readonly Look[] Looks =
        {
            new(Salter, "Engineer Salter: the whole Mech 1 armour, as painted",
                new[] { new Slot(1, AliaDasSecondLook.Medium1Helm, Silver), new Slot(2, AliaDasSecondLook.Light11Boots, Brown), new Slot(3, AliaDasSecondLook.Light11Gloves, Brown),
                        new Slot(13, RifleElectric, 1), new Slot(14, AliaDasTouchups.Hair05, DarkHair), new Slot(15, AliaDasSecondLook.Light11Torso, Brown),
                        new Slot(16, AliaDasSecondLook.Light11Legs, Brown), new Slot(17, 24008, SkinLight) },
                new[] { new Slot(1, Mech1Helm, Unhued), new Slot(2, Mech1Boots, Unhued), new Slot(3, Mech1Gloves, Unhued),
                        new Slot(13, RifleElectric, 1), new Slot(14, AliaDasTouchups.Hair05, DarkHair), new Slot(15, Mech1Torso, Unhued),
                        new Slot(16, Mech1Legs, Unhued), new Slot(17, 24008, SkinLight) }),
            new(AliaDasTouchups.Elise, "Dr. Elise Corman: the Corman set as painted, paler red hair",
                new[] { new Slot(2, AliaDasTouchups.CormanBoots, AliaDasTouchups.RobeBrown), new Slot(3, AliaDasTouchups.CormanGloves, AliaDasTouchups.RobeBrown),
                        new Slot(14, AliaDasTouchups.Hair06, WildernessPostNpcs.RedHair), new Slot(15, AliaDasTouchups.CormanTorso, AliaDasTouchups.RobeBrown),
                        new Slot(16, AliaDasTouchups.CormanLegs, AliaDasTouchups.RobeBrown), new Slot(17, 24009, SkinLight) },
                new[] { new Slot(2, AliaDasTouchups.CormanBoots, Unhued), new Slot(3, AliaDasTouchups.CormanGloves, Unhued),
                        new Slot(14, AliaDasTouchups.Hair06, PaleRedHair), new Slot(15, AliaDasTouchups.CormanTorso, Unhued),
                        new Slot(16, AliaDasTouchups.CormanLegs, Unhued), new Slot(17, 24009, SkinLight) }),
            new(AliaDasTouchups.FieldMedic, "Field Medic: the Officer 2 uniform alone, pure white",
                new[] { new Slot(2, Officer3Boots, White), new Slot(14, AliaDasTouchups.Hair09, WildernessPostNpcs.RedHair),
                        new Slot(15, Officer2Vest, White), new Slot(16, Officer3Legs, White), new Slot(17, 24011, SkinLight) },
                new[] { new Slot(14, AliaDasTouchups.Hair09, WildernessPostNpcs.RedHair),
                        new Slot(15, Officer2Vest, PureWhite), new Slot(17, 24011, SkinLight) }),
        };

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            foreach (var look in Looks)
                foreach (var sql in Change(look.Creature, look.Was, look.Is))
                    yield return sql;
        }

        public static IEnumerable<string> Down()
        {
            foreach (var look in Looks)
                foreach (var sql in Change(look.Creature, look.Is, look.Was))
                    yield return sql;
        }
    }
}
