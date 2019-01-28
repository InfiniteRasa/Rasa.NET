INSERT INTO `rasaworld`.`creature_stats` (`creatureDbId`, `body`, `mind`, `spirit`, `health`, `armor`) VALUES
('11', '15', '15', '15', '120', '70'),
('12', '15', '15', '15', '120', '70'),
('13', '15', '15', '15', '120', '70'),
('14', '15', '15', '15', '120', '70'),
('15', '15', '15', '15', '120', '70'),
('16', '15', '15', '15', '120', '70'),
('17', '15', '15', '15', '120', '70'),
('18', '15', '15', '15', '120', '70'),
('19', '15', '15', '15', '120', '70'),
('20', '15', '15', '15', '120', '70'),
('21', '15', '15', '15', '120', '70'),
('22', '15', '15', '15', '120', '70'),
('23', '15', '15', '15', '120', '70'),
('24', '15', '15', '15', '120', '70'),
('25', '15', '15', '15', '120', '70'),
('26', '15', '15', '15', '120', '70'),
('27', '15', '15', '15', '120', '70'),
('28', '15', '15', '15', '120', '70'),
('29', '15', '15', '15', '120', '70'),
('100', '15', '15', '15', '120', '70'),
('101', '15', '15', '15', '120', '70');

DROP TABLE IF EXISTS `rasaworld`.`creature_action`;
CREATE TABLE IF NOT EXISTS `rasaworld`.`creature_action` (
  `id` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `description` varchar(50) DEFAULT NULL,
  `actionId` int(10) unsigned NOT NULL,
  `actionArgId` int(10) unsigned NOT NULL,
  `rangeMin` float NOT NULL,
  `rangeMax` float NOT NULL,
  `recoverTime` int(10) unsigned NOT NULL,
  `recoverTimeGlobal` int(10) unsigned NOT NULL,
  `windupTime` int(10) unsigned NOT NULL,
  `minDamage` int(10) unsigned NOT NULL,
  `maxDamage` int(10) unsigned NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=latin1;

INSERT INTO `rasaworld`.`creature_action` (`id`, `description`, `actionId`, `actionArgId`, `rangeMin`, `rangeMax`, `recoverTime`, `recoverTimeGlobal`, `windupTime`, `minDamage`, `maxDamage`) VALUES
	(1, 'melee attack thrax soldier', 174, 46, 0.5, 3.5, 1300, 500, 0, 5, 12),
	(2, 'range attack afs light soldier', 1, 133, 1, 20, 800, 500, 0, 10, 15),
	(3, 'thrax kick', 397, 1, 1, 4, 6000, 500, 333, 20, 22),
	(4, 'young boargar melee', 174, 10, 1, 4, 2500, 500, 0, 10, 20),
	(5, 'forean spearman melee', 174, 11, 1, 6, 3000, 500, 0, 10, 45),
	(6, 'forean spearman lighting', 203, 1, 5, 20, 2800, 500, 0, 5, 30),
	(7, 'Supply Sergeant Maddrey Weapon Range Attack', 1, 133, 1, 5, 2000, 500, 0, 8, 35),
	(8, 'Weapon - Emplacement_AFS_Turret_Mini', 1, 242, 0, 50, 400, 400, 400, 10, 20),
	(9, 'bane_hunter_invasion range', 1, 244, 5, 20, 1500, 500, 0, 10, 35),
	(10, 'bane_hunter_invasion melee', 174, 33, 1, 5, 1400, 500, 333, 20, 45),
	(11, 'bane_amoeboid_invasion melee', 431, 1, 1, 3, 2500, 500, 0, 30, 60),
	(12, 'bane_amoeboid_invasion range', 211, 1, 3, 14, 2800, 500, 1800, 20, 50);

UPDATE `rasaworld`.`entityclass` SET `augList`='4,6' WHERE  `classId`=4327;
UPDATE `rasaworld`.`entityclass` SET `augList`='4,6' WHERE  `classId`=6331;
UPDATE `rasaworld`.`entityclass` SET `augList`='4,6' WHERE  `classId`=28699;

INSERT INTO `rasaworld`.`applied_patches` (`patch_name`) VALUES ('rasaworld_2019_01_25_17_00_added_missing_creature_stats_and_actions');