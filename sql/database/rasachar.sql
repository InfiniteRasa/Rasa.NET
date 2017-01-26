-- --------------------------------------------------------
-- Host:                         localhost
-- Server version:               5.6.31-log - MySQL Community Server (GPL)
-- Server OS:                    Win64
-- HeidiSQL Version:             9.3.0.4984
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;

-- Dumping structure for table rasachar.characters
DROP TABLE IF EXISTS `characters`;
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

-- Dumping data for table rasachar.characters: ~0 rows (approximately)
/*!40000 ALTER TABLE `characters` DISABLE KEYS */;
/*!40000 ALTER TABLE `characters` ENABLE KEYS */;


-- Dumping structure for table rasachar.character_abilitydrawer
DROP TABLE IF EXISTS `character_abilitydrawer`;
CREATE TABLE IF NOT EXISTS `character_abilitydrawer` (
  `characterId` int(11) unsigned NOT NULL,
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
  `characterId` int(10) unsigned DEFAULT NULL,
  `slotId` int(11) DEFAULT NULL,
  `slotItem` int(11) DEFAULT NULL,
  `slotHue` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Dumping data for table rasachar.character_appearance: ~0 rows (approximately)
/*!40000 ALTER TABLE `character_appearance` DISABLE KEYS */;
/*!40000 ALTER TABLE `character_appearance` ENABLE KEYS */;


-- Dumping structure for table rasachar.character_inventory
DROP TABLE IF EXISTS `character_inventory`;
CREATE TABLE IF NOT EXISTS `character_inventory` (
  `characterId` int(10) unsigned DEFAULT NULL,
  `slotId` int(11) DEFAULT NULL,
  `itemId` int(10) unsigned DEFAULT NULL,
  UNIQUE KEY `itemId` (`itemId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Dumping data for table rasachar.character_inventory: ~0 rows (approximately)
/*!40000 ALTER TABLE `character_inventory` DISABLE KEYS */;
/*!40000 ALTER TABLE `character_inventory` ENABLE KEYS */;


-- Dumping structure for table rasachar.character_skills
DROP TABLE IF EXISTS `character_skills`;
CREATE TABLE IF NOT EXISTS `character_skills` (
  `characterId` int(11) unsigned NOT NULL,
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
  `characterId` int(11) unsigned DEFAULT NULL,
  `titleId` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Dumping data for table rasachar.character_titles: ~0 rows (approximately)
/*!40000 ALTER TABLE `character_titles` DISABLE KEYS */;
/*!40000 ALTER TABLE `character_titles` ENABLE KEYS */;


-- Dumping structure for table rasachar.items
DROP TABLE IF EXISTS `items`;
CREATE TABLE IF NOT EXISTS `items` (
  `itemId` int(11) unsigned NOT NULL AUTO_INCREMENT,
  `entityClassId` int(11) NOT NULL DEFAULT '0',
  `stackSize` int(11) NOT NULL DEFAULT '0',
  `color` int(11) NOT NULL DEFAULT '0',
  `ammoCount` int(11) NOT NULL DEFAULT '0',
  `crafterName` varchar(64) DEFAULT '0',
  PRIMARY KEY (`itemId`),
  UNIQUE KEY `id` (`itemId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Dumping data for table rasachar.items: ~0 rows (approximately)
/*!40000 ALTER TABLE `items` DISABLE KEYS */;
/*!40000 ALTER TABLE `items` ENABLE KEYS */;
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IF(@OLD_FOREIGN_KEY_CHECKS IS NULL, 1, @OLD_FOREIGN_KEY_CHECKS) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
