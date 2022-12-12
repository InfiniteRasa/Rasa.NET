CREATE TABLE IF NOT EXISTS`rasaworld`.`logos` (
	`id` INT(11) UNSIGNED NOT NULL,
	`classid` INT(10) UNSIGNED NOT NULL DEFAULT '0',
	`mapcontextid` INT(11) UNSIGNED NOT NULL,
	`posx` FLOAT NOT NULL,
	`posy` FLOAT NOT NULL,
	`posz` FLOAT NOT NULL,
	`name` VARCHAR(50) NOT NULL DEFAULT '' COLLATE 'latin1_swedish_ci',
	PRIMARY KEY (`id`) USING BTREE
)
COLLATE='latin1_swedish_ci'
ENGINE=InnoDB
;

/*!40000 ALTER TABLE `rasaworld`.`logos` DISABLE KEYS */;
INSERT INTO `rasaworld`.`logos` (`id`, `classid`, `mapcontextid`, `posx`, `posy`, `posz`, `name`) VALUES
	(1, 7361, 1220, 363.872, 212.211, 619.304, 'Area'),
	(2, 9538, 1220, 41.0646, 189.485, -332.566, 'Attack'),
	(6, 7290, 1220, 406, 244.168, 441, 'Damage'),
	(9, 7292, 1220, -436, 186.325, -620, 'Enemy'),
	(10, 7364, 1220, 832, 159.838, 960, 'Enchance'),
	(23, 7302, 1220, 493.078, 287.531, 319.682, 'Power'),
	(24, 7367, 1220, 187.877, 172.58, 255.913, 'Projectile'),
	(28, 12698, 1220, 795.107, 309.797, 70.8783, 'Time'),
	(38, 12693, 1220, -448.368, 162.678, -108.112, 'Self'),
	(49, 12697, 1220, -906.358, 188.186, -649.29, 'Target'),
	(53, 12684, 1220, -979, 284.096, 744, 'Here'),
	(56, 12689, 1220, -110.341, 217.371, 111.142, 'Mind'),
	(408, 30408, 1220, -828.544, 141, -738.22, 'Earth');
/*!40000 ALTER TABLE `logos` ENABLE KEYS */;

ALTER TABLE `rasachar`.`character_logos`
	CHANGE COLUMN `logosId` `logosId` INT(11) UNSIGNED NOT NULL AFTER `characterSlot`;
	
INSERT INTO `rasaworld`.`applied_patches` (`patch_name`) VALUES ('rasaworld_2022_12_12_12_30_logos_added');