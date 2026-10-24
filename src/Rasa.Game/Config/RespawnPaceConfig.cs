using System.Collections.Generic;

namespace Rasa.Config
{
    /// <summary>
    /// appsettings.json's RespawnPace: the least a cleared spawn pool of the open world stays down, by what it holds
    /// (Managers.RespawnPace) - drawn between a kind's two numbers, in seconds, each time it goes down. A pool whose
    /// row (spawnpool.respown_time) says longer keeps its row's time. Picked up by a config reload, from the next pool
    /// that goes down; a pool already down keeps the time it was given.
    /// </summary>
    public class RespawnPaceConfig
    {
        /// <summary>
        /// Whether the pace is kept. Off - and without the section - every pool comes back after its row's time, as
        /// it always did: two seconds for some of the Wilderness's Bane and its Pinhole Egg-Layers, fifty for the
        /// named bosses of the battlefields.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// The maps it is kept on, by map context id. Empty: the fifteen battlefields
        /// (Managers.RespawnPace.Battlefields). Never a private instance of a map (a mission's place, which keeps its
        /// row's time) nor a squad's instance, which has its own clock (SquadInstances.RespawnMinutes).
        /// </summary>
        public List<uint> Maps { get; set; } = new List<uint>();

        /// <summary>The rank and file: soldiers, beasts, turrets, an armed friend at a post.</summary>
        public int TrashSecondsMin { get; set; } = 60;
        public int TrashSecondsMax { get; set; } = 90;

        /// <summary>Elites: a lieutenant, and a boss's class whose creature row stands in several pools of its map (a camp's Thrax Commander).</summary>
        public int EliteSecondsMin { get; set; } = 180;
        public int EliteSecondsMax { get; set; } = 300;

        /// <summary>A named boss: a boss's class whose creature row stands in one pool of its map alone.</summary>
        public int BossSecondsMin { get; set; } = 300;
        public int BossSecondsMax { get; set; } = 600;

        /// <summary>
        /// A pool holding the creature a mission's objective asks to be killed (by its creature row or its class):
        /// whatever else it is, it is back this soon, so the next player on the mission is not kept waiting for a
        /// boss's time. A row that says longer still keeps its own.
        /// </summary>
        public int MissionSecondsMin { get; set; } = 60;
        public int MissionSecondsMax { get; set; } = 120;

        /// <summary>Somebody to talk to - a vendor, a trainer, a hospital's medic - or an unarmed friend.</summary>
        public int ServiceSecondsMin { get; set; } = 30;
        public int ServiceSecondsMax { get; set; } = 30;
    }
}
