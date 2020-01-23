using System.Collections.Generic;

namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;

    public class CreatePhysicalEntityPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CreatePhysicalEntity;

        public uint EntityId { get; set; }
        public EntityClass ClassId { get; set; }
        public List<PythonPacket> EntityData { get; } = new List<PythonPacket>();

        public CreatePhysicalEntityPacket(uint entityId, EntityClass classId)
        {
            EntityId = entityId;
            ClassId = classId;
        }

        public CreatePhysicalEntityPacket(uint entityId, EntityClass classId, List<PythonPacket> entityData)
        {
            EntityId = entityId;
            ClassId = classId;
            EntityData = entityData;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteUInt(EntityId);
            pw.WriteInt((int) ClassId);

            /*
             * These packets will be called on the newly created entity. It behaves the same as if we've sent them separately. Order matters!
             * 
             * Possible packets that can be appended here: (Not all of them are reasonable to include...)
             *   + PhysicalEntity
             *     - BodyAttributes
             *     - WorldLocationDescriptor
             *     - WorldPlacementDescriptor
             *     - IsTargetable
             *     - ServerSkeleton
             *     - ExamineResults
             *     - GameEffectAttached
             *     - GameEffectAttachFailed
             *     - GameEffectTick
             *     - CallGameEffectMethod
             *     - GameEffectDetached
             *     - GameEffects
             *     - GameEffectUpdateTooltip
             *     - PerformObjectAbility
             *   + CharacterSelectionPod augmentation:
             *        - CharacterInfo
             *        - CloneCreditsChanged
             *   + Other augmentations: ...
             */
            if (EntityData.Count > 0)
            {
                pw.WriteList(EntityData.Count);

                foreach (var packet in EntityData)
                {
                    pw.WriteTuple(2);
                    pw.WriteInt((int) packet.Opcode);

                    packet.Write(pw);
                }
            }
            else
                pw.WriteNoneStruct();
        }
    }
}
