using System.Collections.Generic;
using System.Linq;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The NPCs whose greeting the client's own text gives away: the rows of Add_npc_greetings.
    ///
    /// The client has 1,699 greeting lines (npcgreetinglanguage) and nothing that ties a line to
    /// an NPC. Read against the NPCs the world has, a line is given to one here only where its
    /// words do:
    ///  - named: the speaker says who they are ("I am called Nula", "Captain DeLisi, but the
    ///    guys just call me Captain Mike");
    ///  - mission: the line is that NPC's part of a mission's story - the same people, things and
    ///    turns of phrase as its dialogue in missiontextlanguage;
    ///  - place: the speaker says where they stand, and the NPC is the one there it fits - or,
    ///    where several fit alike, one of them;
    ///  - role: the speaker says what they do, and one NPC does it (the bartender, the drill
    ///    sergeant, the clan registrar).
    ///
    /// Many NPCs had a line for before a mission and others for how it came out; a row holds
    /// one, so it is the line for a player who has done nothing for them yet. A line that only
    /// makes sense after a mission is left out, and so is its NPC if it has no other.
    ///
    /// Every other NPC has no row, and says the default until a game master gives it a line
    /// (".greeting").
    ///
    /// A later migration adds the mark of an important line, and with it the NPCs that start
    /// marked (<see cref="Marked"/>).
    /// </summary>
    public static class NpcGreetingSeed
    {
        /// <summary>(creature row, greeting).</summary>
        public static readonly IReadOnlyList<(uint CreatureId, uint GreetingId)> Rows = new (uint, uint)[]
        {
            (30, 859), // Clan Master, Alia Das: role
            (42, 120), // Council Elder Solis, Alia Das: place
            (72, 535), // Supply Clerk Bradshaw, Alia Das Hospital: place
            (91, 459), // Council Advisor Todae, Daghda's Urn: place
            (92, 457), // Council Luminary Doyan, Daghda's Urn: mission
            (93, 481), // Council Elder Nula, Ranja Gorge Hospital: named
            (94, 380), // Dr. Eleanor Corman, Ranja Gorge: mission
            (96, 490), // Lieutenant Burke, Lake Corman: mission
            (98, 483), // Ranger Anjuhi, Stone Anvil: place
            (99, 465), // Ranger Tarina, Stone Anvil: mission
            (100, 1620), // Outpost Commander Rogers, Alia Das: place
            (102, 485), // Apprentice Juvak, Daghda's Urn Hospital: role
            (106, 450), // Council Elder Baruhi, The Memory Tree: place
            (109, 533), // Dr. Munson, Alia Das: mission
            (510001, 1678), // Archaeologist Wynne Topper, Twin Pillars Outpost: mission
            (510003, 1665), // Commander Armstrong, Twin Pillars Outpost: mission
            (510007, 1096), // Captain Daris Linier, White Oasis Post: role
            (510009, 1040), // Captain Marcus Emmons, Stonewall Ridge: place
            (510010, 1026), // Colonel Mason Linier, Shadow's Edge Post: mission
            (510011, 1050), // Ida Hayner, Shadow's Edge Post: named
            (510014, 1071), // Mechanic Raji Lasat, Ashoka Settlement: mission
            (510016, 952), // Awol Lieutenant Deirdre, Staal: place
            (510019, 889), // Captain DeLisi, Outpost Intrepid: named
            (510022, 947), // Corporal Fiorella, Outpost Intrepid: place
            (510024, 901), // Kappa Grupa Lohen, Staal: named
            (510025, 894), // Labbna Grupa Rigs, Staal: mission
            (510026, 895), // Labbna Sigal Haggus, Staal: role
            (510028, 897), // Larai Sigal Tempa, Staal: role
            (510029, 971), // Larai Zupa Madias, Prometheus Outpost: named
            (510032, 934), // Merchant Cugin, Staal: Top Khana Quarter: place
            (510033, 933), // Merchant Vargo, Staal: Top Khana Quarter: mission
            (510035, 1527), // Special Agent Capriulo, Staal: mission
            (510036, 1528), // Viddea Scientist Eugin, Staal: role
            (510037, 899), // Viddia Grupa Donal, Staal: named
            (510038, 1160), // Supervisor Sheira, Incurables Ward: place
            (510039, 993), // Captain Howry, Substation E-104 Field Hospital: mission
            (510041, 991), // Corporal MacGruber, Substation E-104 Field Hospital: mission
            (510048, 1540), // Sigal Swona, Viddia Camp Hospital: place
            (510051, 1000), // Corporal Cottman, Charon's Crossing: place
            (510052, 1023), // General Perry, Tantalus Base: mission
            (510053, 1238), // Labbna Trader Joss, Tantalus Base: role
            (510054, 1192), // Larai Zupa Monlo, Tampeii Settlement: named
            (510056, 1003), // Private Heard, Icarus Post: place
            (510057, 1586), // Professor Whitney, Icarus Post Infirmary: mission
            (510058, 1204), // Recon Officer Granger, Brimstone Falls: place
            (510059, 1591), // Researcher Dalton, Tampeii Settlement: mission
            (510069, 634), // Major Nicholson, Nyxroq Post: mission
            (510071, 1515), // Private Parsons, Plains Post: mission
            (510074, 818), // Salvage Master Orto, Ortho: role
            (510077, 1511), // Supply Sergeant Otto, Ortho: mission
            (510082, 877), // Agent Zim, Irendas Penal Colony: mission
            (510087, 1147), // Drill Sergeant Luter, Irendas Penal Colony: role
            (510088, 817), // Engineer Tralos, Irendas Penal Colony: place
            (510093, 305), // Master Salvager Miru, Irendas Penal Colony: role
            (510096, 1604), // Lieutenant Donners, Kardash Atta Colony: mission
            (510097, 936), // Sergeant Phenix, Phanin Research Facility: mission
            (510099, 581), // Agent Franz, Foreas Base: mission
            (510104, 122), // Elder Q'uoa, Thoria Das: place
            (510123, 1665), // Commander Aldrin, Cumbria Research Facility: mission
            (510129, 1482), // Private Richardson, River-base Krimm: mission
            (510130, 1483), // Private Robertson, Fort Dew: mission
            (510132, 505), // Ranger Gorodai, Treeback Ridge: place
            (510135, 1474), // Sergeant Mullen, River-base Krimm: place
            (510138, 80), // Warden Brocail, Walk of Giants: place
            (510139, 81), // Warden Kahlee, Walk of Giants: place
            (510140, 82), // Warden Lagori, Walk of Giants: place
            (510143, 1665), // Commander Lovell, Gangus Outpost: mission
            (510144, 1227), // Field Commander Welling, Dia Moroyo: mission
            (510145, 1408), // Major Jones, Gangus Supply Annex Waypoint: place
            (510150, 1304), // Spec-Ops Quartermaster Olivia, Gangus Outpost: role
            (510167, 852), // Retread Samuel, Bane Refueling Station: named
            (510173, 506), // Bartender McLaughlin, AFS Criminal Investigations Division: role
            (510174, 724), // CID Commander Provost, AFS Criminal Investigations Division: role
            (510175, 831), // CID Detective Crais, AFS Criminal Investigations Division: mission
            (510176, 866), // Colonel Bosley, Camp Resistance Hospital: place
            (510180, 1665), // Commander Grissom, AFS Criminal Investigations Division: mission
            (510183, 287), // Field Sgt. Garde, Valverde Chasm: place
            (510186, 810), // Luminary Sampei, New Velon Village: place
            (510188, 734), // Private Remick, Wedge Rock Outpost: mission
            (510190, 816), // Ranger Porthus, Valverde Chasm: named
            (510192, 283), // Senior Engineer Mauer, AFS Criminal Investigations Division: role
            (510193, 512), // Special Agent Perdu, Fort Defiance: mission
        };

        public static IEnumerable<string> InsertStatements =>
            Rows.Select(row => $"insert into {NpcGreetingEntry.TableName} (id, greeting_id) values ({row.CreatureId}, {row.GreetingId});");

        /// <summary>
        /// The NPCs that start with a line marked important - the speech bubble over them
        /// (NpcGreetings) - each with the line: the rows of Add_npc_greeting_important.
        ///
        /// Nothing in the client says which NPCs had the bubble, and these lines' words do not
        /// give their speakers away either. One is here by another kind of evidence:
        ///  - footage: a recording of the live game shows the NPC under the bubble, saying the
        ///    line. It is a Brigadier General who gives 488, "We want to take Earth back. We
        ///    want this war over with.", and the world has one.
        /// </summary>
        public static readonly IReadOnlyList<(uint CreatureId, uint GreetingId)> Marked = new (uint, uint)[]
        {
            (510002, 488), // Brigadier General Beacham, Alia Das: footage
        };

        /// <summary>
        /// The statements that give the marked NPCs their lines. insertOrIgnore is the
        /// provider's insert that leaves a row already there alone ("insert or ignore" in
        /// SQLite, "insert ignore" in MySQL): an NPC a game master has given a line keeps it.
        /// The mark then goes on the row if the line is the one it is for - the seeded row, or
        /// the same line given by hand before - and on no other line.
        /// </summary>
        public static IEnumerable<string> MarkStatements(string insertOrIgnore)
        {
            foreach (var row in Marked)
            {
                yield return $"{insertOrIgnore} into {NpcGreetingEntry.TableName} (id, greeting_id, important) values ({row.CreatureId}, {row.GreetingId}, 1);";
                yield return $"update {NpcGreetingEntry.TableName} set important = 1 where id = {row.CreatureId} and greeting_id = {row.GreetingId};";
            }
        }
    }
}
