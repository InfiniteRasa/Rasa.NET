using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Train (555), on an NPC: <c>(trainerPackageIDs,)</c>. Wired so that it can be sent, and
    /// deliberately sent by nothing: the 1.16.5 client ignores it.
    ///
    /// client/augmentations/npc.py Recv_Train(trainerPackageIDs) is an empty stub - it returns
    /// without reading its argument - and it is the only Recv_Train in the client. No table,
    /// language module or window in the client knows a trainer package, and TrainSkill (556), the
    /// request that would have come back, is only a number in the method table: nothing sends it.
    /// What looks like an early design (an NPC offering the skill packages it teaches, the player
    /// picking one) was replaced before release and this receiver left behind. The element type of
    /// the list is not known, since nothing reads it; ints are a guess.
    ///
    /// Training in the released client goes through Converse instead: CONVO_TYPE_TRAINING
    /// (bCanTrain, dialogId) makes npc.py post UI_SHOW_CONVERSATION_TRAINING, the conversation
    /// window shows the npctrainerdialoglanguage line with a "View Classes" link, and that link
    /// posts UI_SHOW_TIER_SELECT for class and tier selection - the class trainer path this server
    /// already has.
    ///
    /// Only ever to an NPC's entity id (a creature with the NPC augmentation): any other entity
    /// has no Recv_Train, and the client does not survive a method its entity lacks. The C++
    /// server only listed the id.
    /// </summary>
    public class TrainPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Train;

        public IReadOnlyList<int> TrainerPackageIds { get; }

        public TrainPacket(IReadOnlyList<int> trainerPackageIds)
        {
            TrainerPackageIds = trainerPackageIds ?? new List<int>();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(TrainerPackageIds.Count);
            foreach (var id in TrainerPackageIds)
                pw.WriteInt(id);
        }
    }
}
