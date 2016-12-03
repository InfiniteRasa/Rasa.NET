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

-- Dumping structure for table rasaworld.random_name
DROP TABLE IF EXISTS `random_name`;
CREATE TABLE IF NOT EXISTS `random_name` (
  `name` varchar(255) NOT NULL,
  `type` varchar(255) NOT NULL,
  `gender` varchar(255) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Dumping data for table rasaworld.random_name: ~10 rows (approximately)
/*!40000 ALTER TABLE `random_name` DISABLE KEYS */;
INSERT INTO `random_name` (`name`, `type`, `gender`) VALUES
	('John', 'first', 'male'),
	('Marko', 'first', 'male'),
	('Dexa', 'first', 'female'),
	('Boby', 'first', 'neutral'),
	('Dax', 'first', 'neutral'),
	('Mini', 'last', 'neutral'),
	('Walker', 'last', 'neutral'),
	('Diana', 'first', 'female'),
	('Daniels', 'last', 'neutral'),
	('Jack', 'first', 'male');
/*!40000 ALTER TABLE `random_name` ENABLE KEYS */;
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IF(@OLD_FOREIGN_KEY_CHECKS IS NULL, 1, @OLD_FOREIGN_KEY_CHECKS) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
