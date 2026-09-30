using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The heals, repairs and revives Wire_creature_support gives out, one row per (creature, ability):
    /// the client's CR_* action at the argument named for the creature's rank, its windup, a reuse,
    /// and no damage - the amounts are HEAL_AMOUNT scaled to the caster's level when it lands.
    /// </summary>
    public class CreatureSupportPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureActionEntry.TableName, typeof(CreatureActionEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 62001, "Caretaker 531004 caretaker_heal", 239, 1, 0.0, 0.0, 10000, 2499, 0, 0, 1 };
            yield return new object[] { 62002, "Caretaker 531004 caretaker_revive", 242, 1, 0.0, 40.0, 30000, 2850, 0, 0, 1 };
            yield return new object[] { 62003, "Caretaker 531011 caretaker_heal", 239, 1, 0.0, 0.0, 10000, 2499, 0, 0, 1 };
            yield return new object[] { 62004, "Caretaker 531011 caretaker_revive", 242, 1, 0.0, 40.0, 30000, 2850, 0, 0, 1 };
            yield return new object[] { 62005, "Caretaker 531021 caretaker_heal", 239, 1, 0.0, 0.0, 10000, 2499, 0, 0, 1 };
            yield return new object[] { 62006, "Caretaker 531021 caretaker_revive", 242, 1, 0.0, 40.0, 30000, 2850, 0, 0, 1 };
            yield return new object[] { 62007, "Caretaker 531027 caretaker_heal", 239, 1, 0.0, 0.0, 10000, 2499, 0, 0, 1 };
            yield return new object[] { 62008, "Caretaker 531027 caretaker_revive", 242, 1, 0.0, 40.0, 30000, 2850, 0, 0, 1 };
            yield return new object[] { 62009, "Caretaker 531032 caretaker_heal", 239, 1, 0.0, 0.0, 10000, 2499, 0, 0, 1 };
            yield return new object[] { 62010, "Caretaker 531032 caretaker_revive", 242, 1, 0.0, 40.0, 30000, 2850, 0, 0, 1 };
            yield return new object[] { 62011, "Caretaker 531043 caretaker_heal", 239, 1, 0.0, 0.0, 10000, 2499, 0, 0, 1 };
            yield return new object[] { 62012, "Caretaker 531043 caretaker_revive", 242, 1, 0.0, 40.0, 30000, 2850, 0, 0, 1 };
            yield return new object[] { 62013, "Caretaker 531048 caretaker_heal", 239, 1, 0.0, 0.0, 10000, 2499, 0, 0, 1 };
            yield return new object[] { 62014, "Caretaker 531048 caretaker_revive", 242, 1, 0.0, 40.0, 30000, 2850, 0, 0, 1 };
            yield return new object[] { 62015, "Caretaker 531058 caretaker_heal", 239, 1, 0.0, 0.0, 10000, 2499, 0, 0, 1 };
            yield return new object[] { 62016, "Caretaker 531058 caretaker_revive", 242, 1, 0.0, 40.0, 30000, 2850, 0, 0, 1 };
            yield return new object[] { 62017, "Caretaker 531066 caretaker_heal", 239, 1, 0.0, 0.0, 10000, 2499, 0, 0, 1 };
            yield return new object[] { 62018, "Caretaker 531066 caretaker_revive", 242, 1, 0.0, 40.0, 30000, 2850, 0, 0, 1 };
            yield return new object[] { 62019, "Caretaker 531077 caretaker_heal", 239, 1, 0.0, 0.0, 10000, 2499, 0, 0, 1 };
            yield return new object[] { 62020, "Caretaker 531077 caretaker_revive", 242, 1, 0.0, 40.0, 30000, 2850, 0, 0, 1 };
            yield return new object[] { 62021, "Caretaker 531085 caretaker_heal", 239, 1, 0.0, 0.0, 10000, 2499, 0, 0, 1 };
            yield return new object[] { 62022, "Caretaker 531085 caretaker_revive", 242, 1, 0.0, 40.0, 30000, 2850, 0, 0, 1 };
            yield return new object[] { 62023, "Caretaker 531092 caretaker_heal", 239, 1, 0.0, 0.0, 10000, 2499, 0, 0, 1 };
            yield return new object[] { 62024, "Caretaker 531092 caretaker_revive", 242, 1, 0.0, 40.0, 30000, 2850, 0, 0, 1 };
            yield return new object[] { 62025, "Caretaker 531104 caretaker_heal", 239, 1, 0.0, 0.0, 10000, 2499, 0, 0, 1 };
            yield return new object[] { 62026, "Caretaker 531104 caretaker_revive", 242, 1, 0.0, 40.0, 30000, 2850, 0, 0, 1 };
            yield return new object[] { 62027, "Caretaker 2 caretaker_heal", 239, 1, 0.0, 0.0, 10000, 2499, 0, 0, 1 };
            yield return new object[] { 62028, "Caretaker 2 caretaker_revive", 242, 1, 0.0, 40.0, 30000, 2850, 0, 0, 1 };
            yield return new object[] { 62029, "Caretaker 530004 caretaker_heal", 239, 1, 0.0, 0.0, 10000, 2499, 0, 0, 1 };
            yield return new object[] { 62030, "Caretaker 530004 caretaker_revive", 242, 1, 0.0, 40.0, 30000, 2850, 0, 0, 1 };
            yield return new object[] { 62031, "Caretaker boss 79 caretaker_heal", 239, 3, 0.0, 0.0, 10000, 2499, 0, 0, 1 };
            yield return new object[] { 62032, "Caretaker boss 79 caretaker_revive", 242, 3, 0.0, 40.0, 30000, 2850, 0, 0, 1 };
            yield return new object[] { 62033, "Caretaker boss 520015 caretaker_heal", 239, 3, 0.0, 0.0, 10000, 2499, 0, 0, 1 };
            yield return new object[] { 62034, "Caretaker boss 520015 caretaker_revive", 242, 3, 0.0, 40.0, 30000, 2850, 0, 0, 1 };
            yield return new object[] { 62035, "Caretaker boss 520028 caretaker_heal", 239, 3, 0.0, 0.0, 10000, 2499, 0, 0, 1 };
            yield return new object[] { 62036, "Caretaker boss 520028 caretaker_revive", 242, 3, 0.0, 40.0, 30000, 2850, 0, 0, 1 };
            yield return new object[] { 62037, "Thrax Technician 531003 technician_heal", 275, 1, 0.0, 0.0, 8000, 1670, 0, 0, 1 };
            yield return new object[] { 62038, "Thrax Technician 531010 technician_heal", 275, 1, 0.0, 0.0, 8000, 1670, 0, 0, 1 };
            yield return new object[] { 62039, "Thrax Technician 531020 technician_heal", 275, 1, 0.0, 0.0, 8000, 1670, 0, 0, 1 };
            yield return new object[] { 62040, "Thrax Technician 531026 technician_heal", 275, 1, 0.0, 0.0, 8000, 1670, 0, 0, 1 };
            yield return new object[] { 62041, "Thrax Technician 531031 technician_heal", 275, 1, 0.0, 0.0, 8000, 1670, 0, 0, 1 };
            yield return new object[] { 62042, "Thrax Technician 531042 technician_heal", 275, 1, 0.0, 0.0, 8000, 1670, 0, 0, 1 };
            yield return new object[] { 62043, "Thrax Technician 531047 technician_heal", 275, 1, 0.0, 0.0, 8000, 1670, 0, 0, 1 };
            yield return new object[] { 62044, "Thrax Technician 531057 technician_heal", 275, 1, 0.0, 0.0, 8000, 1670, 0, 0, 1 };
            yield return new object[] { 62045, "Thrax Technician 531065 technician_heal", 275, 1, 0.0, 0.0, 8000, 1670, 0, 0, 1 };
            yield return new object[] { 62046, "Thrax Technician 531076 technician_heal", 275, 1, 0.0, 0.0, 8000, 1670, 0, 0, 1 };
            yield return new object[] { 62047, "Thrax Technician 531084 technician_heal", 275, 1, 0.0, 0.0, 8000, 1670, 0, 0, 1 };
            yield return new object[] { 62048, "Thrax Technician 531091 technician_heal", 275, 1, 0.0, 0.0, 8000, 1670, 0, 0, 1 };
            yield return new object[] { 62049, "Thrax Technician 531103 technician_heal", 275, 1, 0.0, 0.0, 8000, 1670, 0, 0, 1 };
            yield return new object[] { 62050, "Thrax Technician 47 technician_heal", 275, 1, 0.0, 0.0, 8000, 1670, 0, 0, 1 };
            yield return new object[] { 62051, "Thrax Technician 530003 technician_heal", 275, 1, 0.0, 0.0, 8000, 1670, 0, 0, 1 };
            yield return new object[] { 62052, "Thrax Technician boss 520009 technician_heal", 275, 2, 0.0, 0.0, 8000, 1670, 0, 0, 1 };
            yield return new object[] { 62053, "Thrax Technician boss 520010 technician_heal", 275, 2, 0.0, 0.0, 8000, 1670, 0, 0, 1 };
            yield return new object[] { 62054, "Thrax Technician boss 520037 technician_heal", 275, 2, 0.0, 0.0, 8000, 1670, 0, 0, 1 };
            yield return new object[] { 62055, "Thrax Technician boss 520041 technician_heal", 275, 2, 0.0, 0.0, 8000, 1670, 0, 0, 1 };
            yield return new object[] { 62056, "Thrax Technician boss 520042 technician_heal", 275, 2, 0.0, 0.0, 8000, 1670, 0, 0, 1 };
            yield return new object[] { 62057, "Thrax Technician boss 520045 technician_heal", 275, 2, 0.0, 0.0, 8000, 1670, 0, 0, 1 };
            yield return new object[] { 62058, "Thrax Technician boss 520052 technician_heal", 275, 2, 0.0, 0.0, 8000, 1670, 0, 0, 1 };
            yield return new object[] { 62059, "Machina 8 machina_revive", 429, 1, 0.0, 0.0, 0, 0, 0, 0, 1 };
            yield return new object[] { 62060, "Machina 520012 machina_revive", 429, 1, 0.0, 0.0, 0, 0, 0, 0, 1 };
            yield return new object[] { 62061, "Machina 520017 machina_revive", 429, 1, 0.0, 0.0, 0, 0, 0, 0, 1 };
        }
    }
}
