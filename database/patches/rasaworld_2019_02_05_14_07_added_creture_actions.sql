DROP TABLE IF EXISTS `rasaworld`.`creature_action`;
CREATE TABLE IF NOT EXISTS `rasaworld`.`creature_action` (
  `id` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `description` varchar(50) DEFAULT NULL,
  `action_id` int(10) NOT NULL,
  `action_arg_id` int(10) NOT NULL,
  `range_min` float NOT NULL,
  `range_max` float NOT NULL,
  `cooldown` int(10) NOT NULL,
  `windup_time` int(10) NOT NULL,
  `min_damage` int(10) NOT NULL,
  `max_damage` int(10) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=latin1;

INSERT INTO `rasaworld`.`creature_action` (`id`, `description`, `action_id`, `action_arg_id`, `range_min`, `range_max`, `cooldown`, `windup_time`, `min_damage`, `max_damage`) VALUES
	(1, 'melee attack thrax soldier', 174, 46, 0.5, 3.5, 1300, 0, 5, 12),
	(2, 'range attack afs light soldier', 1, 133, 1, 20, 800, 0, 10, 15),
	(3, 'thrax kick', 397, 1, 1, 4, 6000, 333, 20, 22),
	(4, 'young boargar melee', 174, 10, 1, 4, 2500, 0, 10, 20),
	(5, 'forean spearman melee', 174, 11, 1, 6, 3000, 0, 10, 45),
	(6, 'forean spearman lighting', 203, 1, 5, 20, 2800, 0, 5, 30),
	(7, 'Supply Sergeant Maddrey Weapon Range Attack', 1, 133, 1, 5, 2000, 0, 8, 35),
	(8, 'Weapon - Emplacement_AFS_Turret_Mini', 1, 242, 0, 50, 400, 400, 10, 20),
	(9, 'bane_hunter_invasion range', 1, 244, 5, 20, 1500, 0, 10, 35),
	(10, 'bane_hunter_invasion melee', 174, 33, 1, 5, 1400, 333, 20, 45),
	(11, 'bane_amoeboid_invasion melee', 431, 1, 1, 3, 2500, 0, 30, 60),
	(12, 'bane_amoeboid_invasion range', 211, 1, 3, 14, 2800, 1800, 20, 50);

ALTER TABLE `rasaworld`.`creatures`
	ADD COLUMN `run_speed` INT UNSIGNED NOT NULL DEFAULT '0' AFTER `nameId`,
	ADD COLUMN `walk_speed` INT UNSIGNED NOT NULL DEFAULT '0' AFTER `run_speed`,
	ADD COLUMN `action1` INT UNSIGNED NOT NULL DEFAULT '0' AFTER `walk_speed`,
	ADD COLUMN `action2` INT UNSIGNED NOT NULL DEFAULT '0' AFTER `action1`,
	ADD COLUMN `action3` INT UNSIGNED NOT NULL DEFAULT '0' AFTER `action2`,
	ADD COLUMN `action4` INT UNSIGNED NOT NULL DEFAULT '0' AFTER `action3`,
	ADD COLUMN `action5` INT UNSIGNED NOT NULL DEFAULT '0' AFTER `action4`,
	ADD COLUMN `action6` INT UNSIGNED NOT NULL DEFAULT '0' AFTER `action5`,
	ADD COLUMN `action7` INT UNSIGNED NOT NULL DEFAULT '0' AFTER `action6`,
	ADD COLUMN `action8` INT UNSIGNED NOT NULL DEFAULT '0' AFTER `action7`;
	
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=1;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=2;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=3;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=4;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=5;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=6;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=7;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=8;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=10;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=11;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=12;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=13;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=14;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=15;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=16;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=17;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=18;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=19;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=20;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=21;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=22;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=23;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=24;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=25;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=26;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=27;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=28;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=29;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=100;
UPDATE `rasaworld`.`creatures` SET `action1`='2' WHERE  `dbId`=101;

INSERT INTO `rasaworld`.`applied_patches` (`patch_name`) VALUES ('rasaworld_2019_02_05_14_07_added_creture_actions');
