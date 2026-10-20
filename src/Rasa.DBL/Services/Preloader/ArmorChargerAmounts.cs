namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Armor Charger's Class VI and VII amounts (Armor_charger_amounts).
    ///
    /// CONSUMABLE_RESTORE_KIT_ARMOR (467) restores HEAL_AMOUNT_MIN-MAX armour a second for 10
    /// seconds. Its item text gives each class's total: 500, 750, 1200, 1850, 2850, 4400, 6800,
    /// 10500, 16200 and 25000, a tenth of which is each level's amount. The client's action data
    /// (ActionPropertyPreloader) has Class VI at 680 and Class VII at 1050, which are Class VII's
    /// and Class VIII's: Class VI restored 6800 for 4400 and Class VII the same as Class VIII.
    /// They are 440 and 680 now.
    ///
    /// Guarded by the old values, so a row changed by hand is left as it is. Plain SQL, valid on
    /// SQLite and MySQL alike.
    /// </summary>
    public static class ArmorChargerAmounts
    {
        public const uint ArmorCharger = 467;
        public const uint HealAmountMin = 17;
        public const uint HealAmountMax = 18;

        /// <summary>(level, as it was, as it is).</summary>
        public static readonly (uint Level, int Was, int Is)[] Corrections = { (6, 680, 440), (7, 1050, 680) };

        public static string[] Up => Statements(up: true);

        public static string[] Down => Statements(up: false);

        /// <summary>The id the migration first had, 20261223000000, which Random_name_quotes shares.</summary>
        public const string OldId = "20261223000000_Armor_charger_amounts";

        /// <summary>The history row a World that ran it under OldId still has.</summary>
        public static readonly string ForgetOldId =
            $"DELETE FROM __EFMigrationsHistory WHERE MigrationId = '{OldId}';";

        private static string[] Statements(bool up)
        {
            var statements = new string[Corrections.Length];

            for (var i = 0; i < Corrections.Length; i++)
            {
                var (level, was, now) = Corrections[i];
                var (from, to) = up ? (was, now) : (now, was);

                statements[i] = $"UPDATE action_property SET value = {to} WHERE action_id = {ArmorCharger} AND level = {level} " +
                                $"AND property_id IN ({HealAmountMin}, {HealAmountMax}) AND value = {from};";
            }

            return statements;
        }
    }
}
