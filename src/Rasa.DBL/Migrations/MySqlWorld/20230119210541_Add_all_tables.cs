using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    public partial class Add_all_tables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "armorclass",
                columns: table => new
                {
                    class_id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    min_damage_absorbed = table.Column<uint>(type: "int unsigned", nullable: false),
                    max_damage_absorbed = table.Column<uint>(type: "int unsigned", nullable: false),
                    regen_rate = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_armorclass", x => x.class_id);
                });

            migrationBuilder.CreateTable(
                name: "creature",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    comment = table.Column<string>(type: "varchar(50)", nullable: false),
                    class_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    faction = table.Column<uint>(type: "int unsigned", nullable: false),
                    level = table.Column<uint>(type: "int unsigned", nullable: false),
                    max_hp = table.Column<uint>(type: "int unsigned", nullable: false),
                    name_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    run_speed = table.Column<uint>(type: "int unsigned", nullable: false),
                    walk_speed = table.Column<uint>(type: "int unsigned", nullable: false),
                    action1 = table.Column<uint>(type: "int unsigned", nullable: false),
                    action2 = table.Column<uint>(type: "int unsigned", nullable: false),
                    action3 = table.Column<uint>(type: "int unsigned", nullable: false),
                    action4 = table.Column<uint>(type: "int unsigned", nullable: false),
                    action5 = table.Column<uint>(type: "int unsigned", nullable: false),
                    action6 = table.Column<uint>(type: "int unsigned", nullable: false),
                    action7 = table.Column<uint>(type: "int unsigned", nullable: false),
                    action8 = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_creature", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "creature_action",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    description = table.Column<string>(type: "varchar(50)", nullable: false),
                    action_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    action_arg_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    range_min = table.Column<double>(type: "double", nullable: false),
                    range_max = table.Column<double>(type: "double", nullable: false),
                    cooldown = table.Column<uint>(type: "int unsigned", nullable: false),
                    windup = table.Column<uint>(type: "int unsigned", nullable: false),
                    min_damage = table.Column<uint>(type: "int unsigned", nullable: false),
                    max_damage = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_creature_action", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "creature_appearance",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false),
                    slot_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    Class_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    color = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "creature_stat",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    body = table.Column<int>(type: "int", nullable: false),
                    mind = table.Column<int>(type: "int", nullable: false),
                    spirit = table.Column<int>(type: "int", nullable: false),
                    health = table.Column<int>(type: "int", nullable: false),
                    armor = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_creature_stat", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "entityclass",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    class_name = table.Column<string>(type: "varchar(60)", nullable: false),
                    mesh_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    class_collision_role = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    target_flag = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    aug_list = table.Column<string>(type: "varchar(60)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entityclass", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "equipableclass",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    slot_id = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipableclass", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "footlocker",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    class_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    map_context_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    pos_x = table.Column<double>(type: "double", nullable: false),
                    pos_y = table.Column<double>(type: "double", nullable: false),
                    pos_z = table.Column<double>(type: "double", nullable: false),
                    rotation = table.Column<double>(type: "double", nullable: false),
                    comment = table.Column<string>(type: "varchar(64)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_footlocker", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "itemclass",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    inventory_icon_string_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    loot_value = table.Column<uint>(type: "int unsigned", nullable: false),
                    hidden_invenotry_flag = table.Column<ulong>(type: "bit", nullable: false),
                    is_consumable_flag = table.Column<ulong>(type: "bit", nullable: false),
                    max_hp = table.Column<int>(type: "int", nullable: false),
                    stack_size = table.Column<uint>(type: "int unsigned", nullable: false),
                    drag_audio_set_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    drop_audio_set_id = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itemclass", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "itemtemplate",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    quality_id = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    has_sellable_flag = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    not_tradable_flag = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    has_character_unique_flag = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    has_account_unique_flag = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    has_boe_flag = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    bound_to_character_flag = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    not_placable_in_lockbox_flag = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    inventory_category = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    buy_price = table.Column<int>(type: "int", nullable: false),
                    sell_price = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itemtemplate", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "itemtemplate_armor",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    armor_value = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itemtemplate_armor", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "itemtemplate_requirement",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    req_type = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    req_value = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itemtemplate_requirement", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "itemtemplate_requirement_race",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    race_id = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itemtemplate_requirement_race", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "itemtemplate_requirement_skill",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    skill_id = table.Column<int>(type: "int", nullable: false),
                    skill_level = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itemtemplate_requirement_skill", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "itemtemplate_resistance",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false),
                    resistance_type = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    resistance_value = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "itemtemplate_weapon",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    aim_rate = table.Column<double>(type: "double", nullable: false),
                    reload_time = table.Column<uint>(type: "int unsigned", nullable: false),
                    alt_action_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    alt_action_arg_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    ae_type = table.Column<uint>(type: "int unsigned", nullable: false),
                    ae_radius = table.Column<uint>(type: "int unsigned", nullable: false),
                    recoil_amount = table.Column<uint>(type: "int unsigned", nullable: false),
                    reuse_override = table.Column<uint>(type: "int unsigned", nullable: false),
                    cool_rate = table.Column<uint>(type: "int unsigned", nullable: false),
                    heat_per_shot = table.Column<double>(type: "double", nullable: false),
                    tool_type = table.Column<uint>(type: "int unsigned", nullable: false),
                    ammo_per_shot = table.Column<uint>(type: "int unsigned", nullable: false),
                    windup = table.Column<uint>(type: "int unsigned", nullable: false),
                    recovery = table.Column<uint>(type: "int unsigned", nullable: false),
                    refire = table.Column<uint>(type: "int unsigned", nullable: false),
                    range = table.Column<uint>(type: "int unsigned", nullable: false),
                    alt_max_damage = table.Column<uint>(type: "int unsigned", nullable: false),
                    alt_damage_type = table.Column<uint>(type: "int unsigned", nullable: false),
                    alt_range = table.Column<uint>(type: "int unsigned", nullable: false),
                    alt_ae_radius = table.Column<uint>(type: "int unsigned", nullable: false),
                    alt_ae_type = table.Column<uint>(type: "int unsigned", nullable: false),
                    attack_type = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itemtemplate_weapon", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "logos",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    class_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    map_context_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    pos_x = table.Column<double>(type: "double", nullable: false),
                    pos_y = table.Column<double>(type: "double", nullable: false),
                    pos_z = table.Column<double>(type: "double", nullable: false),
                    name = table.Column<string>(type: "varchar(64)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_logos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "map_info",
                columns: table => new
                {
                    map_context_id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    map_name = table.Column<string>(type: "varchar(50)", nullable: false),
                    map_version = table.Column<uint>(type: "int unsigned", nullable: false),
                    base_region = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_map_info", x => x.map_context_id);
                });

            migrationBuilder.CreateTable(
                name: "npc_mission",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false),
                    command = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    var1 = table.Column<uint>(type: "int unsigned", nullable: false),
                    var2 = table.Column<uint>(type: "int unsigned", nullable: false),
                    var3 = table.Column<uint>(type: "int unsigned", nullable: false),
                    comment = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "npc_package",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    package_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    comment = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_package", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "spawnpool",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    mode = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    anim_type = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    respown_time = table.Column<uint>(type: "int unsigned", nullable: false),
                    pos_x = table.Column<double>(type: "double", nullable: false),
                    pos_y = table.Column<double>(type: "double", nullable: false),
                    pos_z = table.Column<double>(type: "double", nullable: false),
                    rotation = table.Column<double>(type: "double", nullable: false),
                    map_context_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    creature_1_Id = table.Column<uint>(type: "int unsigned", nullable: false),
                    creature_1_min_count = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    creature_1_max_count = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    creature_2_Id = table.Column<uint>(type: "int unsigned", nullable: false),
                    creature_2_min_count = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    creature_2_max_count = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    creature_3_Id = table.Column<uint>(type: "int unsigned", nullable: false),
                    creature_3_min_count = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    creature_3_max_count = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    creature_4_Id = table.Column<uint>(type: "int unsigned", nullable: false),
                    creature_4_min_count = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    creature_4_max_count = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    creature_5_Id = table.Column<uint>(type: "int unsigned", nullable: false),
                    creature_5_min_count = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    creature_5_max_count = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    creature_6_Id = table.Column<uint>(type: "int unsigned", nullable: false),
                    creature_6_min_count = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    creature_6_max_count = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spawnpool", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "teleporter",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    class_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    type = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    description = table.Column<string>(type: "varchar(64)", nullable: false),
                    pos_x = table.Column<double>(type: "double", nullable: false),
                    pos_y = table.Column<double>(type: "double", nullable: false),
                    pos_z = table.Column<double>(type: "double", nullable: false),
                    rotation = table.Column<double>(type: "double", nullable: false),
                    map_context_id = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teleporter", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vendor",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    package_id = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vendor_item",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false),
                    item_template_id = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "weaponclass",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    weapon_template_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    attack_action_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    attack_action_arg_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    draw_action_id = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    stow_action_id = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    reload_action_id = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    ammo_class_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    clip_size = table.Column<uint>(type: "int unsigned", nullable: false),
                    min_damage = table.Column<int>(type: "int", nullable: false),
                    max_damage = table.Column<int>(type: "int", nullable: false),
                    damage_type = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    velocity = table.Column<int>(type: "int", nullable: false),
                    weapon_anim_condition_code = table.Column<uint>(type: "int unsigned", nullable: false),
                    windup_override = table.Column<ulong>(type: "bit", nullable: false),
                    recovery_override = table.Column<ulong>(type: "bit", nullable: false),
                    reuse_override = table.Column<ulong>(type: "bit", nullable: false),
                    reload_override = table.Column<ulong>(type: "bit", nullable: false),
                    range_type = table.Column<ulong>(type: "bit", nullable: false),
                    unk_arg1 = table.Column<ulong>(type: "bit", nullable: false),
                    unk_arg2 = table.Column<ulong>(type: "bit", nullable: false),
                    unk_arg3 = table.Column<ulong>(type: "bit", nullable: false),
                    unk_arg4 = table.Column<ulong>(type: "bit", nullable: false),
                    unk_arg5 = table.Column<ulong>(type: "bit", nullable: false),
                    unk_arg6 = table.Column<uint>(type: "int unsigned", nullable: false),
                    unk_arg7 = table.Column<ulong>(type: "bit", nullable: false),
                    unk_arg8 = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weaponclass", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "armorclass");

            migrationBuilder.DropTable(
                name: "creature");

            migrationBuilder.DropTable(
                name: "creature_action");

            migrationBuilder.DropTable(
                name: "creature_appearance");

            migrationBuilder.DropTable(
                name: "creature_stat");

            migrationBuilder.DropTable(
                name: "entityclass");

            migrationBuilder.DropTable(
                name: "equipableclass");

            migrationBuilder.DropTable(
                name: "footlocker");

            migrationBuilder.DropTable(
                name: "itemclass");

            migrationBuilder.DropTable(
                name: "itemtemplate");

            migrationBuilder.DropTable(
                name: "itemtemplate_armor");

            migrationBuilder.DropTable(
                name: "itemtemplate_requirement");

            migrationBuilder.DropTable(
                name: "itemtemplate_requirement_race");

            migrationBuilder.DropTable(
                name: "itemtemplate_requirement_skill");

            migrationBuilder.DropTable(
                name: "itemtemplate_resistance");

            migrationBuilder.DropTable(
                name: "itemtemplate_weapon");

            migrationBuilder.DropTable(
                name: "logos");

            migrationBuilder.DropTable(
                name: "map_info");

            migrationBuilder.DropTable(
                name: "npc_mission");

            migrationBuilder.DropTable(
                name: "npc_package");

            migrationBuilder.DropTable(
                name: "spawnpool");

            migrationBuilder.DropTable(
                name: "teleporter");

            migrationBuilder.DropTable(
                name: "vendor");

            migrationBuilder.DropTable(
                name: "vendor_item");

            migrationBuilder.DropTable(
                name: "weaponclass");
        }
    }
}
