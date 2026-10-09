namespace Rasa.Config
{
    /// <summary>
    /// appsettings.json's ItemModules: the two numbers of the weapon modules that the client
    /// does not have (Managers.ItemModuleBonuses). A Steal module "grants the weapon a chance to
    /// steal the target's Health on hit" and a Debuff Resist module "a chance to proc a debuff to
    /// the target's resistance", and neither says how much of a chance. Every value has a
    /// default, so a file without the section has them; picked up by a config reload.
    /// </summary>
    public class ItemModulesConfig
    {
        /// <summary>Chance in percent, on each hit of the weapon, that each Steal module in it takes its amount. 0 or less: never.</summary>
        public double StealChancePercent { get; set; } = 10;

        /// <summary>Chance in percent, on each hit of the weapon, that each Debuff Resist module in it puts its debuff on the target. 0 or less: never.</summary>
        public double ResistDebuffChancePercent { get; set; } = 10;
    }
}
