namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The items made in the old default colour, and the appearance rows that show them, given
    /// the new one: 2139062144 (0x7F7F7F80, a half-alpha mid-grey) to 4294967295 (0xFFFFFFFF,
    /// white), the colour the client dresses an unpainted piece in (the game server's
    /// Color.ItemDefault says where). Every item the server made was made in it - the starting
    /// kit and the recruit armour, loot, rewards, purchases, crafted items - and a paint's colours are
    /// never it, so the rows in it are the unpainted ones. It also keeps old and new stacks of
    /// one thing joining, as they have to be of one colour (InventoryManager.Missions MayJoin).
    ///
    /// Down puts every white row back to the grey, those made since among them.
    /// </summary>
    public static class ItemDefaultColor
    {
        public const uint Old = 2139062144;
        public const uint New = 4294967295;

        public static readonly string[] CharUp = Statements(Old, New);

        public static readonly string[] CharDown = Statements(New, Old);

        private static string[] Statements(uint from, uint to) => new[]
        {
            $"update items set color = {to} where color = {from};",
            $"update character_appearance set color = {to} where color = {from};"
        };
    }
}
