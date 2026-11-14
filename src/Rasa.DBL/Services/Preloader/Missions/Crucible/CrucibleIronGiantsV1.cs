using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Iron Giants (native mission 1213), Ligo Crucible: Larai Zupa Madias at Prometheus Outpost challenges the player to
    /// destroy six Bane Juggernauts at Khulago Base.
    ///
    /// Native: texts 9564-9568 (missionconversation): log "Zupa Madias challenges you to destroy six Bane Juggernauts at
    /// Khalago Base.", offer "... For my challenge, I am thinking you should destroy six Juggernauts! ...", turn-in "They who
    /// are the biggest, fall the hardest, no? ...", reminder "These monsters of metal kill my people, and yours! ..."; objective
    /// 1 9662/9663 ("Destroy 6 Juggernauts"), counter 9664 "Juggernauts Destroyed" (missionobjective). No objective
    /// conversation.
    ///
    /// Sourced: TaRapedia Iron Giants: giver and reward giver Larai Zupa Madias (Prometheus Outpost); requires Confirmed
    /// Kills; 170,000 XP, 8,000 credits; one of ChiTech Cryogenic Blade, ChiTech Incendiary Shotgun, Titan Radial Repair Tool,
    /// ChiTech EMP Rifle. TR MMO DB: level 35.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - The Juggernauts: a kill of the new row 770041 (CruciblePrometheusOutpostNpcsV1), in three point pools of two in
    ///    front of Khulago Base (CreatureKilled, counter 0, 6). A marker (r15) on each pool.
    ///  - The items: the lowest template id of each kind whose Uncommon band holds level 35 (Blade_Ice 47648,
    ///    Shotgun_v1_Fire 14855, Tool_Avatar_Field_Repair_Area_UNC 119370, Rifle_EMP 14774), one to choose.
    /// </summary>
    public static class CrucibleIronGiantsV1
    {
        public const uint MissionId = 1213;
        public const uint Level = 35;
        public const uint Madias = CrucibleKardashAndPrometheusNpcsV1.Madias;
        public const uint Destroy = 1;
        public const uint Juggernauts = 6;
        public const double MarkerRadius = 15;

        public const uint Experience = 170000;
        public const uint Credits = 8000;
        public const uint CryogenicBlade = 47648;       // Blade_Ice
        public const uint IncendiaryShotgun = 14855;    // Shotgun_v1_Fire
        public const uint RadialRepairTool = 119370;    // Tool_Avatar_Field_Repair_Area_UNC
        public const uint EmpRifle = 14774;             // Rifle_EMP

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Iron Giants", 9564, Madias, Madias, Level);
            mission.Reward(Experience, Credits, new[] { CryogenicBlade, IncendiaryShotgun, RadialRepairTool, EmpRifle });
            mission.Prerequisite(CrucibleConfirmedKillsV1.MissionId);

            mission.Objective(Destroy, 9662, 9663, 0, MissionObjectiveState.Incomplete, counterTextId: 9664);
            mission.Transition(Destroy);
            mission.Progress(Destroy, MissionProgressEventKind.CreatureKilled, CruciblePrometheusOutpostNpcsV1.Juggernaut, Juggernauts,
                counter: 0, trigger: 1);
            mission.Action(Destroy, 1, MissionActionKind.GrantReward, reward: 1);
            var pools = CruciblePrometheusOutpostNpcsV1.JuggernautPools;
            for (var i = 0; i < pools.Length; i++)
                mission.Indicator(Destroy, (uint)i + 1, pools[i].At.X, pools[i].At.Y, pools[i].At.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1213",
                "Native1 'Destroy 6 Juggernauts' 9662/9663 counter9664 'Juggernauts Destroyed'; no objectiveconversation. Texts " +
                "9564-9568 (six Bane Juggernauts at Khalago Base). Class 7722 Vehicle_Bane_Juggernaut 'Juggernaut'.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Iron_Giants",
                "Giver/reward giver Larai Zupa Madias (Prometheus Outpost); requires Confirmed Kills; 170000XP, 8000 credits; one of " +
                "ChiTech Cryogenic Blade, ChiTech Incendiary Shotgun, Titan Radial Repair Tool, ChiTech EMP Rifle. TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleIronGiantsV1",
                "CreatureKilled 770041 (a new Juggernaut row, class 7722, level 42), counter 0, 6; three point pools of two in " +
                "front of Khulago Base (no navmesh lava within 80 m), a marker r15 on each. Items 47648, 14855, 119370, 14774 (lowest template of the kind with " +
                "an Uncommon band at 35), one to choose.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
