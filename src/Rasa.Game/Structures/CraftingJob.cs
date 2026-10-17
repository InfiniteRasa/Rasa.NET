using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    /// <summary>
    /// One item a Kraftwerks is making for a player: what comes out, when it is done, which
    /// crafting page asked for it. Mirrors the client's ItemStatus (shared/crafting.py), except
    /// that CraftingStatus reports no time left for it: the client closes its window while any
    /// job has time left (CraftingStatusPacket). The time is the server's to keep, here, and the
    /// server sends a fresh status when a job finishes.
    /// </summary>
    public class CraftingJob
    {
        /// <summary>Identifies the job in CraftingStatus and in RequestRetrieveFinishedCraftItem; not an entity - the result item is created when it is taken.</summary>
        public ulong ResultItemId { get; set; }
        public uint ResultClassId { get; set; }
        public uint ResultItemTemplateId { get; set; }
        public uint Count { get; set; }
        public uint CraftingPage { get; set; }
        public uint QualityId { get; set; }
        public List<uint> LootModuleIds { get; set; } = new List<uint>();

        /// <summary>Environment.TickCount64 at which the station is done with it.</summary>
        public long FinishTick { get; set; }

        /// <summary>Set once the finished status has been sent, so the worker sends it once.</summary>
        public bool FinishReported { get; set; }

        /// <summary>Whether a crafting_job row holds it, under <see cref="ResultItemId"/> (KraftwerksManager.Persist).</summary>
        public bool Persisted { get; set; }

        public double TimeLeftSeconds => Math.Max(0, (FinishTick - Environment.TickCount64) / 1000.0);

        public bool IsFinished => Environment.TickCount64 >= FinishTick;
    }
}
