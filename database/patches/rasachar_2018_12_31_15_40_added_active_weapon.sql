ALTER TABLE `rasachar`.`character`
	ADD COLUMN `active_weapon` TINYINT(3) NOT NULL DEFAULT '0' AFTER `prestige`;
	
INSERT INTO `rasachar`.`applied_patches` (`patch_name`) VALUES ('rasachar_2018_12_31_15_40_added_active_weapon');