namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The attacks of the Proving Grounds' soldiers with pistols and machine guns
    /// (Fix_bootcamp_soldier_weapon_attacks): each the action and argument its weapon class attacks
    /// with in the client (weaponclass), so the client plays the shot.
    /// </summary>
    public static class BootcampSoldierWeapons
    {
        /// <summary>Capture the Flag's escorts and the sandbag post's Infantrymen: Weapon_Human_Redshirt_Pistol_Physical (6271), WEAPON_ATTACK 133.</summary>
        public const uint Pistol = 510218;
        public const uint PistolWeapon = 6271;
        public const uint PistolAction = 1;
        public const uint PistolArg = 133;

        /// <summary>The Field Gunner: Weapon_Human_Redshirt_MachineGun_Physical (20535), WEAPON_MACHINEGUN 1.</summary>
        public const uint MachineGun = 510219;
        public const uint MachineGunWeapon = 20535;
        public const uint MachineGunAction = 149;
        public const uint MachineGunArg = 1;
    }
}
