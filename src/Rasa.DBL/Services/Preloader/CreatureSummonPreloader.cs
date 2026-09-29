using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// What the Technicians and Hunters summon (CreatureSummons), at level 42, scaled to the
    /// summoner's when they come. No spawn pool has them. No name id: the client names them from
    /// the class, "Mini Turret" and "Howler". The turret has no speed - it is an emplacement.
    /// </summary>
    public class CreatureSummonPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureEntry.TableName, typeof(CreatureEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 570001, "Mini Turret (Technician's)", 20359, 0, 42, 3750, 0, 0, 0, 65001, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 570002, "Howler (Hunter's pet)", 7336, 0, 42, 3750, 0, 9, 5, 65002, 65003, 0, 0, 0, 0, 0, 0 };
        }
    }
}
