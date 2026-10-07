using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Managers;
    using Memory;
    using Structures;

    /// <summary>
    /// actor.py Recv_AttributeInfo(attrDict): every attribute of the actor, each made anew on the
    /// client from (normalMax, currentMax, current, refreshAmount, refreshPeriod). It is the one
    /// packet that carries the period; UpdateAttribute sets a 1.
    ///
    /// The client counts health, armour and power up by itself from the amount and the period it
    /// was last given (ActorAttribute._EvaluatePredictedRefresh), so what is sent is what the
    /// server counts by: the amounts as the effects on the actor make them
    /// (GameEffectManager.WithRegen). Sent the attributes as they are held, a player under
    /// Regeneration Wave or a Mech armor aura was put back to the base rate by every one of
    /// these - on entering and leaving combat, on a change of armor, on an attribute point -
    /// while the server went on at the effect's.
    /// </summary>
    public class AttributeInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AttributeInfo;
        
        public Dictionary<Attributes, ActorAttributes> ActorAttributes { get; set; }
        
        public AttributeInfoPacket(Actor actor)
        {
            ActorAttributes = GameEffectManager.WithRegen(actor);
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(ActorAttributes.Count);
            foreach (var entry in ActorAttributes)
            {
                var attribute = entry.Value;
                pw.WriteInt((int)attribute.AttributeId);
                pw.WriteTuple(5);

                // normalMax, currentMax, current - in that order, and not the order the
                // docstring on Recv_AttributeInfo claims. The handler does
                // `apply(ActorAttribute, (entityId, attrType) + attrData)` and the constructor's
                // parameters are (actorId, type, normalMax, currentMax, current, ...), so the
                // first field of the tuple lands in normalMax however the comment above it reads.
                //
                // Current and normalMax were the other way round here. It was invisible for as
                // long as this only went out at moments when they were equal - login, level up,
                // a full stat reset - and stops being invisible the moment it is sent to a player
                // who is hurt, which is exactly what a combat transition is.
                pw.WriteInt(attribute.NormalMax);
                pw.WriteInt(attribute.CurrentMax);
                pw.WriteInt(attribute.Current);
                pw.WriteInt(attribute.RefreshAmount);
                pw.WriteInt(attribute.RefreshPeriod);
            }
        }
    }
}
