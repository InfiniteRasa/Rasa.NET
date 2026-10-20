using System.Collections.Generic;

namespace Rasa.Packets.Crafting.Server
{
    using Data;
    using Memory;
    using Structures;

    /// <summary>
    /// Recv_CraftingStatus(manifestationId, statusList) on the Kraftwerks entity: the jobs the
    /// station holds for this player, in progress or finished. The client rebuilds its list from
    /// every message, so an empty list clears the window. Each entry is the client's ItemStatus
    /// tuple (shared/crafting.py): resultItemId, resultClassId, resultItemTemplateId, count,
    /// timeLeft (seconds), craftingPage, qualityId, lootModuleIds.
    ///
    /// timeLeft goes as 0 for every job, running or not. The 1.16.5 window closes itself on every
    /// frame in which a job it holds has time left (craftingwindownew.Update ->
    /// shared/crafting.py CanManifestationUseKraftwerksNow), and it has no progress display of
    /// its own, so a true figure only shut the window on Fabricate and kept it shut until the job
    /// was done. With 0 the window stays open and shows the item in its output slot while it is
    /// made; the time left goes to the on-screen countdown instead, and a Take before the job is
    /// done is answered with how long is left (KraftwerksManager).
    /// </summary>
    public class CraftingStatusPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CraftingStatus;

        public ulong ManifestationId { get; }
        public IReadOnlyList<CraftingJob> Jobs { get; }

        public CraftingStatusPacket(ulong manifestationId, IReadOnlyList<CraftingJob> jobs)
        {
            ManifestationId = manifestationId;
            Jobs = jobs ?? new List<CraftingJob>();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteULong(ManifestationId);
            pw.WriteList(Jobs.Count);

            foreach (var job in Jobs)
            {
                pw.WriteTuple(8);
                pw.WriteULong(job.ResultItemId);
                pw.WriteUInt(job.ResultClassId);
                pw.WriteUInt(job.ResultItemTemplateId);
                pw.WriteUInt(job.Count);
                pw.WriteDouble(0.0);
                pw.WriteUInt(job.CraftingPage);
                pw.WriteUInt(job.QualityId);
                pw.WriteList(job.LootModuleIds.Count);

                foreach (var moduleId in job.LootModuleIds)
                    pw.WriteUInt(moduleId);
            }
        }
    }
}
