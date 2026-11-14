using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// The Boss Of All Bosses (native mission 1161), Ligo Crucible: Captain DeLisi sends the player to Kappa Grupa Lohen, the
    /// head of the Staal syndicates, in his office at the top of Staal, to offer him an alliance with the AFS.
    ///
    /// Native: texts 8483-8486, 8487 (missionconversation; 8487 is empty): log "Locate Kappa Grupa Lohen and offer him an
    /// informal alliance with the AFS in the war against the Bane. Do whatever it takes to earn his trust.", turn-in "The
    /// giving of a small gift is customary before business can be discussed"; objective 1 "Locate the Kappa Grupa."
    /// 8704/8705, no counter (missionobjective); objectiveconversation (1161,1) package 1222 (Lohen) variant 1 text 10046 and
    /// variant 159 text 8706, the same text.
    ///
    /// Sourced: TaRapedia The Boss Of All Bosses: giver Captain DeLisi (Outpost Intrepid), reward from Kappa Grupa Lohen
    /// (Staal Detention Center); requires The Wire; follow-ups The Protection Racket, Game Wardens, Smoking Guns, Junkyard
    /// Dogs and Earning Respect; 40,000 XP, 3,900 credits; Class VII Concussion Grenade x3, Class VII Basic Med Pack x6, Class
    /// VII Fragmentation Grenade x3, Class VII EMP Bomb; Lohen "at the top most floor in Staal ... use local teleporter close
    /// to the Staal waypoint teleporter" (Kappa Grupa Lohen). The items by their client names (physicalentityclassnamelanguage):
    /// 26268 Class VII Concussion Grenade (template 111018), 22543 Class VII Basic Med Pack (45060), 22963 Class VII
    /// Fragmentation Grenade (45446), 26542 Class VII EMP Bomb (111232).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 40 (no source: the level of The Wire). Lohen moved into his office and the office's teleporters made
    ///    (CrucibleGrupaChiefsNpcsV1).
    ///  - The talk with the client's variant 1; variant 159 (the same text) has no known flag and is not written.
    ///  - The four items one to choose, in TaRapedia's stacks. A marker at Lohen and one at the lower teleporter.
    /// </summary>
    public static class CrucibleBossOfAllBossesV1
    {
        public const uint MissionId = 1161;
        public const uint Level = 40;
        public const uint DeLisi = CrucibleOutpostIntrepidNpcsV1.DeLisi;
        public const uint Lohen = CrucibleGrupaChiefsNpcsV1.Lohen, LohenPackage = CrucibleGrupaChiefsNpcsV1.LohenPackage;
        public const uint Locate = 1;
        public const double MarkerRadius = 8, PadMarkerRadius = 4;

        public const uint Experience = 40000;
        public const uint Credits = 3900;
        public const uint ConcussionGrenade = 111018, ConcussionGrenades = 3;   // Class VII Concussion Grenade (class 26268)
        public const uint BasicMedPack = 45060, BasicMedPacks = 6;              // Class VII Basic Med Pack (class 22543)
        public const uint FragGrenade = 45446, FragGrenades = 3;                // Class VII Fragmentation Grenade (class 22963)
        public const uint EmpBomb = 111232, EmpBombs = 1;                       // Class VII EMP Bomb (class 26542)

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "The Boss Of All Bosses", 8483, DeLisi, Lohen, Level);
            mission.Reward(Experience, Credits,
                new[] { (ConcussionGrenade, ConcussionGrenades), (BasicMedPack, BasicMedPacks), (FragGrenade, FragGrenades), (EmpBomb, EmpBombs) },
                new (uint, uint)[0]);
            mission.Prerequisite(CrucibleWireV1.MissionId);

            mission.Objective(Locate, 8704, 8705, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Locate);
            mission.Conversation(Locate, LohenPackage);
            mission.Action(Locate, 1, MissionActionKind.GrantReward, reward: 1);

            var lohen = CrucibleGrupaChiefsNpcsV1.At(Lohen);
            var pad = CrucibleGrupaChiefsNpcsV1.LowerPadAt;
            mission.Indicator(Locate, 1, lohen.X, lohen.Y, lohen.Z, MarkerRadius);
            mission.Indicator(Locate, 2, pad.X, pad.Y, pad.Z, PadMarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1161",
                "Native1 'Locate the Kappa Grupa.' 8704/8705, no counter; objectiveconversation 1161,1 package1222 variant1 10046, " +
                "variant159 8706 (same text). Texts 8483-8486, 8487 empty. Name 9071 Kappa Grupa Lohen. Items 26268/111018, " +
                "22543/45060, 22963/45446, 26542/111232 by their client names.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Boss_Of_All_Bosses",
                "Giver Captain DeLisi, reward from Kappa Grupa Lohen (Staal); requires The Wire; follow-ups 1146, 1147, 1148, 1149, " +
                "1170; 40000XP, 3900 credits; Class VII Concussion Grenade x3, Basic Med Pack x6, Fragmentation Grenade x3, EMP " +
                "Bomb; Lohen on the top floor of Staal, by the local teleporter near the Staal waypoint.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleBossOfAllBossesV1",
                "Level40 estimated. Lohen moved into the office (-215, 244.91, 326); the office's Brann teleporters made local " +
                "teleporters 909/910. The talk with variant 1 only. Stacks 111018x3, 45060x6, 45446x3, 111232x1, one to choose. " +
                "Markers at Lohen and the lower teleporter.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
