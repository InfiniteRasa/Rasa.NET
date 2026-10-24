using System.Collections.Generic;

namespace Rasa.Config
{
    public class Config
    {
        public SocketAsyncConfig SocketAsyncConfig { get; set; }
        public GameConfig GameConfig { get; set; }
        public CommunicatorConfig CommunicatorConfig { get; set; }
        public Logger.LoggerConfig LoggerConfig { get; set; }
        public ServerInfoConfig ServerInfoConfig { get; set; }
        public QueueConfig QueueConfig { get; set; }
        public GameDataConfig GameDataConfig { get; set; }
        public VoiceConfig VoiceConfig { get; set; }
        public MessageOfTheDayConfig MessageOfTheDay { get; set; } = new MessageOfTheDayConfig();

        /// <summary>The REST API and the status port (Rasa.Api); both off without the section.</summary>
        public ApiConfig ApiConfig { get; set; } = new ApiConfig();

        /// <summary>The numbers of the Edmund Range match (Managers.Battlegrounds).</summary>
        public BattlegroundConfig Battleground { get; set; } = new BattlegroundConfig();

        /// <summary>What is done with a Move through a force field, through a wall, or standing on nothing (Managers.MovementChecks).</summary>
        public MovementChecksConfig MovementChecks { get; set; } = new MovementChecksConfig();

        /// <summary>What is done with a weapon shot at a target behind the shooter, out of reach or behind cover (Managers.WeaponChecks).</summary>
        public WeaponChecksConfig WeaponChecks { get; set; } = new WeaponChecksConfig();

        /// <summary>
        /// The maps that run in several shared copies, by map context id. Edmund Range (2374) is
        /// one without an entry in the file; an entry there for it replaces this one.
        /// </summary>
        public Dictionary<string, MapInstanceConfig> MapInstances { get; set; } = new Dictionary<string, MapInstanceConfig>
        {
            ["2374"] = new MapInstanceConfig()
        };

        /// <summary>The maps entered as a squad's own instance, and when those are closed (Managers.MapChannelManager's squad instances).</summary>
        public SquadInstanceConfig SquadInstances { get; set; } = new SquadInstanceConfig();

        /// <summary>Clan-owned control points: whether there are any, what one pays and when it is given back (Managers.ControlPoints).</summary>
        public ControlPointConfig ControlPoints { get; set; } = new ControlPointConfig();

        /// <summary>How the control points' outposts defend themselves: gate shields, defenders, turret bursts (Managers.ControlPointFields, Defenders, Emplacements).</summary>
        public ControlPointDefenceConfig ControlPointDefence { get; set; } = new ControlPointDefenceConfig();

        /// <summary>The chances of the weapon modules that fire on a hit (Managers.ItemModuleBonuses).</summary>
        public ItemModulesConfig ItemModules { get; set; } = new ItemModulesConfig();

        /// <summary>How the creature think is spread over the loop's passes, and how far a creature searches for a route (Managers.BehaviorManager).</summary>
        public CreatureAiConfig CreatureAi { get; set; } = new CreatureAiConfig();

        /// <summary>When each map's navmesh and cover are read, and let go of (Managers.LazyNavigation).</summary>
        public NavigationConfig Navigation { get; set; } = new NavigationConfig();

        /// <summary>How often a player's killing blow leaves a creature staggering for a Critical Death finisher, and whether a morphed player has them (Managers.CritDeathManager).</summary>
        public CritDeathConfig CritDeath { get; set; } = new CritDeathConfig();

        /// <summary>Whether the missions' radiation and toxic gas volumes hurt, and how hard (Managers.MissionHazards).</summary>
        public HazardsConfig Hazards { get; set; } = new HazardsConfig();

        /// <summary>How long a cleared spawn pool of a battlefield stays down, by what it holds (Managers.RespawnPace); without the section, its row's time, as before.</summary>
        public RespawnPaceConfig RespawnPace { get; set; } = new RespawnPaceConfig();

        /// <summary>What a line of sight is tested against under the terrain, in a cave or a tunnel (Managers.Sight); without the section, as before.</summary>
        public SightConfig Sight { get; set; } = new SightConfig();
    }
}
