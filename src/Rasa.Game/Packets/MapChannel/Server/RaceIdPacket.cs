namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// A player's race: client/augmentations/manifestation.py Recv_RaceId(raceId), on the player's
    /// entity, stored for GetRaceId(). generated.client.constant.race numbers them as
    /// <see cref="Race"/> does: HUMAN 1, FOREAN_HYBRID 2, BRANN_HYBRID 3, THRAX_HYBRID 4.
    ///
    /// The client reads it in two places: Item.CanActorUse, which fails an item whose template has
    /// a race requirement (itemclass.itemTemplateRaceRequirement) the actor's race does not match -
    /// the red tint in the inventory, vendor and auction windows, and the equip and ability-item
    /// checks - and the tooltip's race requirement line, red when the race is not among those
    /// listed. The 70 race-locked templates are all appearance pieces: 61 human faces and hair
    /// styles and 9 hybrid heads, 3 each for Brann, Forean and Thrax. Without this the race is None
    /// and every one of them fails.
    /// </summary>
    public class RaceIdPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RaceId;

        public Race RaceId { get; set; }

        public RaceIdPacket(Race raceId)
        {
            RaceId = raceId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt((int)RaceId);
        }
    }
}
