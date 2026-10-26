namespace Rasa.Services.Preloader
{
    /// <summary>The assault Hunter was introduced after Hunter_shields and missed its weapon row.</summary>
    public static class LandingZoneHunterWeapon
    {
        public const string Up = "INSERT INTO creature_appearance (id,slot_id,Class_id,color) " +
            "SELECT c.id,13,10171,1 FROM creature c WHERE c.id=620004 AND c.class_id=10166 " +
            "AND NOT EXISTS (SELECT 1 FROM creature_appearance a WHERE a.id=c.id AND a.slot_id=13);";
        public const string Down = "DELETE FROM creature_appearance WHERE id=620004 AND slot_id=13 AND Class_id=10171 AND color=1 " +
            "AND EXISTS (SELECT 1 FROM creature c WHERE c.id=620004 AND c.class_id=10166 AND c.comment='Landing Zone Hunter');";
    }
}
