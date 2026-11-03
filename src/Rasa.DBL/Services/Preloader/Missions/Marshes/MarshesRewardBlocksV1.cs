using System.Collections.Generic;
using System.Linq;
using Rasa.Services.Preloader.Missions.Plateau;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Sets 1-4 rewards moved to the client's per-mission template blocks (the method of set 5: the client's item template list,
    /// World DB itemtemplate, holds blocks of four consecutive Uncommon templates whose kinds and levels match the mission's
    /// TaRapedia list). Sets 1-4 used the lowest Uncommon template of each kind before; each mission below has a block whose
    /// kinds, slots and levels are those of its current four (checked 2026-11-03 against docs/gm-items and the World DB):
    ///  - 541 Welcome Wagon 120105-120108 (Laser Machine Gun 38, Fire Net Gun 36, Fire Polarity Gun 38, Sonic Shotgun 37);
    ///  - 498 Cannister Collection 120041-120044 (Ice Injection Gun 37, EMP Machine Gun 33, Electric Rifle 36, Physical Rocket
    ///    Launcher 34);
    ///  - 539 What's That Gas 120133-120136 (Ice Grenade Launcher 35, Fire Leech Gun 34, Electric Net Gun 36, Physical Pistol 35);
    ///  - 917 Divided We Fall 120049-120052 (Hazmat Boots 34, Motor Assist Boots 33, Reflective Boots 34, Stealth Legs 35);
    ///  - 1673 It Lies in Ruins (Plateau revision) 120101-120104 (Hazmat Vest 39, Mech Vest 35, Motor Assist Vest 38,
    ///    Reflective Vest 39);
    ///  - 968 Butcher, Baker, Caretaker 120033-120036 (Electric Blade 36, Area Healing Disc 40, Fire Staff 40, Physical Shotgun 37:
    ///    TaRapedia's four and levels; the block has the staff before the shotgun, and its disc is the Area one, TaRapedia's
    ///    "Conical");
    ///  - 1029 Regulator Part III 120089-120092 (Fire Injection Gun 37, Electric Machine Gun 38, EMP Pistol 35, EMP Rocket
    ///    Launcher 34).
    /// No block, unchanged: 881 (no block holds Bio Vest, Stealth Boots, Hazmat Legs and Reflective Gloves); 890, 913 and 1030
    /// (no item names on any source: no block can be chosen); 959 (armor modules), 961, 925 and 1028 (consumable stacks).
    /// </summary>
    public static class MarshesRewardBlocksV1
    {
        /// <summary>Mission, content revision, the four templates before, the client's block (first id; four in order).</summary>
        public static readonly (uint Mission, string Revision, uint[] Before, uint Block)[] Moves =
        {
            (MarshesWelcomeWagonV1.MissionId, MarshesMissionDataV1.Revision, new uint[] { 45762, 45812, 48680, 14937 }, 120105),
            (MarshesCannisterCollectionV1.MissionId, MarshesMissionDataV1.Revision, new uint[] { 48108, 15150, 14738, 49136 }, 120041),
            (MarshesWhatsThatGasV1.MissionId, MarshesMissionDataV1.Revision, new uint[] { 47944, 47728, 15513, 14404 }, 120133),
            (MarshesDividedWeFallV1.MissionId, MarshesMissionDataV1.Revision, new uint[] { 20335, 28614, 35571, 39191 }, 120049),
            (MarshesItLiesInRuinsV1.MissionId, PlateauMissionDataV1.Revision, new uint[] { 20951, 33193, 29247, 36188 }, 120101),
            (MarshesButcherBakerCaretakerV1.MissionId, MarshesMissionDataV1.Revision, new uint[] { 15475, 18367, 14820, 49496 }, 120033),
            (MarshesRegulatorPartThreeV1.MissionId, MarshesMissionDataV1.Revision, new uint[] { 45777, 15111, 14569, 15437 }, 120089)
        };

        /// <summary>The template a mission rewards at the place of a former one (the same one if it did not move).</summary>
        public static uint Current(uint mission, uint before)
        {
            foreach (var move in Moves.Where(move => move.Mission == mission))
                for (var index = 0; index < move.Before.Length; index++)
                    if (move.Before[index] == before)
                        return move.Block + (uint)index;
            return before;
        }

        public static IEnumerable<string> Up() => Moves.SelectMany(move => move.Before.Select((before, index) =>
            Update(move.Mission, move.Revision, before, move.Block + (uint)index)));

        public static IEnumerable<string> Down() => Moves.SelectMany(move => move.Before.Select((before, index) =>
            Update(move.Mission, move.Revision, move.Block + (uint)index, before)));

        private static string Update(uint mission, string revision, uint from, uint to) =>
            $"UPDATE mission_reward_item SET item_template_id = {to} WHERE mission_id = {mission} AND " +
            $"content_revision = '{revision}' AND reward_id = 1 AND kind = 2 AND item_template_id = {from};";
    }
}
