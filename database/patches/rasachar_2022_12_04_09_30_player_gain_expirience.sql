ALTER TABLE `rasachar`.`character`
	CHANGE COLUMN `experience` `experience` INT(11) NOT NULL DEFAULT '0' AFTER `gender`;
	
INSERT INTO `rasachar`.`applied_patches` (`patch_name`) VALUES ('rasachar_2022_12_04_09_30_player_gain_expirience');
