using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Earning Respect (native mission 1170), Ligo Crucible: Kappa Grupa Lohen will not deal with the AFS until the player
    /// has the trust of the four syndicate Grupas: a talk with each, and a token from each.
    ///
    /// Native: texts 8707-8710, 8711 (missionconversation): log "Kappa Grupa Lohen ... will not discuss any kind of deal with
    /// the AFS until you gain the trust and approval of all four syndicate Grupas by getting a Syndicate Token from each of
    /// them.", offer "You must get four of these, one from each Grupa ... show the tokens to me", turn-in "So, you have
    /// proved yourself to the Grupas?"; objectives (missionobjective, no counters) 1 "Speak to Grupa Munit." 8714/8715, 2
    /// "Acquire the Top Khana token." 8717/8718, 3 "Speak to Grupa Vitto." 8719/8720, 4 "Acquire the Larai token."
    /// 8722/8723, 5 "Speak to Grupa Donal." 8724/8725, 6 "Acquire the Viddia Token." 8727/8728, 7 "Speak to Grupa Rigs."
    /// 8734/8735, 8 "Acquire the Labbna Token." 8737/8738; objectiveconversation (1170,1) package 1229 (Munit) 8716, (1170,3)
    /// 1225 (Vitto) 9864, (1170,5) 1227 (Donal) 8726, (1170,7) 1231 (Rigs) 9865, each variant 1. The token bodies: "by
    /// accepting a mission from Grupa Munit and earning his trust", "by completing a mission for him" (Vitto, Rigs), "by
    /// sucessfully completing Grupa Donal's mission".
    ///
    /// Sourced: TaRapedia Earning Respect: giver Kappa Grupa Lohen (Staal Detention Center); requires The Boss Of All Bosses;
    /// follow-ups Business Proposal (1229) and Irreconcilable Differences (1185); 160,000 XP, 7,800 credits; one of Vitalius
    /// EMP Torqueshell Rifle, Vitalius Electric Polarity Gun, Vextronics Rocket Launcher, Teleract Virulent Leech Gun; each
    /// "Speak to Grupa X" leads to that Grupa's mission (Munit Smoking Guns, Vitto The Protection Racket, Donal Game Wardens,
    /// Rigs Junkyard Dogs); token counters "0/1" (TaRapedia history, 2008-04). TR MMO DB: level 35, 7800.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Receiver Lohen (the turn-in text is his). The eight objectives open at once, in any order. Each token objective is
    ///    that Grupa's mission done (1148, 1146, 1147, 1149: each gives its token as a fixed reward), from the history too,
    ///    as Comic Relief (PlateauComicReliefV1) counts Scavenger Hunt: so a token mission done before Earning Respect counts.
    ///    The tokens stay with the player ("show the tokens to me"; nothing takes them).
    ///  - The items the lowest Uncommon templates of the kinds in the band of level 35 ("Rocket Launcher" with no damage
    ///    kind read as the physical one): EMP Torqueshell Rifle 49592, Electric Polarity Gun 15542, Rocket Launcher 49136,
    ///    Virulent Leech Gun 15295; one to choose. A marker at each Grupa for his talk and for his token.
    /// </summary>
    public static class CrucibleEarningRespectV1
    {
        public const uint MissionId = 1170;
        public const uint Level = 35;
        public const uint Lohen = CrucibleGrupaChiefsNpcsV1.Lohen;
        public const double MarkerRadius = 8;

        /// <summary>The four Grupas: the talk objective, its texts, the token objective, its texts, the NPC, package, and the token's mission.</summary>
        public static readonly (uint Talk, uint TalkName, uint TalkBody, uint Token, uint TokenName, uint TokenBody, uint Npc, uint Package, uint TokenMission)[] Grupas =
        {
            (1, 8714, 8715, 2, 8717, 8718, CrucibleOutpostIntrepidNpcsV1.Munit, CrucibleOutpostIntrepidNpcsV1.MunitPackage, CrucibleSmokingGunsV1.MissionId),
            (3, 8719, 8720, 4, 8722, 8723, CrucibleOutpostIntrepidNpcsV1.Vitto, CrucibleOutpostIntrepidNpcsV1.VittoPackage, CrucibleProtectionRacketV1.MissionId),
            (5, 8724, 8725, 6, 8727, 8728, CrucibleOutpostIntrepidNpcsV1.Donal, CrucibleOutpostIntrepidNpcsV1.DonalPackage, CrucibleGameWardensV1.MissionId),
            (7, 8734, 8735, 8, 8737, 8738, CrucibleOutpostIntrepidNpcsV1.Rigs, CrucibleOutpostIntrepidNpcsV1.RigsPackage, CrucibleJunkyardDogsV1.MissionId)
        };

        public const uint Experience = 160000;
        public const uint Credits = 7800;
        public const uint EmpTorqueshellRifle = 49592;  // Weapon_Avatar_Torqueshell_Rifle_EMP_UNC_33_to_37
        public const uint ElectricPolarityGun = 15542;  // Weapon_Avatar_PolarityGun_Electric_UNC_33_to_37
        public const uint RocketLauncher = 49136;       // Weapon_Avatar_RocketLauncher_Physical_UNC_34_to_38
        public const uint VirulentLeechGun = 15295;     // Weapon_Avatar_LeechGun_Virulent_UNC_34_to_38

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Earning Respect", 8707, Lohen, Lohen, Level);
            mission.Reward(Experience, Credits, new[] { EmpTorqueshellRifle, ElectricPolarityGun, RocketLauncher, VirulentLeechGun });
            mission.Prerequisite(CrucibleBossOfAllBossesV1.MissionId);

            foreach (var (talk, talkName, talkBody, token, tokenName, tokenBody, npc, package, tokenMission) in Grupas)
            {
                mission.Objective(talk, talkName, talkBody, talk - 1, MissionObjectiveState.Incomplete);
                mission.Objective(token, tokenName, tokenBody, token - 1, MissionObjectiveState.Incomplete);
                mission.Transition(talk);
                mission.Conversation(talk, package);
                mission.Transition(token);
                mission.Progress(token, MissionProgressEventKind.MissionCompleted, tokenMission, null);
                var at = CrucibleOutpostIntrepidNpcsV1.At(npc);
                mission.Indicator(talk, talk, at.X, at.Y, at.Z, MarkerRadius);
                mission.Indicator(token, token, at.X, at.Y, at.Z, MarkerRadius);
            }
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1170",
                "Native1 'Speak to Grupa Munit.' 8714/8715, 2 Top Khana token 8717/8718, 3 Vitto 8719/8720, 4 Larai token " +
                "8722/8723, 5 Donal 8724/8725, 6 Viddia token 8727/8728, 7 Rigs 8734/8735, 8 Labbna token 8737/8738, no counters; " +
                "objectiveconversation 1170,1 package1229 8716, 1170,3 1225 9864, 1170,5 1227 8726, 1170,7 1231 9865. Texts " +
                "8707-8711 (show the tokens to me). Name 9071 Kappa Grupa Lohen.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Earning_Respect",
                "Giver Kappa Grupa Lohen (Staal Detention Center); requires The Boss Of All Bosses; follow-ups 1229, 1185; " +
                "160000XP, 7800 credits; Vitalius EMP Torqueshell Rifle, Vitalius Electric Polarity Gun, Vextronics Rocket " +
                "Launcher, Teleract Virulent Leech Gun; each Grupa's talk leads to his token mission (1148, 1146, 1147, 1149). " +
                "TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleEarningRespectV1",
                "Receiver Lohen. The eight objectives open at once; each token objective is the Grupa's mission completed " +
                "(1148, 1146, 1147, 1149), from the history too (as Comic Relief); the tokens stay. Items 49592, 15542, 49136, " +
                "15295, one to choose. A marker at each Grupa for his talk and his token.", 0.3);

            mission.Enable(new MissionSceneDefinition { ExistingFactObjectiveIds = new() { 2, 4, 6, 8 } });
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
