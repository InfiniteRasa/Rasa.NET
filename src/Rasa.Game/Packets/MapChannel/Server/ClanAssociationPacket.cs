namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// ClanAssociation (782): ClanControlPoint.Recv_ClanAssociation(owningClanId), on a usable
    /// object of the CLANCONTROLPOINT kind - "This CCP has changed who owns it"
    /// (client/augmentations/clancontrolpoint.py).
    ///
    /// The owner picks the object's effect, not its state: OnGetStatePkgId answers "your clan
    /// owns" when the id is the clan of the client's own character, "other clan owns" for any
    /// other id above 0, and the AFS's or the Bane's for shared/gameconstants.py's
    /// VIRTUAL_CLAN_AFS (-1) and VIRTUAL_CLAN_BANE (-2). The method only keeps the id - it does
    /// not put the effect on again - so it is sent with the object's entity, before UsableInfo,
    /// whose state then takes the effect of the owner it has been told
    /// (DynamicObjectManager.CreateDynamicObjectOnClient). An owner that changes while the
    /// object is on the client goes with Use, which keeps the id and makes the state over
    /// (<see cref="UsePacket"/>).
    /// </summary>
    public class ClanAssociationPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ClanAssociation;

        public int OwningClanId { get; }

        public ClanAssociationPacket(int owningClanId)
        {
            OwningClanId = owningClanId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(OwningClanId);
        }
    }
}
