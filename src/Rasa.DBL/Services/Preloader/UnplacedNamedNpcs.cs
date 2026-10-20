using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    using static AliaDasNpcs;

    /// <summary>
    /// The named NPCs who have recorded barks in the client but no creature row of ours
    /// (NamedNpcBarks): made here with their looks and barks, and left unplaced. None has a spawn
    /// pool; a pool placed later is all that stands one in the world, and a GM sees one at once
    /// with .creature (Unplaced_named_npcs).
    ///
    ///  - Information Spec. Johnson (creaturenamelanguage 206), bark 22 (ITSpecJohnson.ogg): a man.
    ///  - Field Lt. Yates (214), bark 23 (FieldLieutYates.ogg): a woman.
    ///  - Commander Elvers (3091), barks 27, 205, 206 and 207 (m_elvers_bc_1..3.ogg): a man. The
    ///    recordings' "bc" and the lines ("Move out, recruits!") are the Proving Grounds'.
    ///  - Field Engineer Hernandez (4855), bark 38 (FieldEngHernandez.ogg): a man, not Supply Clerk
    ///    Hernandez (182).
    ///  - Tribal Leader Ryosa (5257), bark 40 (TribLeadRyosa.ogg, "May the Eloh protect you, my
    ///    warriors"): a Forean, made as Tribal Leader Oingin (creature 139) is.
    ///  - Comm. Officer Jennings (5258), bark 43 (ComOffJennings.ogg): a man, not Recon. Spec.
    ///    Jennings (3075, creature 135).
    ///
    /// Who is a man or a woman is read off the recordings. The client says nothing of how any of
    /// them looks: Johnson, Yates and Jennings wear the brown officer's uniform of the Wilderness'
    /// officers (Information Spec. Van Arth's, Recon. Spec. Jennings'), Elvers the olive one of
    /// the Proving Grounds' (BootcampBaseNpcs.OfficerOlive), each with the redshirt pistol; the
    /// engineer wears the sapper's, as Dr. Franja Corman does. Each has a face and hair of their
    /// own. Level 10 and 1000 hit points, as the Wilderness' named
    /// NPCs have.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike; the creature goes in only where its class is and
    /// no creature has its id, its look and barks only where it is, and Down takes out only what
    /// Up put in, a creature only while no spawn pool has it.
    /// </summary>
    public static class UnplacedNamedNpcs
    {
        public const uint Johnson = 400401, Yates = 400402, Elvers = 400403, Hernandez = 400404, Ryosa = 400405, Jennings = 400406;
        public const uint First = Johnson, Last = Jennings;

        public const uint Olive = BootcampBaseNpcs.OfficerOlive;
        public const uint SapperGreen = 12429948;                   // as Dr. Franja Corman's (creature 107)
        public const uint HairBald = 3812;
        public const uint FaceCaucasianLong2 = 24009, FaceCaucasianRound1 = 24020, FaceCaucasianSquare1 = 24032, FaceCaucasianSquare2 = 24033;
        public const uint FaceCaucasianStandard2 = 7508;

        public sealed record Npc(NewCreature Creature, Slot[] Look, uint[] Barks);

        private static Slot[] Officer(uint hue, uint hair, uint hairColor, uint face, uint skin) => new[]
        {
            new Slot(2, OfficerBoots, hue),
            new Slot(13, PistolRedshirt, 1),
            new Slot(14, hair, hairColor),
            new Slot(15, OfficerTunic, hue),
            new Slot(16, OfficerLegs, hue),
            new Slot(17, face, skin)
        };

        public static IReadOnlyList<Npc> Npcs { get; } = new[]
        {
            new Npc(new NewCreature(Johnson, "Information Spec. Johnson - unplaced", SwapsetMale, 10, 1000, 206, 0, 0, 0),
                Officer(Brown, WildernessPostNpcs.Hair05, DarkHair, FaceCaucasianSquare1, SkinLight), new uint[] { 22 }),
            new Npc(new NewCreature(Yates, "Field Lt. Yates - unplaced", SwapsetFemale, 10, 1000, 214, 0, 0, 0),
                Officer(Brown, WildernessPostNpcs.Hair01, WildernessPostNpcs.RedHair, FaceCaucasianLong2, SkinLight), new uint[] { 23 }),
            new Npc(new NewCreature(Elvers, "Commander Elvers - unplaced", SwapsetMale, 10, 1000, 3091, 0, 0, 0),
                Officer(Olive, HairBald, DarkHair, FaceCaucasianRound1, SkinMedium), new uint[] { 27, 205, 206, 207 }),
            new Npc(new NewCreature(Hernandez, "Field Engineer Hernandez - unplaced", SwapsetMale, 10, 1000, 4855, 0, 0, 0),
                new[]
                {
                    new Slot(2, SapperBoots, SapperGreen),
                    new Slot(3, SapperGloves, SapperGreen),
                    new Slot(14, WildernessPostNpcs.Hair02, DarkHair),
                    new Slot(15, SapperTorso, SapperGreen),
                    new Slot(16, SapperLegs, SapperGreen),
                    new Slot(17, FaceCaucasianStandard2, SkinMedium)
                },
                new uint[] { 38 }),
            new Npc(new NewCreature(Ryosa, "Forean Tribal Leader Ryosa - unplaced", ForeanSpearman, 10, 1000, 5257, 0, 0, 5),
                new Slot[0], new uint[] { 40 }),
            new Npc(new NewCreature(Jennings, "Comm. Officer Jennings - unplaced", SwapsetMale, 10, 1000, 5258, 0, 0, 0),
                Officer(Brown, WildernessPostNpcs.Hair08, DarkHair, FaceCaucasianSquare2, SkinLight), new uint[] { 43 })
        };

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            foreach (var npc in Npcs)
            {
                var c = npc.Creature;
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                    "action1, action2, action3, action4, action5, action6, action7, action8) " +
                    $"SELECT {c.Id}, {S(c.Comment)}, {c.ClassId}, 1, {c.Level}, {c.MaxHp}, {c.NameId}, {c.RunSpeed}, {c.WalkSpeed}, {c.Action}, 0, 0, 0, 0, 0, 0, 0 " +
                    $"FROM entityclass e WHERE e.id = {c.ClassId} AND NOT EXISTS (SELECT 1 FROM creature x WHERE x.id = {c.Id});";

                foreach (var sql in Change(c.Id, new Slot[0], npc.Look))
                    yield return sql;

                foreach (var bark in npc.Barks)
                    yield return $"INSERT INTO npc_bark (creature_id, bark_id) SELECT {c.Id}, {bark} FROM creature " +
                        $"WHERE id = {c.Id} AND name_id = {c.NameId} AND NOT EXISTS (SELECT 1 FROM npc_bark WHERE creature_id = {c.Id} AND bark_id = {bark});";
            }
        }

        public static IEnumerable<string> Down()
        {
            for (var i = Npcs.Count - 1; i >= 0; i--)
            {
                var npc = Npcs[i];
                var c = npc.Creature;

                foreach (var bark in npc.Barks)
                    yield return $"DELETE FROM npc_bark WHERE creature_id = {c.Id} AND bark_id = {bark};";

                foreach (var sql in Change(c.Id, npc.Look, new Slot[0]))
                    yield return sql;

                yield return $"DELETE FROM creature WHERE id = {c.Id} AND name_id = {c.NameId} " +
                    $"AND NOT EXISTS (SELECT 1 FROM spawnpool p WHERE {c.Id} IN (p.creature_1_Id, p.creature_2_Id, p.creature_3_Id, p.creature_4_Id, p.creature_5_Id, p.creature_6_Id));";
            }
        }
    }
}
