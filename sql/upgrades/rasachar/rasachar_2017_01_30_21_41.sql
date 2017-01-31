DROP TABLE IF EXISTS `items`;
CREATE TABLE IF NOT EXISTS `items` (
  `itemId` int(11) unsigned NOT NULL AUTO_INCREMENT,
  `entityClassId` int(11) NOT NULL DEFAULT '0',
  `stackSize` int(11) NOT NULL DEFAULT '0',
  `color` int(11) NOT NULL DEFAULT '0',
  `ammoCount` int(11) NOT NULL DEFAULT '0',
  `crafterName` varchar(64) DEFAULT '',
  PRIMARY KEY (`itemId`),
  UNIQUE KEY `id` (`itemId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;