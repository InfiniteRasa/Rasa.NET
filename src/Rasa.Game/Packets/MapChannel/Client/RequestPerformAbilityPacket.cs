using System.IO;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// Firing an armed ability. <c>actions/abilities/baseactorability.py</c> sends
    /// <c>(actionId, actionArgId, target, itemId)</c>, and a fifth element - the actor's yaw -
    /// when the ability is a cone:
    ///
    /// <code>
    /// serverArgs = (self.actionId, self.actionArgId, target, self.itemId)
    /// if self.useClientYaw:
    ///     serverArgs += (actor.body.GetYaw(),)
    /// </code>
    ///
    /// Both variable parts were read as if they were fixed, and either one closes the connection.
    ///
    /// The target slot carries three different things, as it does for weapons and tools: an
    /// entity id, None, or a position. An ability with RADIUS_AROUND_SOURCE or CONE_RADIUS in its
    /// properties sets <c>targetType = TARGET_NONE</c> and sends None, and the ten
    /// location-targeted ability modules - turret, trap, airstrike, fire support, reality ripper
    /// and the thrown consumables - send a position. Reading the slot as a long threw on both.
    ///
    /// And the fifth element has to be consumed even though nothing uses it yet, because whatever
    /// the reader stops on is what the framing check reads as the terminator.
    ///
    /// Together that was 216 of the 1084 ability (action, arg) pairs the client knows, including
    /// every class's signature wave and staples like the Engineer's turret and trap, the Ranger's
    /// fire support, Soldier's rage and shrapnel, and the thrown consumables.
    /// </summary>
    public class RequestPerformAbilityPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestPerformAbility;

        public ActionId ActionId { get; set; }
        public int ActionArgId { get; set; }
        public ActionTarget Target { get; set; }
        public ulong ItemId { get; set; }

        /// <summary>
        /// Which way the caster was facing, for a cone ability. Informational: the server holds
        /// its own rotation for the actor, and a client is free to claim any angle.
        /// </summary>
        public double Yaw { get; set; }

        /// <summary>False when the client sent the four-element form, which is most of them.</summary>
        public bool HasYaw { get; set; }

        public override void Read(PythonReader pr)
        {
            var count = pr.ReadTuple();
            if (count is not (4 or 5))
                throw new InvalidDataException("Expected four or five ability request arguments.");

            ActionId = (ActionId)pr.ReadInt();
            ActionArgId = pr.ReadInt();
            Target = ActionTarget.Read(pr);

            switch (pr.PeekType())
            {
                case PythonType.Long:
                    ItemId = pr.ReadULong();
                    break;
                case PythonType.Int:
                    ItemId = pr.ReadUInt();
                    break;
                default:
                    pr.ReadUnkStruct();
                    ItemId = 0;
                    break;
            }

            HasYaw = count == 5;
            Yaw = 0;
            if (!HasYaw)
                return;

            Yaw = pr.ReadNumber();
        }
    }
}
