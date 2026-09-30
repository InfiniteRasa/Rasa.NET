using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The rows Wire_creature_summons adds: the summoned turret's and Howler's attacks at level 42
    /// (65001-65003), and each summoner's summon, cocoon or egg (65004-65043).
    /// </summary>
    public class CreatureSummonActionPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureActionEntry.TableName, typeof(CreatureActionEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 65001, "Technician's turret weapon 1/245", 1, 245, 0.0, 40.0, 1000, 0, 320, 480, 1 };
            yield return new object[] { 65002, "Hunter's pet howler_melee_attack", 402, 1, 0.5, 3.0, 4000, 0, 166, 218, 1 };
            yield return new object[] { 65003, "Hunter's pet howler_sonic_attack", 200, 1, 1.0, 40.0, 10000, 925, 87, 262, 7 };
            yield return new object[] { 65004, "Bane Thrax Technician technician_turret", 276, 1, 0.0, 40.0, 60000, 2300, 0, 0, 1 };
            yield return new object[] { 65005, "Thrax Technician - Divide technician_turret", 276, 1, 0.0, 40.0, 60000, 2300, 0, 0, 1 };
            yield return new object[] { 65006, "Thrax Technician - Abyss technician_turret", 276, 1, 0.0, 40.0, 60000, 2300, 0, 0, 1 };
            yield return new object[] { 65007, "Thrax Technician - Ashen Desert technician_turret", 276, 1, 0.0, 40.0, 60000, 2300, 0, 0, 1 };
            yield return new object[] { 65008, "Thrax Technician - Crucible technician_turret", 276, 1, 0.0, 40.0, 60000, 2300, 0, 0, 1 };
            yield return new object[] { 65009, "Thrax Technician - Descent technician_turret", 276, 1, 0.0, 40.0, 60000, 2300, 0, 0, 1 };
            yield return new object[] { 65010, "Thrax Technician - Howling Maw technician_turret", 276, 1, 0.0, 40.0, 60000, 2300, 0, 0, 1 };
            yield return new object[] { 65011, "Thrax Technician - Incline technician_turret", 276, 1, 0.0, 40.0, 60000, 2300, 0, 0, 1 };
            yield return new object[] { 65012, "Thrax Technician - Marshes technician_turret", 276, 1, 0.0, 40.0, 60000, 2300, 0, 0, 1 };
            yield return new object[] { 65013, "Thrax Technician - Mires technician_turret", 276, 1, 0.0, 40.0, 60000, 2300, 0, 0, 1 };
            yield return new object[] { 65014, "Thrax Technician - Palisades technician_turret", 276, 1, 0.0, 40.0, 60000, 2300, 0, 0, 1 };
            yield return new object[] { 65015, "Thrax Technician - Plains technician_turret", 276, 1, 0.0, 40.0, 60000, 2300, 0, 0, 1 };
            yield return new object[] { 65016, "Thrax Technician - Plateau technician_turret", 276, 1, 0.0, 40.0, 60000, 2300, 0, 0, 1 };
            yield return new object[] { 65017, "Thrax Technician - Pools technician_turret", 276, 1, 0.0, 40.0, 60000, 2300, 0, 0, 1 };
            yield return new object[] { 65018, "Thrax Technician - Thunderhead technician_turret", 276, 1, 0.0, 40.0, 60000, 2300, 0, 0, 1 };
            yield return new object[] { 65019, "The Collector - Bootcamp technician_turret", 276, 1, 0.0, 40.0, 60000, 2300, 0, 0, 1 };
            yield return new object[] { 65020, "The Dissector - Bootcamp technician_turret", 276, 1, 0.0, 40.0, 60000, 2300, 0, 0, 1 };
            yield return new object[] { 65021, "Davinx - Palisades technician_turret", 276, 1, 0.0, 40.0, 60000, 2300, 0, 0, 1 };
            yield return new object[] { 65022, "Lawfoid - Palisades technician_turret", 276, 1, 0.0, 40.0, 60000, 2300, 0, 0, 1 };
            yield return new object[] { 65023, "Mirtanz - Palisades technician_turret", 276, 1, 0.0, 40.0, 60000, 2300, 0, 0, 1 };
            yield return new object[] { 65024, "Sinatrix - Palisades technician_turret", 276, 1, 0.0, 40.0, 60000, 2300, 0, 0, 1 };
            yield return new object[] { 65025, "Inquisitor Krakatus - Plateau technician_turret", 276, 1, 0.0, 40.0, 60000, 2300, 0, 0, 1 };
            yield return new object[] { 65026, "Bane Hunter Invasion hunter_pet", 277, 1, 0.0, 40.0, 30000, 4866, 0, 0, 1 };
            yield return new object[] { 65027, "Bane Hunter Invasion hunter_pet", 277, 1, 0.0, 40.0, 30000, 4866, 0, 0, 1 };
            yield return new object[] { 65028, "Hunter - Divide hunter_pet", 277, 1, 0.0, 40.0, 30000, 4866, 0, 0, 1 };
            yield return new object[] { 65029, "Hunter Lieutenant - Divide hunter_pet", 277, 1, 0.0, 40.0, 30000, 4866, 0, 0, 1 };
            yield return new object[] { 65030, "Hunter - Descent hunter_pet", 277, 1, 0.0, 40.0, 30000, 4866, 0, 0, 1 };
            yield return new object[] { 65031, "Hunter - Marshes hunter_pet", 277, 1, 0.0, 40.0, 30000, 4866, 0, 0, 1 };
            yield return new object[] { 65032, "Hunter Lieutenant - Marshes hunter_pet", 277, 1, 0.0, 40.0, 30000, 4866, 0, 0, 1 };
            yield return new object[] { 65033, "Hunter - Mires hunter_pet", 277, 1, 0.0, 40.0, 30000, 4866, 0, 0, 1 };
            yield return new object[] { 65034, "Hunter Lieutenant - Mires hunter_pet", 277, 1, 0.0, 40.0, 30000, 4866, 0, 0, 1 };
            yield return new object[] { 65035, "Hunter - Palisades hunter_pet", 277, 1, 0.0, 40.0, 30000, 4866, 0, 0, 1 };
            yield return new object[] { 65036, "Hunter - Pools hunter_pet", 277, 1, 0.0, 40.0, 30000, 4866, 0, 0, 1 };
            yield return new object[] { 65037, "Hunter Lieutenant - Pools hunter_pet", 277, 1, 0.0, 40.0, 30000, 4866, 0, 0, 1 };
            yield return new object[] { 65038, "Atta Grub - Ashen Desert atta_grub_cocoon", 500, 1, 0.0, 0.0, 0, 8000, 0, 0, 1 };
            yield return new object[] { 65039, "Atta Grub - Incline atta_grub_cocoon", 500, 1, 0.0, 0.0, 0, 8000, 0, 0, 1 };
            yield return new object[] { 65040, "Atta Grub - Plains atta_grub_cocoon", 500, 1, 0.0, 0.0, 0, 8000, 0, 0, 1 };
            yield return new object[] { 65041, "Atta Grub - Thunderhead atta_grub_cocoon", 500, 1, 0.0, 0.0, 0, 8000, 0, 0, 1 };
            yield return new object[] { 65042, "Stalker - Marshes stalker_ovulate", 441, 1, 0.0, 20.0, 60000, 0, 44, 87, 1 };
            yield return new object[] { 65043, "Stalker - Marshes stalker_egg_drop", 442, 1, 0.0, 0.0, 0, 3666, 1636, 1963, 5 };
        }
    }
}
