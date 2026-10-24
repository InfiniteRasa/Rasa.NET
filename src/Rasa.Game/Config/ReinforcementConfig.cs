using System.Collections.Generic;

namespace Rasa.Config
{
    /// <summary>
    /// appsettings.json's Reinforcements: how fresh troops come into the open world (Managers.Reinforcements).
    /// Every value has a default, so a file without the section has them on.
    /// </summary>
    public class ReinforcementConfig
    {
        /// <summary>
        /// Whether a pool of Bane troops on one of <see cref="ShipMaps"/> that has no arrival point of
        /// its own (spawnpool_arrival) restocks by Bane dropship when there is open sky over some of its
        /// ground - or, for one within an outpost's walls, outside them. Off, only pools whose own
        /// anim_type or arrival rows ask for a ship get one.
        /// </summary>
        public bool BaneDropships { get; set; } = true;

        /// <summary>
        /// Whether the soldiers of an AFS control point garrison come back by AFS dropship, when their pool
        /// restocks or the AFS take the point.
        /// </summary>
        public bool AfsDropships { get; set; } = true;

        /// <summary>
        /// Whether troops that restock without a ship - no open sky over their ground, indoors, a pool of
        /// some that cannot walk - beam in where they stand: their class's CREATURE_BIRTH, for the classes
        /// that have one. Off, they appear as before.
        /// </summary>
        public bool BeamIn { get; set; } = true;

        /// <summary>
        /// Whether a spawner dropship (9268 the Bane's, 9269 the AFS's) stays in each of its states for as
        /// long as its class's clip there runs, and never less than the five seconds each had (Structures.Dropship):
        /// the Bane ship's fly-in 5.75 s, its landing 8.25 s, its fly-out 6 s; the AFS ship's fly-out 5.65 s.
        /// Off, every state is five seconds and the landing two before the troops and three after.
        /// </summary>
        public bool ClipTimings { get; set; } = true;

        /// <summary>How much open sky a ship needs over the spot it comes down on, in metres.</summary>
        public float SkyClearance { get; set; } = 40f;

        /// <summary>
        /// The maps ships fly over, by map context id: the battlefields of the client's gamecontext table
        /// (type 4) that are the open world. Empty: those fifteen (Reinforcements.DefaultShipMaps).
        /// </summary>
        public List<uint> ShipMaps { get; set; } = new List<uint>();
    }
}
