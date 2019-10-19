ALTER TABLE `rasachar`.`character`
	ADD COLUMN `credits` INT NULL DEFAULT '0' AFTER `rotation`,
	ADD COLUMN `prestige` INT NULL DEFAULT '0' AFTER `credits`;
	
INSERT INTO `rasachar`.`applied_patches` (`patch_name`) VALUES ('rasachar_2018_12_17_18_40_added_credits');
