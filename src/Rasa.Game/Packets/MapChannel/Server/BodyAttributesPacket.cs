namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Rasa.Structures;

    /// <summary>
    /// Recv_BodyAttributes(scale, hue, ignoreABVs, ignoreWS, hue2) (client/physicalentity.py): the
    /// size of an entity's body, its two tints, and two switches of the engine's.
    ///
    ///  - scale: body.SetScale, and the only thing in the client that sizes an entity the
    ///    server made. A character's height is this (CHARACTER_CREATION_MIN_HEIGHT 0.9 to
    ///    _MAX_HEIGHT 1.06): the selection screen sizes its mannequin from CharacterInfo's
    ///    BodyData, and in the world nothing does unless this is sent. The client also plays
    ///    movement animation at 1 / scale of its speed (CharacterAnimationMgr.GetMovementAnimScale).
    ///  - hue and hue2: body.SetHue2 on the mesh, for anything that is no piece of equipment and
    ///    has a mesh of its own. None for no tint: the client tints only when hue is not None,
    ///    and then reads hue2 without looking, so they are sent both or neither. A player's
    ///    body is the bare avatar mesh and is sent neither.
    ///  - ignoreABVs and ignoreWS: body.SetIgnoreABVs and body.SetIgnoreWS, in the engine. An
    ///    ABV is a collision volume (a door adds and removes its own from the collision
    ///    manager); WS is walkable surfaces. What each does to a body is not in the client's
    ///    Python. Creatures have always been sent 1 and 1 (<see cref="Ignore"/>): they are
    ///    where the server puts them. A player is sent 0 and 0 (<see cref="Collide"/>): a
    ///    player sent nothing collides and walks on surfaces, so that is taken to be what 0 is.
    /// </summary>
    public class BodyAttributesPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.BodyAttributes;

        /// <summary>For ignoreABVs and ignoreWS: the body ignores them, as every creature's has been told to.</summary>
        public const int Ignore = 1;

        /// <summary>For ignoreABVs and ignoreWS: the body does not ignore them.</summary>
        public const int Collide = 0;

        public double Scale { get; set; }

        /// <summary>The first tint, or null for none.</summary>
        public Color Hue { get; set; }
        public int IgnoreABVs { get; set; }
        public int IgnoreWS { get; set; }      // WS is WalkableSurfaces

        /// <summary>The second tint. Sent only with the first.</summary>
        public Color Hue2 { get; set; }

        public BodyAttributesPacket(double scale, Color hue, int ignoreABVs, int ignoreWS, Color hue2)
        {
            Scale = scale;
            Hue = hue;
            IgnoreABVs = ignoreABVs;
            IgnoreWS = ignoreWS;
            Hue2 = hue2;
        }

        /// <summary>
        /// A player's: their height, no tint, and a body that collides. A height that is no
        /// height - a row from before it was kept, a fixture's - is the default, 1.0.
        /// </summary>
        public static BodyAttributesPacket ForPlayer(double height)
        {
            return new BodyAttributesPacket(height > 0 && !double.IsNaN(height) && !double.IsInfinity(height) ? height : 1.0, null, Collide, Collide, null);
        }

        public override void Write(PythonWriter pw)
        {
            var tinted = Hue != null && Hue2 != null;

            pw.WriteTuple(5);
            pw.WriteDouble(Scale);

            if (tinted)
                pw.WriteStruct(Hue);
            else
                pw.WriteNoneStruct();

            pw.WriteInt(IgnoreABVs);
            pw.WriteInt(IgnoreWS);

            if (tinted)
                pw.WriteStruct(Hue2);
            else
                pw.WriteNoneStruct();
        }
    }
}
