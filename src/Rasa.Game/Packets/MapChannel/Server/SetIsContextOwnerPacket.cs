namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// SetIsContextOwner (365), a client method (sent on SysEntity.ClientMethodId): whether the
    /// player owns the context - the map instance - they are in. Wired so that it can be sent, and
    /// deliberately sent by nothing: the 1.16.5 client never uses it, and this server has no use
    /// for it either.
    ///
    /// client/clientmethod.py Recv_SetIsContextOwner(isOwner) finds the player's manifestation and
    /// calls SetIsContextOwner(isOwner), which stores it (default 0); IsContextOwner() answers
    /// whether it is 1. Nothing calls IsContextOwner(): in the decompiled source the name appears
    /// only in its definition, and in the shipped trpython.zip only clientmethod.pyo and
    /// manifestation.pyo carry it - the sender and the store. tabula_rasa.exe does not contain
    /// "ContextOwner" at all. Receiving it changes a stored value and nothing else.
    ///
    /// It presumably marked the owner of a private instance, for whatever that owner could do with
    /// it; that code is not in this build. This server's maps are shared and permanent - one map
    /// channel per context, loaded at startup, owned by no one - and the C++ server sent
    /// SetCurrentContextId on map entry and never this.
    ///
    /// isOwner is written as an int: the client compares it with 1.
    /// </summary>
    public class SetIsContextOwnerPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetIsContextOwner;

        public bool IsOwner { get; set; }

        public SetIsContextOwnerPacket(bool isOwner)
        {
            IsOwner = isOwner;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(IsOwner ? 1 : 0);
        }
    }
}
