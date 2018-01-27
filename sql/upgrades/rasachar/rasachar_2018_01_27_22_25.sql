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

-- Dumping structure for table rasachar.characters
DROP TABLE IF EXISTS `characters`;
CREATE TABLE IF NOT EXISTS `characters` (
  `accountId` int(11) unsigned NOT NULL,
  `characterSlot` int(11) unsigned NOT NULL,
  `name` varchar(64) NOT NULL,
  `familyName` varchar(64) NOT NULL,
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
  `logos` varbinary(410) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Dumping data for table rasachar.characters: ~0 rows (approximately)
/*!40000 ALTER TABLE `characters` DISABLE KEYS */;
/*!40000 ALTER TABLE `characters` ENABLE KEYS */;

-- Dumping structure for table rasachar.character_abilitydrawer
DROP TABLE IF EXISTS `character_abilitydrawer`;
CREATE TABLE IF NOT EXISTS `character_abilitydrawer` (
  `accountId` int(11) unsigned NOT NULL,
  `characterSlot` int(11) unsigned NOT NULL,
  `abilitySlotId` int(11) NOT NULL DEFAULT '0',
  `abilityId` int(10) NOT NULL DEFAULT '0',
  `abilityLevel` int(11) NOT NULL DEFAULT '0'
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Dumping data for table rasachar.character_abilitydrawer: ~0 rows (approximately)
/*!40000 ALTER TABLE `character_abilitydrawer` DISABLE KEYS */;
/*!40000 ALTER TABLE `character_abilitydrawer` ENABLE KEYS */;

-- Dumping structure for table rasachar.character_appearance
DROP TABLE IF EXISTS `character_appearance`;
CREATE TABLE IF NOT EXISTS `character_appearance` (
  `accountId` int(10) unsigned NOT NULL,
  `characterSlot` int(10) unsigned NOT NULL,
  `slotId` int(11) NOT NULL,
  `classId` int(11) NOT NULL,
  `color` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Dumping data for table rasachar.character_appearance: ~0 rows (approximately)
/*!40000 ALTER TABLE `character_appearance` DISABLE KEYS */;
/*!40000 ALTER TABLE `character_appearance` ENABLE KEYS */;

-- Dumping structure for table rasachar.character_inventory
DROP TABLE IF EXISTS `character_inventory`;
CREATE TABLE IF NOT EXISTS `character_inventory` (
  `accountId` int(10) unsigned NOT NULL,
  `characterSlot` int(10) unsigned NOT NULL,
  `inventoryType` int(11) NOT NULL,
  `slotId` int(11) NOT NULL,
  `itemId` int(10) unsigned NOT NULL,
  UNIQUE KEY `itemId` (`itemId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Dumping data for table rasachar.character_inventory: ~0 rows (approximately)
/*!40000 ALTER TABLE `character_inventory` DISABLE KEYS */;
/*!40000 ALTER TABLE `character_inventory` ENABLE KEYS */;

-- Dumping structure for table rasachar.character_lockbox
DROP TABLE IF EXISTS `character_lockbox`;
CREATE TABLE IF NOT EXISTS `character_lockbox` (
  `accountId` int(10) unsigned NOT NULL,
  `credits` int(10) unsigned NOT NULL DEFAULT '0',
  `purashedTabs` int(10) unsigned NOT NULL DEFAULT '1',
  PRIMARY KEY (`accountId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Dumping data for table rasachar.character_lockbox: ~1 rows (approximately)
/*!40000 ALTER TABLE `character_lockbox` DISABLE KEYS */;
INSERT INTO `character_lockbox` (`accountId`, `credits`, `purashedTabs`) VALUES
	(1, 0, 1);
/*!40000 ALTER TABLE `character_lockbox` ENABLE KEYS */;

-- Dumping structure for table rasachar.character_logos
DROP TABLE IF EXISTS `character_logos`;
CREATE TABLE IF NOT EXISTS `character_logos` (
  `accountId` int(10) unsigned NOT NULL,
  `characterSlot` int(10) unsigned NOT NULL,
  `logosId` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Dumping data for table rasachar.character_logos: ~0 rows (approximately)
/*!40000 ALTER TABLE `character_logos` DISABLE KEYS */;
/*!40000 ALTER TABLE `character_logos` ENABLE KEYS */;

-- Dumping structure for table rasachar.character_missions
DROP TABLE IF EXISTS `character_missions`;
CREATE TABLE IF NOT EXISTS `character_missions` (
  `accountId` int(11) unsigned NOT NULL,
  `characterSlot` int(11) unsigned NOT NULL,
  `missionId` int(11) NOT NULL,
  `missionState` tinyint(4) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Dumping data for table rasachar.character_missions: ~0 rows (approximately)
/*!40000 ALTER TABLE `character_missions` DISABLE KEYS */;
/*!40000 ALTER TABLE `character_missions` ENABLE KEYS */;

-- Dumping structure for table rasachar.character_skills
DROP TABLE IF EXISTS `character_skills`;
CREATE TABLE IF NOT EXISTS `character_skills` (
  `accountId` int(11) unsigned NOT NULL,
  `characterSlot` int(11) unsigned NOT NULL,
  `skillId` int(11) NOT NULL,
  `abilityId` int(11) NOT NULL,
  `skillLevel` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Dumping data for table rasachar.character_skills: ~0 rows (approximately)
/*!40000 ALTER TABLE `character_skills` DISABLE KEYS */;
/*!40000 ALTER TABLE `character_skills` ENABLE KEYS */;

-- Dumping structure for table rasachar.character_titles
DROP TABLE IF EXISTS `character_titles`;
CREATE TABLE IF NOT EXISTS `character_titles` (
  `accountId` int(11) unsigned NOT NULL,
  `characterSlot` int(11) unsigned NOT NULL,
  `titleId` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Dumping data for table rasachar.character_titles: ~0 rows (approximately)
/*!40000 ALTER TABLE `character_titles` DISABLE KEYS */;
/*!40000 ALTER TABLE `character_titles` ENABLE KEYS */;

-- Dumping structure for table rasachar.items
DROP TABLE IF EXISTS `items`;
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

-- Dumping data for table rasachar.items: ~0 rows (approximately)
/*!40000 ALTER TABLE `items` DISABLE KEYS */;
/*!40000 ALTER TABLE `items` ENABLE KEYS */;

/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IF(@OLD_FOREIGN_KEY_CHECKS IS NULL, 1, @OLD_FOREIGN_KEY_CHECKS) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
