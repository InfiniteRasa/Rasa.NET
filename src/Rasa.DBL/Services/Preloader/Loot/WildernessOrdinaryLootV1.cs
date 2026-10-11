using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Loot
{
    /// <summary>
    /// Frozen ordinary content inventoried on an owned SQLite World; schema boundary 20261203000100.
    /// All rates, equal within-species junk weights, ammunition assignments and level bands
    /// are reconstructed starting balance, not recovered retail distributions.
    /// Native identities come from itemtemplate -> itemtemplate_itemclass -> entityclass/
    /// itemclass; level/skill requirements are keyed by CLASS, not template. The selected
    /// equipment is physical pistols (Rare level-1 rifle) and Motor Assist boots, with level
    /// requirements no higher than the lowest bound creature. No recycled classes, recipes,
    /// Mission quality, unique/bound rewards or hidden inventory items are selected.
    ///
    /// Explicit boss choices: creature rows 75..80, 82..84, 89, 90 identify the named bosses/
    /// overseers in CreaturePreloader and WildernessSpawnpoolPreloader. The migrated named
    /// Pinhole Egg-Layer 630040 (class 10240) and Skeev 630130 (class 28589) are also bosses:
    /// WildernessWaveAPopulationV1/WildernessSkeevWorldV1 and the Corman finale encounter
    /// bind their identities. This is a fixed content decision, never a runtime name/flag rule.
    /// Regular 2 remains level 20 as stored; 3 remains level 11. Do not infer zone-wide levels.
    ///
    /// Coverage includes all 36 rewarding hostile creature IDs in map 1220: 154 populated
    /// pool slots (99 pools), the five Bane CP garrison slots in pool 580001, and mission 682's
    /// three reward-enabled Xanx roles (creature 87) on the exact Anjuhi encounter's map.
    /// There are no Wilderness arrival rows or private experiences at this migration boundary.
    /// Scene spawn-group metadata is not used as a map constraint. Friendly, nonrewarding,
    /// summoned and explicit-authored-loot sources are not added by this content.
    ///
    /// Species-specific junk is verified for ten families. No such identity is established
    /// for Amoeboid, Mox, Linker, Shield Drone, mortar or Predator, so their junk group is
    /// deliberately absent. Generic machine junk is not substituted. Only explicitly mapped
    /// Thrax/technician profiles receive cartridges and Lightbenders receive batteries.
    /// Medpack 44917 links to class 21969 with level 1 and item action 419, not a recipe.
    /// Some ordinary native items are also mission rewards; none is quest-only or unique.
    ///
    /// Regular equipment uses two Normal weights 35, one Uncommon 25 and one Rare 5.
    /// Boss equipment uses two Uncommon weights 65 and one Rare 70: aggregate 65/35.
    /// Arrays below are literal migration data. Never rebuild them from a live content factory,
    /// native queries, runtime classifications or the current equipment catalog.
    /// </summary>
    public static class WildernessOrdinaryLootV1
    {
        public const string Revision = "wilderness_ordinary_loot_v1";
        private const string Provenance = "reconstructed starter balance; native IDs verified against migrated World";
        private const string NativeEvidence =
            "Native itemtemplate/class links, Item augmentation, inventory category, visibility, stack, flags, "
            + "equip slots and class-keyed level/skill requirements verified at schema boundary 20261203000100. "
            + "Medpack 44917 -> class 21969, level 1, item action 419 (ItemTemplateActionPreloader). "
            + "Rates and pool selection are reconstruction, not retail evidence. ";
        private const string Thrax = NativeEvidence + "Thrax junk 41665/41666 -> classes 20306/20307; cartridges 28 -> 3147.";
        private const string Technician = NativeEvidence + "Technician junk 42369/42370 -> classes 20950/20951; cartridges 28 -> 3147.";
        private const string Miasma = NativeEvidence + "Miasma junk 41651/41652/42341/42342 -> classes 20293/20294/20922/20923.";
        private const string Boargar = NativeEvidence + "Boargar junk 41636/41639 -> classes 20277/20281.";
        private const string Fithik = NativeEvidence + "Fithik junk 41644/41645 -> classes 20286/20287.";
        private const string Filcher = NativeEvidence + "Filcher junk 41642/41643 -> classes 20284/20285.";
        private const string Hunter = NativeEvidence + "Hunter junk 42326/42327 -> classes 20907/20908.";
        private const string Caretaker = NativeEvidence + "Caretaker junk 42300/42301 -> classes 20881/20882.";
        private const string Lightbender = NativeEvidence + "Lightbender junk 42332/42333 -> classes 20913/20914; batteries 56 -> 3807.";
        private const string Xanx = NativeEvidence + "Xanx junk 41677/41678 -> classes 20318/20319; mission 682 creates three rewarding creature-87 roles on reserved spawn 176's map 1220.";
        private const string Machina = NativeEvidence + "Machina junk 50211 -> class 24579; creature 630100 is Forean Machina.";
        private const string NoJunk = NativeEvidence + "No verified species-specific native junk identity; junk deliberately omitted, no generic substitution.";

        private static readonly object[,] Profiles =
        {
            { "wilderness_v1_amoeboid_5_9", "Amoeboid, levels 5..9", Revision, Provenance, NoJunk },
            { "wilderness_v1_boargar_1_4", "Boargar, levels 1..4", Revision, Provenance, Boargar },
            { "wilderness_v1_boargar_5_9", "Boargar, levels 5..9", Revision, Provenance, Boargar },
            { "wilderness_v1_caretaker_15_19_boss", "Horntail, level 15 boss", Revision, Provenance, Caretaker + " Explicit boss: creature 79, class 10321, native level 15 (CreaturePreloader)." },
            { "wilderness_v1_caretaker_20_24", "Caretaker, levels 20..24 (native creature 2)", Revision, Provenance, Caretaker },
            { "wilderness_v1_drone_5_9", "Shield Drone, levels 5..9", Revision, Provenance, NoJunk },
            { "wilderness_v1_filcher_5_9", "Filcher, levels 5..9", Revision, Provenance, Filcher },
            { "wilderness_v1_fithik_5_9", "Fithik, levels 5..9", Revision, Provenance, Fithik },
            { "wilderness_v1_fithik_10_14_boss", "Hive Monarch, level 10 boss", Revision, Provenance, Fithik + " Explicit boss: creature 89, class 10323, native level 10 (CreaturePreloader)." },
            { "wilderness_v1_hunter_5_9", "Hunter, levels 5..9", Revision, Provenance, Hunter },
            { "wilderness_v1_lightbender_5_9", "Lightbender and snipers, levels 5..9", Revision, Provenance, Lightbender },
            { "wilderness_v1_lightbender_15_19_boss", "Proctor Fulgor, level 15 boss", Revision, Provenance, Lightbender + " Explicit boss: creature 76, class 10857, native level 15 (CreaturePreloader)." },
            { "wilderness_v1_linker_15_19_boss", "Atropos, level 15 boss", Revision, Provenance, NoJunk + " Explicit boss: creature 78, class 10335, native level 15 (CreaturePreloader)." },
            { "wilderness_v1_machina_5_9", "Forean Machina, levels 5..9", Revision, Provenance, Machina },
            { "wilderness_v1_miasma_5_9", "Miasma, levels 5..9", Revision, Provenance, Miasma },
            { "wilderness_v1_miasma_5_9_boss", "Pinhole Egg-Layer, level 7 boss", Revision, Provenance, Miasma + " Explicit reconstructed boss: creature 630040, class 10240, level 7 (WildernessWaveAPopulationV1; WildernessElohPinholeV1)." },
            { "wilderness_v1_mortar_5_9", "Bane mortars, levels 5..9", Revision, Provenance, NoJunk },
            { "wilderness_v1_mox_5_9", "Mox, levels 5..9", Revision, Provenance, NoJunk },
            { "wilderness_v1_predator_10_14", "Predator, levels 10..14", Revision, Provenance, NoJunk },
            { "wilderness_v1_technician_5_9", "Thrax technician, levels 5..9", Revision, Provenance, Technician },
            { "wilderness_v1_thrax_5_9", "Thrax grenadiers, levels 5..9", Revision, Provenance, Thrax },
            { "wilderness_v1_thrax_5_9_boss", "Glognar, Phlegg and Rankash, level 6 bosses", Revision, Provenance, Thrax + " Explicit bosses: creatures 82/83/84, class 10504, native level 6 (CreaturePreloader)." },
            { "wilderness_v1_thrax_10_14", "Thrax soldiers, levels 10..14 (native creature 3)", Revision, Provenance, Thrax },
            { "wilderness_v1_thrax_10_14_boss", "Graal, level 10 boss", Revision, Provenance, Thrax + " Explicit boss: creature 90, class 10504, native level 10 (CreaturePreloader)." },
            { "wilderness_v1_thrax_15_19_boss", "Archfiend, Old Scratch and Skeev, level 15 bosses", Revision, Provenance, Thrax + " Explicit bosses: creatures 75/80/630130, classes 10612/10504/28589, level 15 (CreaturePreloader; WildernessSkeevWorldV1; reserved mission-701 finale)." },
            { "wilderness_v1_xanx_5_9", "Xanx, levels 5..9, including rewarding scene roles", Revision, Provenance, Xanx },
            { "wilderness_v1_xanx_15_19_boss", "Arioch, level 15 boss", Revision, Provenance, Xanx + " Explicit boss: creature 77, class 10523, native level 15 (CreaturePreloader)." }
        };

        private static readonly object[,] Groups =
        {
            { "wilderness_v1_amoeboid_5_9", "consumable", (byte)LootGroupKind.Consumable, 1000, 1, 1, 30 },
            { "wilderness_v1_amoeboid_5_9", "equipment", (byte)LootGroupKind.Equipment, 800, 1, 1, 40 },
            { "wilderness_v1_boargar_1_4", "junk", (byte)LootGroupKind.Junk, 7000, 1, 1, 10 },
            { "wilderness_v1_boargar_1_4", "consumable", (byte)LootGroupKind.Consumable, 1000, 1, 1, 30 },
            { "wilderness_v1_boargar_1_4", "equipment", (byte)LootGroupKind.Equipment, 800, 1, 1, 40 },
            { "wilderness_v1_boargar_5_9", "junk", (byte)LootGroupKind.Junk, 7000, 1, 1, 10 },
            { "wilderness_v1_boargar_5_9", "consumable", (byte)LootGroupKind.Consumable, 1000, 1, 1, 30 },
            { "wilderness_v1_boargar_5_9", "equipment", (byte)LootGroupKind.Equipment, 800, 1, 1, 40 },
            { "wilderness_v1_caretaker_15_19_boss", "junk", (byte)LootGroupKind.Junk, 9000, 1, 1, 10 },
            { "wilderness_v1_caretaker_15_19_boss", "consumable", (byte)LootGroupKind.Consumable, 2500, 1, 1, 30 },
            { "wilderness_v1_caretaker_15_19_boss", "equipment", (byte)LootGroupKind.Equipment, 6500, 1, 1, 40 },
            { "wilderness_v1_caretaker_20_24", "junk", (byte)LootGroupKind.Junk, 7000, 1, 1, 10 },
            { "wilderness_v1_caretaker_20_24", "consumable", (byte)LootGroupKind.Consumable, 1000, 1, 1, 30 },
            { "wilderness_v1_caretaker_20_24", "equipment", (byte)LootGroupKind.Equipment, 800, 1, 1, 40 },
            { "wilderness_v1_drone_5_9", "consumable", (byte)LootGroupKind.Consumable, 1000, 1, 1, 30 },
            { "wilderness_v1_drone_5_9", "equipment", (byte)LootGroupKind.Equipment, 800, 1, 1, 40 },
            { "wilderness_v1_filcher_5_9", "junk", (byte)LootGroupKind.Junk, 7000, 1, 1, 10 },
            { "wilderness_v1_filcher_5_9", "consumable", (byte)LootGroupKind.Consumable, 1000, 1, 1, 30 },
            { "wilderness_v1_filcher_5_9", "equipment", (byte)LootGroupKind.Equipment, 800, 1, 1, 40 },
            { "wilderness_v1_fithik_5_9", "junk", (byte)LootGroupKind.Junk, 7000, 1, 1, 10 },
            { "wilderness_v1_fithik_5_9", "consumable", (byte)LootGroupKind.Consumable, 1000, 1, 1, 30 },
            { "wilderness_v1_fithik_5_9", "equipment", (byte)LootGroupKind.Equipment, 800, 1, 1, 40 },
            { "wilderness_v1_fithik_10_14_boss", "junk", (byte)LootGroupKind.Junk, 9000, 1, 1, 10 },
            { "wilderness_v1_fithik_10_14_boss", "consumable", (byte)LootGroupKind.Consumable, 2500, 1, 1, 30 },
            { "wilderness_v1_fithik_10_14_boss", "equipment", (byte)LootGroupKind.Equipment, 6500, 1, 1, 40 },
            { "wilderness_v1_hunter_5_9", "junk", (byte)LootGroupKind.Junk, 7000, 1, 1, 10 },
            { "wilderness_v1_hunter_5_9", "consumable", (byte)LootGroupKind.Consumable, 1000, 1, 1, 30 },
            { "wilderness_v1_hunter_5_9", "equipment", (byte)LootGroupKind.Equipment, 800, 1, 1, 40 },
            { "wilderness_v1_lightbender_5_9", "junk", (byte)LootGroupKind.Junk, 7000, 1, 1, 10 },
            { "wilderness_v1_lightbender_5_9", "ammo", (byte)LootGroupKind.Ammunition, 3000, 1, 1, 20 },
            { "wilderness_v1_lightbender_5_9", "consumable", (byte)LootGroupKind.Consumable, 1000, 1, 1, 30 },
            { "wilderness_v1_lightbender_5_9", "equipment", (byte)LootGroupKind.Equipment, 800, 1, 1, 40 },
            { "wilderness_v1_lightbender_15_19_boss", "junk", (byte)LootGroupKind.Junk, 9000, 1, 1, 10 },
            { "wilderness_v1_lightbender_15_19_boss", "ammo", (byte)LootGroupKind.Ammunition, 4500, 1, 1, 20 },
            { "wilderness_v1_lightbender_15_19_boss", "consumable", (byte)LootGroupKind.Consumable, 2500, 1, 1, 30 },
            { "wilderness_v1_lightbender_15_19_boss", "equipment", (byte)LootGroupKind.Equipment, 6500, 1, 1, 40 },
            { "wilderness_v1_linker_15_19_boss", "consumable", (byte)LootGroupKind.Consumable, 2500, 1, 1, 30 },
            { "wilderness_v1_linker_15_19_boss", "equipment", (byte)LootGroupKind.Equipment, 6500, 1, 1, 40 },
            { "wilderness_v1_machina_5_9", "junk", (byte)LootGroupKind.Junk, 7000, 1, 1, 10 },
            { "wilderness_v1_machina_5_9", "consumable", (byte)LootGroupKind.Consumable, 1000, 1, 1, 30 },
            { "wilderness_v1_machina_5_9", "equipment", (byte)LootGroupKind.Equipment, 800, 1, 1, 40 },
            { "wilderness_v1_miasma_5_9", "junk", (byte)LootGroupKind.Junk, 7000, 1, 1, 10 },
            { "wilderness_v1_miasma_5_9", "consumable", (byte)LootGroupKind.Consumable, 1000, 1, 1, 30 },
            { "wilderness_v1_miasma_5_9", "equipment", (byte)LootGroupKind.Equipment, 800, 1, 1, 40 },
            { "wilderness_v1_miasma_5_9_boss", "junk", (byte)LootGroupKind.Junk, 9000, 1, 1, 10 },
            { "wilderness_v1_miasma_5_9_boss", "consumable", (byte)LootGroupKind.Consumable, 2500, 1, 1, 30 },
            { "wilderness_v1_miasma_5_9_boss", "equipment", (byte)LootGroupKind.Equipment, 6500, 1, 1, 40 },
            { "wilderness_v1_mortar_5_9", "consumable", (byte)LootGroupKind.Consumable, 1000, 1, 1, 30 },
            { "wilderness_v1_mortar_5_9", "equipment", (byte)LootGroupKind.Equipment, 800, 1, 1, 40 },
            { "wilderness_v1_mox_5_9", "consumable", (byte)LootGroupKind.Consumable, 1000, 1, 1, 30 },
            { "wilderness_v1_mox_5_9", "equipment", (byte)LootGroupKind.Equipment, 800, 1, 1, 40 },
            { "wilderness_v1_predator_10_14", "consumable", (byte)LootGroupKind.Consumable, 1000, 1, 1, 30 },
            { "wilderness_v1_predator_10_14", "equipment", (byte)LootGroupKind.Equipment, 800, 1, 1, 40 },
            { "wilderness_v1_technician_5_9", "junk", (byte)LootGroupKind.Junk, 7000, 1, 1, 10 },
            { "wilderness_v1_technician_5_9", "ammo", (byte)LootGroupKind.Ammunition, 3000, 1, 1, 20 },
            { "wilderness_v1_technician_5_9", "consumable", (byte)LootGroupKind.Consumable, 1000, 1, 1, 30 },
            { "wilderness_v1_technician_5_9", "equipment", (byte)LootGroupKind.Equipment, 800, 1, 1, 40 },
            { "wilderness_v1_thrax_5_9", "junk", (byte)LootGroupKind.Junk, 7000, 1, 1, 10 },
            { "wilderness_v1_thrax_5_9", "ammo", (byte)LootGroupKind.Ammunition, 3000, 1, 1, 20 },
            { "wilderness_v1_thrax_5_9", "consumable", (byte)LootGroupKind.Consumable, 1000, 1, 1, 30 },
            { "wilderness_v1_thrax_5_9", "equipment", (byte)LootGroupKind.Equipment, 800, 1, 1, 40 },
            { "wilderness_v1_thrax_5_9_boss", "junk", (byte)LootGroupKind.Junk, 9000, 1, 1, 10 },
            { "wilderness_v1_thrax_5_9_boss", "ammo", (byte)LootGroupKind.Ammunition, 4500, 1, 1, 20 },
            { "wilderness_v1_thrax_5_9_boss", "consumable", (byte)LootGroupKind.Consumable, 2500, 1, 1, 30 },
            { "wilderness_v1_thrax_5_9_boss", "equipment", (byte)LootGroupKind.Equipment, 6500, 1, 1, 40 },
            { "wilderness_v1_thrax_10_14", "junk", (byte)LootGroupKind.Junk, 7000, 1, 1, 10 },
            { "wilderness_v1_thrax_10_14", "ammo", (byte)LootGroupKind.Ammunition, 3000, 1, 1, 20 },
            { "wilderness_v1_thrax_10_14", "consumable", (byte)LootGroupKind.Consumable, 1000, 1, 1, 30 },
            { "wilderness_v1_thrax_10_14", "equipment", (byte)LootGroupKind.Equipment, 800, 1, 1, 40 },
            { "wilderness_v1_thrax_10_14_boss", "junk", (byte)LootGroupKind.Junk, 9000, 1, 1, 10 },
            { "wilderness_v1_thrax_10_14_boss", "ammo", (byte)LootGroupKind.Ammunition, 4500, 1, 1, 20 },
            { "wilderness_v1_thrax_10_14_boss", "consumable", (byte)LootGroupKind.Consumable, 2500, 1, 1, 30 },
            { "wilderness_v1_thrax_10_14_boss", "equipment", (byte)LootGroupKind.Equipment, 6500, 1, 1, 40 },
            { "wilderness_v1_thrax_15_19_boss", "junk", (byte)LootGroupKind.Junk, 9000, 1, 1, 10 },
            { "wilderness_v1_thrax_15_19_boss", "ammo", (byte)LootGroupKind.Ammunition, 4500, 1, 1, 20 },
            { "wilderness_v1_thrax_15_19_boss", "consumable", (byte)LootGroupKind.Consumable, 2500, 1, 1, 30 },
            { "wilderness_v1_thrax_15_19_boss", "equipment", (byte)LootGroupKind.Equipment, 6500, 1, 1, 40 },
            { "wilderness_v1_xanx_5_9", "junk", (byte)LootGroupKind.Junk, 7000, 1, 1, 10 },
            { "wilderness_v1_xanx_5_9", "consumable", (byte)LootGroupKind.Consumable, 1000, 1, 1, 30 },
            { "wilderness_v1_xanx_5_9", "equipment", (byte)LootGroupKind.Equipment, 800, 1, 1, 40 },
            { "wilderness_v1_xanx_15_19_boss", "junk", (byte)LootGroupKind.Junk, 9000, 1, 1, 10 },
            { "wilderness_v1_xanx_15_19_boss", "consumable", (byte)LootGroupKind.Consumable, 2500, 1, 1, 30 },
            { "wilderness_v1_xanx_15_19_boss", "equipment", (byte)LootGroupKind.Equipment, 6500, 1, 1, 40 }
        };

        private static readonly object[,] Items =
        {
            { "wilderness_v1_amoeboid_5_9", "consumable", 44917U, 1, 1, 1, 10 },
            { "wilderness_v1_amoeboid_5_9", "equipment", 3057U, 35, 1, 1, 10 },
            { "wilderness_v1_amoeboid_5_9", "equipment", 11561U, 35, 1, 1, 20 },
            { "wilderness_v1_amoeboid_5_9", "equipment", 17130U, 25, 1, 1, 30 },
            { "wilderness_v1_amoeboid_5_9", "equipment", 48545U, 5, 1, 1, 40 },
            { "wilderness_v1_boargar_1_4", "junk", 41636U, 1, 1, 1, 10 },
            { "wilderness_v1_boargar_1_4", "junk", 41639U, 1, 1, 1, 20 },
            { "wilderness_v1_boargar_1_4", "consumable", 44917U, 1, 1, 1, 10 },
            { "wilderness_v1_boargar_1_4", "equipment", 145U, 35, 1, 1, 10 },
            { "wilderness_v1_boargar_1_4", "equipment", 13066U, 35, 1, 1, 20 },
            { "wilderness_v1_boargar_1_4", "equipment", 11557U, 25, 1, 1, 30 },
            { "wilderness_v1_boargar_1_4", "equipment", 14763U, 5, 1, 1, 40 },
            { "wilderness_v1_boargar_5_9", "junk", 41636U, 1, 1, 1, 10 },
            { "wilderness_v1_boargar_5_9", "junk", 41639U, 1, 1, 1, 20 },
            { "wilderness_v1_boargar_5_9", "consumable", 44917U, 1, 1, 1, 10 },
            { "wilderness_v1_boargar_5_9", "equipment", 3057U, 35, 1, 1, 10 },
            { "wilderness_v1_boargar_5_9", "equipment", 11561U, 35, 1, 1, 20 },
            { "wilderness_v1_boargar_5_9", "equipment", 17130U, 25, 1, 1, 30 },
            { "wilderness_v1_boargar_5_9", "equipment", 48545U, 5, 1, 1, 40 },
            { "wilderness_v1_caretaker_15_19_boss", "junk", 42300U, 1, 1, 1, 10 },
            { "wilderness_v1_caretaker_15_19_boss", "junk", 42301U, 1, 1, 1, 20 },
            { "wilderness_v1_caretaker_15_19_boss", "consumable", 44917U, 1, 1, 1, 10 },
            { "wilderness_v1_caretaker_15_19_boss", "equipment", 13716U, 65, 1, 1, 10 },
            { "wilderness_v1_caretaker_15_19_boss", "equipment", 28548U, 65, 1, 1, 20 },
            { "wilderness_v1_caretaker_15_19_boss", "equipment", 13717U, 70, 1, 1, 30 },
            { "wilderness_v1_caretaker_20_24", "junk", 42300U, 1, 1, 1, 10 },
            { "wilderness_v1_caretaker_20_24", "junk", 42301U, 1, 1, 1, 20 },
            { "wilderness_v1_caretaker_20_24", "consumable", 44917U, 1, 1, 1, 10 },
            { "wilderness_v1_caretaker_20_24", "equipment", 3072U, 35, 1, 1, 10 },
            { "wilderness_v1_caretaker_20_24", "equipment", 13081U, 35, 1, 1, 20 },
            { "wilderness_v1_caretaker_20_24", "equipment", 17149U, 25, 1, 1, 30 },
            { "wilderness_v1_caretaker_20_24", "equipment", 17150U, 5, 1, 1, 40 },
            { "wilderness_v1_drone_5_9", "consumable", 44917U, 1, 1, 1, 10 },
            { "wilderness_v1_drone_5_9", "equipment", 3057U, 35, 1, 1, 10 },
            { "wilderness_v1_drone_5_9", "equipment", 11561U, 35, 1, 1, 20 },
            { "wilderness_v1_drone_5_9", "equipment", 17130U, 25, 1, 1, 30 },
            { "wilderness_v1_drone_5_9", "equipment", 48545U, 5, 1, 1, 40 },
            { "wilderness_v1_filcher_5_9", "junk", 41642U, 1, 1, 1, 10 },
            { "wilderness_v1_filcher_5_9", "junk", 41643U, 1, 1, 1, 20 },
            { "wilderness_v1_filcher_5_9", "consumable", 44917U, 1, 1, 1, 10 },
            { "wilderness_v1_filcher_5_9", "equipment", 3057U, 35, 1, 1, 10 },
            { "wilderness_v1_filcher_5_9", "equipment", 11561U, 35, 1, 1, 20 },
            { "wilderness_v1_filcher_5_9", "equipment", 17130U, 25, 1, 1, 30 },
            { "wilderness_v1_filcher_5_9", "equipment", 48545U, 5, 1, 1, 40 },
            { "wilderness_v1_fithik_5_9", "junk", 41644U, 1, 1, 1, 10 },
            { "wilderness_v1_fithik_5_9", "junk", 41645U, 1, 1, 1, 20 },
            { "wilderness_v1_fithik_5_9", "consumable", 44917U, 1, 1, 1, 10 },
            { "wilderness_v1_fithik_5_9", "equipment", 3057U, 35, 1, 1, 10 },
            { "wilderness_v1_fithik_5_9", "equipment", 11561U, 35, 1, 1, 20 },
            { "wilderness_v1_fithik_5_9", "equipment", 17130U, 25, 1, 1, 30 },
            { "wilderness_v1_fithik_5_9", "equipment", 48545U, 5, 1, 1, 40 },
            { "wilderness_v1_fithik_10_14_boss", "junk", 41644U, 1, 1, 1, 10 },
            { "wilderness_v1_fithik_10_14_boss", "junk", 41645U, 1, 1, 1, 20 },
            { "wilderness_v1_fithik_10_14_boss", "consumable", 44917U, 1, 1, 1, 10 },
            { "wilderness_v1_fithik_10_14_boss", "equipment", 17133U, 65, 1, 1, 10 },
            { "wilderness_v1_fithik_10_14_boss", "equipment", 13739U, 65, 1, 1, 20 },
            { "wilderness_v1_fithik_10_14_boss", "equipment", 17132U, 70, 1, 1, 30 },
            { "wilderness_v1_hunter_5_9", "junk", 42326U, 1, 1, 1, 10 },
            { "wilderness_v1_hunter_5_9", "junk", 42327U, 1, 1, 1, 20 },
            { "wilderness_v1_hunter_5_9", "consumable", 44917U, 1, 1, 1, 10 },
            { "wilderness_v1_hunter_5_9", "equipment", 3057U, 35, 1, 1, 10 },
            { "wilderness_v1_hunter_5_9", "equipment", 11561U, 35, 1, 1, 20 },
            { "wilderness_v1_hunter_5_9", "equipment", 17130U, 25, 1, 1, 30 },
            { "wilderness_v1_hunter_5_9", "equipment", 48545U, 5, 1, 1, 40 },
            { "wilderness_v1_lightbender_5_9", "junk", 42332U, 1, 1, 1, 10 },
            { "wilderness_v1_lightbender_5_9", "junk", 42333U, 1, 1, 1, 20 },
            { "wilderness_v1_lightbender_5_9", "ammo", 56U, 1, 8, 16, 10 },
            { "wilderness_v1_lightbender_5_9", "consumable", 44917U, 1, 1, 1, 10 },
            { "wilderness_v1_lightbender_5_9", "equipment", 3057U, 35, 1, 1, 10 },
            { "wilderness_v1_lightbender_5_9", "equipment", 11561U, 35, 1, 1, 20 },
            { "wilderness_v1_lightbender_5_9", "equipment", 17130U, 25, 1, 1, 30 },
            { "wilderness_v1_lightbender_5_9", "equipment", 48545U, 5, 1, 1, 40 },
            { "wilderness_v1_lightbender_15_19_boss", "junk", 42332U, 1, 1, 1, 10 },
            { "wilderness_v1_lightbender_15_19_boss", "junk", 42333U, 1, 1, 1, 20 },
            { "wilderness_v1_lightbender_15_19_boss", "ammo", 56U, 1, 8, 16, 10 },
            { "wilderness_v1_lightbender_15_19_boss", "consumable", 44917U, 1, 1, 1, 10 },
            { "wilderness_v1_lightbender_15_19_boss", "equipment", 13716U, 65, 1, 1, 10 },
            { "wilderness_v1_lightbender_15_19_boss", "equipment", 28548U, 65, 1, 1, 20 },
            { "wilderness_v1_lightbender_15_19_boss", "equipment", 13717U, 70, 1, 1, 30 },
            { "wilderness_v1_linker_15_19_boss", "consumable", 44917U, 1, 1, 1, 10 },
            { "wilderness_v1_linker_15_19_boss", "equipment", 13716U, 65, 1, 1, 10 },
            { "wilderness_v1_linker_15_19_boss", "equipment", 28548U, 65, 1, 1, 20 },
            { "wilderness_v1_linker_15_19_boss", "equipment", 13717U, 70, 1, 1, 30 },
            { "wilderness_v1_machina_5_9", "junk", 50211U, 1, 1, 1, 10 },
            { "wilderness_v1_machina_5_9", "consumable", 44917U, 1, 1, 1, 10 },
            { "wilderness_v1_machina_5_9", "equipment", 3057U, 35, 1, 1, 10 },
            { "wilderness_v1_machina_5_9", "equipment", 11561U, 35, 1, 1, 20 },
            { "wilderness_v1_machina_5_9", "equipment", 17130U, 25, 1, 1, 30 },
            { "wilderness_v1_machina_5_9", "equipment", 48545U, 5, 1, 1, 40 },
            { "wilderness_v1_miasma_5_9", "junk", 41651U, 1, 1, 1, 10 },
            { "wilderness_v1_miasma_5_9", "junk", 41652U, 1, 1, 1, 20 },
            { "wilderness_v1_miasma_5_9", "junk", 42341U, 1, 1, 1, 30 },
            { "wilderness_v1_miasma_5_9", "junk", 42342U, 1, 1, 1, 40 },
            { "wilderness_v1_miasma_5_9", "consumable", 44917U, 1, 1, 1, 10 },
            { "wilderness_v1_miasma_5_9", "equipment", 3057U, 35, 1, 1, 10 },
            { "wilderness_v1_miasma_5_9", "equipment", 11561U, 35, 1, 1, 20 },
            { "wilderness_v1_miasma_5_9", "equipment", 17130U, 25, 1, 1, 30 },
            { "wilderness_v1_miasma_5_9", "equipment", 48545U, 5, 1, 1, 40 },
            { "wilderness_v1_miasma_5_9_boss", "junk", 41651U, 1, 1, 1, 10 },
            { "wilderness_v1_miasma_5_9_boss", "junk", 41652U, 1, 1, 1, 20 },
            { "wilderness_v1_miasma_5_9_boss", "junk", 42341U, 1, 1, 1, 30 },
            { "wilderness_v1_miasma_5_9_boss", "junk", 42342U, 1, 1, 1, 40 },
            { "wilderness_v1_miasma_5_9_boss", "consumable", 44917U, 1, 1, 1, 10 },
            { "wilderness_v1_miasma_5_9_boss", "equipment", 17130U, 65, 1, 1, 10 },
            { "wilderness_v1_miasma_5_9_boss", "equipment", 13736U, 65, 1, 1, 20 },
            { "wilderness_v1_miasma_5_9_boss", "equipment", 48545U, 70, 1, 1, 30 },
            { "wilderness_v1_mortar_5_9", "consumable", 44917U, 1, 1, 1, 10 },
            { "wilderness_v1_mortar_5_9", "equipment", 3057U, 35, 1, 1, 10 },
            { "wilderness_v1_mortar_5_9", "equipment", 11561U, 35, 1, 1, 20 },
            { "wilderness_v1_mortar_5_9", "equipment", 17130U, 25, 1, 1, 30 },
            { "wilderness_v1_mortar_5_9", "equipment", 48545U, 5, 1, 1, 40 },
            { "wilderness_v1_mox_5_9", "consumable", 44917U, 1, 1, 1, 10 },
            { "wilderness_v1_mox_5_9", "equipment", 3057U, 35, 1, 1, 10 },
            { "wilderness_v1_mox_5_9", "equipment", 11561U, 35, 1, 1, 20 },
            { "wilderness_v1_mox_5_9", "equipment", 17130U, 25, 1, 1, 30 },
            { "wilderness_v1_mox_5_9", "equipment", 48545U, 5, 1, 1, 40 },
            { "wilderness_v1_predator_10_14", "consumable", 44917U, 1, 1, 1, 10 },
            { "wilderness_v1_predator_10_14", "equipment", 3061U, 35, 1, 1, 10 },
            { "wilderness_v1_predator_10_14", "equipment", 11567U, 35, 1, 1, 20 },
            { "wilderness_v1_predator_10_14", "equipment", 17133U, 25, 1, 1, 30 },
            { "wilderness_v1_predator_10_14", "equipment", 17132U, 5, 1, 1, 40 },
            { "wilderness_v1_technician_5_9", "junk", 42369U, 1, 1, 1, 10 },
            { "wilderness_v1_technician_5_9", "junk", 42370U, 1, 1, 1, 20 },
            { "wilderness_v1_technician_5_9", "ammo", 28U, 1, 12, 24, 10 },
            { "wilderness_v1_technician_5_9", "consumable", 44917U, 1, 1, 1, 10 },
            { "wilderness_v1_technician_5_9", "equipment", 3057U, 35, 1, 1, 10 },
            { "wilderness_v1_technician_5_9", "equipment", 11561U, 35, 1, 1, 20 },
            { "wilderness_v1_technician_5_9", "equipment", 17130U, 25, 1, 1, 30 },
            { "wilderness_v1_technician_5_9", "equipment", 48545U, 5, 1, 1, 40 },
            { "wilderness_v1_thrax_5_9", "junk", 41665U, 1, 1, 1, 10 },
            { "wilderness_v1_thrax_5_9", "junk", 41666U, 1, 1, 1, 20 },
            { "wilderness_v1_thrax_5_9", "ammo", 28U, 1, 12, 24, 10 },
            { "wilderness_v1_thrax_5_9", "consumable", 44917U, 1, 1, 1, 10 },
            { "wilderness_v1_thrax_5_9", "equipment", 3057U, 35, 1, 1, 10 },
            { "wilderness_v1_thrax_5_9", "equipment", 11561U, 35, 1, 1, 20 },
            { "wilderness_v1_thrax_5_9", "equipment", 17130U, 25, 1, 1, 30 },
            { "wilderness_v1_thrax_5_9", "equipment", 48545U, 5, 1, 1, 40 },
            { "wilderness_v1_thrax_5_9_boss", "junk", 41665U, 1, 1, 1, 10 },
            { "wilderness_v1_thrax_5_9_boss", "junk", 41666U, 1, 1, 1, 20 },
            { "wilderness_v1_thrax_5_9_boss", "ammo", 28U, 1, 12, 24, 10 },
            { "wilderness_v1_thrax_5_9_boss", "consumable", 44917U, 1, 1, 1, 10 },
            { "wilderness_v1_thrax_5_9_boss", "equipment", 17130U, 65, 1, 1, 10 },
            { "wilderness_v1_thrax_5_9_boss", "equipment", 13736U, 65, 1, 1, 20 },
            { "wilderness_v1_thrax_5_9_boss", "equipment", 48545U, 70, 1, 1, 30 },
            { "wilderness_v1_thrax_10_14", "junk", 41665U, 1, 1, 1, 10 },
            { "wilderness_v1_thrax_10_14", "junk", 41666U, 1, 1, 1, 20 },
            { "wilderness_v1_thrax_10_14", "ammo", 28U, 1, 12, 24, 10 },
            { "wilderness_v1_thrax_10_14", "consumable", 44917U, 1, 1, 1, 10 },
            { "wilderness_v1_thrax_10_14", "equipment", 3061U, 35, 1, 1, 10 },
            { "wilderness_v1_thrax_10_14", "equipment", 11567U, 35, 1, 1, 20 },
            { "wilderness_v1_thrax_10_14", "equipment", 17133U, 25, 1, 1, 30 },
            { "wilderness_v1_thrax_10_14", "equipment", 17132U, 5, 1, 1, 40 },
            { "wilderness_v1_thrax_10_14_boss", "junk", 41665U, 1, 1, 1, 10 },
            { "wilderness_v1_thrax_10_14_boss", "junk", 41666U, 1, 1, 1, 20 },
            { "wilderness_v1_thrax_10_14_boss", "ammo", 28U, 1, 12, 24, 10 },
            { "wilderness_v1_thrax_10_14_boss", "consumable", 44917U, 1, 1, 1, 10 },
            { "wilderness_v1_thrax_10_14_boss", "equipment", 17133U, 65, 1, 1, 10 },
            { "wilderness_v1_thrax_10_14_boss", "equipment", 13739U, 65, 1, 1, 20 },
            { "wilderness_v1_thrax_10_14_boss", "equipment", 17132U, 70, 1, 1, 30 },
            { "wilderness_v1_thrax_15_19_boss", "junk", 41665U, 1, 1, 1, 10 },
            { "wilderness_v1_thrax_15_19_boss", "junk", 41666U, 1, 1, 1, 20 },
            { "wilderness_v1_thrax_15_19_boss", "ammo", 28U, 1, 12, 24, 10 },
            { "wilderness_v1_thrax_15_19_boss", "consumable", 44917U, 1, 1, 1, 10 },
            { "wilderness_v1_thrax_15_19_boss", "equipment", 13716U, 65, 1, 1, 10 },
            { "wilderness_v1_thrax_15_19_boss", "equipment", 28548U, 65, 1, 1, 20 },
            { "wilderness_v1_thrax_15_19_boss", "equipment", 13717U, 70, 1, 1, 30 },
            { "wilderness_v1_xanx_5_9", "junk", 41677U, 1, 1, 1, 10 },
            { "wilderness_v1_xanx_5_9", "junk", 41678U, 1, 1, 1, 20 },
            { "wilderness_v1_xanx_5_9", "consumable", 44917U, 1, 1, 1, 10 },
            { "wilderness_v1_xanx_5_9", "equipment", 3057U, 35, 1, 1, 10 },
            { "wilderness_v1_xanx_5_9", "equipment", 11561U, 35, 1, 1, 20 },
            { "wilderness_v1_xanx_5_9", "equipment", 17130U, 25, 1, 1, 30 },
            { "wilderness_v1_xanx_5_9", "equipment", 48545U, 5, 1, 1, 40 },
            { "wilderness_v1_xanx_15_19_boss", "junk", 41677U, 1, 1, 1, 10 },
            { "wilderness_v1_xanx_15_19_boss", "junk", 41678U, 1, 1, 1, 20 },
            { "wilderness_v1_xanx_15_19_boss", "consumable", 44917U, 1, 1, 1, 10 },
            { "wilderness_v1_xanx_15_19_boss", "equipment", 13716U, 65, 1, 1, 10 },
            { "wilderness_v1_xanx_15_19_boss", "equipment", 28548U, 65, 1, 1, 20 },
            { "wilderness_v1_xanx_15_19_boss", "equipment", 13717U, 70, 1, 1, 30 }
        };

        private static readonly object[,] Bindings =
        {
            { 1220U, 1U, "wilderness_v1_fithik_5_9" },
            { 1220U, 2U, "wilderness_v1_caretaker_20_24" },
            { 1220U, 3U, "wilderness_v1_thrax_10_14" },
            { 1220U, 33U, "wilderness_v1_amoeboid_5_9" },
            { 1220U, 35U, "wilderness_v1_boargar_1_4" },
            { 1220U, 41U, "wilderness_v1_hunter_5_9" },
            { 1220U, 44U, "wilderness_v1_boargar_5_9" },
            { 1220U, 47U, "wilderness_v1_technician_5_9" },
            { 1220U, 57U, "wilderness_v1_thrax_5_9" },
            { 1220U, 58U, "wilderness_v1_boargar_5_9" },
            { 1220U, 59U, "wilderness_v1_mox_5_9" },
            { 1220U, 60U, "wilderness_v1_filcher_5_9" },
            { 1220U, 75U, "wilderness_v1_thrax_15_19_boss" },
            { 1220U, 76U, "wilderness_v1_lightbender_15_19_boss" },
            { 1220U, 77U, "wilderness_v1_xanx_15_19_boss" },
            { 1220U, 78U, "wilderness_v1_linker_15_19_boss" },
            { 1220U, 79U, "wilderness_v1_caretaker_15_19_boss" },
            { 1220U, 80U, "wilderness_v1_thrax_15_19_boss" },
            { 1220U, 82U, "wilderness_v1_thrax_5_9_boss" },
            { 1220U, 83U, "wilderness_v1_thrax_5_9_boss" },
            { 1220U, 84U, "wilderness_v1_thrax_5_9_boss" },
            { 1220U, 85U, "wilderness_v1_drone_5_9" },
            { 1220U, 87U, "wilderness_v1_xanx_5_9" },
            { 1220U, 88U, "wilderness_v1_miasma_5_9" },
            { 1220U, 89U, "wilderness_v1_fithik_10_14_boss" },
            { 1220U, 90U, "wilderness_v1_thrax_10_14_boss" },
            { 1220U, 630040U, "wilderness_v1_miasma_5_9_boss" },
            { 1220U, 630046U, "wilderness_v1_lightbender_5_9" },
            { 1220U, 630071U, "wilderness_v1_lightbender_5_9" },
            { 1220U, 630076U, "wilderness_v1_mortar_5_9" },
            { 1220U, 630077U, "wilderness_v1_mortar_5_9" },
            { 1220U, 630078U, "wilderness_v1_mortar_5_9" },
            { 1220U, 630079U, "wilderness_v1_mortar_5_9" },
            { 1220U, 630100U, "wilderness_v1_machina_5_9" },
            { 1220U, 630120U, "wilderness_v1_predator_10_14" },
            { 1220U, 630130U, "wilderness_v1_thrax_15_19_boss" }
        };

        public static void Up(MigrationBuilder migration)
        {
            migration.InsertData("loot_profile",
                new[] { "profile_key", "description", "content_revision", "provenance", "evidence_notes" }, Profiles);
            migration.InsertData("ordinary_loot_group",
                new[] { "profile_key", "group_key", "kind", "chance_basis_points", "minimum_rolls", "maximum_rolls", "sort_order" }, Groups);
            migration.InsertData("loot_item",
                new[] { "profile_key", "group_key", "item_template_id", "weight", "minimum_quantity", "maximum_quantity", "sort_order" }, Items);
            migration.InsertData("creature_loot_binding",
                new[] { "map_context_id", "creature_id", "profile_key" }, Bindings);
            migration.InsertData("loot_map_policy", new[] { "map_context_id", "require_profiles" },
                new object[,] { { 1220U, true } });
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.DeleteData("creature_loot_binding", new[] { "map_context_id", "creature_id" }, Keys(Bindings, 2));
            migration.DeleteData("loot_item", new[] { "profile_key", "group_key", "item_template_id" }, Keys(Items, 3));
            migration.DeleteData("loot_map_policy", "map_context_id", 1220U);
            migration.DeleteData("ordinary_loot_group", new[] { "profile_key", "group_key" }, Keys(Groups, 2));
            migration.DeleteData("loot_profile", new[] { "profile_key" }, Keys(Profiles, 1));
        }

        private static object[,] Keys(object[,] rows, int columns)
        {
            var keys = new object[rows.GetLength(0), columns];
            for (var row = 0; row < keys.GetLength(0); row++)
                for (var column = 0; column < columns; column++)
                    keys[row, column] = rows[row, column];
            return keys;
        }
    }
}
