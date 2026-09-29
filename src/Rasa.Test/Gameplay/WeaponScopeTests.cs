using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Memory;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Missions;

namespace Rasa.Test.Gameplay
{
    [TestClass]
    public class WeaponScopeTests
    {
        [TestMethod]
        [DataRow(8932U, WeaponScopes.TorqueShellRifleScope)]      // Weapon_Avatar_Torqueshell_Rifle_Physical_CMN_33_to_37
        [DataRow(30707U, WeaponScopes.TorqueShellRifleScope)]     // Weapon_Avatar_Sunset_Torqueshell_Rifle_Laser_Line
        [DataRow(30233U, WeaponScopes.RocketLauncherSeries3Scope)] // Weapon_Avatar_RocketLauncher_v3_Physical_CMN_44_to_46
        [DataRow(29845U, WeaponScopes.RocketLauncherSeries3Scope)] // ZZZ_Delete_Me0, a "Series 3 Rocket Launcher" still carried by an item template
        [DataRow(30468U, WeaponScopes.None)]                      // Test_Weapon_Avatar_Torqueshell_Pistol_V2_Fire: a pistol
        [DataRow(6073U, WeaponScopes.None)]                       // Weapon_Avatar_Rifle_Physical_CMN_50_to_50
        public void ScopedWeaponsGetTheirProfile(uint weaponClassId, int profile)
        {
            Assert.AreEqual(profile, WeaponScopes.ProfileOf(weaponClassId));
        }

        [TestMethod]
        public void TheTablesAreTheClientsTorqueshellRiflesAndSeries3Launchers()
        {
            Assert.HasCount(123, WeaponScopes.TorqueshellRifles.ToArray());
            Assert.HasCount(47, WeaponScopes.Series3RocketLaunchers.ToArray());
            Assert.IsFalse(WeaponScopes.TorqueshellRifles.Intersect(WeaponScopes.Series3RocketLaunchers).Any());
        }

        [TestMethod]
        [DataRow(8932U, 1)]
        [DataRow(30233U, 2)]
        [DataRow(6073U, 0)]
        public void WeaponInfoCarriesTheProfileLast(uint weaponClassId, int profile)
        {
            var weaponClass = new EntityClass(weaponClassId, "fixture", 0, 1, new() { AugmentationType.Weapon }, true)
            {
                WeaponClassInfo = new WeaponClassInfo(new WeaponClassEntry { Id = weaponClassId, ClipSize = 5, WeaponAnimConditionCode = 1 })
            };
            var template = new ItemTemplate(new ItemTemplateItemClassEntry { ItemTemplateId = 1, ItemClass = weaponClassId })
            {
                WeaponInfo = new WeaponInfo(new ItemTemplateWeaponEntry { Id = 1, AmmoPerShot = 1, ToolType = 15 })
            };
            var item = new Item { ItemTemplate = template, CurrentAmmo = 5 };

            using var reader = new PythonReader(new BinaryReader(new MemoryStream(MissionTestContext.Encode(new WeaponInfoPacket(item, weaponClass)))));
            Assert.AreEqual(17, reader.ReadTuple());
            for (var i = 0; i < 16; i++)
                reader.SkipValue();
            Assert.AreEqual(profile, reader.ReadInt());
        }
    }
}
