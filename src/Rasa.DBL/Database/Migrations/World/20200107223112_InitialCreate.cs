using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Database.Migrations.World
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "armorClass",
                columns: table => new
                {
                    classId = table.Column<int>(type: "int(11)", nullable: false),
                    minDamageAbsorbed = table.Column<int>(type: "int(11)", nullable: false),
                    maxDamageAbsorbed = table.Column<int>(type: "int(11)", nullable: false),
                    regenRate = table.Column<int>(type: "int(11)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "creature_action",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(10) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    description = table.Column<string>(type: "varchar(50)", nullable: true, defaultValueSql: "NULL"),
                    action_id = table.Column<int>(type: "int(10)", nullable: false),
                    action_arg_id = table.Column<int>(type: "int(10)", nullable: false),
                    range_min = table.Column<float>(nullable: false),
                    range_max = table.Column<float>(nullable: false),
                    cooldown = table.Column<int>(type: "int(10)", nullable: false),
                    windup_time = table.Column<int>(type: "int(10)", nullable: false),
                    min_damage = table.Column<int>(type: "int(10)", nullable: false),
                    max_damage = table.Column<int>(type: "int(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_creature_action", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "creature_appearance",
                columns: table => new
                {
                    creature_id = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    slot_id = table.Column<sbyte>(type: "tinyint(2)", nullable: false),
                    class_id = table.Column<uint>(type: "int(11) unsigned", nullable: false, defaultValueSql: "0"),
                    color = table.Column<uint>(type: "int(11) unsigned", nullable: false, defaultValueSql: "0")
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "creature_stats",
                columns: table => new
                {
                    creatureDbId = table.Column<uint>(type: "int(10) unsigned", nullable: false, comment: "dbId from cratures table"),
                    body = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    mind = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    spirit = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    health = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    armor = table.Column<uint>(type: "int(10) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.creatureDbId);
                });

            migrationBuilder.CreateTable(
                name: "creatures",
                columns: table => new
                {
                    dbId = table.Column<uint>(type: "int(10) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    comment = table.Column<string>(type: "varchar(50)", nullable: true, defaultValueSql: "''"),
                    classId = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    faction = table.Column<int>(type: "int(10)", nullable: false, defaultValueSql: "0"),
                    level = table.Column<int>(type: "int(10)", nullable: false, defaultValueSql: "0"),
                    maxHitPoints = table.Column<int>(type: "int(10)", nullable: false, defaultValueSql: "0"),
                    nameId = table.Column<int>(type: "int(10)", nullable: false, defaultValueSql: "0"),
                    run_speed = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValueSql: "0"),
                    walk_speed = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValueSql: "0"),
                    action1 = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValueSql: "0"),
                    action2 = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValueSql: "0"),
                    action3 = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValueSql: "0"),
                    action4 = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValueSql: "0"),
                    action5 = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValueSql: "0"),
                    action6 = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValueSql: "0"),
                    action7 = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValueSql: "0"),
                    action8 = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValueSql: "0")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.dbId);
                });

            migrationBuilder.CreateTable(
                name: "entityClass",
                columns: table => new
                {
                    classId = table.Column<int>(type: "int(11)", nullable: false),
                    className = table.Column<string>(type: "char(58)", nullable: false),
                    meshId = table.Column<int>(type: "int(11)", nullable: false),
                    classCollisionRole = table.Column<sbyte>(type: "tinyint(4)", nullable: false),
                    targetFlag = table.Column<ulong>(type: "bit(1)", nullable: false),
                    augList = table.Column<string>(type: "char(11)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.classId);
                });

            migrationBuilder.CreateTable(
                name: "equipableclass",
                columns: table => new
                {
                    classId = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    slotId = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.classId);
                });

            migrationBuilder.CreateTable(
                name: "footlockers",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    entity_class_id = table.Column<uint>(type: "int(11) unsigned", nullable: false, defaultValueSql: "0"),
                    map_context_id = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValueSql: "0"),
                    coord_x = table.Column<float>(nullable: false, defaultValueSql: "0"),
                    coord_y = table.Column<float>(nullable: false, defaultValueSql: "0"),
                    coord_z = table.Column<float>(nullable: false, defaultValueSql: "0"),
                    orientation = table.Column<float>(nullable: false, defaultValueSql: "0"),
                    comment = table.Column<string>(type: "varchar(64)", nullable: true, defaultValueSql: "NULL")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_footlockers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "itemClass",
                columns: table => new
                {
                    classId = table.Column<int>(type: "int(11)", nullable: false),
                    inventoryIconStringId = table.Column<int>(type: "mediumint(9)", nullable: false),
                    lootValue = table.Column<int>(type: "mediumint(9)", nullable: false),
                    hiddenInventoryFlag = table.Column<ulong>(type: "bit(1)", nullable: false),
                    isConsumableFlag = table.Column<ulong>(type: "bit(1)", nullable: false),
                    maxHitPoints = table.Column<int>(type: "mediumint(9)", nullable: false),
                    stackSize = table.Column<int>(type: "int(11)", nullable: false),
                    dragAudioSetId = table.Column<int>(type: "int(11)", nullable: false),
                    dropAudioSetId = table.Column<int>(type: "int(11)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.classId);
                });

            migrationBuilder.CreateTable(
                name: "itemtemplate",
                columns: table => new
                {
                    itemTemplateId = table.Column<int>(type: "int(11)", nullable: false),
                    qualityId = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0"),
                    hasSellableFlag = table.Column<sbyte>(type: "tinyint(4)", nullable: false, defaultValueSql: "0"),
                    notTradableFlag = table.Column<sbyte>(type: "tinyint(4)", nullable: false, defaultValueSql: "0"),
                    hasCharacterUniqueFlag = table.Column<sbyte>(type: "tinyint(4)", nullable: false, defaultValueSql: "0"),
                    hasAccountUniqueFlag = table.Column<sbyte>(type: "tinyint(4)", nullable: false, defaultValueSql: "0"),
                    hasBoEFlag = table.Column<sbyte>(type: "tinyint(4)", nullable: false, defaultValueSql: "0"),
                    boundToCharacterFlag = table.Column<sbyte>(type: "tinyint(4)", nullable: false, defaultValueSql: "0"),
                    notPlaceableInLockBoxFlag = table.Column<sbyte>(type: "tinyint(4)", nullable: false, defaultValueSql: "0"),
                    inventoryCategory = table.Column<sbyte>(type: "tinyint(4)", nullable: false, defaultValueSql: "0"),
                    buyPrice = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0"),
                    sellPrice = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0")
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "itemtemplate_armor",
                columns: table => new
                {
                    itemTemplateId = table.Column<int>(type: "int(11)", nullable: false),
                    armorValue = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.itemTemplateId);
                });

            migrationBuilder.CreateTable(
                name: "itemtemplate_itemclass",
                columns: table => new
                {
                    itemTemplateId = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0"),
                    itemClassId = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0")
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "itemtemplate_racerequirement",
                columns: table => new
                {
                    itemTemplateId = table.Column<int>(type: "int(11)", nullable: false),
                    raceId = table.Column<sbyte>(type: "tinyint(4)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "itemtemplate_requirements",
                columns: table => new
                {
                    itemTemplateId = table.Column<int>(type: "int(11)", nullable: false),
                    reqType = table.Column<sbyte>(type: "tinyint(4)", nullable: false),
                    reqValue = table.Column<sbyte>(type: "tinyint(4)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "itemtemplate_resistance",
                columns: table => new
                {
                    itemtemplate_id = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0"),
                    resistance_type = table.Column<sbyte>(type: "tinyint(4)", nullable: false, defaultValueSql: "0"),
                    resistance_value = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0")
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "itemtemplate_skillrequirement",
                columns: table => new
                {
                    itemTemplateId = table.Column<int>(type: "int(11)", nullable: false),
                    skillId = table.Column<sbyte>(type: "tinyint(4)", nullable: false),
                    skillLevel = table.Column<sbyte>(type: "tinyint(4)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "itemtemplate_weapon",
                columns: table => new
                {
                    itemTemplateId = table.Column<int>(type: "int(11)", nullable: false),
                    aimRate = table.Column<double>(nullable: false, defaultValueSql: "0"),
                    reloadTime = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0"),
                    altActionId = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0"),
                    altActionArg = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0"),
                    aeType = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0"),
                    aeRadius = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0"),
                    recoilAmount = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0"),
                    reuseOverride = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0"),
                    coolRate = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0"),
                    heatPerShot = table.Column<double>(nullable: false, defaultValueSql: "0"),
                    toolType = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0"),
                    ammoPerShot = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0"),
                    windupTime = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0"),
                    recoveryTime = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0"),
                    refireTime = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0"),
                    range = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0"),
                    altMaxDamage = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0"),
                    altDamageType = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0"),
                    altRange = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0"),
                    altAERadius = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0"),
                    altAEType = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0"),
                    attackType = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "0")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.itemTemplateId);
                });

            migrationBuilder.CreateTable(
                name: "npc_missions",
                columns: table => new
                {
                    missionId = table.Column<int>(type: "int(11)", nullable: false),
                    command = table.Column<int>(type: "int(11)", nullable: false),
                    var1 = table.Column<int>(type: "int(11)", nullable: false, comment: "mission/objective type"),
                    var2 = table.Column<int>(type: "int(11)", nullable: false, comment: "mission/objective id"),
                    var3 = table.Column<int>(type: "int(11)", nullable: false),
                    comment = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "npc_packages",
                columns: table => new
                {
                    creatureDbId = table.Column<int>(type: "int(11)", nullable: false),
                    packageId = table.Column<int>(type: "int(11)", nullable: false),
                    comment = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "player_exp_for_level",
                columns: table => new
                {
                    level = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    experience = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.level);
                });

            migrationBuilder.CreateTable(
                name: "player_random_name",
                columns: table => new
                {
                    name = table.Column<string>(type: "varchar(64)", nullable: false),
                    type = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    gender = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.name, x.type, x.gender });
                });

            migrationBuilder.CreateTable(
                name: "spawnpool",
                columns: table => new
                {
                    dbId = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    mode = table.Column<sbyte>(type: "tinyint(2)", nullable: false, defaultValueSql: "0"),
                    animType = table.Column<sbyte>(type: "tinyint(2)", nullable: false, defaultValueSql: "0"),
                    respawnTime = table.Column<int>(type: "int(11)", nullable: false, defaultValueSql: "100", comment: "in secounds"),
                    posX = table.Column<float>(nullable: false),
                    posY = table.Column<float>(nullable: false),
                    posZ = table.Column<float>(nullable: false),
                    orientation = table.Column<float>(nullable: false),
                    contextId = table.Column<int>(type: "int(5)", nullable: false),
                    creatureId1 = table.Column<uint>(type: "int(11) unsigned", nullable: false, defaultValueSql: "0", comment: "dbId from creatures table"),
                    creatureMinCount1 = table.Column<sbyte>(type: "tinyint(4)", nullable: false, defaultValueSql: "0"),
                    creatureMaxCount1 = table.Column<sbyte>(type: "tinyint(4)", nullable: false, defaultValueSql: "0"),
                    creatureId2 = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValueSql: "0", comment: "dbId from creatures table"),
                    creatureMinCount2 = table.Column<sbyte>(type: "tinyint(4)", nullable: false, defaultValueSql: "0"),
                    creatureMaxCount2 = table.Column<sbyte>(type: "tinyint(4)", nullable: false, defaultValueSql: "0"),
                    creatureId3 = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValueSql: "0", comment: "dbId from creatures table"),
                    creatureMinCount3 = table.Column<sbyte>(type: "tinyint(4)", nullable: false, defaultValueSql: "0"),
                    creatureMaxCount3 = table.Column<sbyte>(type: "tinyint(4)", nullable: false, defaultValueSql: "0"),
                    creatureId4 = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValueSql: "0", comment: "dbId from creatures table"),
                    creatureMinCount4 = table.Column<sbyte>(type: "tinyint(4)", nullable: false, defaultValueSql: "0"),
                    creatureMaxCount4 = table.Column<sbyte>(type: "tinyint(4)", nullable: false, defaultValueSql: "0"),
                    creatureId5 = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValueSql: "0", comment: "dbId from creatures table"),
                    creatureMinCount5 = table.Column<sbyte>(type: "tinyint(4)", nullable: false, defaultValueSql: "0"),
                    creatureMaxCount5 = table.Column<sbyte>(type: "tinyint(4)", nullable: false, defaultValueSql: "0"),
                    creatureId6 = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValueSql: "0", comment: "dbId from creatures table"),
                    creatureMinCount6 = table.Column<sbyte>(type: "tinyint(4)", nullable: false, defaultValueSql: "0"),
                    creatureMaxCount6 = table.Column<sbyte>(type: "tinyint(4)", nullable: false, defaultValueSql: "0")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.dbId);
                });

            migrationBuilder.CreateTable(
                name: "starter_items",
                columns: table => new
                {
                    classId = table.Column<int>(type: "int(11)", nullable: false),
                    itemTemplateId = table.Column<int>(type: "int(11)", nullable: false),
                    slotId = table.Column<int>(type: "int(11)", nullable: true, defaultValueSql: "'20'"),
                    comment = table.Column<string>(type: "char(50)", nullable: true, defaultValueSql: "NULL")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.classId);
                });

            migrationBuilder.CreateTable(
                name: "teleporter",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    entity_class_id = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    type = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    description = table.Column<string>(type: "varchar(50)", nullable: false),
                    coord_x = table.Column<float>(nullable: false),
                    coord_y = table.Column<float>(nullable: false),
                    coord_z = table.Column<float>(nullable: false),
                    orientation = table.Column<float>(nullable: false),
                    map_context_id = table.Column<uint>(type: "int(10) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teleporter", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vendor_items",
                columns: table => new
                {
                    creatureDbId = table.Column<int>(type: "int(11)", nullable: false),
                    itemTemplateId = table.Column<int>(type: "int(11)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "vendors",
                columns: table => new
                {
                    creatureDbId = table.Column<int>(type: "int(11)", nullable: false),
                    packageId = table.Column<int>(type: "int(11)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.creatureDbId);
                });

            migrationBuilder.CreateTable(
                name: "weaponClass",
                columns: table => new
                {
                    classId = table.Column<int>(type: "int(11)", nullable: false),
                    weaponTemplateid = table.Column<short>(type: "smallint(6)", nullable: false),
                    weaponAttackActionId = table.Column<short>(type: "smallint(6)", nullable: false),
                    weaponAttackArgId = table.Column<short>(type: "smallint(6)", nullable: false),
                    drawActionId = table.Column<sbyte>(type: "tinyint(4)", nullable: false),
                    stowActionId = table.Column<sbyte>(type: "tinyint(4)", nullable: false),
                    reloadActionId = table.Column<sbyte>(type: "tinyint(4)", nullable: false),
                    ammoClassId = table.Column<short>(type: "smallint(6)", nullable: false),
                    clipSize = table.Column<short>(type: "smallint(6)", nullable: false),
                    minDamage = table.Column<int>(type: "mediumint(9)", nullable: false),
                    maxDamage = table.Column<int>(type: "mediumint(9)", nullable: false),
                    damageType = table.Column<sbyte>(type: "tinyint(4)", nullable: false),
                    velocity = table.Column<sbyte>(type: "tinyint(4)", nullable: false),
                    weaponAnimConditionCode = table.Column<sbyte>(type: "tinyint(4)", nullable: false),
                    windupOverride = table.Column<ulong>(type: "bit(1)", nullable: false),
                    recoveryOverride = table.Column<ulong>(type: "bit(1)", nullable: false),
                    reuseOverride = table.Column<ulong>(type: "bit(1)", nullable: false),
                    reloadOverride = table.Column<ulong>(type: "bit(1)", nullable: false),
                    rangeType = table.Column<ulong>(type: "bit(1)", nullable: false),
                    unkArg1 = table.Column<ulong>(type: "bit(1)", nullable: false),
                    unkArg2 = table.Column<ulong>(type: "bit(1)", nullable: false),
                    unkArg3 = table.Column<ulong>(type: "bit(1)", nullable: false),
                    unkArg4 = table.Column<ulong>(type: "bit(1)", nullable: false),
                    unkArg5 = table.Column<ulong>(type: "bit(1)", nullable: false),
                    unkArg6 = table.Column<short>(type: "smallint(6)", nullable: false),
                    unkArg7 = table.Column<ulong>(type: "bit(1)", nullable: false),
                    unkArg8 = table.Column<sbyte>(type: "tinyint(4)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.Sql("ALTER TABLE `armorClass` ENGINE=InnoDB DEFAULT CHARACTER SET=utf8 COLLATE=utf8_general_ci;");
            migrationBuilder.Sql("ALTER TABLE `creature_action` ENGINE=InnoDB DEFAULT CHARACTER SET=utf8 COLLATE=utf8_general_ci;");
            migrationBuilder.Sql("ALTER TABLE `creature_appearance` ENGINE=InnoDB DEFAULT CHARACTER SET=utf8 COLLATE=utf8_general_ci;");
            migrationBuilder.Sql("ALTER TABLE `creature_stats` ENGINE=InnoDB DEFAULT CHARACTER SET=utf8 COLLATE=utf8_general_ci;");
            migrationBuilder.Sql("ALTER TABLE `creatures` ENGINE=InnoDB DEFAULT CHARACTER SET=utf8 COLLATE=utf8_general_ci;");
            migrationBuilder.Sql("ALTER TABLE `entityClass` ENGINE=InnoDB DEFAULT CHARACTER SET=utf8 COLLATE=utf8_general_ci;");
            migrationBuilder.Sql("ALTER TABLE `equipableclass` ENGINE=InnoDB DEFAULT CHARACTER SET=utf8 COLLATE=utf8_general_ci;");
            migrationBuilder.Sql("ALTER TABLE `footlockers` ENGINE=InnoDB DEFAULT CHARACTER SET=utf8 COLLATE=utf8_general_ci;");
            migrationBuilder.Sql("ALTER TABLE `itemClass` ENGINE=InnoDB DEFAULT CHARACTER SET=utf8 COLLATE=utf8_general_ci;");
            migrationBuilder.Sql("ALTER TABLE `itemtemplate` ENGINE=InnoDB DEFAULT CHARACTER SET=utf8 COLLATE=utf8_general_ci;");
            migrationBuilder.Sql("ALTER TABLE `itemtemplate_armor` ENGINE=InnoDB DEFAULT CHARACTER SET=utf8 COLLATE=utf8_general_ci;");
            migrationBuilder.Sql("ALTER TABLE `itemtemplate_itemclass` ENGINE=InnoDB DEFAULT CHARACTER SET=utf8 COLLATE=utf8_general_ci;");
            migrationBuilder.Sql("ALTER TABLE `itemtemplate_racerequirement` ENGINE=InnoDB DEFAULT CHARACTER SET=utf8 COLLATE=utf8_general_ci;");
            migrationBuilder.Sql("ALTER TABLE `itemtemplate_requirements` ENGINE=InnoDB DEFAULT CHARACTER SET=utf8 COLLATE=utf8_general_ci;");
            migrationBuilder.Sql("ALTER TABLE `itemtemplate_resistance` ENGINE=InnoDB DEFAULT CHARACTER SET=utf8 COLLATE=utf8_general_ci;");
            migrationBuilder.Sql("ALTER TABLE `itemtemplate_skillrequirement` ENGINE=InnoDB DEFAULT CHARACTER SET=utf8 COLLATE=utf8_general_ci;");
            migrationBuilder.Sql("ALTER TABLE `itemtemplate_weapon` ENGINE=InnoDB DEFAULT CHARACTER SET=utf8 COLLATE=utf8_general_ci;");
            migrationBuilder.Sql("ALTER TABLE `npc_missions` ENGINE=InnoDB DEFAULT CHARACTER SET=utf8 COLLATE=utf8_general_ci;");
            migrationBuilder.Sql("ALTER TABLE `npc_packages` ENGINE=InnoDB DEFAULT CHARACTER SET=utf8 COLLATE=utf8_general_ci;");
            migrationBuilder.Sql("ALTER TABLE `player_exp_for_level` ENGINE=InnoDB DEFAULT CHARACTER SET=utf8 COLLATE=utf8_general_ci;");
            migrationBuilder.Sql("ALTER TABLE `player_random_name` ENGINE=InnoDB DEFAULT CHARACTER SET=utf8 COLLATE=utf8_general_ci;");
            migrationBuilder.Sql("ALTER TABLE `spawnpool` ENGINE=InnoDB DEFAULT CHARACTER SET=utf8 COLLATE=utf8_general_ci;");
            migrationBuilder.Sql("ALTER TABLE `starter_items` ENGINE=InnoDB DEFAULT CHARACTER SET=utf8 COLLATE=utf8_general_ci;");
            migrationBuilder.Sql("ALTER TABLE `teleporter` ENGINE=InnoDB DEFAULT CHARACTER SET=utf8 COLLATE=utf8_general_ci;");
            migrationBuilder.Sql("ALTER TABLE `vendor_items` ENGINE=InnoDB DEFAULT CHARACTER SET=utf8 COLLATE=utf8_general_ci;");
            migrationBuilder.Sql("ALTER TABLE `vendors` ENGINE=InnoDB DEFAULT CHARACTER SET=utf8 COLLATE=utf8_general_ci;");
            migrationBuilder.Sql("ALTER TABLE `weaponClass` ENGINE=InnoDB DEFAULT CHARACTER SET=utf8 COLLATE=utf8_general_ci;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "armorClass");

            migrationBuilder.DropTable(
                name: "creature_action");

            migrationBuilder.DropTable(
                name: "creature_appearance");

            migrationBuilder.DropTable(
                name: "creature_stats");

            migrationBuilder.DropTable(
                name: "creatures");

            migrationBuilder.DropTable(
                name: "entityClass");

            migrationBuilder.DropTable(
                name: "equipableclass");

            migrationBuilder.DropTable(
                name: "footlockers");

            migrationBuilder.DropTable(
                name: "itemClass");

            migrationBuilder.DropTable(
                name: "itemtemplate");

            migrationBuilder.DropTable(
                name: "itemtemplate_armor");

            migrationBuilder.DropTable(
                name: "itemtemplate_itemclass");

            migrationBuilder.DropTable(
                name: "itemtemplate_racerequirement");

            migrationBuilder.DropTable(
                name: "itemtemplate_requirements");

            migrationBuilder.DropTable(
                name: "itemtemplate_resistance");

            migrationBuilder.DropTable(
                name: "itemtemplate_skillrequirement");

            migrationBuilder.DropTable(
                name: "itemtemplate_weapon");

            migrationBuilder.DropTable(
                name: "npc_missions");

            migrationBuilder.DropTable(
                name: "npc_packages");

            migrationBuilder.DropTable(
                name: "player_exp_for_level");

            migrationBuilder.DropTable(
                name: "player_random_name");

            migrationBuilder.DropTable(
                name: "spawnpool");

            migrationBuilder.DropTable(
                name: "starter_items");

            migrationBuilder.DropTable(
                name: "teleporter");

            migrationBuilder.DropTable(
                name: "vendor_items");

            migrationBuilder.DropTable(
                name: "vendors");

            migrationBuilder.DropTable(
                name: "weaponClass");
        }
    }
}
