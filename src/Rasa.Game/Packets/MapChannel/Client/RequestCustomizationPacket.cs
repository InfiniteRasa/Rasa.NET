using System;
using System.IO;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;
    using Structures;

    /// <summary>
    /// RequestCustomization(actionArgId, customizationEntityId, selectedClassTemplateId,
    /// targetEntityId, hue): a customization item used, sent by CustomizeAction.SendServerRequest
    /// (client/actions/customize.py) for action CUSTOMIZE (82).
    ///
    /// The last three are each given or None, by what the item changes (augmentations/customization.py):
    ///
    ///  - CUSTOMIZE_CHANGE_HAIRSTYLE (5), CUSTOMIZE_CHANGE_FACE (6): the item template picked;
    ///  - CUSTOMIZE_HUE_HAIR (1), CUSTOMIZE_HUE_SKIN (2): the hue;
    ///  - CUSTOMIZE_HUE_CLOTHING (4), CUSTOMIZE_HUE_WEAPON (3), CUSTOMIZE_HUE_DECORATION (10): the
    ///    item to colour and the hue.
    ///
    /// The hue is the swatch widget's colour, (red, green, blue, alpha) out of 255. The reader
    /// took the third for an int and the last two for None always, so an armour paint - None,
    /// an entity id and a hue - threw on the None and closed the connection.
    /// </summary>
    public class RequestCustomizationPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestCustomization;

        public int CustomizationActionArgId { get; set; }
        public ulong CustomizationEntityId { get; set; }

        /// <summary>The item template picked, or null.</summary>
        public int? SelectedClassTemplateId { get; set; }

        /// <summary>The item to colour, or 0 for none.</summary>
        public ulong TargetEntityId { get; set; }

        /// <summary>The colour picked, or null. Null too for one that is not four values of 0 to 255.</summary>
        public Color Hue { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            CustomizationActionArgId = (int)ReadWhole(pr);
            CustomizationEntityId = (ulong)ReadWhole(pr);
            SelectedClassTemplateId = (int?)ReadWholeOrNone(pr);
            TargetEntityId = (ulong)(ReadWholeOrNone(pr) ?? 0);
            Hue = ReadHue(pr);
        }

        /// <summary>An int or a long: Python marshals a small id as either.</summary>
        private static long ReadWhole(PythonReader pr)
        {
            return pr.PeekType() switch
            {
                PythonType.Int => pr.ReadInt(),
                PythonType.Long => pr.ReadLong(),
                var other => throw new InvalidDataException($"Expected an int or a long. Got: {other}")
            };
        }

        private static long? ReadWholeOrNone(PythonReader pr)
        {
            if (pr.PeekType() != PythonType.Structs)
                return ReadWhole(pr);

            pr.ReadUnkStruct();
            return null;
        }

        private static Color ReadHue(PythonReader pr)
        {
            if (pr.PeekType() != PythonType.Tuple)
            {
                pr.SkipValue();
                return null;
            }

            var count = pr.ReadTuple();
            var channels = new double[count];

            for (var i = 0; i < count; i++)
                channels[i] = pr.ReadNumber();

            if (count != 4)
                return null;

            foreach (var channel in channels)
                if (!(channel >= 0 && channel <= 255))
                    return null;

            return new Color((byte)Math.Round(channels[0]), (byte)Math.Round(channels[1]),
                (byte)Math.Round(channels[2]), (byte)Math.Round(channels[3]));
        }
    }
}
