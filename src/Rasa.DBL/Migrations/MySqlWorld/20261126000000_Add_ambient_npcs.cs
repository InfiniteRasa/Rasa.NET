using System.Collections.Generic;

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// ambient_npc: the client's ambient figures placed on maps (AmbientNpcEntry) - a model with
    /// its own animations, no name and no target, which is not a creature. They were server
    /// world data, like the FX emitters of map_emitter.
    ///
    /// And the second lot of people for the Proving Grounds' refugee base
    /// (BootcampGarrisonNpcPreloaders): six more Infantrymen holding their rifles, two soldiers
    /// firing at the range, two men seated behind it, one standing by, and an
    /// officer reading a tablet on the command deck. Captain Delessio moves 1.3 m, to where the
    /// GM stood for him, and faces the way the GM faced.
    ///
    /// Down drops the table, deletes the six pools and their poses, and puts the Captain back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Add_ambient_npcs : Migration
    {
        private readonly ICollection<IPreloader> _preloaders = new List<IPreloader>
        {
            new BootcampGarrisonSpawnpoolPreloader(),
            new BootcampGarrisonPosePreloader(),
            new BootcampAmbientNpcPreloader()
        };

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: AmbientNpcEntry.TableName,
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    map_context_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    class_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    pos_x = table.Column<double>(type: "double", nullable: false),
                    pos_y = table.Column<double>(type: "double", nullable: false),
                    pos_z = table.Column<double>(type: "double", nullable: false),
                    rotation = table.Column<double>(type: "double", nullable: false),
                    comment = table.Column<string>(type: "varchar(96)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ambient_npc", x => x.id);
                });

            foreach (var preloader in _preloaders)
            {
                preloader.Preload(migrationBuilder);
            }

            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set {BootcampGarrisonNpcs.DelessioIs} where id = {BootcampGarrisonNpcs.DelessioPoolId};");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set {BootcampGarrisonNpcs.DelessioWas} where id = {BootcampGarrisonNpcs.DelessioPoolId};");

            migrationBuilder.DropTable(name: AmbientNpcEntry.TableName);

            foreach (var table in new[] { SpawnPoolPoseEntry.TableName, SpawnPoolEntry.TableName })
            {
                migrationBuilder.Sql($"delete from {table} where id between {BootcampGarrisonNpcs.FirstPoolId} and {BootcampGarrisonNpcs.LastPoolId};");
            }
        }
    }
}
