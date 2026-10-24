namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Logos stone "Those" (logos 321, Minos Caverns) lifted out of the ground (Logos_those_height). Its row, from the
    /// retail capture, has it at -34.49, 2.0 m BELOW the floor of the pedestal it belongs to: the retail map's dispenser
    /// base (ArchElohLogosDispenserBase, class 25593) stands at -33.23, the client's ground under it is at -32.51, and so
    /// nothing of the stone shows above the floor and it cannot be clicked. Every other stone of the world hovers about
    /// 2.0 m over the floor of its pedestal (169 of the 171 stand within 1.04 m of a pedestal; the median height over the
    /// floor is 2.01 m). Its height becomes the floor's plus 2.0 m, -30.51. Its place on the map (x, z) is the base's
    /// already (0.1 m off), and is not changed.
    ///
    /// One column of one row, guarded by the old value: a row that is no longer as the capture had it is left alone.
    /// Down puts the old height back under the same guard. Plain SQL, valid on SQLite and MySQL alike.
    /// </summary>
    public static class LogosThoseHeightSeed
    {
        public const uint LogosId = 321;
        public const uint MapContextId = 1347;
        public const double OldHeight = -34.48828;
        public const double NewHeight = -30.51;

        public static readonly string[] UpStatements =
        {
            "update logos set pos_y = -30.51 where id = 321 and map_context_id = 1347 and abs(pos_y - (-34.48828)) < 0.01;"
        };

        public static readonly string[] DownStatements =
        {
            "update logos set pos_y = -34.48828 where id = 321 and map_context_id = 1347 and abs(pos_y - (-30.51)) < 0.01;"
        };
    }
}
