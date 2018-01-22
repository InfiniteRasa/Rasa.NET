-- --------------------------------------------------------
-- Host:                         127.0.0.1
-- Server version:               5.6.37-log - MySQL Community Server (GPL)
-- Server OS:                    Win64
-- HeidiSQL Verzija:             9.4.0.5125
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;


-- Dumping database structure for rasaauth
CREATE DATABASE IF NOT EXISTS `rasaauth` /*!40100 DEFAULT CHARACTER SET utf8 */;
USE `rasaauth`;

-- Dumping structure for table rasaauth.account
CREATE TABLE IF NOT EXISTS `account` (
  `id` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `email` varchar(255) NOT NULL,
  `username` varchar(64) NOT NULL,
  `password` varchar(64) NOT NULL,
  `salt` varchar(40) NOT NULL,
  `level` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `last_ip` varchar(45) NOT NULL DEFAULT '0.0.0.0',
  `last_server_id` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `last_login` datetime DEFAULT NULL,
  `join_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `locked` bit(1) NOT NULL DEFAULT b'0',
  `validated` bit(1) NOT NULL DEFAULT b'0',
  `validation_token` varchar(40) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `email_UNIQUE` (`email`),
  UNIQUE KEY `username_UNIQUE` (`username`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping database structure for rasachar
CREATE DATABASE IF NOT EXISTS `rasachar` /*!40100 DEFAULT CHARACTER SET utf8 */;
USE `rasachar`;

-- Dumping structure for table rasachar.characters
CREATE TABLE IF NOT EXISTS `characters` (
  `characterId` int(11) unsigned NOT NULL AUTO_INCREMENT,
  `name` varchar(64) NOT NULL,
  `familyName` varchar(64) NOT NULL,
  `accountId` int(11) unsigned NOT NULL,
  `slotId` int(11) NOT NULL DEFAULT '0',
  `gender` int(11) NOT NULL DEFAULT '0',
  `scale` double NOT NULL DEFAULT '0',
  `raceId` int(11) NOT NULL DEFAULT '0',
  `classId` int(11) NOT NULL DEFAULT '0',
  `mapContextId` int(11) NOT NULL DEFAULT '0',
  `posX` double NOT NULL DEFAULT '0',
  `posY` double NOT NULL DEFAULT '0',
  `posZ` double NOT NULL DEFAULT '0',
  `rotation` double NOT NULL DEFAULT '0',
  `experience` int(11) NOT NULL DEFAULT '0',
  `level` int(11) NOT NULL DEFAULT '0',
  `body` int(11) NOT NULL DEFAULT '0',
  `mind` int(11) NOT NULL DEFAULT '0',
  `spirit` int(11) NOT NULL DEFAULT '0',
  `cloneCredits` int(11) NOT NULL DEFAULT '0',
  `numLogins` int(11) NOT NULL DEFAULT '0',
  `totalTimePlayed` int(11) NOT NULL DEFAULT '0',
  `timeSinceLastPlayed` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `clanId` int(11) NOT NULL DEFAULT '0',
  `clanName` varchar(50) NOT NULL DEFAULT '',
  `credits` int(11) NOT NULL DEFAULT '0',
  `prestige` int(11) NOT NULL DEFAULT '0',
  `currentAbilityDrawer` int(11) NOT NULL DEFAULT '0',
  `logos` varbinary(410) NOT NULL,
  PRIMARY KEY (`characterId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasachar.character_abilitydrawer
CREATE TABLE IF NOT EXISTS `character_abilitydrawer` (
  `characterId` int(11) unsigned NOT NULL,
  `abilitySlotId` int(11) NOT NULL DEFAULT '0',
  `abilityId` int(10) NOT NULL DEFAULT '0',
  `abilityLevel` int(11) NOT NULL DEFAULT '0'
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasachar.character_appearance
CREATE TABLE IF NOT EXISTS `character_appearance` (
  `characterId` int(10) unsigned DEFAULT NULL,
  `slotId` int(11) DEFAULT NULL,
  `classId` int(11) DEFAULT NULL,
  `color` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasachar.character_inventory
CREATE TABLE IF NOT EXISTS `character_inventory` (
  `characterId` int(10) unsigned DEFAULT NULL,
  `slotId` int(11) DEFAULT NULL,
  `itemId` int(10) unsigned DEFAULT NULL,
  UNIQUE KEY `itemId` (`itemId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasachar.character_logos
CREATE TABLE IF NOT EXISTS `character_logos` (
  `characterId` int(10) unsigned DEFAULT NULL,
  `logosId` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasachar.character_missions
CREATE TABLE IF NOT EXISTS `character_missions` (
  `characterId` int(11) NOT NULL,
  `missionId` int(11) NOT NULL,
  `missionState` tinyint(4) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasachar.character_skills
CREATE TABLE IF NOT EXISTS `character_skills` (
  `characterId` int(11) unsigned NOT NULL,
  `skillId` int(11) NOT NULL,
  `abilityId` int(11) NOT NULL,
  `skillLevel` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasachar.character_titles
CREATE TABLE IF NOT EXISTS `character_titles` (
  `characterId` int(11) unsigned DEFAULT NULL,
  `titleId` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasachar.items
CREATE TABLE IF NOT EXISTS `items` (
  `itemId` int(11) unsigned NOT NULL AUTO_INCREMENT,
  `itemTemplateId` int(11) NOT NULL DEFAULT '0',
  `stackSize` int(11) NOT NULL DEFAULT '0',
  `currentHitPoints` int(11) NOT NULL DEFAULT '0',
  `color` int(11) NOT NULL DEFAULT '0',
  `ammoCount` int(11) NOT NULL DEFAULT '0',
  `crafterName` varchar(64) NOT NULL DEFAULT '',
  `createdAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`itemId`),
  UNIQUE KEY `id` (`itemId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.

-- Dumping database structure for rasaworld
CREATE DATABASE IF NOT EXISTS `rasaworld` /*!40100 DEFAULT CHARACTER SET utf8 */;
USE `rasaworld`;

-- Dumping structure for table rasaworld.armorclass
CREATE TABLE IF NOT EXISTS `armorclass` (
  `classId` int(11) DEFAULT NULL,
  `minDamageAbsorbed` int(11) DEFAULT NULL,
  `maxDamageAbsorbed` int(11) DEFAULT NULL,
  `regenRate` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasaworld.creatures
CREATE TABLE IF NOT EXISTS `creatures` (
  `dbId` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `comment` varchar(50) DEFAULT '',
  `classId` int(10) unsigned NOT NULL,
  `faction` int(10) NOT NULL DEFAULT '0',
  `level` int(10) NOT NULL DEFAULT '0',
  `maxHitPoints` int(10) NOT NULL DEFAULT '0',
  `nameId` int(10) NOT NULL DEFAULT '0',
  PRIMARY KEY (`dbId`)
) ENGINE=InnoDB AUTO_INCREMENT=102 DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasaworld.creature_appearance
CREATE TABLE IF NOT EXISTS `creature_appearance` (
  `dbId` int(10) unsigned NOT NULL,
  `slotId` tinyint(2) NOT NULL,
  `classId` int(11) NOT NULL DEFAULT '0',
  `color` int(11) NOT NULL DEFAULT '0'
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasaworld.creature_stats
CREATE TABLE IF NOT EXISTS `creature_stats` (
  `creatureDbId` int(10) unsigned NOT NULL COMMENT 'dbId from cratures table',
  `body` int(10) unsigned NOT NULL,
  `mind` int(10) unsigned NOT NULL,
  `spirit` int(10) unsigned NOT NULL,
  `health` int(10) unsigned NOT NULL,
  `armor` int(10) unsigned NOT NULL,
  PRIMARY KEY (`creatureDbId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasaworld.entityclass
CREATE TABLE IF NOT EXISTS `entityclass` (
  `classId` int(11) NOT NULL,
  `className` char(58) NOT NULL,
  `meshId` int(11) NOT NULL,
  `classCollisionRole` tinyint(4) NOT NULL,
  `targetFlag` bit(1) NOT NULL,
  `augList` char(11) NOT NULL,
  PRIMARY KEY (`classId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasaworld.equipableclass
CREATE TABLE IF NOT EXISTS `equipableclass` (
  `classId` int(11) unsigned NOT NULL,
  `slotId` int(11) DEFAULT NULL,
  PRIMARY KEY (`classId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasaworld.itemclass
CREATE TABLE IF NOT EXISTS `itemclass` (
  `classId` int(11) NOT NULL,
  `inventoryIconStringId` mediumint(9) NOT NULL,
  `lootValue` mediumint(9) NOT NULL,
  `hiddenInventoryFlag` bit(1) NOT NULL,
  `isConsumableFlag` bit(1) NOT NULL,
  `maxHitPoints` mediumint(9) NOT NULL,
  `stackSize` int(11) NOT NULL,
  `dragAudioSetId` int(11) NOT NULL,
  `dropAudioSetId` int(11) NOT NULL,
  PRIMARY KEY (`classId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasaworld.itemtemplate
CREATE TABLE IF NOT EXISTS `itemtemplate` (
  `itemTemplateId` int(11) NOT NULL,
  `qualityId` int(11) NOT NULL DEFAULT '0',
  `hasSellableFlag` tinyint(4) NOT NULL DEFAULT '0',
  `notTradableFlag` tinyint(4) NOT NULL DEFAULT '0',
  `hasCharacterUniqueFlag` tinyint(4) NOT NULL DEFAULT '0',
  `hasAccountUniqueFlag` tinyint(4) NOT NULL DEFAULT '0',
  `hasBoEFlag` tinyint(4) NOT NULL DEFAULT '0',
  `boundToCharacterFlag` tinyint(4) NOT NULL DEFAULT '0',
  `notPlaceableInLockBoxFlag` tinyint(4) NOT NULL DEFAULT '0',
  `inventoryCategory` tinyint(4) NOT NULL DEFAULT '0',
  `buyPrice` int(11) NOT NULL DEFAULT '0',
  `sellPrice` int(11) NOT NULL DEFAULT '0'
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasaworld.itemtemplate_armor
CREATE TABLE IF NOT EXISTS `itemtemplate_armor` (
  `itemTemplateId` int(11) NOT NULL,
  `armorValue` int(11) NOT NULL DEFAULT '0',
  PRIMARY KEY (`itemTemplateId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasaworld.itemtemplate_itemclass
CREATE TABLE IF NOT EXISTS `itemtemplate_itemclass` (
  `itemTemplateId` int(11) DEFAULT NULL,
  `itemClassId` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasaworld.itemtemplate_racerequirement
CREATE TABLE IF NOT EXISTS `itemtemplate_racerequirement` (
  `itemTemplateId` int(11) NOT NULL,
  `raceId` tinyint(4) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasaworld.itemtemplate_requirements
CREATE TABLE IF NOT EXISTS `itemtemplate_requirements` (
  `itemTemplateId` int(11) NOT NULL,
  `reqType` tinyint(4) NOT NULL,
  `reqValue` tinyint(4) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasaworld.itemtemplate_skillrequirement
CREATE TABLE IF NOT EXISTS `itemtemplate_skillrequirement` (
  `itemTemplateId` int(11) NOT NULL,
  `skillId` tinyint(4) NOT NULL,
  `skillLevel` tinyint(4) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasaworld.itemtemplate_weapon
CREATE TABLE IF NOT EXISTS `itemtemplate_weapon` (
  `itemTemplateId` int(11) NOT NULL,
  `aimRate` double NOT NULL DEFAULT '0',
  `reloadTime` int(11) NOT NULL DEFAULT '0',
  `altActionId` int(11) NOT NULL DEFAULT '0',
  `altActionArg` int(11) NOT NULL DEFAULT '0',
  `aeType` int(11) NOT NULL DEFAULT '0',
  `aeRadius` int(11) NOT NULL DEFAULT '0',
  `recoilAmount` int(11) NOT NULL DEFAULT '0',
  `reuseOverride` int(11) NOT NULL DEFAULT '0',
  `coolRate` int(11) NOT NULL DEFAULT '0',
  `heatPerShot` double NOT NULL DEFAULT '0',
  `toolType` int(11) NOT NULL DEFAULT '0',
  `ammoPerShot` int(11) NOT NULL DEFAULT '0',
  `windupTime` int(11) NOT NULL DEFAULT '0',
  `recoveryTime` int(11) NOT NULL DEFAULT '0',
  `refireTime` int(11) NOT NULL DEFAULT '0',
  `range` int(11) NOT NULL DEFAULT '0',
  `altMaxDamage` int(11) NOT NULL DEFAULT '0',
  `altDamageType` int(11) NOT NULL DEFAULT '0',
  `altRange` int(11) NOT NULL DEFAULT '0',
  `altAERadius` int(11) NOT NULL DEFAULT '0',
  `altAEType` int(11) NOT NULL DEFAULT '0',
  `attackType` int(11) NOT NULL DEFAULT '0',
  PRIMARY KEY (`itemTemplateId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasaworld.npc_missions
CREATE TABLE IF NOT EXISTS `npc_missions` (
  `missionId` int(11) NOT NULL,
  `command` int(11) NOT NULL,
  `var1` int(11) NOT NULL COMMENT 'mission/objective type',
  `var2` int(11) NOT NULL COMMENT 'mission/objective id',
  `var3` int(11) NOT NULL,
  `comment` varchar(50) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasaworld.npc_packages
CREATE TABLE IF NOT EXISTS `npc_packages` (
  `creatureDbId` int(11) NOT NULL,
  `packageId` int(11) NOT NULL,
  `comment` varchar(50) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasaworld.random_name
CREATE TABLE IF NOT EXISTS `random_name` (
  `name` varchar(45) NOT NULL,
  `type` varchar(45) NOT NULL,
  `gender` varchar(45) NOT NULL DEFAULT '',
  PRIMARY KEY (`name`,`type`,`gender`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasaworld.spawnpool
CREATE TABLE IF NOT EXISTS `spawnpool` (
  `dbId` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `mode` tinyint(2) NOT NULL DEFAULT '0',
  `animType` tinyint(2) NOT NULL DEFAULT '0',
  `respawnTime` int(11) NOT NULL DEFAULT '100' COMMENT 'in secounds',
  `posX` double NOT NULL,
  `posY` double NOT NULL,
  `posZ` double NOT NULL,
  `rotation` double NOT NULL,
  `contextId` int(5) NOT NULL,
  `creatureId1` int(11) unsigned NOT NULL DEFAULT '0' COMMENT 'dbId from creatures table',
  `creatureMinCount1` tinyint(4) NOT NULL DEFAULT '0',
  `creatureMaxCount1` tinyint(4) NOT NULL DEFAULT '0',
  `creatureId2` int(10) unsigned NOT NULL DEFAULT '0' COMMENT 'dbId from creatures table',
  `creatureMinCount2` tinyint(4) NOT NULL DEFAULT '0',
  `creatureMaxCount2` tinyint(4) NOT NULL DEFAULT '0',
  `creatureId3` int(10) unsigned NOT NULL DEFAULT '0' COMMENT 'dbId from creatures table',
  `creatureMinCount3` tinyint(4) NOT NULL DEFAULT '0',
  `creatureMaxCount3` tinyint(4) NOT NULL DEFAULT '0',
  `creatureId4` int(10) unsigned NOT NULL DEFAULT '0' COMMENT 'dbId from creatures table',
  `creatureMinCount4` tinyint(4) NOT NULL DEFAULT '0',
  `creatureMaxCount4` tinyint(4) NOT NULL DEFAULT '0',
  `creatureId5` int(10) unsigned NOT NULL DEFAULT '0' COMMENT 'dbId from creatures table',
  `creatureMinCount5` tinyint(4) NOT NULL DEFAULT '0',
  `creatureMaxCount5` tinyint(4) NOT NULL DEFAULT '0',
  `creatureId6` int(10) unsigned NOT NULL DEFAULT '0' COMMENT 'dbId from creatures table',
  `creatureMinCount6` tinyint(4) NOT NULL DEFAULT '0',
  `creatureMaxCount6` tinyint(4) NOT NULL DEFAULT '0',
  PRIMARY KEY (`dbId`)
) ENGINE=InnoDB AUTO_INCREMENT=102 DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasaworld.starter_items
CREATE TABLE IF NOT EXISTS `starter_items` (
  `itemTemplateId` int(11) NOT NULL,
  `classId` int(11) NOT NULL,
  `slotId` int(11) DEFAULT '20',
  `comment` char(50) DEFAULT NULL,
  PRIMARY KEY (`itemTemplateId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasaworld.vendors
CREATE TABLE IF NOT EXISTS `vendors` (
  `creatureDbId` int(11) NOT NULL,
  `packageId` int(11) NOT NULL,
  PRIMARY KEY (`creatureDbId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasaworld.vendor_items
CREATE TABLE IF NOT EXISTS `vendor_items` (
  `creatureDbId` int(11) NOT NULL,
  `itemTemplateId` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
-- Dumping structure for table rasaworld.weaponclass
CREATE TABLE IF NOT EXISTS `weaponclass` (
  `classId` int(11) NOT NULL,
  `weaponTemplateid` smallint(6) NOT NULL,
  `weaponAttackActionId` smallint(6) NOT NULL,
  `weaponAttackArgId` smallint(6) NOT NULL,
  `drawActionId` tinyint(4) NOT NULL,
  `stowActionId` tinyint(4) NOT NULL,
  `reloadActionId` tinyint(4) NOT NULL,
  `ammoClassId` smallint(6) NOT NULL,
  `clipSize` smallint(6) NOT NULL,
  `minDamage` mediumint(9) NOT NULL,
  `maxDamage` mediumint(9) NOT NULL,
  `damageType` tinyint(4) NOT NULL,
  `velocity` tinyint(4) NOT NULL,
  `weaponAnimConditionCode` tinyint(4) NOT NULL,
  `windupOverride` bit(1) NOT NULL,
  `recoveryOverride` bit(1) NOT NULL,
  `reuseOverride` bit(1) NOT NULL,
  `reloadOverride` bit(1) NOT NULL,
  `rangeType` bit(1) NOT NULL,
  `unkArg1` bit(1) NOT NULL,
  `unkArg2` bit(1) NOT NULL,
  `unkArg3` bit(1) NOT NULL,
  `unkArg4` bit(1) NOT NULL,
  `unkArg5` bit(1) NOT NULL,
  `unkArg6` smallint(6) NOT NULL,
  `unkArg7` bit(1) NOT NULL,
  `unkArg8` tinyint(4) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Data exporting was unselected.
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IF(@OLD_FOREIGN_KEY_CHECKS IS NULL, 1, @OLD_FOREIGN_KEY_CHECKS) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
