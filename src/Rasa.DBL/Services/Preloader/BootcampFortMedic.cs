using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    using static AliaDasNpcs;

    /// <summary>
    /// Medic Schramm, a medical vendor at the Proving Grounds' fort (adv_bootcamp, map context
    /// 1985), where gameplay footage has her after the fort is retaken; placed where a GM stood
    /// and faced, read off the server's .where line (Bootcamp_fort_medic).
    ///
    /// Schramm (creaturenamelanguage 10577) is the one medical name among the Proving Grounds'
    /// own (10574-10580: Van Valkenberg, Youngblood, Delessio, Schramm, Hartmann, DeSimone,
    /// Oceana). A woman with brown hair in the teal officer's uniform the Wilderness medics wear
    /// (WildernessPostNpcs), with the pistol they carry; she sells the hospital stock, as Medical
    /// Supply Sgt. Savage does: Solomon's (creature 31), under the medical vendor package 87.
    ///
    /// She stands from the start, as the base's other staff do, not only once the fort is
    /// retaken: her pool is an ordinary one. Level 5, the level of the base's medic; with no
    /// speed she stands on her pool's point.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike; the creature, pool and stock go in only where
    /// none is, and Down takes out only what Up put in.
    /// </summary>
    public static class BootcampFortMedic
    {
        public const uint BootcampMapContextId = 1985;

        public const uint Schramm = 400201;
        public const uint SchrammPool = 400201;
        public const uint SchrammName = 10577;

        public const uint MedicalPackage = 87;
        public const uint Solomon = 31;

        public const double X = 112.5312, Y = 109.4336, Z = 111.2734, Rotation = 5.3924;
        public const uint Respawn = 20;

        public static readonly NewCreature Creature =
            new(Schramm, "Medic Schramm - Proving Grounds fort", VendorFemale, 5, 555, SchrammName, 0, 0, 2);

        /// <summary>The teal officer's uniform (NPC_Clothing_Officer_1), pistol, brown Hair_06, the long female face.</summary>
        public static readonly Slot[] Look =
        {
            new(2, OfficerBoots, WildernessPostNpcs.Teal),
            new(13, PistolRedshirt, 1),
            new(14, WildernessPostNpcs.Hair06, WildernessPostNpcs.BrownHair),
            new(15, OfficerTunic, WildernessPostNpcs.Teal),
            new(16, OfficerLegs, WildernessPostNpcs.Teal),
            new(17, 24009, SkinLight),
        };

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            var c = Creature;
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                "action1, action2, action3, action4, action5, action6, action7, action8) " +
                $"SELECT {c.Id}, {S(c.Comment)}, {c.ClassId}, 1, {c.Level}, {c.MaxHp}, {c.NameId}, {c.RunSpeed}, {c.WalkSpeed}, {c.Action}, 0, 0, 0, 0, 0, 0, 0 " +
                $"FROM entityclass e WHERE e.id = {c.ClassId} AND NOT EXISTS (SELECT 1 FROM creature x WHERE x.id = {c.Id});";

            foreach (var sql in Change(Schramm, new Slot[0], Look))
                yield return sql;

            yield return "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
                "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
                "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
                "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
                $"SELECT {SchrammPool}, 0, 0, {Respawn}, {N(X)}, {N(Y)}, {N(Z)}, {N(Rotation)}, {BootcampMapContextId}, {Schramm}, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 " +
                $"FROM creature c WHERE c.id = {Schramm} AND NOT EXISTS (SELECT 1 FROM spawnpool x WHERE x.id = {SchrammPool});";

            yield return $"INSERT INTO vendor (id, package_id) SELECT {Schramm}, {MedicalPackage} FROM creature c " +
                $"WHERE c.id = {Schramm} AND NOT EXISTS (SELECT 1 FROM vendor x WHERE x.id = {Schramm});";

            yield return $"INSERT INTO vendor_item (id, item_template_id) SELECT {Schramm}, i.item_template_id FROM vendor_item i " +
                $"WHERE i.id = {Solomon} AND NOT EXISTS (SELECT 1 FROM vendor_item x WHERE x.id = {Schramm} AND x.item_template_id = i.item_template_id);";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM vendor_item WHERE id = {Schramm};";
            yield return $"DELETE FROM vendor WHERE id = {Schramm} AND package_id = {MedicalPackage};";
            yield return $"DELETE FROM spawnpool WHERE id = {SchrammPool} AND creature_1_Id = {Schramm};";

            foreach (var sql in Change(Schramm, Look, new Slot[0]))
                yield return sql;

            yield return $"DELETE FROM creature WHERE id = {Schramm} AND name_id = {SchrammName} " +
                $"AND NOT EXISTS (SELECT 1 FROM spawnpool p WHERE {Schramm} IN (p.creature_1_Id, p.creature_2_Id, p.creature_3_Id, p.creature_4_Id, p.creature_5_Id, p.creature_6_Id));";
        }
    }
}
