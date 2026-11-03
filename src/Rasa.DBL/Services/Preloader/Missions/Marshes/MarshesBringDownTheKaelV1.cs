using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Bring Down the Kael (native mission 542), Valverde Marshes: Elder Toida in Paludos wants the Thrax guards of a Bane
    /// prison caravan on the Long Walk dead, so that its Forean prisoners can go free.
    ///
    /// Native: texts 1458-1461 and 7554 (missionconversation): log "Free the transported Forean prisoners on the Long Walk by
    /// eliminating all of their Thrax guardians. Report back to Elder Toida in Paludos", offer "transporting prisoners from
    /// the ruins of P'reo Das in the northeast to the supply depot in the south. Seek out one of these prison caravans along
    /// the Long Walk and kills the guards", turn-in "Those are three less of our people who will be enslaved by the Bane";
    /// objective 1 "Eliminate the Bane Prison Guards to free the Escorted Foreans" 1528/7555, counter 1529 "Thrax Guards
    /// Eliminated:" (missionobjective); no objective conversation. Name 3125 Elder Toida.
    ///
    /// Sourced: TaRapedia Bring Down the Kael: giver and receiver Elder Toida (Paludos); 150,000 XP, 7,600 credits; one of
    /// Wellcare Graviton Armor Vest, Prodigy Reflective Armor Vest, Dynamo Bio Armor Boots, CryoGen Motor Assist Armor Vest
    /// (no levels); TaR-hist: the guards "in the Burning Marsh, marching their prisoners along the Eastern side of the long
    /// defensive line", "approximately 500, 315", later (420, -300), (500, 100), (502, 134), "don't seem to fight back". TR
    /// MMO DB: level 33. World DB: Elder Toida 510160.
    ///
    /// Reconstructed (ESTIMATE): the caravan MarshesEastAndNorthNpcsV1's public squad (one prisoner leader, four guards of
    /// their own row) on the Long Walk; each of its guards killed by the player counts, four to finish (the player's kill
    /// credit, as the server gives it). The guards fight back (the server's Thrax). Level TR MMO DB's (TaRapedia has none).
    /// The items the lowest Uncommon templates of the bands that hold level 33 (no client block of these four found).
    /// Markers along the route.
    /// </summary>
    public static class MarshesBringDownTheKaelV1
    {
        public const uint MissionId = 542;
        public const uint Level = 33;
        public const uint Toida = MarshesWelcomeWagonV1.Toida;
        public const uint Kill = 1;
        public const uint Guard = MarshesEastAndNorthNpcsV1.Guard, Guards = MarshesEastAndNorthNpcsV1.Guards;
        public const double RouteRadius = 60;

        /// <summary>Markers along the Long Walk: its north end, (500, 100), its south end.</summary>
        public static readonly (double X, double Y, double Z)[] Markers =
        {
            MarshesEastAndNorthNpcsV1.LongWalk[0], MarshesEastAndNorthNpcsV1.LongWalk[8], MarshesEastAndNorthNpcsV1.LongWalk[^1]
        };

        public const uint Experience = 150000;
        public const uint Credits = 7600;
        public const uint GravitonVest = 24046;         // Armor_T3_Graviton_V02_UNC_Vest_30_to_34
        public const uint ReflectiveVest = 36148;       // Armor_T2_Reflective_V02_UNC_Vest_29_to_33
        public const uint BioBoots = 18730;             // Armor_T3_Bio_V02_UNC_Boots_30_to_34
        public const uint MotorAssistVest = 29227;      // Armor_T1_MotorAssist_V04_UNC_Vest_33_to_37
        public static readonly uint[] Rewards = { GravitonVest, ReflectiveVest, BioBoots, MotorAssistVest };

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Bring Down the Kael", 1458, Toida, Toida, Level);
            mission.Reward(Experience, Credits, Rewards);

            mission.Objective(Kill, 1528, 7555, 0, MissionObjectiveState.Incomplete, counterTextId: 1529);
            mission.Transition(Kill);
            mission.Progress(Kill, MissionProgressEventKind.CreatureKilled, Guard, Guards, counter: 0);
            mission.Action(Kill, 1, MissionActionKind.GrantReward, reward: 1);

            for (var marker = 0; marker < Markers.Length; marker++)
                mission.Indicator(Kill, (uint)marker + 1, Markers[marker].X, Markers[marker].Y, Markers[marker].Z, RouteRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/542",
                "Native1 'Eliminate the Bane Prison Guards to free the Escorted Foreans' 1528/7555 counter1529 'Thrax Guards " +
                "Eliminated:'; no objectiveconversation. Texts 1458-1461, 7554 (caravans on the Long Walk, P'reo Das to the " +
                "supply depot; 'three less of our people'). Names 3125 Elder Toida, 5311 Forean Prisoner Leader.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Bring_Down_the_Kael",
                "Giver/receiver Elder Toida (Paludos); 150000XP, 7600 credits; one of Wellcare Graviton Armor Vest, Prodigy " +
                "Reflective Armor Vest, Dynamo Bio Armor Boots, CryoGen Motor Assist Armor Vest. TaR-hist: east side of the long " +
                "defensive line, ~500,315; later 420,-300 / 500,100 / 502,134. TR MMO DB level33.", 0.5);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesBringDownTheKaelV1",
                "Public squad 760355: prisoner leader 760355 (bystander) + 4 guards 760356 on the Long Walk (480,320) to " +
                "(420,-300), there and back, 300 s respawn. 4 guard kills by the player. One prisoner, not three. Items 24046, " +
                "36148, 18730, 29227 (lowest UNC of the bands holding 33). Markers along the route.", 0.3);

            mission.Enable();
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
